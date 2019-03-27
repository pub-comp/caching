using PubComp.Caching.Core;
using PubComp.Caching.Core.Config;

namespace PubComp.Caching.AopCaching.UnitTests.Mocks
{
    public class MockCacheConfig : CacheConfig
    {
        public override ICache CreateCache()
        {
            return new MockCache(this.Name);
        }
    }
}