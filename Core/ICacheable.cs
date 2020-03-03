namespace PubComp.Caching.Core
{
    public interface ICacheable
    {
        CacheMethod DefaultMethod { get; }
        double DefaultMinimumAgeInMilliseconds { get; }
    }
}
