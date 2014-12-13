using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    }
}
