using System;
using PubComp.Caching.Core;
using PubComp.Caching.RedisCaching.StackExchange;

namespace PubComp.Caching.SystemRuntime
{
    public abstract class ObjectCache : ICache
    {
        private readonly String name;
        private System.Runtime.Caching.ObjectCache innerCache;
        private readonly Object sync = new Object();
        private readonly InMemoryPolicy policy;
        private readonly CacheSynchronizer synchronizer;

        protected ObjectCache(
            String name, System.Runtime.Caching.ObjectCache innerCache, InMemoryPolicy policy)
        {
            this.name = name;
            this.policy = policy;
            this.innerCache = innerCache;

            this.synchronizer = CacheSynchronizer.CreateCacheSynchronizer(this, this.policy.SyncProvider);
        }
        
        public string Name { get { return this.name; } }

        protected System.Runtime.Caching.ObjectCache InnerCache { get { return this.innerCache; } }

        protected InMemoryPolicy Policy { get { return this.policy; } }
        
        public bool TryGet<TValue>(string key, out TValue value)
        {
            return TryGetInner(key, out value);
        }

        public void Set<TValue>(string key, TValue value)
        {
            Add(key, value);
        }

        protected virtual bool TryGetInner<TValue>(String key, out TValue value)
        {
            var item = innerCache.Get(key, null) as CacheItem;

            if (item != null)
            {
                // ReSharper disable once CanBeReplacedWithTryCastAndCheckForNull
                value = item.Value is TValue ? (TValue)item.Value : default(TValue);
                return true;
            }

            value = default(TValue);
            return false;
        }

        protected virtual void Add<TValue>(String key, TValue value)
        {
            innerCache.Set(key, new CacheItem(value), ToRuntimePolicy(policy), null);
        }

        // ReSharper disable once ParameterHidesMember
        protected System.Runtime.Caching.CacheItemPolicy ToRuntimePolicy(InMemoryPolicy policy)
        {
            TimeSpan slidingExpiration;
            DateTimeOffset absoluteExpiration;

            if (policy.SlidingExpiration != null && policy.SlidingExpiration.Value < TimeSpan.MaxValue)
            {
                absoluteExpiration = System.Runtime.Caching.ObjectCache.InfiniteAbsoluteExpiration;
                slidingExpiration = policy.SlidingExpiration.Value;                
            }
            else if (policy.ExpirationFromAdd != null && policy.ExpirationFromAdd.Value < TimeSpan.MaxValue)
            {
                absoluteExpiration = DateTimeOffset.Now.Add(policy.ExpirationFromAdd.Value);
                slidingExpiration = System.Runtime.Caching.ObjectCache.NoSlidingExpiration;
            }
            else if (policy.AbsoluteExpiration != null && policy.AbsoluteExpiration.Value < DateTimeOffset.MaxValue)
            {
                absoluteExpiration = policy.AbsoluteExpiration.Value;
                slidingExpiration = System.Runtime.Caching.ObjectCache.NoSlidingExpiration;
            }
            else
            {
                absoluteExpiration = System.Runtime.Caching.ObjectCache.InfiniteAbsoluteExpiration;
                slidingExpiration = System.Runtime.Caching.ObjectCache.NoSlidingExpiration;
            }

            return new System.Runtime.Caching.CacheItemPolicy
            {
                AbsoluteExpiration = absoluteExpiration,
                SlidingExpiration = slidingExpiration,
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
