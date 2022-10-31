using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PostSharp.Aspects;
using PubComp.Caching.Core;
using System.Collections;
using System.Threading.Tasks;
using PubComp.Caching.Core.Attributes;
using NLog;
using PostSharp.Serialization;

namespace PubComp.Caching.AopCaching
{
    [Serializable]
    public class CacheListAttribute : MethodInterceptionAspect
    {
        private string cacheName;
        private ICache cache;
        private string className;
        private string methodName;
        private string[] parameterTypeNames;
        private string[] genericArgumentTypeNames;
        private int[] indexesNotToCache;
        private Type dataKeyConverterType;
        private int keyParameterNumber;
        private Type keyType;
        private Type dataType;
        private ConstructorInfo createKeyList;
        private ConstructorInfo createDataList;
        private ConstructorInfo createDataKeyConverter;
        private MethodInfo addKey;
        private MethodInfo addData;
        private MethodInfo addDataRange;
        private MethodInfo convertDataToKey;
        private MethodInfo keysCount;
        private bool isClassGeneric;
        private bool isMethodGeneric;

        public CacheListAttribute(Type dataKeyConverterType, int keyParameterNumber = 0)
        {
            this.dataKeyConverterType = dataKeyConverterType;
            this.keyParameterNumber = keyParameterNumber;
        }

        public CacheListAttribute(string cacheName, Type dataKeyConverterType, int keyParameterNumber = 0)
        {
            this.cacheName = cacheName;
            this.dataKeyConverterType = dataKeyConverterType;
            this.keyParameterNumber = keyParameterNumber;
        }

        private static bool TryGetKeyDataTypes(Type dataKeyConverterType, out Type keyType, out Type dataType)
        {
            keyType = null;
            dataType = null;

            var interfaces = dataKeyConverterType.GetInterfaces();

            foreach (var @interface in interfaces)
            {
                if (!@interface.IsGenericType)
                    continue;

                var args = @interface.GetGenericArguments();

                if (args.Length != 2)
                    continue;

                if (@interface.GetGenericTypeDefinition() != typeof(IDataKeyConverter<,>))
                    return false;

                keyType = args[0];
                dataType = args[1];
                return true;
            }

            return false;
        }

        public sealed override bool CompileTimeValidate(MethodBase method)
        {
            Type keyType, dataType;
            if (!TryGetKeyDataTypes(this.dataKeyConverterType, out keyType, out dataType))
            {
                PostSharp.Extensibility.Message.Write(
                    PostSharp.Extensibility.MessageLocation.Of(method),
                    PostSharp.Extensibility.SeverityType.Error,
                    "Custom01",
                    "The type provided does not implement IDataKeyConverter<TKey, TData>.");
                
                return false;
            }

            if (this.dataKeyConverterType.IsAbstract || this.dataKeyConverterType.GetConstructor(new Type[0]) == null)
            {
                PostSharp.Extensibility.Message.Write(
                    PostSharp.Extensibility.MessageLocation.Of(method),
                    PostSharp.Extensibility.SeverityType.Error,
                    "Custom01",
                    $"{this.dataKeyConverterType.FullName} is abstract or does not have a public parameter-less constructor");

                return false;
            }

            var keyIListType = typeof(IList<>).MakeGenericType(keyType);
            var dataIListType = typeof(IList<>).MakeGenericType(dataType);

            if (method.GetParameters().Length <= this.keyParameterNumber)
            {
                PostSharp.Extensibility.Message.Write(
                    PostSharp.Extensibility.MessageLocation.Of(method),
                    PostSharp.Extensibility.SeverityType.Error,
                    "Custom01",
                    $"The method {method.Name} does not have a parameter {this.keyParameterNumber}.");
                
                return false;
            }

            var keysParameterType = method.GetParameters()[this.keyParameterNumber].ParameterType;
            if (keysParameterType != keyIListType)
            {
                PostSharp.Extensibility.Message.Write(
                    PostSharp.Extensibility.MessageLocation.Of(method),
                    PostSharp.Extensibility.SeverityType.Error,
                    "Custom01",
                    string.Format("The parameter {1} of method {0} != IList<TKey>.", method.Name, this.keyParameterNumber));
                
                return false;
            }

            var methodInfo = method as MethodInfo;
            if (methodInfo == null || methodInfo.ReturnType == typeof(void))
            {
                PostSharp.Extensibility.Message.Write(
                    PostSharp.Extensibility.MessageLocation.Of(method),
                    PostSharp.Extensibility.SeverityType.Error,
                    "Custom01",
                    $"The method {method.Name} has no return type.");
                
                return false;
            }

            var returnType = methodInfo.ReturnType;
            if (returnType != dataIListType && returnType != typeof(Task<>).MakeGenericType(dataIListType))
            {
                PostSharp.Extensibility.Message.Write(
                    PostSharp.Extensibility.MessageLocation.Of(method),
                    PostSharp.Extensibility.SeverityType.Error,
                    "Custom01",
                    $"The return type of method {method.Name} != IList<TData> or Task<IList<TData>>.");
                
                return false;
            }

            return true;
        }

        public sealed override void CompileTimeInitialize(MethodBase method, AspectInfo aspectInfo)
        {
            var type = method.DeclaringType;

            if (this.cacheName == null)
                this.cacheName = type.FullName;

            this.isClassGeneric = type.IsGenericType;
            this.isMethodGeneric = method.IsGenericMethod;

            this.className = type.FullName;
            this.methodName = method.Name;
            var parameters = method.GetParameters();
            this.parameterTypeNames = parameters.Select(p => p.ParameterType.FullName).ToArray();
            this.genericArgumentTypeNames = method.GetGenericArguments().Select(a => a.FullName).ToArray();

            var indexes = new List<int>();

            for (int cnt = 0; cnt < parameters.Length; cnt++)
            {
                var doNotIncludeInCacheKey =
                    parameters[cnt].CustomAttributes
                        .Any(a => a.AttributeType == typeof(DoNotIncludeInCacheKeyAttribute));

                if (doNotIncludeInCacheKey)
                    indexes.Add(cnt);
            }

            this.indexesNotToCache = indexes.ToArray();

            TryGetKeyDataTypes(this.dataKeyConverterType, out this.keyType, out this.dataType);

            this.createDataKeyConverter = this.dataKeyConverterType.GetConstructor(new Type[0]);
            this.convertDataToKey = this.dataKeyConverterType.GetMethod("GetKey", new [] { this.dataType });

            var keyListType = typeof(List<>).MakeGenericType(this.keyType);
            var dataListType = typeof(List<>).MakeGenericType(this.dataType);
            var dataEnumerableType = typeof(IEnumerable<>).MakeGenericType(this.dataType);

            this.createKeyList = keyListType.GetConstructor(new Type[0]);
            this.createDataList = dataListType.GetConstructor(new Type[0]);

            this.addKey = keyListType.GetMethod("Add", new[] { this.keyType });
            this.addData = dataListType.GetMethod("Add", new[] { this.dataType });
            this.addDataRange = dataListType.GetMethod("AddRange", new[] { dataEnumerableType });
            this.keysCount = keyListType.GetProperty("Count").GetGetMethod();
        }

        public sealed override void OnInvoke(MethodInterceptionArgs args)
        {
            if (this.cache == null)
            {
                this.cache = CacheManager.GetCache(this.cacheName);
                if (this.cache == null)
                {
                    LogManager.GetCurrentClassLogger().Warn($"AOP cache list [{this.cacheName}] is not initialized, define NoCache if needed!");
                }
            }

            var cacheToUse = this.cache;
            if (!cacheToUse.IsUseable())
            {
                base.OnInvoke(args);
                return;
            }

            var parameterValues = args.Arguments.Where((arg, index) => !this.indexesNotToCache.Contains(index)).ToArray();

            var allKeys = parameterValues[this.keyParameterNumber];
            var allKeysCollection = allKeys as IEnumerable;

            var resultList = this.createDataList.Invoke(new object[0]);
            var missingKeys = this.createKeyList.Invoke(new object[0]);

            foreach (object k in allKeysCollection)
            {
                var keyList = this.createKeyList.Invoke(new object[0]);
                this.addKey.Invoke(keyList, new [] { k });
                parameterValues[this.keyParameterNumber] = keyList;

                var key = new CacheKey(this.className, this.methodName, this.parameterTypeNames, parameterValues, this.genericArgumentTypeNames).ToString();
                object value;
                if (cacheToUse.TryGet(key, out value))
                {
                    addData.Invoke(resultList, new [] { value });
                }
                else
                {
                    addKey.Invoke(missingKeys, new [] { k });
                }
            }

            if (missingKeys == null || 0 == (int)keysCount.Invoke(missingKeys, new object[0]))
            {
                args.ReturnValue = resultList;
                return;
            }

            var newValuesTimestamp = DateTimeOffset.UtcNow;

            args.Arguments[this.keyParameterNumber] = missingKeys;
            base.OnInvoke(args);
            var resultsFromInner = args.ReturnValue;
            addDataRange.Invoke(resultList, new [] { resultsFromInner });

            var values = GetKeyValues(args, resultsFromInner, parameterValues);

            if (cacheToUse is IScopedCache scopedCacheToUse)
            {
                foreach (var value in values)
                {
                    scopedCacheToUse.SetScoped(value.Key, value.Value, newValuesTimestamp);
                }
            }
            else
            {
                foreach (var value in values)
                {
                    cacheToUse.Set(value.Key, value.Value);
                }
            }

            args.ReturnValue = resultList;
        }

        public sealed override async Task OnInvokeAsync(MethodInterceptionArgs args)
        {
            if (this.cache == null)
            {
                this.cache = CacheManager.GetCache(this.cacheName);
                if (this.cache == null)
                {
                    LogManager.GetCurrentClassLogger().Warn($"AOP cache list [{this.cacheName}] is not initialized, define NoCache if needed!");
                }
            }

            var cacheToUse = this.cache;
            if (!cacheToUse.IsUseable())
            {
                await base.OnInvokeAsync(args).ConfigureAwait(false);
                return;
            }

            var parameterValues = args.Arguments.Where((arg, index) => !this.indexesNotToCache.Contains(index)).ToArray();

            var allKeys = parameterValues[this.keyParameterNumber];
            var allKeysCollection = allKeys as IEnumerable;

            var resultList = this.createDataList.Invoke(new object[0]);
            var missingKeys = this.createKeyList.Invoke(new object[0]);

            foreach (object k in allKeysCollection)
            {
                var keyList = this.createKeyList.Invoke(new object[0]);
                this.addKey.Invoke(keyList, new [] { k });
                parameterValues[this.keyParameterNumber] = keyList;

                var key = new CacheKey(this.className, this.methodName, this.parameterTypeNames, parameterValues, genericArgumentTypeNames).ToString();
                var result = await cacheToUse.TryGetAsync<object>(key).ConfigureAwait(false);
                if (result.WasFound)
                {
                    addData.Invoke(resultList, new [] { result.Value });
                }
                else
                {
                    addKey.Invoke(missingKeys, new [] { k });
                }
            }

            if (missingKeys == null || 0 == (int)keysCount.Invoke(missingKeys, new object[0]))
            {
                args.ReturnValue = resultList;
                return;
            }

            var newValuesTimestamp = DateTimeOffset.UtcNow;

            args.Arguments[this.keyParameterNumber] = missingKeys;
            await base.OnInvokeAsync(args).ConfigureAwait(false);
            var resultsFromerInner = args.ReturnValue;
            addDataRange.Invoke(resultList, new [] { resultsFromerInner });

            var values = GetKeyValues(args, resultsFromerInner, parameterValues);

            if (cacheToUse is IScopedCache scopedCacheToUse)
            {
                var tasks = values.Select(async x => await scopedCacheToUse
                    .SetScopedAsync(x.Key, x.Value, newValuesTimestamp).ConfigureAwait(false));
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            else
            {
                var tasks = values.Select(async x => await cacheToUse
                    .SetAsync(x.Key, x.Value).ConfigureAwait(false));
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

            args.ReturnValue = resultList;
        }

        private Dictionary<string, object> GetKeyValues(MethodInterceptionArgs args, object resultsFromInner, object[] parameterValues)
        {
            var converter = createDataKeyConverter.Invoke(new object[0]);

            var classNameNonGeneric = !this.isClassGeneric
                ? this.className
                : args.Method.DeclaringType.FullName;

            var parameterTypeNamesNonGeneric = !this.isMethodGeneric
                ? this.parameterTypeNames
                : args.Method.GetParameters().Select(p => p.ParameterType.FullName).ToArray();

            Dictionary<string, object> values = new Dictionary<string, object>();
            foreach (object result in resultsFromInner as IEnumerable)
            {
                var k = convertDataToKey.Invoke(converter, new[] {result});

                var keyList = this.createKeyList.Invoke(new object[0]);
                this.addKey.Invoke(keyList, new[] {k});
                parameterValues[this.keyParameterNumber] = keyList;

                var key = new CacheKey(classNameNonGeneric, this.methodName, parameterTypeNamesNonGeneric, parameterValues, genericArgumentTypeNames)
                    .ToString();
                values[key] = result;
            }

            return values;
        }
    }
}
