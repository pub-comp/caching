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
    }
}
