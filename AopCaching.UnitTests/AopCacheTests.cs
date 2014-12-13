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
        private static MockCache cache1;
        private static MockCache cache2;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            CacheManager.RemoveAllCaches();

            cache1 = new MockCache("PubComp.Caching.AopCaching.UnitTests.Mocks.*");
            CacheManager.SetCache(cache1.Name, cache1);

            cache2 = new MockCache("localCache");
            CacheManager.SetCache(cache2.Name, cache2);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            cache1.ClearAll();
            cache2.ClearAll();
        }

        [TestMethod]
        public void TestCacheWithImplicitName()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new Service1();
            var result = service.MethodToCache1();

            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(2, cache1.Misses);
        }

        [TestMethod]
        public void TestNamedCache1()
        {
            Assert.AreEqual(0, cache2.Hits);
            Assert.AreEqual(0, cache2.Misses);

            var service = new Service2();
            var result = service.MethodToCache1();

            Assert.AreEqual(0, cache2.Hits);
            Assert.AreEqual(2, cache2.Misses);
        }

        [TestMethod]
        public void TestNamedCache2()
        {
            var service = new Service2();
            var result = service.MethodToCache1(2);

            LinqAssert.AreSame(new [] { "1", "2" }, result);
            LinqAssert.AreSame(new[] { "1", "2" }, result);
        }

        [TestMethod]
        public void TestNamedCache3()
        {
            Assert.AreEqual(0, cache2.Hits);
            Assert.AreEqual(0, cache2.Misses);

            var service = new Service2();
            IEnumerable<string> result;
            
            result = service.MethodToCache1(2.0);
            Assert.AreEqual(0, cache2.Hits);
            Assert.AreEqual(2, cache2.Misses);
            LinqAssert.AreSame(new[] { "0.9", "1.9" }, result);

            result = service.MethodToCache1(2.0);
            Assert.AreEqual(1, cache2.Hits);
            Assert.AreEqual(2, cache2.Misses);
            LinqAssert.AreSame(new[] { "0.9", "1.9" }, result);

            result = service.MethodToCache1(2);
            Assert.AreEqual(1, cache2.Hits);
            Assert.AreEqual(4, cache2.Misses);
            LinqAssert.AreSame(new[] { "1", "2" }, result);

            result = service.MethodToCache1(2);
            Assert.AreEqual(2, cache2.Hits);
            Assert.AreEqual(4, cache2.Misses);
            LinqAssert.AreSame(new[] { "1", "2" }, result);
        }
    }
}
