using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.SystemRuntime;

namespace PubComp.Caching.Core.UnitTests
{
    [TestClass]
    public class NoCacheTests
    {
        [TestMethod]
        public void TestNoCacheStruct()
        {
            var cache = new NoCache();

            int hits = 0;

            Func<int> getter = () => { hits++; return hits; };

            int result;

            result = cache.Get("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual(1, result);

            result = cache.Get("key", getter);
            Assert.AreEqual(2, hits);
            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void TestNoCacheObject()
        {
            var cache = new NoCache();

            int hits = 0;

            Func<string> getter = () => { hits++; return hits.ToString(); };

            string result;

            result = cache.Get("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual("1", result);

            result = cache.Get("key", getter);
            Assert.AreEqual(2, hits);
            Assert.AreEqual("2", result);
        }

        [TestMethod]
        public void TestLayeredNoCacheObject()
        {
            var level1Cache = new NoCache();
            var level2Cache = new NoCache();
            var cache = new LayeredCache("lc", level1Cache, level2Cache);

            int hits = 0;

            Func<string> getter = () => { hits++; return hits.ToString(); };

            string result;

            result = cache.Get("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual("1", result);

            result = cache.Get("key", getter);
            Assert.AreEqual(2, hits);
            Assert.AreEqual("2", result);
        }

        [TestMethod]
        public async Task TestNoCacheObjectAsync()
        {
            var cache = new NoCache();

            int hits = 0;

            Func<Task<string>> getter = async () =>
            {
                await Task.Delay(10);
                hits++;
                return hits.ToString();
            };

            string result;

            result = await cache.GetAsync("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual("1", result);

            result = await cache.GetAsync("key", getter);
            Assert.AreEqual(2, hits);
            Assert.AreEqual("2", result);
        }

        [TestMethod]
        public async Task TestLayeredNoCacheObjectAsync()
        {
            var level1Cache = new NoCache();
            var level2Cache = new NoCache();
            var cache = new LayeredCache("lc", level1Cache,level2Cache);

            int hits = 0;

            Func<Task<string>> getter = async () =>
            {
                await Task.Delay(10);
                hits++;
                return hits.ToString();
            };

            string result;

            result = await cache.GetAsync("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual("1", result);

            result = await cache.GetAsync("key", getter);
            Assert.AreEqual(2, hits);
            Assert.AreEqual("2", result);
        }
    }
}
