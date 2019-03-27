using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core.UnitTests.Mocks;

namespace PubComp.Caching.Core.UnitTests
{
    [TestClass]
    public class MockConfigLoaderTests
    {
        private CacheManagerInternals cacheManagerInternals;

        [TestInitialize]
        public void TestInitialize()
        {
            var settings = new CacheManagerSettings
            {
                ConfigLoader = new MockCacheConfigLoader()
            };

            cacheManagerInternals = new CacheManagerInternals(settings);
            cacheManagerInternals.InitializeFromConfig();
        }

        [TestMethod]
        public void TestConfigLoader()
        {
            Assert.IsNotNull(cacheManagerInternals);
            Assert.IsNotNull(cacheManagerInternals.Settings);
            Assert.IsInstanceOfType(cacheManagerInternals.Settings.ConfigLoader, typeof(MockCacheConfigLoader));
        }

        [TestMethod]
        public void TestCaches()
        {
            var cacheNames = cacheManagerInternals.GetCacheNames();

            Assert.AreEqual(2, cacheNames.Count());
            var cache1 = cacheManagerInternals.GetCache("mockCache1");
            Assert.IsNotNull(cache1);
            Assert.IsInstanceOfType(cache1, typeof(MockNoCache));
            var cache2 = cacheManagerInternals.GetCache("mockCache2");
            Assert.IsNotNull(cache2);
            Assert.IsInstanceOfType(cache2, typeof(MockNoCache));
            var cacheNotExist = cacheManagerInternals.GetCache("mockCache3");
            Assert.IsNull(cacheNotExist);
        }
    }
}