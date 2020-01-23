namespace PubComp.Caching.Core.UnitTests.Mocks
{
    public interface IMockCache : ICache
    {
        int Hits { get; }

        int Misses { get; }

        void ClearAll(bool doResetCounters);
    }
}