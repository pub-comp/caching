using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.AopCaching;
using PubComp.Caching.Core.Exceptions;

namespace PubComp.Caching.Core.UnitTests
{
    [TestClass]
    public class CacheControllerUtilTests
    {
        private CacheControllerUtilExposed cacheControllerUtil;
        private Mocks.MockMemCache cache1;
        private Mocks.MockMemCache cache2;
        private Mocks.MockMemCache cache3;
        private Mocks.MockMemCache cache4;

        [TestInitialize]
        public void TestInitialize()
        {
            CacheManager.Settings = null;
            CacheManager.CacheManagerInternals = null;

            CacheManager.SetCache("cache*", new NoCache());

            cache1 = new Mocks.MockMemCache("cache1");
            CacheManager.SetCache(cache1.Name, cache1);

            cache2 = new Mocks.MockMemCache("cache2");
            CacheManager.SetCache(cache2.Name, cache2);

            cache3 = new Mocks.MockMemCache(typeof(TestProvider).FullName);
            CacheManager.SetCache(cache3.Name, cache3);

            cache4 = new Mocks.MockMemCache("mockCache*");
            CacheManager.SetCache(cache4.Name, cache4);

            this.cacheControllerUtil = new CacheControllerUtilExposed();
            this.cacheControllerUtil.ClearRegistrations();

            TestProvider.Hits1 = 0;
            TestProvider.Hits2 = 0;
        }

        #region Tests

        [TestMethod]
        public void TestCacheRegisterCache_GetRegisteredCacheNames()
        {
            this.cacheControllerUtil.RegisterCache("cache1", true);
            this.cacheControllerUtil.RegisterCache("cache2", true);

            var cacheNames = this.cacheControllerUtil.GetRegisteredCacheNames().ToList();
            CollectionAssert.AreEquivalent(new[] { "cache1", "cache2" }, cacheNames);
        }

        [TestMethod]
        public void TestCacheRegisterCache_GetRegisteredCacheNamesLifetimeTest()
        {
            this.cacheControllerUtil.RegisterCache("cache1", true);
            this.cacheControllerUtil.RegisterCache("cache2", true);

            this.cacheControllerUtil = new CacheControllerUtilExposed();

            var cacheNames = this.cacheControllerUtil.GetRegisteredCacheNames().ToList();
            CollectionAssert.AreEquivalent(new[] { "cache1", "cache2" }, cacheNames);
        }

        [TestMethod]
        public void TestCacheRegisterAllCaches_GetRegisteredCacheNames()
        {
            CacheManager.RemoveAllCaches();
            CacheManager.SetCache(cache1.Name, cache1);
            CacheManager.SetCache(cache2.Name, cache2);

            this.cacheControllerUtil.RegisterAllCaches();
            
            var cacheNames = this.cacheControllerUtil.GetRegisteredCacheNames().ToList();
            CollectionAssert.AreEquivalent(new[] { cache1.Name, cache2.Name }, cacheNames);
        }

        [TestMethod]
        public void TestGetRegisteredCacheNames()
        {
            var cacheNames = this.cacheControllerUtil.GetRegisteredCacheNames().ToList();
            CollectionAssert.AreEquivalent(new string[0], cacheNames);
        }

        [TestMethod][ExpectedException(typeof(CacheClearException))]
        public void TestCacheRegisterCacheItems_NullCacheName()
        {
            this.cacheControllerUtil.GetRegisteredCacheItemKeys(null);
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestCacheRegisterCacheItems_EmptyCacheName()
        {
            this.cacheControllerUtil.GetRegisteredCacheItemKeys(string.Empty);
        }

        [TestMethod]
        public void TestClearCache()
        {
            this.cacheControllerUtil.ClearCache("cache1");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestClearCache_FallbackToWildcard()
        {
            this.cacheControllerUtil.ClearCache("cache4");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestClearCache_NotFound()
        {
            this.cacheControllerUtil.ClearCache("nosuchcache");
        }

        [TestMethod]
        public void TestRegisterCacheWithTrue_ClearCache()
        {
            this.cacheControllerUtil.RegisterCache("cache2", true);

            this.cacheControllerUtil.ClearCache("cache2");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestRegisterCacheWithFalse_ClearCache()
        {
            this.cacheControllerUtil.RegisterCache("cache2", false);

            this.cacheControllerUtil.ClearCache("cache2");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestRegisterCache_EmptyName()
        {
            this.cacheControllerUtil.RegisterCache(string.Empty, true);
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestRegisterCache_NullName()
        {
            this.cacheControllerUtil.RegisterCache(null, true);
        }

        [TestMethod]
        public void TestRegisterCacheItem()
        {
            this.cacheControllerUtil.RegisterCacheItem<SubClass1>("cache1", "keyA");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestRegisterCacheItem_NullCacheName()
        {
            this.cacheControllerUtil.RegisterCacheItem<SubClass1>(null, "keyA");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestRegisterCacheItem_EmptyCacheName()
        {
            this.cacheControllerUtil.RegisterCacheItem<SubClass1>(string.Empty, "keyA");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestRegisterCacheItem_NullItemKey()
        {
            this.cacheControllerUtil.RegisterCacheItem<SubClass1>("cache1", null);
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestRegisterCacheItem_EmptyItemKey()
        {
            this.cacheControllerUtil.RegisterCacheItem<SubClass1>("cache1", string.Empty);
        }

        [TestMethod]
        public void TestRegisterCacheWithTrue_RegisterCacheItems_ClearCache()
        {
            this.cacheControllerUtil.RegisterCache("cache2", true);

            this.cacheControllerUtil.RegisterCacheItem(
                "cache2", "keyA", () => new SubClass1 { Key = "keyA", Data1 = "dataA" }, true);

            this.cacheControllerUtil.ClearCache("cache2");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestRegisterCacheWithFalse_RegisterCacheItems_ClearCache()
        {
            this.cacheControllerUtil.RegisterCache("cache2", false);

            this.cacheControllerUtil.RegisterCacheItem(
                "cache2", "keyA", () => new SubClass1 { Key = "keyA", Data1 = "dataA" }, true);

            this.cacheControllerUtil.ClearCache("cache2");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestCacheRegisterCacheItems_ClearCache()
        {
            this.cacheControllerUtil.RegisterCacheItem(
                "cache2", "keyA", () => new SubClass1 { Key = "keyA", Data1 = "dataA" }, true);

            this.cacheControllerUtil.ClearCache("cache2");
        }

        [TestMethod]
        public void TestRegisterCacheWithTrue_RegisterCacheItems_ClearCacheItem()
        {
            this.cacheControllerUtil.RegisterCacheItem(
                "cache2", "keyA", () => new SubClass1 { Key = "keyA", Data1 = "dataA" }, true);

            SubClass1 value1;
            var found1 = this.cache2.TryGet("keyA", out value1);
            Assert.IsTrue(found1);

            this.cacheControllerUtil.ClearCacheItem("cache2", "keyA");

            SubClass1 value2;
            var found2 = this.cache2.TryGet("keyA", out value2);
            Assert.IsFalse(found2);
        }

        [TestMethod]
        public void TestRegisterCacheWithTrue_RegisterCacheItems_ClearCacheItem_NotFound()
        {
            this.cacheControllerUtil.ClearCacheItem("cache1", "keyA");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestRegisterCacheWithTrue_RegisterCacheItems_ClearCacheItem_WrongCacheName()
        {
            this.cacheControllerUtil.ClearCacheItem("nosuchcache", "keyA");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestRegisterCacheWithTrue_RegisterCacheItems_ClearCacheItem_EmptyKey()
        {
            this.cacheControllerUtil.ClearCacheItem("cache1", string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestRegisterCacheWithTrue_RegisterCacheItems_ClearCacheItem_NullKey()
        {
            this.cacheControllerUtil.ClearCacheItem("cache1", null);
        }

        [TestMethod]
        public void TestCacheRegisterCacheItems()
        {
            this.cacheControllerUtil.RegisterCacheItem(
                "cache2", "keyA", () => new SubClass1 { Key = "keyA", Data1 = "dataA" }, true);

            this.cacheControllerUtil.RegisterCacheItem(
                "cache2", "keyB", () => new SubClass1 { Key = "keyB", Data1 = "dataB" }, false);

            var cacheNames = this.cacheControllerUtil.GetRegisteredCacheNames().ToList();
            CollectionAssert.AreEquivalent(new[] { "cache2" }, cacheNames);

            var cacheKeys = this.cacheControllerUtil.GetRegisteredCacheItemKeys("cache2").ToList();
            CollectionAssert.AreEquivalent(new[] { "keyA", "keyB" }, cacheKeys);
        }

        [TestMethod]
        public void TestCacheRefreshItem()
        {
            this.cacheControllerUtil.RegisterCacheItem(
                "cache2", "keyA", () => new SubClass1 { Key = "keyA", Data1 = "dataA" }, true);
            
            this.cacheControllerUtil.RegisterCacheItem(
                "cache2", "keyB", () => new SubClass1 { Key = "keyB", Data1 = "dataB" }, false);

            BaseClass value1;
            var found1 = this.cache2.TryGet("keyA", out value1);
            Assert.IsTrue(found1);
            Assert.IsInstanceOfType(value1, typeof(SubClass1));
            Assert.AreEqual(0, cache2.Misses);
            
            BaseClass value2;
            var found2 = this.cache2.TryGet("keyB", out value2);
            Assert.IsFalse(found2);
            Assert.AreEqual(1, cache2.Misses);

            this.cache2.Clear("keyB");
            this.cacheControllerUtil.RefreshCacheItem("cache2", "keyB");

            BaseClass value3;
            var found3 = this.cache2.TryGet("keyB", out value3);
            Assert.IsTrue(found3);
            Assert.IsInstanceOfType(value3, typeof(SubClass1));
            Assert.AreEqual(1, cache2.Misses);
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestCacheRefreshItem_NoSuchCache()
        {
            this.cacheControllerUtil.RefreshCacheItem("nosuchcache", "keyB");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestCacheRefreshItem_NullCacheName()
        {
            this.cacheControllerUtil.RefreshCacheItem(null, "keyB");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestCacheRefreshItem_EmptyCacheName()
        {
            this.cacheControllerUtil.RefreshCacheItem(string.Empty, "keyB");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestCacheRefreshItem_NullKey()
        {
            this.cacheControllerUtil.RefreshCacheItem("cache1", null);
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestCacheRefreshItem_EmptyKey()
        {
            this.cacheControllerUtil.RefreshCacheItem(string.Empty, null);
        }

        [TestMethod]
        public void TestCacheRefreshItemAopImpliedNamed()
        {
            this.cacheControllerUtil.RegisterCacheItem(
                () => new TestProvider().GetData4("v1", 2, true), true);

            Assert.AreEqual(0, cache4.Hits);
            Assert.AreEqual(1, TestProvider.Hits4);

            var result = new TestProvider().GetData4("v1", 2, true);
            Assert.AreEqual("GetData-v1/2/True-1", result);
            Assert.AreEqual(1, TestProvider.Hits4);
            Assert.AreEqual(1, cache4.Hits);
        }

        [TestMethod]
        public void TestCacheRefreshItemAopImpliedNamedNoCache()
        {
            this.cacheControllerUtil.RegisterCacheItem(
                () => new TestProvider().GetData0("v1", 2, true), true);

            Assert.AreEqual(1, TestProvider.Hits0);

            var result = new TestProvider().GetData0("v1", 2, true);
            Assert.AreEqual("GetData-v1/2/True-2", result);
            Assert.AreEqual(2, TestProvider.Hits0);
        }

        [TestMethod]
        public void TestCacheRefreshItemAopNamed()
        {
            this.cacheControllerUtil.RegisterCacheItem(
                () => new TestProvider().GetData("v1", 2, true), true);

            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(1, TestProvider.Hits1);

            var result = new TestProvider().GetData("v1", 2, true);
            Assert.AreEqual("GetData-v1/2/True-1", result);
            Assert.AreEqual(1, TestProvider.Hits1);
            Assert.AreEqual(1, cache1.Hits);
        }

        [TestMethod]
        public void TestCacheRefreshItemAopUnnamed()
        {
            this.cacheControllerUtil.RegisterCacheItem(
                () => new TestProvider().GetData2("v1", 2, true), true);

            Assert.AreEqual(0, cache3.Hits);
            Assert.AreEqual(1, TestProvider.Hits2);

            var result = new TestProvider().GetData2("v1", 2, true);
            Assert.AreEqual("GetData2-v1/2/True-1", result);
            Assert.AreEqual(1, TestProvider.Hits2);
            Assert.AreEqual(1, cache3.Hits);
        }


        [TestMethod]
        public void TestTryClearCache()
        {
            this.cacheControllerUtil.TryClearCache("cache1", "123", "456");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestTryClearCache_FallbackToWildcard()
        {
            this.cacheControllerUtil.TryClearCache("cache4", "123", "456");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestTryClearCache_NotFound()
        {
            this.cacheControllerUtil.TryClearCache("nosuchcache", "123", "456");
        }

        [TestMethod]
        public void TestRegisterCacheWithTrue_TryClearCache()
        {
            this.cacheControllerUtil.RegisterCache("cache2", true);

            this.cacheControllerUtil.TryClearCache("cache2", "123", "456");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestRegisterCacheWithFalse_TryClearCache()
        {
            this.cacheControllerUtil.RegisterCache("cache2", false);

            this.cacheControllerUtil.TryClearCache("cache2", "123", "456");
        }

        [TestMethod]
        public void TestRegisterCacheWithTrue_RegisterCacheItems_TryClearCache()
        {
            this.cacheControllerUtil.RegisterCache("cache2", true);

            this.cacheControllerUtil.RegisterCacheItem(
                "cache2", "keyA", () => new SubClass1 { Key = "keyA", Data1 = "dataA" }, true);

            this.cacheControllerUtil.TryClearCache("cache2", "123", "456");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestRegisterCacheWithFalse_RegisterCacheItems_TryClearCache()
        {
            this.cacheControllerUtil.RegisterCache("cache2", false);

            this.cacheControllerUtil.RegisterCacheItem(
                "cache2", "keyA", () => new SubClass1 { Key = "keyA", Data1 = "dataA" }, true);

            this.cacheControllerUtil.TryClearCache("cache2", "123", "456");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheClearException))]
        public void TestCacheRegisterCacheItems_TryClearCache()
        {
            this.cacheControllerUtil.RegisterCacheItem(
                "cache2", "keyA", () => new SubClass1 { Key = "keyA", Data1 = "dataA" }, true);

            this.cacheControllerUtil.TryClearCache("cache2", "123", "456");
        }


        #endregion

        #region Nested Types

        public class CacheControllerUtilExposed : CacheControllerUtil
        {
            public new void RegisterCacheItem<TItem>(string cacheName, string itemKey)
                where TItem : class
            {
                base.RegisterCacheItem<TItem>(cacheName, itemKey);
            }

            public new void RegisterCacheItem<TItem>(
                string cacheName, string itemKey, Func<TItem> getter, bool doInitialize)
                where TItem : class
            {
                base.RegisterCacheItem(cacheName, itemKey, getter, doInitialize);
            }

            public new void ClearRegistrations()
            {
                base.ClearRegistrations();
            }
        }

        public class BaseClass
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            public string Key { get; set; }
        }

        public class SubClass1 : BaseClass
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Global
            public string Data1 { get; set; }
        }

        public class TestProvider
        {
            // ReSharper disable RedundantDefaultMemberInitializer
            internal static int Hits0 = 0;
            internal static int Hits1 = 0;
            internal static int Hits2 = 0;
            internal static int Hits4 = 0;

            [Cache("cache1")]
            public string GetData(string v1, int v2, bool v3)
            {
                var hits = Interlocked.Increment(ref Hits1);
                return string.Concat("GetData-", v1, '/', v2, '/', v3, '-', hits);
            }

            [Cache]
            public string GetData2(string v1, int v2, bool v3)
            {
                var hits = Interlocked.Increment(ref Hits2);
                return string.Concat("GetData2-", v1, '/', v2, '/', v3, '-', hits);
            }

            [Cache("cache0")]
            public string GetData0(string v1, int v2, bool v3)
            {
                var hits = Interlocked.Increment(ref Hits0);
                return string.Concat("GetData-", v1, '/', v2, '/', v3, '-', hits);
            }

            [Cache("mockCache0")]
            public string GetData4(string v1, int v2, bool v3)
            {
                var hits = Interlocked.Increment(ref Hits4);
                return string.Concat("GetData-", v1, '/', v2, '/', v3, '-', hits);
            }
        }

        #endregion
    }
}
