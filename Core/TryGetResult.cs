namespace PubComp.Caching.Core
{
    public class TryGetResult<TValue>
    {
        public bool WasFound { get; set; }
        public TValue Value { get; set; }
    }
}