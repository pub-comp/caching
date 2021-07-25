using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PubComp.Caching.Core.UnitTests
{
    // http://softwareonastring.com/502/testing-every-implementer-of-an-interface-with-the-same-tests-using-mstest

    [TestClass]
    public abstract class CacheInterfaceTests
    {
        protected abstract ICache GetCache(string name);
        protected abstract ICache GetCacheWithSlidingExpiration(string name, TimeSpan slidingExpiration);
        protected abstract ICache GetCacheWithExpirationFromAdd(string name, TimeSpan expirationFromAdd);
        protected abstract ICache GetCacheWithAbsoluteExpiration(string name, DateTimeOffset expireAt);

        private IDisposable cacheDirectives;

        [TestInitialize]
        public void TestInitialize()
        {
            cacheDirectives = CacheDirectives.SetScope(CacheMethod.GetOrSet, DateTimeOffset.UtcNow);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            cacheDirectives?.Dispose();
            cacheDirectives = null;
        }

        [TestMethod]
        public void TestCacheStruct()
        {
            var cache = GetCacheWithSlidingExpiration("cache1", TimeSpan.FromMinutes(2));
            cache.ClearAll();

            int hits = 0;

            Func<int> getter = () => { hits++; return hits; };

            int result;

            result = cache.Get("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual(1, result);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void TestCacheObject()
        {
            var cache = GetCacheWithSlidingExpiration("cache1", TimeSpan.FromMinutes(2));
            cache.ClearAll();

            int hits = 0;

            Func<string> getter = () => { hits++; return hits.ToString(); };

            string result;

            result = cache.Get("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual("1", result);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual("1", result);
        }

        [TestMethod]
        public async Task TestCacheObjectAsync()
        {
            var cache = GetCacheWithSlidingExpiration("cache1", TimeSpan.FromMinutes(2));
            cache.ClearAll();

            int hits = 0;

            Func<Task<string>> getter = async () => { hits++; return hits.ToString(); };

            string result;

            result = await cache.GetAsync("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual("1", result);

            result = await cache.GetAsync("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual("1", result);
        }

        [TestMethod]
        public void TestCacheNull()
        {
            var cache = GetCacheWithSlidingExpiration("cache1", TimeSpan.FromMinutes(2));
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
        public void TestCacheTimeToLive_FromInsert()
        {
            var ttl = 3;
            int misses = 0;
            string result;
            var stopwatch = new Stopwatch();
            Func<string> getter = () => { misses++; return misses.ToString(); };

            var cache = GetCacheWithExpirationFromAdd("insert-expire-cache", TimeSpan.FromSeconds(ttl));
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
        public void TestCacheTimeToLive_Sliding()
        {
            return;
            var ttl = 3;
            int misses = 0;
            string result;
            var stopwatch = new Stopwatch();
            Func<string> getter = () => { misses++; return misses.ToString(); };

            var cache = GetCacheWithSlidingExpiration("sliding-expire-cache", TimeSpan.FromSeconds(ttl));
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
        public void TestCacheTimeToLive_Constant()
        {
            var ttl = 3;
            int misses = 0;
            string result;
            var stopwatch = new Stopwatch();
            Func<string> getter = () => { misses++; return misses.ToString(); };

            var expireAt = DateTime.Now.AddSeconds(ttl);
            stopwatch.Start();

            var cache = GetCacheWithAbsoluteExpiration("constant-expire", expireAt);
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
        [Ignore("Diagnose manually the memory consumption")]
        public void LotsOfClearAll()
        {
            var cache = GetCache("cache1");
            for (var i = 0; i < 5000; i++)
            {
                cache.ClearAll();
            }
            Thread.Sleep(1000);
            GC.Collect();
            Thread.Sleep(1000);
            for (var i = 0; i < 5000; i++)
            {
                cache.ClearAll();
            }
        }

        [TestMethod]
        [Ignore("Diagnose manually the memory consumption")]
        public async Task LotsOfClearAsyncAll()
        {
            var cache = GetCache("cache1");
            for (var i = 0; i < 5000; i++)
            {
                await cache.ClearAllAsync().ConfigureAwait(false);
            }
            await Task.Delay(1000);
            GC.Collect();
            await Task.Delay(1000);
            for (var i = 0; i < 5000; i++)
            {
                await cache.ClearAllAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        public void TestCacheTryGet()
        {
            var cache = GetCache("cache1");
            cache.ClearAll();

            cache.Set("key", "1");

            var result = cache.TryGet<string>("key", out var value);
            Assert.AreEqual("1", value);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestCacheTryGet_NotFound()
        {
            var cache = GetCache("cache1");
            cache.ClearAll();

            cache.Set("key", "1");

            var result = cache.TryGet<string>("wrongKey", out var value);
            Assert.AreEqual(null, value);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task TestCacheTryGetAsync()
        {
            var cache = GetCache("cache1");
            cache.ClearAll();

            cache.Set("key", "1");

            var result = await cache.TryGetAsync<string>("key");
            Assert.AreEqual("1", result.Value);
            Assert.IsTrue(result.WasFound);
        }

        [TestMethod]
        public async Task TestCacheTryGetAsync_NotFound()
        {
            var cache = GetCache("cache1");
            cache.ClearAll();

            cache.Set("key", "1");

            var result = await cache.TryGetAsync<string>("wrongKey");
            Assert.AreEqual(null, result.Value);
            Assert.IsFalse(result.WasFound);
        }

        [TestMethod]
        public void TestCacheGetTwice()
        {
            var cache = GetCache("cache1");
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
        public void TestCacheSetTwice()
        {
            var cache = GetCache("cache1");
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

        [TestMethod]
        public void TestCacheUpdated()
        {
            var cache = GetCacheWithSlidingExpiration("cache1", TimeSpan.FromMinutes(2));
            cache.ClearAll();

            cache.Set("key", 1);
            var result = cache.Get("key", () => 0);
            Assert.AreEqual(1, result);

            cache.Set("key", 2);
            result = cache.Get("key", () => 0);
            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void TestCacheBasic()
        {
            var cache = GetCacheWithSlidingExpiration("cache1", TimeSpan.FromMinutes(2));
            cache.ClearAll();

            int misses = 0;
            Func<string> getter = () => { misses++; return misses.ToString(); };

            string result;

            result = cache.Get("key1", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            result = cache.Get("key2", getter);
            Assert.AreEqual(2, misses);
            Assert.AreEqual("2", result);

            cache.ClearAll();

            result = cache.Get("key1", getter);
            Assert.AreEqual(3, misses);
            Assert.AreEqual("3", result);

            result = cache.Get("key2", getter);
            Assert.AreEqual(4, misses);
            Assert.AreEqual("4", result);
        }


        [TestMethod]
        public void TestCacheTwoCaches()
        {
            var cache1 = GetCacheWithSlidingExpiration("cache1", TimeSpan.FromMinutes(2));
            cache1.ClearAll();

            var cache2 = GetCacheWithSlidingExpiration("cache2", TimeSpan.FromMinutes(2));
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
    }
}
