using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Testing.TestingUtils;
using PubComp.Caching.Core.UnitTests;

namespace PubComp.Caching.MongoDbCaching.UnitTests
{
    [TestClass]
    public class MongoDbCacheTests
    {
        [TestMethod]
        public void TestMongoDbCacheTwoCaches()
        {
            var cache1 = new MongoDbCache(
                "cache1",
                new MongoDbCachePolicy
                {
                    DatabaseName = "TestCacheDb",
                });
            cache1.ClearAll();


            var cache2 = new MongoDbCache(
                "cache2",
                new MongoDbCachePolicy
                {
                    DatabaseName = "TestCacheDb",
                });
            cache2.ClearAll();

            int misses1 = 0;
            Func<string> getter1 = () => { misses1++; return misses1.ToString(); };

            int misses2 = 0;
            Func<string> getter2 = () => { misses2++; return misses2.ToString(); };

            string result;

            result = cache1.Get("key1", getter1);
            Assert.AreEqual(1, misses1);
            Assert.AreEqual("1", result);

            result = cache1.Get("key2", getter1);
            Assert.AreEqual(2, misses1);
            Assert.AreEqual("2", result);

            result = cache2.Get("key1", getter2);
            Assert.AreEqual(1, misses2);
            Assert.AreEqual("1", result);

            result = cache2.Get("key2", getter2);
            Assert.AreEqual(2, misses2);
            Assert.AreEqual("2", result);

            cache1.ClearAll();

            result = cache1.Get("key1", getter1);
            Assert.AreEqual(3, misses1);
            Assert.AreEqual("3", result);

            result = cache1.Get("key2", getter1);
            Assert.AreEqual(4, misses1);
            Assert.AreEqual("4", result);

            result = cache2.Get("key1", getter2);
            Assert.AreEqual(2, misses2);
            Assert.AreEqual("1", result);

            result = cache2.Get("key2", getter2);
            Assert.AreEqual(2, misses2);
            Assert.AreEqual("2", result);
        }

        [TestMethod]
        public void TestMongoDbCacheStruct()
        {
            var cache = new MongoDbCache(
                "cache1",
                new MongoDbCachePolicy
                {
                    DatabaseName = "TestCacheDb",
                });
            cache.ClearAll();

            int misses = 0;

            Func<int> getter = () => { misses++; return misses; };

            int result;

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual(1, result);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void TestMongoDbCacheObject()
        {
            var cache = new MongoDbCache(
                "cache1",
                new MongoDbCachePolicy
                {
                    DatabaseName = "TestCacheDb",
                });
            cache.ClearAll();

            int misses = 0;

            Func<string> getter = () => { misses++; return misses.ToString(); };

            string result;

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);
        }

        [TestMethod]
        public async Task TestMongoDbCacheObjectAsync()
        {
            var cache = new MongoDbCache(
                "cache1",
                new MongoDbCachePolicy
                {
                    DatabaseName = "TestCacheDb",
                });
            await cache.ClearAllAsync();

            int misses = 0;

            Func<Task<string>> getter = () => Task.Run(() => { misses++; return misses.ToString(); });

            var result = await cache.GetAsync("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            result = await cache.GetAsync("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);
        }

        [TestMethod]
        public void TestMongoDbCacheNull()
        {
            var cache = new MongoDbCache(
                "cache1",
                new MongoDbCachePolicy
                {
                    DatabaseName = "TestCacheDb",
                });
            cache.ClearAll();

            int misses = 0;

            Func<string> getter = () => { misses++; return null; };

            string result;

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual(null, result);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual(null, result);
        }

        [TestMethod]
        public void TestMongoDbCacheObjectMutated()
        {
            var cache = new MongoDbCache(
                "cache1",
                new MongoDbCachePolicy
                {
                    DatabaseName = "TestCacheDb",
                });
            cache.ClearAll();

            List<string> value = new List<string> { "1" };

            Func<IEnumerable<object>> getter = () => { return value; };

            IEnumerable<object> result;

            result = cache.Get("key", getter);
            LinqAssert.AreSame(new object[] { "1" }, result);

            value.Add("2");

            result = cache.Get("key", getter);
            LinqAssert.AreSame(new object[] { "1" }, result);
        }

        [TestMethod]
        public void TestMongoDbCacheTimeToLive_FromInsert()
        {
            var ttl = 3;
            int misses = 0;
            string result;
            var stopwatch = new Stopwatch();
            Func<string> getter = () => { misses++; return misses.ToString(); };

            var cache = new MongoDbCache(
                "insert-expire-cache",
                new MongoDbCachePolicy
                {
                    DatabaseName = "TestCacheDb",
                    ExpirationFromAdd = TimeSpan.FromSeconds(ttl),
                });
            cache.ClearAll();

            stopwatch.Start();
            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            CacheTestTools.AssertValueDoesntChangeWithin(cache, "key", "1", getter, stopwatch, ttl);

            // Should expire within TTL+60sec from insert
            CacheTestTools.AssertValueDoesChangeWithin(cache, "key", "1", getter, stopwatch, 60.1);

            result = cache.Get("key", getter);
            Assert.AreNotEqual(1, misses);
            Assert.AreNotEqual("1", result);
        }

        [TestMethod]
        public void TestMongoDbCacheTimeToLive_Sliding()
        {
            var ttl = 3;
            int misses = 0;
            string result;
            var stopwatch = new Stopwatch();
            Func<string> getter = () => { misses++; return misses.ToString(); };

            var cache = new MongoDbCache(
                "sliding-expire-cache",
                new MongoDbCachePolicy
                {
                    DatabaseName = "TestCacheDb",
                    SlidingExpiration = TimeSpan.FromSeconds(ttl),
                });
            cache.ClearAll();

            stopwatch.Start();
            result = cache.Get("key", getter);
            DateTime insertTime = DateTime.Now;
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            CacheTestTools.AssertValueDoesntChangeWithin(cache, "key", "1", getter, stopwatch, ttl + 60);

            // Should expire within TTL+60sec from last access
            CacheTestTools.AssertValueDoesChangeAfter(cache, "key", "1", getter, stopwatch, ttl + 60.1);

            result = cache.Get("key", getter);
            Assert.AreNotEqual(1, misses);
            Assert.AreNotEqual("1", result);
        }

        [TestMethod]
        public void TestMongoDbCacheTimeToLive_Constant()
        {
            var ttl = 3;
            int misses = 0;
            string result;
            var stopwatch = new Stopwatch();
            Func<string> getter = () => { misses++; return misses.ToString(); };

            var expireAt = DateTime.Now.AddSeconds(ttl);
            stopwatch.Start();

            var cache = new MongoDbCache(
                "constant-expire",
                new MongoDbCachePolicy
                {
                    DatabaseName = "TestCacheDb",
                    AbsoluteExpiration = expireAt,
                });
            cache.ClearAll();

            result = cache.Get("key", getter);
            DateTime insertTime = DateTime.Now;
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            CacheTestTools.AssertValueDoesntChangeWithin(cache, "key", "1", getter, stopwatch, ttl);

            // Should expire within TTL+60sec from insert
            CacheTestTools.AssertValueDoesChangeWithin(cache, "key", "1", getter, stopwatch, 60.1);

            result = cache.Get("key", getter);
            Assert.AreNotEqual(1, misses);
            Assert.AreNotEqual("1", result);
        }

        [TestMethod]
        public void TestMongoDbCacheGetTwice()
        {
            var cache = new MongoDbCache(
                "cache1",
                new MongoDbCachePolicy
                {
                    DatabaseName = "TestCacheDb",
                });
            cache.ClearAll();

            int misses = 0;

            Func<string> getter = () => { misses++; return misses.ToString(); };

            string result;

            result = cache.Get("key", getter);
            Assert.AreEqual("1", result);

            result = cache.Get("key", getter);
            Assert.AreEqual("1", result);
        }

        [TestMethod]
        public void TestMongoDbCacheSetTwice()
        {
            var cache = new MongoDbCache(
                "cache1",
                new MongoDbCachePolicy
                {
                    DatabaseName = "TestCacheDb",
                });
            cache.ClearAll();

            int misses = 0;

            Func<string> getter = () => { misses++; return misses.ToString(); };

            string result;
            bool wasFound;

            cache.Set("key", getter());
            wasFound = cache.TryGet("key", out result);
            Assert.AreEqual(true, wasFound);
            Assert.AreEqual("1", result);

            cache.Set("key", getter());
            wasFound = cache.TryGet("key", out result);
            Assert.AreEqual(true, wasFound);
            Assert.AreEqual("2", result);
        }
    }
}
