namespace PubComp.Caching.Core
{
    public class GetScopedResult<TValue>
    {
        public CacheMethodTaken MethodTaken { get; set; }
        public ScopedValue<TValue> ScopedValue { get; set; }
    }
}
