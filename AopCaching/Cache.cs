using System;
using System.Linq;
using PostSharp.Aspects;
using PubComp.Caching.Core;

namespace PubComp.Caching.AopCaching
{
    [Serializable]
    public class Cache : MethodInterceptionAspect
    {
        private string cacheName;
        private ICache cache;
        private volatile bool wasInitialized;
        private object sync = new object();

        public Cache()
        {
        }

        public Cache(string cacheName)
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

            var className = args.Method.DeclaringType.FullName;
            var methodName = args.Method.Name;
            var parameterTypes = args.Method.GetParameters().Select(p => p.ParameterType).ToArray();
            var parameterValues = args.Arguments.ToArray();

            var key = new CacheKey(className, methodName, parameterTypes, parameterValues).ToString();

            args.ReturnValue = cacheToUse.Get<object>(key, () => { base.OnInvoke(args); return args.ReturnValue; });
        }
    }
}
