namespace PubComp.Caching.Core
{
    public class TryGetScopedResult<TValue>
    {
        public CacheMethodTaken MethodTaken { get; set; }
        public ScopedCacheItem<TValue> ScopedCacheItem { get; set; }
    }
}
