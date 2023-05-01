using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PubComp.Caching.MongoDbCaching.UnitTests
{
    [TestClass]
    public class MongoDbCacheTests
    {
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
            CollectionAssert.AreEqual(new object[] { "1" }, result.ToArray());

            value.Add("2");

            result = cache.Get("key", getter);
            CollectionAssert.AreEqual(new object[] { "1" }, result.ToArray());
        }
    }
}
