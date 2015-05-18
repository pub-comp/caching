using System;
using PubComp.Caching.Core;

namespace PubComp.Caching.SystemRuntime
{
    public abstract class ObjectCache : ICache
    {
        private readonly String name;
        private System.Runtime.Caching.ObjectCache innerCache;
        private readonly Object sync = new Object();
        private readonly InMemoryPolicy policy;

        protected ObjectCache(
            String name, System.Runtime.Caching.ObjectCache innerCache, InMemoryPolicy policy)
        {
            this.name = name;
            this.policy = policy;
            this.innerCache = innerCache;
        }

        public string Name { get { return this.name; } }

        protected System.Runtime.Caching.ObjectCache InnerCache { get { return this.innerCache; } }

        protected InMemoryPolicy Policy { get { return this.policy; } }

        public bool TryGet<TValue>(string key, out TValue value)
        {
            return TryGetInner<TValue>(key, out value);
        }

        public void Set<TValue>(string key, TValue value)
        {
            Add<TValue>(key, value);
        }

        protected virtual bool TryGetInner<TValue>(String key, out TValue value)
        {
            Object val = innerCache.Get(key, null);

            if (val != null)
            {
                value = val is TValue ? (TValue)val : default(TValue);
                return true;
            }

            value = default(TValue);
            return false;
        }

        protected virtual void Add<TValue>(String key, TValue value)
        {
            innerCache.Set(key, value, ToRuntimePolicy(policy), null);
        }

        protected System.Runtime.Caching.CacheItemPolicy ToRuntimePolicy(InMemoryPolicy policy)
        {
            var absoluteExpiration =
                (policy.ExpirationFromAdd != System.Runtime.Caching.ObjectCache.NoSlidingExpiration)
                ? DateTimeOffset.Now.Add(policy.ExpirationFromAdd)
                : System.Runtime.Caching.ObjectCache.InfiniteAbsoluteExpiration;

            if (absoluteExpiration > policy.AbsoluteExpiration)
                absoluteExpiration = policy.AbsoluteExpiration;

            return new System.Runtime.Caching.CacheItemPolicy
            {
                AbsoluteExpiration = absoluteExpiration,
                SlidingExpiration = policy.SlidingExpiration,
            };
        }

        public TValue Get<TValue>(String key, Func<TValue> getter)
        {
            TValue value;

            if (TryGetInner(key, out value))
                return value;

            lock (sync)
            {
                if (TryGetInner(key, out value))
                    return value;

                value = getter();
                Add(key, value);
            }

            return value;
        }

        public void Clear(String key)
        {
            innerCache.Remove(key, null);
        }

        public void ClearAll()
        {
            innerCache = new System.Runtime.Caching.MemoryCache(this.name);
        }
    }
}
