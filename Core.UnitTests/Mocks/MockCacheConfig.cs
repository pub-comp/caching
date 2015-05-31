using System.Runtime.Caching;

namespace PubComp.Caching.Core.UnitTests.Mocks
{
    public class MockCacheConfig : CacheConfig
    {
        public MockCachePolicy Policy { get; set; }

        public override ICache CreateCache()
        {
            return new MockCache(this.Name, this.Policy);
        }
    }
}
