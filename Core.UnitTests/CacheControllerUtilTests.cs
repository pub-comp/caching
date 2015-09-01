using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.AopCaching;

namespace PubComp.Caching.Core.UnitTests
{
    [TestClass]
    public class CacheControllerUtilTests
    {
        private ControllerExposed controller;
        private Mocks.MockMemCache cache1;
        private Mocks.MockMemCache cache2;
        private Mocks.MockMemCache cache3;

        [TestInitialize]
        public void TestInitialize()
        {
            CacheManager.RemoveAllCaches();
            
            CacheManager.SetCache("cache*", new NoCache());

            cache1 = new Mocks.MockMemCache("cache1");
            CacheManager.SetCache(cache1.Name, cache1);

            cache2 = new Mocks.MockMemCache("cache2");
            CacheManager.SetCache(cache2.Name, cache2);

            cache3 = new Mocks.MockMemCache(typeof(TestProvider).FullName);
            CacheManager.SetCache(cache3.Name, cache3);

            this.controller = new ControllerExposed();

            TestProvider.Hits1 = 0;
            TestProvider.Hits2 = 0;
        }

        #region Tests

        [TestMethod]
        public void TestCacheRegisterCache_GetRegisteredCacheNames()
        {
            this.controller.RegisterCache("cache1", true);
            this.controller.RegisterCache("cache2", true);

            var cacheNames = this.controller.GetRegisteredCacheNames().ToList();
            CollectionAssert.AreEquivalent(new[] { "cache1", "cache2" }, cacheNames);
        }

        [TestMethod]
        public void TestGetRegisteredCacheNames()
        {
            var cacheNames = this.controller.GetRegisteredCacheNames().ToList();
            CollectionAssert.AreEquivalent(new string[0], cacheNames);
        }

        [TestMethod][ExpectedException(typeof(CacheException))]
        public void TestCacheRegisterCacheItems_NullCacheName()
        {
            this.controller.GetRegisteredCacheItemKeys(null);
        }

        [TestMethod]
        [ExpectedException(typeof(CacheException))]
        public void TestCacheRegisterCacheItems_EmptyCacheName()
        {
            this.controller.GetRegisteredCacheItemKeys(string.Empty);
        }

        [TestMethod]
        public void TestClearCache()
        {
            this.controller.ClearCache("cache1");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheException))]
        public void TestClearCache_FallbackToWildcard()
        {
            this.controller.ClearCache("cache4");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheException))]
        public void TestClearCache_NotFound()
        {
            this.controller.ClearCache("nosuchcache");
        }

        [TestMethod]
        public void TestRegisterCacheWithTrue_ClearCache()
        {
            this.controller.RegisterCache("cache2", true);

            this.controller.ClearCache("cache2");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheException))]
        public void TestRegisterCacheWithFalse_ClearCache()
        {
            this.controller.RegisterCache("cache2", false);

            this.controller.ClearCache("cache2");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheException))]
        public void TestRegisterCache_EmptyName()
        {
            this.controller.RegisterCache(string.Empty, true);
        }

        [TestMethod]
        [ExpectedException(typeof(CacheException))]
        public void TestRegisterCache_NullName()
        {
            this.controller.RegisterCache(null, true);
        }

        [TestMethod]
        public void TestRegisterCacheItem()
        {
            this.controller.RegisterCacheItem<SubClass1>("cache1", "keyA");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheException))]
        public void TestRegisterCacheItem_NullCacheName()
        {
            this.controller.RegisterCacheItem<SubClass1>(null, "keyA");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheException))]
        public void TestRegisterCacheItem_EmptyCacheName()
        {
            this.controller.RegisterCacheItem<SubClass1>(string.Empty, "keyA");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheException))]
        public void TestRegisterCacheItem_NullItemKey()
        {
            this.controller.RegisterCacheItem<SubClass1>("cache1", null);
        }

        [TestMethod]
        [ExpectedException(typeof(CacheException))]
        public void TestRegisterCacheItem_EmptyItemKey()
        {
            this.controller.RegisterCacheItem<SubClass1>("cache1", string.Empty);
        }

        [TestMethod]
        public void TestRegisterCacheWithTrue_RegisterCacheItems_ClearCache()
        {
            this.controller.RegisterCache("cache2", true);

            this.controller.RegisterCacheItem(
                "cache2", "keyA", () => new SubClass1 { Key = "keyA", Data1 = "dataA" }, true);

            this.controller.ClearCache("cache2");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheException))]
        public void TestRegisterCacheWithFalse_RegisterCacheItems_ClearCache()
        {
            this.controller.RegisterCache("cache2", false);

            this.controller.RegisterCacheItem(
                "cache2", "keyA", () => new SubClass1 { Key = "keyA", Data1 = "dataA" }, true);

            this.controller.ClearCache("cache2");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheException))]
        public void TestCacheRegisterCacheItems_ClearCache()
        {
            this.controller.RegisterCacheItem(
                "cache2", "keyA", () => new SubClass1 { Key = "keyA", Data1 = "dataA" }, true);

            this.controller.ClearCache("cache2");
        }

        [TestMethod]
        public void TestRegisterCacheWithTrue_RegisterCacheItems_ClearCacheItem()
        {
            this.controller.RegisterCacheItem(
                "cache2", "keyA", () => new SubClass1 { Key = "keyA", Data1 = "dataA" }, true);

            SubClass1 value1;
            var found1 = this.cache2.TryGet("keyA", out value1);
            Assert.IsTrue(found1);

            this.controller.ClearCacheItem("cache2", "keyA");

            SubClass1 value2;
            var found2 = this.cache2.TryGet("keyA", out value2);
            Assert.IsFalse(found2);
        }

        [TestMethod]
        public void TestRegisterCacheWithTrue_RegisterCacheItems_ClearCacheItem_NotFound()
        {
            this.controller.ClearCacheItem("cache1", "keyA");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheException))]
        public void TestRegisterCacheWithTrue_RegisterCacheItems_ClearCacheItem_WrongCacheName()
        {
            this.controller.ClearCacheItem("nosuchcache", "keyA");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheException))]
        public void TestRegisterCacheWithTrue_RegisterCacheItems_ClearCacheItem_EmptyKey()
        {
            this.controller.ClearCacheItem("cache1", string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(CacheException))]
        public void TestRegisterCacheWithTrue_RegisterCacheItems_ClearCacheItem_NullKey()
        {
            this.controller.ClearCacheItem("cache1", null);
        }

        [TestMethod]
        public void TestCacheRegisterCacheItems()
        {
            this.controller.RegisterCacheItem(
                "cache2", "keyA", () => new SubClass1 { Key = "keyA", Data1 = "dataA" }, true);

            this.controller.RegisterCacheItem(
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
            var found1 = this.cache2.TryGet("keyA", out value1);
            Assert.IsTrue(found1);
            Assert.IsInstanceOfType(value1, typeof(SubClass1));
            Assert.AreEqual(0, cache2.Misses);
            
            BaseClass value2;
            var found2 = this.cache2.TryGet("keyB", out value2);
            Assert.IsFalse(found2);
            Assert.AreEqual(1, cache2.Misses);

            this.cache2.Clear("keyB");
            this.controller.RefreshCacheItem("cache2", "keyB");

            BaseClass value3;
            var found3 = this.cache2.TryGet("keyB", out value3);
            Assert.IsTrue(found3);
            Assert.IsInstanceOfType(value3, typeof(SubClass1));
            Assert.AreEqual(1, cache2.Misses);
        }

        [TestMethod]
        [ExpectedException(typeof(CacheException))]
        public void TestCacheRefreshItem_NoSuchCache()
        {
            this.controller.RefreshCacheItem("nosuchcache", "keyB");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheException))]
        public void TestCacheRefreshItem_NullCacheName()
        {
            this.controller.RefreshCacheItem(null, "keyB");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheException))]
        public void TestCacheRefreshItem_EmptyCacheName()
        {
            this.controller.RefreshCacheItem(string.Empty, "keyB");
        }

        [TestMethod]
        [ExpectedException(typeof(CacheException))]
        public void TestCacheRefreshItem_NullKey()
        {
            this.controller.RefreshCacheItem("cache1", null);
        }

        [TestMethod]
        [ExpectedException(typeof(CacheException))]
        public void TestCacheRefreshItem_EmptyKey()
        {
            this.controller.RefreshCacheItem(string.Empty, null);
        }

        [TestMethod]
        public void TestCacheRefreshItemAopNamed()
        {
            this.controller.RegisterCacheItem(
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
            this.controller.RegisterCacheItem(
                () => new TestProvider().GetData2("v1", 2, true), true);

            Assert.AreEqual(0, cache3.Hits);
            Assert.AreEqual(1, TestProvider.Hits2);

            var result = new TestProvider().GetData2("v1", 2, true);
            Assert.AreEqual("GetData2-v1/2/True-1", result);
            Assert.AreEqual(1, TestProvider.Hits2);
            Assert.AreEqual(1, cache3.Hits);
        }

        #endregion

        #region Nested Types

        public class ControllerExposed : CacheControllerUtil
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
            internal static int Hits1 = 0;
            internal static int Hits2 = 0;

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
        }

        #endregion
    }
}
