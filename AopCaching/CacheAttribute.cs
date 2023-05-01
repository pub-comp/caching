using NLog;
using PostSharp.Aspects;
using PostSharp.Serialization;
using PubComp.Caching.Core;
using PubComp.Caching.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace PubComp.Caching.AopCaching
{
    [PSerializable]
    public class CacheAttribute : MethodInterceptionAspect
    {
        private string cacheName;
        private ICache cache;
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
            if (this.cache == null)
            {
                this.cache = CacheManager.GetCache(this.cacheName);
                if (this.cache == null)
                {
                    LogManager.GetCurrentClassLogger().Warn($"AOP cache [{this.cacheName}] is not initialized, define NoCache if needed!");
                }
            }

            var cacheToUse = this.cache;
            if (!cacheToUse.IsUseable())
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
            if (this.cache == null)
            {
                this.cache = CacheManager.GetCache(this.cacheName);
                if (this.cache == null)
                {
                    LogManager.GetCurrentClassLogger().Warn($"AOP cache [{this.cacheName}] is not initialized, define NoCache if needed!");
                }
            }

            var cacheToUse = this.cache;
            if (!cacheToUse.IsUseable())
            {
                await base.OnInvokeAsync(args).ConfigureAwait(false);
                return;
            }

            var key = GetCacheKey(args);
            var result = await cacheToUse
                .GetAsync(key, async () =>
                {
                    await base.OnInvokeAsync(args).ConfigureAwait(false);
                    return args.ReturnValue;
                })
                .ConfigureAwait(false);
            var returnType = GetReturnType(args.Method);
            args.ReturnValue = SafeCasting.CastTo(returnType, result);
        }

        private string GetCacheKey(MethodInterceptionArgs args)
        {
            var classNameNonGeneric = !this.isClassGeneric
                ? this.className
                : args.Method.DeclaringType.FullName;

            var parameterTypeNamesNonGeneric = !this.isMethodGeneric
                ? this.parameterTypeNames
                : args.Method.GetParameters().Select(p => p.ParameterType.FullName).ToArray();

            var genericArgumentTypeNames = args.Method.GetGenericArguments().Select(a => a.FullName).ToArray();

            var parameterValues = args.Arguments.Where((arg, index) => !this.indexesNotToCache.Contains(index)).ToArray();

            var key = new CacheKey(classNameNonGeneric, this.methodName, parameterTypeNamesNonGeneric, parameterValues, genericArgumentTypeNames).ToString();
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