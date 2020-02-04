using System;

namespace PubComp.Caching.Core
{
    public class TryGetScoped
    {
        public CacheMethodTaken MethodTaken { get; set; }
        public DateTimeOffset ValueTimestamp { get; set; }

        public TryGetScoped(CacheMethodTaken methodTaken)
        {
            this.MethodTaken = methodTaken;
        }

        public TryGetScoped(CacheMethodTaken methodTaken, DateTimeOffset valueTimestamp)
        {
            this.MethodTaken = methodTaken;
            this.ValueTimestamp = valueTimestamp;
        }
    }
}
