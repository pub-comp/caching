using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core;
using PubComp.Caching.SystemRuntime;
using PubComp.Caching.AopCaching.UnitTests.Mocks;
using PubComp.Testing.TestingUtils;

namespace PubComp.Caching.AopCaching.UnitTests
{
    [TestClass]
    public class AopCacheTests
    {
        [TestMethod]
        public void TestCacheWithImplicitName()
        {
            var cache = new MockCache("PubComp.Caching.AopCaching.UnitTests.Mocks.*");
            CacheManager.SetCache(cache.Name, cache);

            Assert.AreEqual(0, cache.Hits);
            Assert.AreEqual(0, cache.Misses);

            var service = new Service1();
            var result = service.MethodToCache1();

            Assert.AreEqual(0, cache.Hits);
            Assert.AreEqual(2, cache.Misses);
        }

        [TestMethod]
        public void TestNamedCache1()
        {
            var cache = new MockCache("localCache");
            CacheManager.SetCache(cache.Name, cache);

            Assert.AreEqual(0, cache.Hits);
            Assert.AreEqual(0, cache.Misses);

            var service = new Service2();
            var result = service.MethodToCache1();

            Assert.AreEqual(0, cache.Hits);
            Assert.AreEqual(2, cache.Misses);
        }

        [TestMethod]
        public void TestNamedCache2()
        {
            var cache = new MockCache("localCache");
            CacheManager.SetCache(cache.Name, cache);

            var service = new Service2();
            var result = service.MethodToCache1(2);

            LinqAssert.AreSame(new [] { "1", "2" }, result);
            LinqAssert.AreSame(new[] { "1", "2" }, result);
        }

        [TestMethod]
        public void TestNamedCache3()
        {
            var cache = new MockCache("localCache");
            CacheManager.SetCache(cache.Name, cache);

            var service = new Service2();

            IEnumerable<string> result;

            Assert.AreEqual(0, cache.Hits);
            Assert.AreEqual(0, cache.Misses);
            
            result = service.MethodToCache1(2.0);
            Assert.AreEqual(0, cache.Hits);
            Assert.AreEqual(2, cache.Misses);
            LinqAssert.AreSame(new[] { "0.9", "1.9" }, result);

            result = service.MethodToCache1(2.0);
            Assert.AreEqual(1, cache.Hits);
            Assert.AreEqual(2, cache.Misses);
            LinqAssert.AreSame(new[] { "0.9", "1.9" }, result);

            result = service.MethodToCache1(2);
            Assert.AreEqual(1, cache.Hits);
            Assert.AreEqual(4, cache.Misses);
            LinqAssert.AreSame(new[] { "1", "2" }, result);

            result = service.MethodToCache1(2);
            Assert.AreEqual(2, cache.Hits);
            Assert.AreEqual(4, cache.Misses);
            LinqAssert.AreSame(new[] { "1", "2" }, result);
        }
    }
}
