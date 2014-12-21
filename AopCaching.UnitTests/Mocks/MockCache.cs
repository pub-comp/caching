using System;
using PubComp.Caching.Core;
using PubComp.Caching.SystemRuntime;

namespace PubComp.Caching.AopCaching.UnitTests
{
    public class MockCache : ICache
    {
        private MockCacheInner innerCache;

        public MockCache(string name)
        {
            this.innerCache = new MockCacheInner(name);
        }

        public int Hits { get { return this.innerCache.Hits; } }

        public int Misses { get { return this.innerCache.Misses; } }

        public string Name { get { return this.innerCache.Name; } }

        public bool TryGet<TValue>(string key, out TValue value)
        {
            return this.innerCache.TryGet<TValue>(key, out value);
        }

        public void Set<TValue>(string key, TValue value)
        {
            this.innerCache.Set<TValue>(key, value);
        }

        public TValue Get<TValue>(string key, Func<TValue> getter)
        {
            return this.innerCache.Get<TValue>(key, getter);
        }

        public void Clear(string key)
        {
            this.innerCache.Clear(key);
        }

        public void ClearAll()
        {
            this.innerCache.ClearAll();
            this.innerCache.ResetCounters();
        }

        public class MockCacheInner : ObjectCache
        {
            private int hits;
            private int misses;

            public MockCacheInner(string name)
                : base(name, new System.Runtime.Caching.MemoryCache(name),
                    new System.Runtime.Caching.CacheItemPolicy
                    {
                        SlidingExpiration = new TimeSpan(0, 2, 0)
                    })
            {
            }

            protected override bool TryGetInner<TValue>(string key, out TValue value)
            {
                Object val = InnerCache.Get(key, null);

                if (val != null)
                {
                    hits++;
                    value = val is TValue ? (TValue)val : default(TValue);
                    return true;
                }

                misses++;
                value = default(TValue);
                return false;
            }

            public int Hits { get { return this.hits; } }

            public int Misses { get { return this.misses; } }

            internal void ResetCounters()
            {
                this.hits = 0;
                this.misses = 0;
            }
        }
    }
}
