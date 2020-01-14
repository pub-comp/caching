using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PubComp.Caching.SystemRuntime.UnitTests
{
    [TestClass]
    public class InMemorySerializedCacheTests
    {
        [TestMethod]
        public void TestInMemorySerializedCacheStruct()
        {
            var cache = new InMemorySerializedCache("cache1", new TimeSpan(0, 2, 0));

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
        public void TestInMemorySerializedCacheObject()
        {
            var cache = new InMemorySerializedCache("cache1", new TimeSpan(0, 2, 0));

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
        public void TestInMemorySerializedCacheObjectMutated()
        {
            var cache = new InMemorySerializedCache("cache1", new TimeSpan(0, 2, 0));

            List<string> value = new List<string> { "1" };

            Func<IEnumerable<object>> getter = () => { return value; };

            IEnumerable<object> result;

            result = cache.Get("key", getter);
            CollectionAssert.AreEqual(new object[] { "1" }, result.ToArray());

            value.Add("2");

            result = cache.Get("key", getter);
            CollectionAssert.AreEqual(new object[] { "1" }, result.ToArray());
        }

        [TestMethod]
        public void TestInMemorySerializedCacheNull()
        {
            var cache = new InMemorySerializedCache("cache1", new TimeSpan(0, 2, 0));

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
        public void TestInMemorySerializedCacheUpdated()
        {
            var cache = new InMemorySerializedCache("cache1", new TimeSpan(0, 2, 0));

            cache.Set("key", 1);
            var result = cache.Get("key", ()=> 0);
            Assert.AreEqual(1, result);

            cache.Set("key", 2);
            result = cache.Get("key", ()=> 0);
            Assert.AreEqual(2, result);
        }
    }
}
