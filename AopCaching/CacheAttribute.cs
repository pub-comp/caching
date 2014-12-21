using System;
using System.Linq;
using PostSharp.Aspects;
using PubComp.Caching.Core;

namespace PubComp.Caching.AopCaching
{
    [Serializable]
    public class CacheAttribute : MethodInterceptionAspect
    {
        private string cacheName;
        private ICache cache;
        private volatile bool wasInitialized;
        private object sync = new object();
        private string className;
        private string methodName;
        private string[] parameterTypeNames;

        public CacheAttribute()
        {
        }

        public CacheAttribute(string cacheName)
        {
            this.cacheName = cacheName;
        }

        private void Initialize()
        {
            lock (sync)
            {
                if (wasInitialized)
                    return;

                var cacheToUse = CacheManager.GetCache(this.cacheName);
                this.cache = cacheToUse;
                wasInitialized = true;
            }
        }

        public override void CompileTimeInitialize(System.Reflection.MethodBase method, AspectInfo aspectInfo)
        {
            if (this.cacheName == null)
                this.cacheName = method.DeclaringType.FullName;

            this.className = method.DeclaringType.FullName;
            this.methodName = method.Name;
            this.parameterTypeNames = method.GetParameters().Select(p => p.ParameterType.FullName).ToArray();
        }

        public override void OnInvoke(MethodInterceptionArgs args)
        {
            if (!wasInitialized)
                Initialize();

            var cacheToUse = this.cache;

            if (cacheToUse == null)
            {
                base.OnInvoke(args);
                return;
            }

            var parameterValues = args.Arguments.ToArray();

            var key = new CacheKey(this.className, this.methodName, this.parameterTypeNames, parameterValues).ToString();

            args.ReturnValue = cacheToUse.Get<object>(key, () => { base.OnInvoke(args); return args.ReturnValue; });
        }
    }
}
