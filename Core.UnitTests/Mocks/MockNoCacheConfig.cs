namespace PubComp.Caching.Core.UnitTests.Mocks
{
    public class MockNoCacheConfig : CacheConfig
    {
        public MockCachePolicy Policy { get; set; }

        public override ICache CreateCache()
        {
            return new MockNoCache(this.Name, this.Policy);
        }
    }
}
