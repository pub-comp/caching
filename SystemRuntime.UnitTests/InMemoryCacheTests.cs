using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.SystemRuntime;
using PubComp.Testing.TestingUtils;

namespace PubComp.Caching.SystemRuntime.UnitTests
{
    [TestClass]
    public class InMemoryCacheTests
    {
        [TestMethod]
        public void TestInMemoryCacheStruct()
        {
            var cache = new InMemoryCache("cache1", new TimeSpan(0, 2, 0));

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
        public void TestInMemoryCacheObject()
        {
            var cache = new InMemoryCache("cache1", new TimeSpan(0, 2, 0));

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
        public void TestInMemoryCacheObjectMutated()
        {
            var cache = new InMemoryCache("cache1", new TimeSpan(0, 2, 0));

            List<string> value = new List<string> { "1" };

            Func<IEnumerable<object>> getter = () => { return value; };

            IEnumerable<object> result;

            result = cache.Get("key", getter);
            LinqAssert.AreSame(new object[] { "1" }, result);

            value.Add("2");

            result = cache.Get("key", getter);
            LinqAssert.AreSame(new object[] { "1", "2" }, result);
        }
    }
}
