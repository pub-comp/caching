using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core;
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
            const string cache1Name = "PubComp.Caching.AopCaching.UnitTests.Mocks.*";
            cache1 = CacheManager.GetCache(cache1Name) as MockCache;
            if (cache1 == null || cache1.Name != cache1Name)
                cache1 = new MockCache(cache1Name);
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

            results = service.GetItems(new[] { "k1", "k2", "k3" });

            Assert.AreEqual(3, cache1.Hits);
            Assert.AreEqual(3, cache1.Misses);
            LinqAssert.Count(results, 3);
            LinqAssert.Any(results, r => r.Id == "k1");
            LinqAssert.Any(results, r => r.Id == "k2");
            LinqAssert.Any(results, r => r.Id == "k3");
        }

        [TestMethod]
        public async Task TestMultiCacheWith1ItemsAsync()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new MultiService();
            var results = await service.GetItemsAsync(new[] { "k1" });

            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(1, cache1.Misses);
            LinqAssert.Count(results, 1);
        }

        [TestMethod]
        public async Task TestMultiCacheWith3ItemsAsync()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new MultiService();
            var results = await service.GetItemsAsync(new[] { "k1", "k2", "k3" });

            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(3, cache1.Misses);
            LinqAssert.Count(results, 3);
            LinqAssert.Any(results, r => r.Id == "k1");
            LinqAssert.Any(results, r => r.Id == "k2");
            LinqAssert.Any(results, r => r.Id == "k3");

            results = await service.GetItemsAsync(new[] { "k1", "k2", "k3" });

            Assert.AreEqual(3, cache1.Hits);
            Assert.AreEqual(3, cache1.Misses);
            LinqAssert.Count(results, 3);
            LinqAssert.Any(results, r => r.Id == "k1");
            LinqAssert.Any(results, r => r.Id == "k2");
            LinqAssert.Any(results, r => r.Id == "k3");
        }

        [TestMethod]
        public void TestKeyGeneration_CacheList()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new MultiService();
            var results = service.GetItems(new[] { "k1", "k2", "k3" });

            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(3, cache1.Misses);
            LinqAssert.Count(results, 3);
            LinqAssert.Any(results, r => r.Id == "k1");
            LinqAssert.Any(results, r => r.Id == "k2");
            LinqAssert.Any(results, r => r.Id == "k3");

            results = service.GetItems(new[] { "k1", "k2", "k3" });

            Assert.AreEqual(3, cache1.Hits);
            Assert.AreEqual(3, cache1.Misses);
            LinqAssert.Count(results, 3);
            LinqAssert.Any(results, r => r.Id == "k1");
            LinqAssert.Any(results, r => r.Id == "k2");
            LinqAssert.Any(results, r => r.Id == "k3");

            // Note: must use List<string>, not string[] here in order to get correct key
            var key = CacheKey.GetKey(() => service.GetItems(new List<string> { "k2" }));
            cache1.Clear(key);

            results = service.GetItems(new[] { "k1" });
            Assert.AreEqual(4, cache1.Hits);
            Assert.AreEqual(3, cache1.Misses);
            LinqAssert.Count(results, 1);
            LinqAssert.Any(results, r => r.Id == "k1");

            results = service.GetItems(new[] { "k2" });
            Assert.AreEqual(4, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);
            LinqAssert.Count(results, 1);
            LinqAssert.Any(results, r => r.Id == "k2");

            results = service.GetItems(new[] { "k1", "k2", "k3" });

            Assert.AreEqual(7, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);
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
        public async Task TestMultiCacheWith3ItemsThen5Async()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new MultiService();
            var results1 = await service.GetItemsAsync(new[] { "k1", "k2", "k3" });

            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(3, cache1.Misses);
            LinqAssert.Count(results1, 3);
            LinqAssert.Any(results1, r => r.Id == "k1");
            LinqAssert.Any(results1, r => r.Id == "k2");
            LinqAssert.Any(results1, r => r.Id == "k3");

            var results2 = await service.GetItemsAsync(new[] { "k5", "k2", "k1", "k4", "k3" });

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

        [TestMethod]
        public void TestDoNotIncludeInCacheKey()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new MultiService();
            IList<MockData> results;

            results = service.GetItems(new[] { "a", "b", "c" }, new MockObject(1111));
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(3, cache1.Misses);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("a", results[0].Id);
            Assert.AreEqual("a1111", results[0].Value);
            Assert.AreEqual("b", results[1].Id);
            Assert.AreEqual("b1111", results[1].Value);
            Assert.AreEqual("c", results[2].Id);
            Assert.AreEqual("c1111", results[2].Value);

            results = service.GetItems(new[] { "a", "b", "d" }, new MockObject(2222));
            Assert.AreEqual(2, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("a", results[0].Id);
            Assert.AreEqual("a1111", results[0].Value);
            Assert.AreEqual("b", results[1].Id);
            Assert.AreEqual("b1111", results[1].Value);
            Assert.AreEqual("d", results[2].Id);
            Assert.AreEqual("d2222", results[2].Value);
        }

        [TestMethod]
        public async Task TestDoNotIncludeInCacheKeyAsync()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new MultiService();
            IList<MockData> results;

            results = await service.GetItemsAsync(new[] { "a", "b", "c" }, new MockObject(1111));
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(3, cache1.Misses);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("a", results[0].Id);
            Assert.AreEqual("a1111", results[0].Value);
            Assert.AreEqual("b", results[1].Id);
            Assert.AreEqual("b1111", results[1].Value);
            Assert.AreEqual("c", results[2].Id);
            Assert.AreEqual("c1111", results[2].Value);

            results = await service.GetItemsAsync(new[] { "a", "b", "d" }, new MockObject(2222));
            Assert.AreEqual(2, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("a", results[0].Id);
            Assert.AreEqual("a1111", results[0].Value);
            Assert.AreEqual("b", results[1].Id);
            Assert.AreEqual("b1111", results[1].Value);
            Assert.AreEqual("d", results[2].Id);
            Assert.AreEqual("d2222", results[2].Value);
        }

        [TestMethod]
        public void TestMultiCacheWith3ItemsGuidKey()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();

            var service = new MultiService2();
            var results = service.GetItems(new[] { id1, id2, id3 });

            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(3, cache1.Misses);
            LinqAssert.Count(results, 3);
            LinqAssert.Any(results, r => r.Id == id1);
            LinqAssert.Any(results, r => r.Id == id2);
            LinqAssert.Any(results, r => r.Id == id3);

            results = service.GetItems(new[] { id1, id2, id3 });

            Assert.AreEqual(3, cache1.Hits);
            Assert.AreEqual(3, cache1.Misses);
            LinqAssert.Count(results, 3);
            LinqAssert.Any(results, r => r.Id == id1);
            LinqAssert.Any(results, r => r.Id == id2);
            LinqAssert.Any(results, r => r.Id == id3);
        }
    }
}
