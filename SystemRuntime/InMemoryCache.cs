using System;
using PubComp.Caching.Core;

namespace PubComp.Caching.SystemRuntime
{
    public class InMemoryCache : ObjectCache
    {
        public InMemoryCache(String name, System.Runtime.Caching.CacheItemPolicy policy)
            : base(name, new System.Runtime.Caching.MemoryCache(name), policy)
        {
        }

        public InMemoryCache(String name, TimeSpan slidingExpiration)
            : this(name,
                new System.Runtime.Caching.CacheItemPolicy
                {
                    SlidingExpiration = slidingExpiration
                })
        {
        }

        public InMemoryCache(String name, DateTimeOffset absoluteExpiration)
            : this(name,
                new System.Runtime.Caching.CacheItemPolicy
                {
                    AbsoluteExpiration = absoluteExpiration
                })
        {
        }
    }
}
