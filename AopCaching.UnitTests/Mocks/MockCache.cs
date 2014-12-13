using System;
using PubComp.Caching.Core;
using PubComp.Caching.SystemRuntime;

namespace PubComp.Caching.AopCaching.UnitTests
{
    public class MockCache : ObjectCache
    {
        private int hits;
        private int misses;

        public MockCache(string name)
            : base(name, new System.Runtime.Caching.MemoryCache(name),
                new System.Runtime.Caching.CacheItemPolicy
                {
                    SlidingExpiration = new TimeSpan(0, 2, 0)
                })
        {
        }

        protected override bool TryGet<TValue>(string key, out TValue value)
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
    }
}
