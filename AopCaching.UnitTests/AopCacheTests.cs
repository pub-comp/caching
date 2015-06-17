using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core;
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
            const string cache1Name = "PubComp.Caching.AopCaching.UnitTests.Mocks.*";
            cache1 = CacheManager.GetCache(cache1Name) as MockCache;
            if (cache1 == null || cache1.Name != cache1Name)
                cache1 = new MockCache(cache1Name);
            CacheManager.SetCache(cache1.Name, cache1);

            const string cache2Name = "localCache";
            cache2 = CacheManager.GetCache(cache2Name) as MockCache;
            if (cache2 == null || cache2.Name != cache2Name)
                cache2 = new MockCache(cache2Name);
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

        [TestMethod]
        public void TestDoNotIncludeInCacheKey()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new Service1();
            string result;

            result = service.MethodToCache1(11, new MockObject(1111));
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(2, cache1.Misses);
            Assert.AreEqual("111111", result);

            result = service.MethodToCache1(11, new MockObject(2222));
            Assert.AreEqual(1, cache1.Hits);
            Assert.AreEqual(2, cache1.Misses);
            Assert.AreEqual("111111", result);

            result = service.MethodToCache1(22, new MockObject(2222));
            Assert.AreEqual(1, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);
            Assert.AreEqual("222222", result);
        }

        [TestMethod]
        public void TestCacheWithGenericKey()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new Service1();
            var result1 = service.MethodToCache2("5");
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(2, cache1.Misses);

            var result2 = service.MethodToCache2(5);
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);

            var result3 = service.MethodToCache2(5.0);
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(6, cache1.Misses);

            var result4 = service.MethodToCache2(5);
            Assert.AreEqual(1, cache1.Hits);
            Assert.AreEqual(6, cache1.Misses);
        }

        [TestMethod]
        public void TestKeyGeneration()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var methodInfo = typeof(Service1).GetMethod("MethodToCache1", new[] { typeof(double) });

            var service = new Service1();
            
            service.MethodToCache1(5.0);
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(2, cache1.Misses);

            service.MethodToCache1(5.0);
            Assert.AreEqual(1, cache1.Hits);
            Assert.AreEqual(2, cache1.Misses);

            var key = CacheKey.GetKey(methodInfo, 5.0);
            cache1.Clear(key);

            service.MethodToCache1(5.0);
            Assert.AreEqual(1, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);

            service.MethodToCache1(5.0);
            Assert.AreEqual(2, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);
        }

        [TestMethod]
        public void TestKeyGenerationLambda()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new Service1();

            service.MethodToCache1(5.0);
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(2, cache1.Misses);

            service.MethodToCache1(5.0);
            Assert.AreEqual(1, cache1.Hits);
            Assert.AreEqual(2, cache1.Misses);

            var key = CacheKey.GetKey((Service1 s) => s.MethodToCache1(5.0));
            cache1.Clear(key);

            service.MethodToCache1(5.0);
            Assert.AreEqual(1, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);

            service.MethodToCache1(5.0);
            Assert.AreEqual(2, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);
        }
    }
}
