using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core;
using PubComp.Caching.SystemRuntime;
using PubComp.Caching.AopCaching.UnitTests.Mocks;
using PubComp.Testing.TestingUtils;

namespace PubComp.Caching.AopCaching.UnitTests
{
    [TestClass]
    public class AopMultiCacheTests
    {
        private static MockCache cache1;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            CacheManager.RemoveAllCaches();

            cache1 = new MockCache("PubComp.Caching.AopCaching.UnitTests.Mocks.*");
            CacheManager.SetCache(cache1.Name, cache1);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            cache1.ClearAll();
        }

        [TestMethod]
        public void TestMultiCacheWith1Items()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new MultiService();
            var results = service.GetItems(new[] { "k1" });

            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(1, cache1.Misses);
            LinqAssert.Count(results, 1);
        }

        [TestMethod]
        public void TestMultiCacheWith3Items()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new MultiService();
            var results = service.GetItems(new [] { "k1", "k2", "k3" });

            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(3, cache1.Misses);
            LinqAssert.Count(results, 3);
            LinqAssert.Any(results, r => r.Id == "k1");
            LinqAssert.Any(results, r => r.Id == "k2");
            LinqAssert.Any(results, r => r.Id == "k3");
        }

        [TestMethod]
        public void TestMultiCacheWith3ItemsThen5()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new MultiService();
            var results1 = service.GetItems(new[] { "k1", "k2", "k3" });

            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(3, cache1.Misses);
            LinqAssert.Count(results1, 3);
            LinqAssert.Any(results1, r => r.Id == "k1");
            LinqAssert.Any(results1, r => r.Id == "k2");
            LinqAssert.Any(results1, r => r.Id == "k3");

            var results2 = service.GetItems(new[] { "k5", "k2", "k1", "k4", "k3" });

            Assert.AreEqual(3, cache1.Hits);
            Assert.AreEqual(5, cache1.Misses);
            LinqAssert.Count(results2, 5);
            LinqAssert.Any(results2, r => r.Id == "k1");
            LinqAssert.Any(results2, r => r.Id == "k2");
            LinqAssert.Any(results2, r => r.Id == "k3");
            LinqAssert.Any(results2, r => r.Id == "k4");
            LinqAssert.Any(results2, r => r.Id == "k5");
        }

        [TestMethod]
        public void TestMultiCacheWith3ItemsThen1And1()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new MultiService();
            var results1 = service.GetItems(new[] { "k1", "k2", "k3" });

            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(3, cache1.Misses);
            LinqAssert.Count(results1, 3);
            LinqAssert.Any(results1, r => r.Id == "k1");
            LinqAssert.Any(results1, r => r.Id == "k2");
            LinqAssert.Any(results1, r => r.Id == "k3");

            var results2 = service.GetItems(new[] { "k3", "k4" });

            Assert.AreEqual(1, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);
            LinqAssert.Count(results2, 2);
            LinqAssert.Any(results2, r => r.Id == "k3");
            LinqAssert.Any(results2, r => r.Id == "k4");
        }

        [TestMethod]
        public void TestMultiCacheWith3ItemsThen1()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new MultiService();
            var results1 = service.GetItems(new[] { "k1", "k2", "k3" });

            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(3, cache1.Misses);
            LinqAssert.Count(results1, 3);
            LinqAssert.Any(results1, r => r.Id == "k1");
            LinqAssert.Any(results1, r => r.Id == "k2");
            LinqAssert.Any(results1, r => r.Id == "k3");

            var results2 = service.GetItems(new[] { "k1" });

            Assert.AreEqual(1, cache1.Hits);
            Assert.AreEqual(3, cache1.Misses);
            LinqAssert.Count(results2, 1);
            LinqAssert.Any(results2, r => r.Id == "k1");
        }

        [TestMethod]
        public void TestMultiCacheWith3ItemsThen0And2()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new MultiService();
            var results1 = service.GetItems(new[] { "k1", "k2", "k3" });

            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(3, cache1.Misses);
            LinqAssert.Count(results1, 3);
            LinqAssert.Any(results1, r => r.Id == "k1");
            LinqAssert.Any(results1, r => r.Id == "k2");
            LinqAssert.Any(results1, r => r.Id == "k3");

            var results2 = service.GetItems(new[] { "k4", "k5" });

            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(5, cache1.Misses);
            LinqAssert.Count(results2, 2);
            LinqAssert.Any(results2, r => r.Id == "k4");
            LinqAssert.Any(results2, r => r.Id == "k5");
        }
    }
}
