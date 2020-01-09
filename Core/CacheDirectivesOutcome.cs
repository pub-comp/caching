using System;

namespace PubComp.Caching.Core
{
    public class CacheDirectivesOutcome
    {
        public static string HeadersKey = $"X-{nameof(CacheDirectivesOutcome)}";

        public CacheMethodTaken MethodTaken { get; set; }
        public DateTimeOffset? ValueTimestamp { get; set; }

        public CacheDirectivesOutcome(CacheMethodTaken methodTaken)
        {
            this.MethodTaken = methodTaken;
        }

        public CacheDirectivesOutcome(CacheMethodTaken methodTaken, DateTimeOffset? valueTimestamp)
        {
            this.MethodTaken = methodTaken;
            this.ValueTimestamp = valueTimestamp;
        }
    }
}
