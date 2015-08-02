using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Testing.TestingUtils;
using PubComp.Caching.Core.UnitTests;

namespace PubComp.Caching.Core.UnitTests
{
    [TestClass]
    public class LayeredCacheTests
    {
        [TestMethod]
        public void TestLayeredCacheValidInnerCaches()
        {
            var l1 = new Mocks.MockCache2("l1");
            var l2 = new Mocks.MockCache2("l2");

            var cache = new LayeredCache("cache0", l1, l2);

            int hits = 0;

            Func<int> getter = () => { hits++; return hits; };

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

        [TestMethod][ExpectedException(typeof(ApplicationException))]
        public void TestLayeredCacheNullLevel1()
        {
            var cache = new LayeredCache("cache0", null, new Mocks.MockCache2("l2"));
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestLayeredCacheNullLevel2()
        {
            var cache = new LayeredCache("cache0",  new Mocks.MockCache2("l1"), null);
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestLayeredCacheLevelsEqual()
        {
            var l = new Mocks.MockCache2("l");
            var cache = new LayeredCache("cache0", l, l);
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestLayeredCacheNullLevel1Name()
        {
            var cache = new LayeredCache("cache0", null, "l2");
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestLayeredCacheNullLevel2Name()
        {
            var cache = new LayeredCache("cache0", "l1", null);
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestLayeredCacheLevel2NotRegistered()
        {
            CacheManager.RemoveCache("l");
            CacheManager.RemoveCache("l*");
            CacheManager.RemoveCache("l1");
            CacheManager.RemoveCache("l2");

            CacheManager.SetCache("l1", new Mocks.MockCache2("l1"));

            var cache = new LayeredCache("cache0", "l1", "l2");
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestLayeredCacheLevel1NotRegistered()
        {
            CacheManager.RemoveCache("l");
            CacheManager.RemoveCache("l*");
            CacheManager.RemoveCache("l1");
            CacheManager.RemoveCache("l2");

            CacheManager.SetCache("l2", new Mocks.MockCache2("l2"));

            var cache = new LayeredCache("cache0", "l1", "l2");
        }

        [TestMethod]
        public void TestLayeredCacheBothRegistered()
        {
            CacheManager.RemoveCache("l");
            CacheManager.RemoveCache("l*");
            CacheManager.RemoveCache("l1");
            CacheManager.RemoveCache("l2");

            CacheManager.SetCache("l1", new Mocks.MockCache2("l1"));
            CacheManager.SetCache("l2", new Mocks.MockCache2("l2"));

            var cache = new LayeredCache("cache0", "l1", "l2");
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestLayeredCacheBothRegisteredToSameInstance1()
        {
            CacheManager.RemoveCache("l");
            CacheManager.RemoveCache("l*");
            CacheManager.RemoveCache("l1");
            CacheManager.RemoveCache("l2");

            var l = new Mocks.MockCache2("l");

            CacheManager.SetCache("l1", l);
            CacheManager.SetCache("l2", l);

            var cache = new LayeredCache("cache0", "l1", "l2");
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestLayeredCacheBothRegisteredToSameInstance2()
        {
            CacheManager.RemoveCache("l");
            CacheManager.RemoveCache("l*");
            CacheManager.RemoveCache("l1");
            CacheManager.RemoveCache("l2");

            CacheManager.SetCache("l*", new Mocks.MockCache2("l*"));

            var cache = new LayeredCache("cache0", "l1", "l2");
        }
    }
}
