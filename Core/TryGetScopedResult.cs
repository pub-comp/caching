namespace PubComp.Caching.Core
{
    public class TryGetScopedResult<TValue> : GetScopedResult<TValue>
    {
        public bool WasFound
        {
            get
            {
                return MethodTaken.HasFlag(CacheMethodTaken.Get);
            }
        }
    }
}
