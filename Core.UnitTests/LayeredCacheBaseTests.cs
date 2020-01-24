using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core.UnitTests.Mocks;
using System;
using System.Threading.Tasks;

namespace PubComp.Caching.Core.UnitTests
{
    // http://softwareonastring.com/502/testing-every-implementer-of-an-interface-with-the-same-tests-using-mstest

    [TestClass]
    public abstract class LayeredCacheBaseTests
    {
        protected abstract IMockCache GetMockCache(string name);
        protected abstract ICache GetLayeredCache(string name, ICache level1, ICache level2);
        protected abstract ICache GetLayeredCache(string name, string level1, string level2);

        private IDisposable cacheDirectives;

        [TestInitialize]
        public void TestInitialize()
        {
            CacheManager.Settings = null;
            CacheManager.CacheManagerInternals = null;

            cacheDirectives = CacheDirectives.SetScope(CacheMethod.GetOrSet, DateTimeOffset.UtcNow);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            cacheDirectives?.Dispose();
            cacheDirectives = null;
        }

        [TestMethod]
        public void TestLayeredCacheValidInnerCaches()
        {
            var l1 = GetMockCache("l1");
            var l2 = GetMockCache("l2");

            var cache = GetLayeredCache("cache0", l1, l2);

            int hits = 0;

            Func<int> getter = () =>
            {
                hits++;
                return hits;
            };

            int result;

            result = cache.Get("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual(1, result);
            Assert.AreEqual(2, l1.Misses);
            Assert.AreEqual(2, l2.Misses);
            Assert.AreEqual(0, l1.Hits);
            Assert.AreEqual(0, l2.Hits);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual(1, result);
            Assert.AreEqual(2, l1.Misses);
            Assert.AreEqual(2, l2.Misses);
            Assert.AreEqual(1, l1.Hits);
            Assert.AreEqual(0, l2.Hits);

            l1.ClearAll(false);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual(1, result);
            Assert.AreEqual(4, l1.Misses);
            Assert.AreEqual(2, l2.Misses);
            Assert.AreEqual(1, l1.Hits);
            Assert.AreEqual(1, l2.Hits);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual(1, result);
            Assert.AreEqual(4, l1.Misses);
            Assert.AreEqual(2, l2.Misses);
            Assert.AreEqual(2, l1.Hits);
            Assert.AreEqual(1, l2.Hits);

            l2.ClearAll(false);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual(1, result);
            Assert.AreEqual(4, l1.Misses);
            Assert.AreEqual(2, l2.Misses);
            Assert.AreEqual(3, l1.Hits);
            Assert.AreEqual(1, l2.Hits);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual(1, result);
            Assert.AreEqual(4, l1.Misses);
            Assert.AreEqual(2, l2.Misses);
            Assert.AreEqual(4, l1.Hits);
            Assert.AreEqual(1, l2.Hits);

            l2.ClearAll(false);
            l1.ClearAll(false);

            result = cache.Get("key", getter);
            Assert.AreEqual(2, hits);
            Assert.AreEqual(2, result);
            Assert.AreEqual(6, l1.Misses);
            Assert.AreEqual(4, l2.Misses);
            Assert.AreEqual(4, l1.Hits);
            Assert.AreEqual(1, l2.Hits);

            result = cache.Get("key", getter);
            Assert.AreEqual(2, hits);
            Assert.AreEqual(2, result);
            Assert.AreEqual(6, l1.Misses);
            Assert.AreEqual(4, l2.Misses);
            Assert.AreEqual(5, l1.Hits);
            Assert.AreEqual(1, l2.Hits);

            // Clears counters too
            cache.ClearAll();

            result = cache.Get("key", getter);
            Assert.AreEqual(3, hits);
            Assert.AreEqual(3, result);
            Assert.AreEqual(2, l1.Misses);
            Assert.AreEqual(2, l2.Misses);
            Assert.AreEqual(0, l1.Hits);
            Assert.AreEqual(0, l2.Hits);

            result = cache.Get("key", getter);
            Assert.AreEqual(3, hits);
            Assert.AreEqual(3, result);
            Assert.AreEqual(2, l1.Misses);
            Assert.AreEqual(2, l2.Misses);
            Assert.AreEqual(1, l1.Hits);
            Assert.AreEqual(0, l2.Hits);
        }

        [TestMethod]
        public async Task TestLayeredCacheValidInnerCachesAsync()
        {
            var l1 = GetMockCache("l1");
            var l2 = GetMockCache("l2");

            var cache = GetLayeredCache("cache0", l1, l2);

            int hits = 0;

            Func<Task<int>> getter = async () =>
            {
                hits++;
                return hits;
            };

            int result;

            result = await cache.GetAsync("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual(1, result);
            Assert.AreEqual(2, l1.Misses);
            Assert.AreEqual(2, l2.Misses);
            Assert.AreEqual(0, l1.Hits);
            Assert.AreEqual(0, l2.Hits);

            result = await cache.GetAsync("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual(1, result);
            Assert.AreEqual(2, l1.Misses);
            Assert.AreEqual(2, l2.Misses);
            Assert.AreEqual(1, l1.Hits);
            Assert.AreEqual(0, l2.Hits);

            l1.ClearAll(false);

            result = await cache.GetAsync("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual(1, result);
            Assert.AreEqual(4, l1.Misses);
            Assert.AreEqual(2, l2.Misses);
            Assert.AreEqual(1, l1.Hits);
            Assert.AreEqual(1, l2.Hits);

            result = await cache.GetAsync("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual(1, result);
            Assert.AreEqual(4, l1.Misses);
            Assert.AreEqual(2, l2.Misses);
            Assert.AreEqual(2, l1.Hits);
            Assert.AreEqual(1, l2.Hits);

            l2.ClearAll(false);

            result = await cache.GetAsync("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual(1, result);
            Assert.AreEqual(4, l1.Misses);
            Assert.AreEqual(2, l2.Misses);
            Assert.AreEqual(3, l1.Hits);
            Assert.AreEqual(1, l2.Hits);

            result = await cache.GetAsync("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual(1, result);
            Assert.AreEqual(4, l1.Misses);
            Assert.AreEqual(2, l2.Misses);
            Assert.AreEqual(4, l1.Hits);
            Assert.AreEqual(1, l2.Hits);

            l2.ClearAll(false);
            l1.ClearAll(false);

            result = await cache.GetAsync("key", getter);
            Assert.AreEqual(2, hits);
            Assert.AreEqual(2, result);
            Assert.AreEqual(6, l1.Misses);
            Assert.AreEqual(4, l2.Misses);
            Assert.AreEqual(4, l1.Hits);
            Assert.AreEqual(1, l2.Hits);

            result = await cache.GetAsync("key", getter);
            Assert.AreEqual(2, hits);
            Assert.AreEqual(2, result);
            Assert.AreEqual(6, l1.Misses);
            Assert.AreEqual(4, l2.Misses);
            Assert.AreEqual(5, l1.Hits);
            Assert.AreEqual(1, l2.Hits);

            // Clears counters too
            cache.ClearAll();

            result = await cache.GetAsync("key", getter);
            Assert.AreEqual(3, hits);
            Assert.AreEqual(3, result);
            Assert.AreEqual(2, l1.Misses);
            Assert.AreEqual(2, l2.Misses);
            Assert.AreEqual(0, l1.Hits);
            Assert.AreEqual(0, l2.Hits);

            result = await cache.GetAsync("key", getter);
            Assert.AreEqual(3, hits);
            Assert.AreEqual(3, result);
            Assert.AreEqual(2, l1.Misses);
            Assert.AreEqual(2, l2.Misses);
            Assert.AreEqual(1, l1.Hits);
            Assert.AreEqual(0, l2.Hits);
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestLayeredCacheNullLevel1()
        {
            GetLayeredCache("cache0", null, GetMockCache("l2"));
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestLayeredCacheNullLevel2()
        {
            GetLayeredCache("cache0", GetMockCache("l1"), null);
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestLayeredCacheLevelsEqual()
        {
            var l = GetMockCache("l");
            GetLayeredCache("cache0", l, l);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestLayeredCacheNullLevel1Name()
        {
            CacheManager.RemoveCache("l");
            CacheManager.RemoveCache("l*");
            CacheManager.RemoveCache("l1");
            CacheManager.RemoveCache("l2");

            CacheManager.SetCache("l1", GetMockCache("l1"));
            CacheManager.SetCache("l2", GetMockCache("l2"));

            GetLayeredCache("cache0", null, "l2");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestLayeredCacheNullLevel2Name()
        {
            CacheManager.RemoveCache("l");
            CacheManager.RemoveCache("l*");
            CacheManager.RemoveCache("l1");
            CacheManager.RemoveCache("l2");

            CacheManager.SetCache("l1", GetMockCache("l1"));
            CacheManager.SetCache("l2", GetMockCache("l2"));

            GetLayeredCache("cache0", "l1", null);
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestLayeredCacheLevel2NotRegistered()
        {
            CacheManager.RemoveCache("l");
            CacheManager.RemoveCache("l*");
            CacheManager.RemoveCache("l1");
            CacheManager.RemoveCache("l2");

            CacheManager.SetCache("l1", GetMockCache("l1"));

            GetLayeredCache("cache0", "l1", "l2");
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestLayeredCacheLevel1NotRegistered()
        {
            CacheManager.RemoveCache("l");
            CacheManager.RemoveCache("l*");
            CacheManager.RemoveCache("l1");
            CacheManager.RemoveCache("l2");

            CacheManager.SetCache("l2", GetMockCache("l2"));

            GetLayeredCache("cache0", "l1", "l2");
        }

        [TestMethod]
        public void TestLayeredCacheBothRegistered()
        {
            CacheManager.RemoveCache("l");
            CacheManager.RemoveCache("l*");
            CacheManager.RemoveCache("l1");
            CacheManager.RemoveCache("l2");

            CacheManager.SetCache("l1", GetMockCache("l1"));
            CacheManager.SetCache("l2", GetMockCache("l2"));

            GetLayeredCache("cache0", "l1", "l2");
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestLayeredCacheBothRegisteredToSameInstance1()
        {
            CacheManager.RemoveCache("l");
            CacheManager.RemoveCache("l*");
            CacheManager.RemoveCache("l1");
            CacheManager.RemoveCache("l2");

            var l = GetMockCache("l");

            CacheManager.SetCache("l1", l);
            CacheManager.SetCache("l2", l);

            GetLayeredCache("cache0", "l1", "l2");
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestLayeredCacheBothRegisteredToSameInstance2()
        {
            CacheManager.RemoveCache("l");
            CacheManager.RemoveCache("l*");
            CacheManager.RemoveCache("l1");
            CacheManager.RemoveCache("l2");

            CacheManager.SetCache("l*", GetMockCache("l*"));

            GetLayeredCache("cache0", "l1", "l2");
        }
    }
}