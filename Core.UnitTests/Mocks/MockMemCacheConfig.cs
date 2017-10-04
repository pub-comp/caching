using PubComp.Caching.Core.Config;

namespace PubComp.Caching.Core.UnitTests.Mocks
{
    public class MockMemCacheConfig : CacheConfig
    {
        public override ICache CreateCache()
        {
            return new MockMemCache(this.Name);
        }
    }
}
