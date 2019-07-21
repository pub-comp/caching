using System;
using System.Linq;
using System.Threading;
using PostSharp.Aspects;
using PubComp.Caching.Core;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using PubComp.Caching.Core.Attributes;

namespace PubComp.Caching.AopCaching
{
    [Serializable]
    public class CacheAttribute : MethodInterceptionAspect
    {
        private string cacheName;
        private ICache cache;
        private long initialized = 0L;
        private string className;
        private string methodName;
        private string[] parameterTypeNames;
        private int[] indexesNotToCache;
        private bool isClassGeneric;
        private bool isMethodGeneric;

        public CacheAttribute()
        {
        }

        public CacheAttribute(string cacheName)
        {
            this.cacheName = cacheName;
        }

        public sealed override void CompileTimeInitialize(System.Reflection.MethodBase method, AspectInfo aspectInfo)
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
        }

        public sealed override void OnInvoke(MethodInterceptionArgs args)
        {
            if (Interlocked.Read(ref initialized) == 0L)
            {
                this.cache = CacheManager.GetCache(this.cacheName);
                Interlocked.Exchange(ref initialized, 1L);
            }

            var cacheToUse = this.cache;

            if (cacheToUse == null)
            {
                base.OnInvoke(args);
                return;
            }

            var key = GetCacheKey(args);
            var result = cacheToUse.Get<object>(key, () => { base.OnInvoke(args); return args.ReturnValue; });
            var returnType = GetReturnType(args.Method);
            args.ReturnValue = SafeCasting.CastTo(returnType, result);
        }

        /// <inheritdoc />
        public sealed override async Task OnInvokeAsync(MethodInterceptionArgs args)
        {
            if (Interlocked.Read(ref initialized) == 0L)
            {
                this.cache = CacheManager.GetCache(this.cacheName);
                Interlocked.Exchange(ref initialized, 1L);
            }

            var cacheToUse = this.cache;

            if (cacheToUse == null)
            {
                await base.OnInvokeAsync(args);
            }
            else
            {
                var key = GetCacheKey(args);
                var result = await cacheToUse.GetAsync(key, async () => { await base.OnInvokeAsync(args); return args.ReturnValue; });
                var returnType = GetReturnType(args.Method);
                args.ReturnValue = SafeCasting.CastTo(returnType, result);
            }
        }

        private string GetCacheKey(MethodInterceptionArgs args)
        {
            var classNameNonGeneric = !this.isClassGeneric
                ? this.className
                : args.Method.DeclaringType.FullName;

            var parameterTypeNamesNonGeneric = !this.isMethodGeneric
                ? this.parameterTypeNames
                : args.Method.GetParameters().Select(p => p.ParameterType.FullName).ToArray();

            var genericArgumentsTypeNames = args.Method.GetGenericArguments().Select(a => a.FullName).ToArray();

            var parameterValues = args.Arguments.Where((arg, index) => !this.indexesNotToCache.Contains(index)).ToArray();

            var key = new CacheKey(classNameNonGeneric, this.methodName, parameterTypeNames, parameterValues, genericArgumentsTypeNames).ToString();
            return key;
        }
        
        private static Type GetReturnType(MethodBase method)
        {
            var returnType = (method as MethodInfo)?.ReturnType;

            if (returnType != null &&
                returnType.IsGenericType &&
                returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                return returnType.GetGenericArguments()[0];
            }
            return returnType;
        }
    }
}