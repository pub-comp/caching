using System.Runtime.Caching;
using PubComp.Caching.Core;

namespace PubComp.Caching.Core.UnitTests.Mocks
{
    public class MockCacheConfig : CacheConfig
    {
        public CacheItemPolicy Policy { get; set; }

        public override ICache CreateCache()
        {
            return new MockCache(this.Name, this.Policy);
        }
    }
}
