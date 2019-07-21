using System.Collections.Generic;
using PubComp.Caching.Core.Config;
using PubComp.Caching.Core.Config.Loaders;

namespace PubComp.Caching.Core.UnitTests.Mocks
{
    public class MockCacheConfigLoader : ICacheConfigLoader
    {
        public IList<ConfigNode> LoadConfig()
        {
            var c1 = new MockNoCacheConfig
                { Action = ConfigAction.Add, Name = "mockCache1", Policy = new MockCachePolicy() };
            var c2 = new MockNoCacheConfig
                { Action = ConfigAction.Add, Name = "mockCache2", Policy = new MockCachePolicy() };
            return new List<ConfigNode> {c1, c2};
        }
    }
}