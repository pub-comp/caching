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

        public Cache()
        {
        }

        public Cache(string cacheName)
        {
            this.cacheName = cacheName;
        }

        public override void CompileTimeInitialize(System.Reflection.MethodBase method, AspectInfo aspectInfo)
        {
            if (this.cacheName == null)
            {
                this.cacheName = method.DeclaringType.FullName;
            }
        }

        public override void OnInvoke(MethodInterceptionArgs args)
        {
            var cache = CacheManager.GetCache(cacheName);

            if (cache == null)
            {
                base.OnInvoke(args);
                return;
            }

            var className = args.Method.DeclaringType.FullName;
            var methodName = args.Method.Name;
            var parameterTypes = args.Method.GetParameters().Select(p => p.ParameterType).ToArray();
            var parameterValues = args.Arguments.ToArray();

            var key = new CacheKey(className, methodName, parameterTypes, parameterValues).ToString();

            args.ReturnValue = cache.Get<object>(key, () => { base.OnInvoke(args); return args.ReturnValue; });
        }
    }
}
