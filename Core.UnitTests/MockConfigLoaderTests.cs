using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core.UnitTests.Mocks;

namespace PubComp.Caching.Core.UnitTests
{
    [TestClass]
    public class MockConfigLoaderTests
    {
        private CacheManagerLogic _cacheManagerLogic;
        [TestInitialize]
        public void TestInitialize()
        {
            var settings = new CacheManagerSettings
            {
                ConfigLoader = new MockCacheConfigLoader()
            };
            _cacheManagerLogic = new CacheManagerLogic(settings);
            _cacheManagerLogic.InitializeFromConfig();
        }

        [TestMethod]
        public void TestConfigLoader()
        {
            Assert.IsNotNull(_cacheManagerLogic);
            Assert.IsNotNull(_cacheManagerLogic.Settings);
            Assert.IsInstanceOfType(_cacheManagerLogic.Settings.ConfigLoader, typeof(MockCacheConfigLoader));
        }

        [TestMethod]
        public void TestCaches()
        {
            var cacheNames = _cacheManagerLogic.GetCacheNames();

            Assert.AreEqual(2, cacheNames.Count());
            var cache1 = _cacheManagerLogic.GetCache("mockCache1");
            Assert.IsNotNull(cache1);
            Assert.IsInstanceOfType(cache1, typeof(MockNoCache));
            var cache2 = _cacheManagerLogic.GetCache("mockCache2");
            Assert.IsNotNull(cache2);
            Assert.IsInstanceOfType(cache2, typeof(MockNoCache));
            var cacheNotExist = _cacheManagerLogic.GetCache("mockCache3");
            Assert.IsNull(cacheNotExist);
        }
    }
}