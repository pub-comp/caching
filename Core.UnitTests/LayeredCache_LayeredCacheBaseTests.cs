using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core.UnitTests.Mocks;

namespace PubComp.Caching.Core.UnitTests
{
    [TestClass]
    public class LayeredCache_LayeredCacheBaseTests : LayeredCacheBaseTests
    {
        protected override IMockCache GetMockCache(string name)
        {
            return new MockMemCache(name);
        }

        protected override ICache GetLayeredCache(string name, ICache level1, ICache level2)
        {
            return new LayeredCache(name, level1, level2);
        }

        protected override ICache GetLayeredCache(string name, string level1CacheName, string level2CacheName)
        {
            return new LayeredCache(name, level1CacheName, level2CacheName);
        }
    }
}
