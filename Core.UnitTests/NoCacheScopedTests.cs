using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PubComp.Caching.Core.UnitTests
{
    [TestClass]
    public class NoCacheScopedTests
    {
        [TestMethod]
        public void TestNoCacheStruct()
        {
            var cache = new NoCache();

            int hits = 0;

            Func<ScopedValue<int>> getter = () => { hits++; return new ScopedValue<int>(hits, DateTimeOffset.UtcNow); };

            GetScopedResult<int> result;

            using (CacheDirectives.SetScope(CacheMethod.GetOrSet, DateTimeOffset.UtcNow))
            {
                result = cache.GetScoped("key", getter);
                Assert.AreEqual(1, hits);
                Assert.AreEqual(1, result.ScopedValue.Value);
                Assert.AreEqual(CacheMethodTaken.None, result.MethodTaken);

                result = cache.GetScoped("key", getter);
                Assert.AreEqual(2, hits);
                Assert.AreEqual(2, result.ScopedValue.Value);
                Assert.AreEqual(CacheMethodTaken.None, result.MethodTaken);
            }
        }

        [TestMethod]
        public void TestNoCacheObject()
        {
            var cache = new NoCache();

            int hits = 0;

            Func<ScopedValue<string>> getter = () => { hits++; return new ScopedValue<string>(hits.ToString(), DateTimeOffset.UtcNow); };

            GetScopedResult<string> result;

            result = cache.GetScoped("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual("1", result.ScopedValue.Value);
            Assert.AreEqual(CacheMethodTaken.None, result.MethodTaken);

            result = cache.GetScoped("key", getter);
            Assert.AreEqual(2, hits);
            Assert.AreEqual("2", result.ScopedValue.Value);
            Assert.AreEqual(CacheMethodTaken.None, result.MethodTaken);
        }

        [TestMethod]
        public void TestLayeredScopedNoCacheObject()
        {
            var level1Cache = new NoCache();
            var level2Cache = new NoCache();
            var cache = new LayeredScopedCache("lc", level1Cache, level2Cache);

            int hits = 0;

            Func<ScopedValue<string>> getter = () => { hits++; return new ScopedValue<string>(hits.ToString(), DateTimeOffset.UtcNow); };

            GetScopedResult<string> result;

            result = cache.GetScoped("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual("1", result.ScopedValue.Value);
            Assert.AreEqual(CacheMethodTaken.None, result.MethodTaken);

            result = cache.GetScoped("key", getter);
            Assert.AreEqual(2, hits);
            Assert.AreEqual("2", result.ScopedValue.Value);
            Assert.AreEqual(CacheMethodTaken.None, result.MethodTaken);
        }

        [TestMethod]
        public async Task TestNoCacheObjectAsync()
        {
            var cache = new NoCache();

            int hits = 0;

            Func<Task<ScopedValue<string>>> getter = async () =>
            {
                await Task.Delay(10);
                hits++;
                return new ScopedValue<string>(hits.ToString(), DateTimeOffset.UtcNow);
            };

            GetScopedResult<string> result;

            result = await cache.GetScopedAsync("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual("1", result.ScopedValue.Value);
            Assert.AreEqual(CacheMethodTaken.None, result.MethodTaken);

            result = await cache.GetScopedAsync("key", getter);
            Assert.AreEqual(2, hits);
            Assert.AreEqual("2", result.ScopedValue.Value);
            Assert.AreEqual(CacheMethodTaken.None, result.MethodTaken);
        }

        [TestMethod]
        public async Task TestLayeredScopedNoCacheObjectAsync()
        {
            var level1Cache = new NoCache();
            var level2Cache = new NoCache();
            var cache = new LayeredScopedCache("lc", level1Cache, level2Cache);

            int hits = 0;

            Func<Task<ScopedValue<string>>> getter = async () =>
            {
                await Task.Delay(10);
                hits++;
                return new ScopedValue<string>(hits.ToString(), DateTimeOffset.UtcNow);
            };

            GetScopedResult<string> result;

            result = await cache.GetScopedAsync("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual("1", result.ScopedValue.Value);
            Assert.AreEqual(CacheMethodTaken.None, result.MethodTaken);

            result = await cache.GetScopedAsync("key", getter);
            Assert.AreEqual(2, hits);
            Assert.AreEqual("2", result.ScopedValue.Value);
            Assert.AreEqual(CacheMethodTaken.None, result.MethodTaken);
        }
    }
}
