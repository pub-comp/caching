namespace PubComp.Caching.Core
{
    public class TryGetScopedResult<TValue>
    {
        public CacheDirectivesOutcome Outcome { get; set; }
        public TValue Value { get; set; }
    }
}
