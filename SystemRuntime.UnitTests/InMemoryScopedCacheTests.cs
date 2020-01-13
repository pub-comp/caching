using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core;

namespace PubComp.Caching.SystemRuntime.UnitTests
{
    [TestClass]
    public class InMemoryScopedCacheTests
    {
        [TestMethod]
        public void TestInMemoryScopedCacheStruct()
        {
            var cache = new InMemoryScopedCache("cache1", new TimeSpan(0, 2, 0));

            int hits = 0;

            Func<int> getter = () => { hits++; return hits; };

            int result;

            using (CacheDirectives.SetScope(CacheMethod.GetOrSet, DateTimeOffset.UtcNow))
            {
                result = cache.Get("key", getter);
                Assert.AreEqual(1, hits);
                Assert.AreEqual(1, result);

                result = cache.Get("key", getter);
                Assert.AreEqual(1, hits);
                Assert.AreEqual(1, result);
            }
        }

        [TestMethod]
        public void TestInMemoryScopedCacheObject()
        {
            var cache = new InMemoryScopedCache("cache1", new TimeSpan(0, 2, 0));

            int hits = 0;

            Func<string> getter = () => { hits++; return hits.ToString(); };

            string result;

            using (CacheDirectives.SetScope(CacheMethod.GetOrSet, DateTimeOffset.UtcNow))
            {
                result = cache.Get("key", getter);
                Assert.AreEqual(1, hits);
                Assert.AreEqual("1", result);

                result = cache.Get("key", getter);
                Assert.AreEqual(1, hits);
                Assert.AreEqual("1", result);
            }
        }

        [TestMethod]
        public void TestInMemoryScopedCacheNull()
        {
            var cache = new InMemoryScopedCache("cache1", new TimeSpan(0, 2, 0));

            int misses = 0;

            Func<string> getter = () => { misses++; return null; };

            string result;

            using (CacheDirectives.SetScope(CacheMethod.GetOrSet, DateTimeOffset.UtcNow))
            {
                result = cache.Get("key", getter);
                Assert.AreEqual(1, misses);
                Assert.AreEqual(null, result);

                result = cache.Get("key", getter);
                Assert.AreEqual(1, misses);
                Assert.AreEqual(null, result);
            }
        }
    }
}
