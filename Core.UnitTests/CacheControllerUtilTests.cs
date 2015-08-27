using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.AopCaching;

namespace PubComp.Caching.Core.UnitTests
{
    [TestClass]
    public class CacheControllerUtilTests
    {
        private CacheControllerUtil controller;
        private Mocks.MockMemCache cache1;
        private Mocks.MockMemCache memCache;
        private Mocks.MockMemCache cache3;

        [TestInitialize]
        public void TestInitialize()
        {
            CacheManager.RemoveAllCaches();
            
            CacheManager.SetCache("cache*", new NoCache());

            cache1 = new Mocks.MockMemCache("cache1");
            CacheManager.SetCache(cache1.Name, cache1);

            memCache = new Mocks.MockMemCache("cache2");
            CacheManager.SetCache(memCache.Name, memCache);

            cache3 = new Mocks.MockMemCache(typeof(TestProvider).FullName);
            CacheManager.SetCache(cache3.Name, cache3);

            this.controller = new CacheControllerUtil(null, null);

            TestProvider.Hits1 = 0;
            TestProvider.Hits2 = 0;
        }

        [TestMethod]
        public void TestCacheRegisterCache()
        {
            this.controller.RegisterCache("cache1");
            this.controller.RegisterCache("cache2");

            var cacheNames = this.controller.GetRegisteredCacheNames().ToList();
            CollectionAssert.AreEquivalent(new[] { "cache1", "cache2" }, cacheNames);
        }

        [TestMethod]
        public void TestCacheRegisterCacheItems()
        {
            this.controller.RegisterCacheItem<SubClass1>(
                "cache2", "keyA", () => new SubClass1 { Key = "keyA", Data1 = "dataA" }, true);

            this.controller.RegisterCacheItem<SubClass1>(
                "cache2", "keyB", () => new SubClass1 { Key = "keyB", Data1 = "dataB" }, false);

            var cacheNames = this.controller.GetRegisteredCacheNames().ToList();
            CollectionAssert.AreEquivalent(new[] { "cache2" }, cacheNames);

            var cacheKeys = this.controller.GetRegisteredCacheItemKeys("cache2").ToList();
            CollectionAssert.AreEquivalent(new[] { "keyA", "keyB" }, cacheKeys);
        }

        [TestMethod]
        public void TestCacheRefreshItem()
        {
            this.controller.RegisterCacheItem(
                "cache2", "keyA", () => new SubClass1 { Key = "keyA", Data1 = "dataA" }, true);
            
            this.controller.RegisterCacheItem(
                "cache2", "keyB", () => new SubClass1 { Key = "keyB", Data1 = "dataB" }, false);

            BaseClass value1;
            var found1 = this.memCache.TryGet("keyA", out value1);
            Assert.IsTrue(found1);
            Assert.IsInstanceOfType(value1, typeof(SubClass1));
            Assert.AreEqual(0, memCache.Misses);
            
            BaseClass value2;
            var found2 = this.memCache.TryGet("keyB", out value2);
            Assert.IsFalse(found2);
            Assert.AreEqual(1, memCache.Misses);

            this.controller.RefreshCacheItem("cache2", "keyB");

            BaseClass value3;
            var found3 = this.memCache.TryGet("keyB", out value3);
            Assert.IsTrue(found3);
            Assert.IsInstanceOfType(value3, typeof(SubClass1));
            Assert.AreEqual(1, memCache.Misses);
        }

        [TestMethod]
        public void TestCacheRefreshItemAopNamed()
        {
            this.controller.RegisterCacheItem(
                () => new TestProvider().GetData("v1", 2, true), true);

            var result = new TestProvider().GetData("v1", 2, true);
            Assert.AreEqual("GetData-v1/2/True-1", result);
            Assert.AreEqual(1, TestProvider.Hits1);
        }

        [TestMethod]
        public void TestCacheRefreshItemAopUnnamed()
        {
            this.controller.RegisterCacheItem(
                () => new TestProvider().GetData2("v1", 2, true), true);

            var result = new TestProvider().GetData2("v1", 2, true);
            Assert.AreEqual("GetData2-v1/2/True-1", result);
            Assert.AreEqual(1, TestProvider.Hits2);
        }

        public class BaseClass
        {
            public string Key { get; set; }
        }

        public class SubClass1 : BaseClass
        {
            public string Data1 { get; set; }
        }

        public class TestProvider
        {
            internal static int Hits1 = 0;
            internal static int Hits2 = 0;

            [Cache("cache1")]
            public string GetData(string v1, int v2, bool v3)
            {
                var hits = Interlocked.Increment(ref Hits1);
                return string.Concat("GetData-", v1, "/", v2, "/", v3, "-", hits);
            }

            [Cache]
            public string GetData2(string v1, int v2, bool v3)
            {
                var hits = Interlocked.Increment(ref Hits2);
                return string.Concat("GetData2-", v1, "/", v2, "/", v3, "-", hits);
            }
        }
    }
}
