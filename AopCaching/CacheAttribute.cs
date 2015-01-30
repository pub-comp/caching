using System;
using System.Linq;
using System.Threading;
using PostSharp.Aspects;
using PubComp.Caching.Core;

namespace PubComp.Caching.AopCaching
{
    [Serializable]
    public class CacheAttribute : MethodInterceptionAspect
    {
        private string cacheName;
        private ICache cache;
        private long initialized = 0L;
        private object syncObj = new Object();
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

            var parameterValues = args.Arguments.ToArray();

            var key = new CacheKey(this.className, this.methodName, this.parameterTypeNames, parameterValues).ToString();

            args.ReturnValue = cacheToUse.Get<object>(key, () => { base.OnInvoke(args); return args.ReturnValue; });
        }
    }
}
