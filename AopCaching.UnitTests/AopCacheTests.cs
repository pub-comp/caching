using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core;
using PubComp.Caching.AopCaching.UnitTests.Mocks;

namespace PubComp.Caching.AopCaching.UnitTests
{
    [TestClass]
    public class AopCacheTests
    {
        private static MockCache cache1;
        private static MockCache cache2;
        private static MockCache cache3;

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            // The following caches are set in the Assembly Initialize

            const string cache1Name = "PubComp.Caching.AopCaching.UnitTests.Mocks.Service*";
            cache1 = CacheManager.GetCache(cache1Name) as MockCache;

            const string cache2Name = "localCache";
            cache2 = CacheManager.GetCache(cache2Name) as MockCache;

            const string cache3Name = "PubComp.Caching.AopCaching.UnitTests.Mocks.Generic*";
            cache3 = CacheManager.GetCache(cache3Name) as MockCache;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            cache1.ClearAll();
            cache2.ClearAll();
            cache3.ClearAll();
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
        public async Task TestNamedCache1Async()
        {
            Assert.AreEqual(0, cache2.Hits);
            Assert.AreEqual(0, cache2.Misses);

            var service = new Service2();
            var result = await service.MethodToCache1Async().ConfigureAwait(false);

            Assert.AreEqual(0, cache2.Hits);
            Assert.AreEqual(2, cache2.Misses);
        }

        [TestMethod]
        public void TestNamedCache2()
        {
            var service = new Service2();
            var result = service.MethodToCache1(2);

           CollectionAssert.AreEqual(new[] { "1", "2" }, result.ToArray());
           CollectionAssert.AreEqual(new[] { "1", "2" }, result.ToArray());
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
           CollectionAssert.AreEqual(new[] { "0.9", "1.9" }, result.ToArray());

            result = service.MethodToCache1(2.0);
            Assert.AreEqual(1, cache2.Hits);
            Assert.AreEqual(2, cache2.Misses);
           CollectionAssert.AreEqual(new[] { "0.9", "1.9" }, result.ToArray());

            result = service.MethodToCache1(2);
            Assert.AreEqual(1, cache2.Hits);
            Assert.AreEqual(4, cache2.Misses);
           CollectionAssert.AreEqual(new[] { "1", "2" }, result.ToArray());

            result = service.MethodToCache1(2);
            Assert.AreEqual(2, cache2.Hits);
            Assert.AreEqual(4, cache2.Misses);
           CollectionAssert.AreEqual(new[] { "1", "2" }, result.ToArray());
        }

        [TestMethod]
        public void TestDoNotIncludeInCacheKeyParameter()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new Service1();
            string result;

            result = service.MethodToCache1(11, new MockObject { Data = 1111 });
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(2, cache1.Misses);
            Assert.AreEqual("111111", result);

            result = service.MethodToCache1(11, new MockObject { Data = 2222 });
            Assert.AreEqual(1, cache1.Hits);
            Assert.AreEqual(2, cache1.Misses);
            Assert.AreEqual("111111", result);

            result = service.MethodToCache1(22, new MockObject { Data = 2222 });
            Assert.AreEqual(1, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);
            Assert.AreEqual("222222", result);

            result = service.MethodToCache1(11, new MockObject { Data = 2222 });
            Assert.AreEqual(2, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);
            Assert.AreEqual("111111", result);
        }

        [TestMethod]
        public async Task TestDoNotIncludeInCacheKeyParameterAsync()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new Service1();
            string result;

            result = await service.MethodToCache1Async(11, new MockObject { Data = 1111 }).ConfigureAwait(false);
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(2, cache1.Misses);
            Assert.AreEqual("111111", result);

            result = await service.MethodToCache1Async(11, new MockObject { Data = 2222 }).ConfigureAwait(false);
            Assert.AreEqual(1, cache1.Hits);
            Assert.AreEqual(2, cache1.Misses);
            Assert.AreEqual("111111", result);

            result = await service.MethodToCache1Async(22, new MockObject { Data = 2222 }).ConfigureAwait(false);
            Assert.AreEqual(1, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);
            Assert.AreEqual("222222", result);

            result = await service.MethodToCache1Async(11, new MockObject { Data = 2222 }).ConfigureAwait(false);
            Assert.AreEqual(2, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);
            Assert.AreEqual("111111", result);
        }

        [TestMethod]
        public void TestDoNotIncludeInCacheKeyProperty()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new Service1();
            string result;

            result = service.MethodToCache1(new ClassA(11, "1111"));
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(2, cache1.Misses);
            Assert.AreEqual("111111", result);

            result = service.MethodToCache1(new ClassA(11, "2222"));
            Assert.AreEqual(1, cache1.Hits);
            Assert.AreEqual(2, cache1.Misses);
            Assert.AreEqual("111111", result);

            result = service.MethodToCache1(new ClassA(22, "2222"));
            Assert.AreEqual(1, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);
            Assert.AreEqual("222222", result);

            result = service.MethodToCache1(new ClassA(11, "2222"));
            Assert.AreEqual(2, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);
            Assert.AreEqual("111111", result);
        }

        [TestMethod]
        public async Task TestDoNotIncludeInCacheKeyPropertyAsync()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new Service1();
            string result;

            result = await service.MethodToCache1Async(new ClassA(11, "1111")).ConfigureAwait(false);
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(2, cache1.Misses);
            Assert.AreEqual("111111", result);

            result = await service.MethodToCache1Async(new ClassA(11, "2222")).ConfigureAwait(false);
            Assert.AreEqual(1, cache1.Hits);
            Assert.AreEqual(2, cache1.Misses);
            Assert.AreEqual("111111", result);

            result = await service.MethodToCache1Async(new ClassA(22, "2222")).ConfigureAwait(false);
            Assert.AreEqual(1, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);
            Assert.AreEqual("222222", result);

            result = await service.MethodToCache1Async(new ClassA(11, "2222")).ConfigureAwait(false);
            Assert.AreEqual(2, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);
            Assert.AreEqual("111111", result);
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
        public void TestCacheWithGenericClass()
        {
            Assert.AreEqual(0, cache3.Hits);
            Assert.AreEqual(0, cache3.Misses);

            var result1 = new GenericService<int>().MethodToCache1("5");
            Assert.AreEqual(0, cache3.Hits);
            Assert.AreEqual(2, cache3.Misses);

            var result2 = new GenericService<int>().MethodToCache1("5");
            Assert.AreEqual(1, cache3.Hits);
            Assert.AreEqual(2, cache3.Misses);

            var result3 = new GenericService<byte>().MethodToCache1("5");
            Assert.AreEqual(1, cache3.Hits);
            Assert.AreEqual(4, cache3.Misses);
        }

        [TestMethod]
        public void TestCacheWithGenericMethodAndWithoutGenericParameter()
        {
            Assert.AreEqual(0, cache3.Hits);
            Assert.AreEqual(0, cache3.Misses);

            var result1 = new GenericService<int>().GenericMethodToCache<Enum1>();
            Assert.AreEqual(0, cache3.Hits);
            Assert.AreEqual(2, cache3.Misses);
            Assert.AreEqual(result1, (int)Enum1.Value);

            var result2 = new GenericService<int>().GenericMethodToCache<Enum1>();
            Assert.AreEqual(1, cache3.Hits);
            Assert.AreEqual(2, cache3.Misses);
            Assert.AreEqual(result2, (int)Enum1.Value);

            var result3 = new GenericService<int>().GenericMethodToCache<Enum2>();
            Assert.AreEqual(1, cache3.Hits);
            Assert.AreEqual(4, cache3.Misses);
            Assert.AreEqual(result3, (int)Enum2.Value);
        }

        [TestMethod]
        public void TestCacheWithGenericMethodAndWithoutGenericReturnValue()
        {
            Assert.AreEqual(0, cache3.Hits);
            Assert.AreEqual(0, cache3.Misses);

            new GenericService1<Enum1>().GenericMethodToCacheWithGenericReturnType((int) Enum1.Value);
            Assert.AreEqual(0, cache3.Hits);
            Assert.AreEqual(2, cache3.Misses);

            new GenericService1<Enum1>().GenericMethodToCacheWithGenericReturnType((int)Enum1.Value);
            Assert.AreEqual(1, cache3.Hits);
            Assert.AreEqual(2, cache3.Misses);

            new GenericService1<Enum2>().GenericMethodToCacheWithGenericReturnType((int)Enum1.Value);
            Assert.AreEqual(1, cache3.Hits);
            Assert.AreEqual(4, cache3.Misses);

            new GenericService1<Enum2>().GenericMethodToCacheWithGenericReturnType((int)Enum2.Value);
            Assert.AreEqual(1, cache3.Hits);
            Assert.AreEqual(6, cache3.Misses);
        }

        [TestMethod]
        public async Task TestCacheWithGenericAsyncClass()
        {
            Assert.AreEqual(0, cache3.Hits);
            Assert.AreEqual(0, cache3.Misses);

            var result1 = await new GenericService<int>().MethodToCache1Async("5").ConfigureAwait(false);
            Assert.AreEqual(0, cache3.Hits);
            Assert.AreEqual(2, cache3.Misses);

            var result2 = await new GenericService<int>().MethodToCache1Async("5").ConfigureAwait(false);
            Assert.AreEqual(1, cache3.Hits);
            Assert.AreEqual(2, cache3.Misses);

            var result3 = await new GenericService<byte>().MethodToCache1Async("5").ConfigureAwait(false);
            Assert.AreEqual(1, cache3.Hits);
            Assert.AreEqual(4, cache3.Misses);
        }

        [TestMethod]
        public void TestCacheWithGenericClassAndGenericKey()
        {
            Assert.AreEqual(0, cache3.Hits);
            Assert.AreEqual(0, cache3.Misses);

            var result1 = new GenericService<int>().MethodToCache2<string>("5");
            Assert.AreEqual(0, cache3.Hits);
            Assert.AreEqual(2, cache3.Misses);

            var result2 = new GenericService<int>().MethodToCache2<string>("5");
            Assert.AreEqual(1, cache3.Hits);
            Assert.AreEqual(2, cache3.Misses);

            var result3 = new GenericService<int>().MethodToCache2<int>(5);
            Assert.AreEqual(1, cache3.Hits);
            Assert.AreEqual(4, cache3.Misses);
        }

        [TestMethod]
        public async Task TestCacheWithGenericClassAndGenericKeyAsync()
        {
            Assert.AreEqual(0, cache3.Hits);
            Assert.AreEqual(0, cache3.Misses);

            var result1 = await new GenericService<int>().MethodToCache2Async<string>("5").ConfigureAwait(false);
            Assert.AreEqual(0, cache3.Hits);
            Assert.AreEqual(2, cache3.Misses);

            var result2 = await new GenericService<int>().MethodToCache2Async<string>("5").ConfigureAwait(false);
            Assert.AreEqual(1, cache3.Hits);
            Assert.AreEqual(2, cache3.Misses);

            var result3 = await new GenericService<int>().MethodToCache2Async<int>(5).ConfigureAwait(false);
            Assert.AreEqual(1, cache3.Hits);
            Assert.AreEqual(4, cache3.Misses);
        }

        [TestMethod]
        public void TestKeyGeneration()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var methodInfo = typeof(Service1).GetMethod(nameof(Service1.MethodToCache1), new[] { typeof(double) });

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
        public void TestKeyGenerationGenericMethod()
        {
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(0, cache1.Misses);

            var service = new Service1();

            service.MethodToCache2(5.0);
            Assert.AreEqual(0, cache1.Hits);
            Assert.AreEqual(2, cache1.Misses);

            service.MethodToCache2(5.0);
            Assert.AreEqual(1, cache1.Hits);
            Assert.AreEqual(2, cache1.Misses);

            var key = CacheKey.GetKey(() => service.MethodToCache2<double>(5.0));
            cache1.Clear(key);

            service.MethodToCache2(5.0);
            Assert.AreEqual(1, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);

            service.MethodToCache2(5.0);
            Assert.AreEqual(2, cache1.Hits);
            Assert.AreEqual(4, cache1.Misses);
        }

        [TestMethod]
        public void TestKeyGenerationGenericClass()
        {
            Assert.AreEqual(0, cache3.Hits);
            Assert.AreEqual(0, cache3.Misses);

            var methodInfo = typeof(GenericService<int>).GetMethod(nameof(GenericService<int>.MethodToCache1), new[] { typeof(double) });

            var service = new GenericService<int>();

            service.MethodToCache1(5.0);
            Assert.AreEqual(0, cache3.Hits);
            Assert.AreEqual(2, cache3.Misses);

            service.MethodToCache1(5.0);
            Assert.AreEqual(1, cache3.Hits);
            Assert.AreEqual(2, cache3.Misses);

            var key = CacheKey.GetKey(methodInfo, 5.0);
            cache3.Clear(key);

            service.MethodToCache1(5.0);
            Assert.AreEqual(1, cache3.Hits);
            Assert.AreEqual(4, cache3.Misses);

            service.MethodToCache1(5.0);
            Assert.AreEqual(2, cache3.Hits);
            Assert.AreEqual(4, cache3.Misses);
        }

        [TestMethod]
        public void TestKeyGenerationLambdaFromConst()
        {
            const double doubleVal = 5.0;

            var service = new Service1();
            Action cachedAction = () => service.MethodToCache1(doubleVal);
            Func<string> cacheKeyGetter = () => CacheKey.GetKey((Service1 s) => s.MethodToCache1(doubleVal));

            AssertCacheWithClear(cache1, cachedAction, cacheKeyGetter);
        }

        [TestMethod]
        public void TestKeyGenerationLambdaFromProperty()
        {
            const double doubleVal = 5.0;
            var testObject = new TestObj { DoubleProperty = doubleVal };
            var service = new Service1();
            Action cachedAction = () => service.MethodToCache1(testObject.DoubleProperty);
            Func<string> cacheKeyGetter = () => CacheKey.GetKey((Service1 s) => s.MethodToCache1(testObject.DoubleProperty));

            AssertCacheWithClear(cache1, cachedAction, cacheKeyGetter);
        }

        [TestMethod]
        public void TestKeyGenerationLambdaFromField()
        {
            const double doubleVal = 5.0;
            var testObject = new TestObj { DoubleField = doubleVal };
            var service = new Service1();
            Action cachedAction = () => service.MethodToCache1(testObject.DoubleField);
            Func<string> cacheKeyGetter = () => CacheKey.GetKey((Service1 s) => s.MethodToCache1(testObject.DoubleField));

            AssertCacheWithClear(cache1, cachedAction, cacheKeyGetter);
        }

        [TestMethod]
        public void TestKeyGenerationLambdaFromStaticProperty()
        {
            const double doubleVal = 5.0;
            TestObj.StaticDoubleProperty = doubleVal;
            var service = new Service1();
            Action cachedAction = () => service.MethodToCache1(TestObj.StaticDoubleProperty);
            Func<string> cacheKeyGetter = () => CacheKey.GetKey((Service1 s) => s.MethodToCache1(TestObj.StaticDoubleProperty));

            AssertCacheWithClear(cache1, cachedAction, cacheKeyGetter);
        }

        [TestMethod]
        public void TestKeyGenerationLambdaFromStaticField()
        {
            const double doubleVal = 5.0;
            TestObj.StaticDoubleField = doubleVal;
            var service = new Service1();
            Action cachedAction = () => service.MethodToCache1(TestObj.StaticDoubleField);
            Func<string> cacheKeyGetter = () => CacheKey.GetKey((Service1 s) => s.MethodToCache1(TestObj.StaticDoubleField));

            AssertCacheWithClear(cache1, cachedAction, cacheKeyGetter);
        }

        [TestMethod]
        public void TestKeyGenerationLambdaFromInnerProperty()
        {
            const double doubleVal = 5.0;
            var testObject = new TestObj { TestObjProperty = new TestObj { DoubleProperty = doubleVal } };
            var service = new Service1();
            Action cachedAction = () => service.MethodToCache1(testObject.TestObjProperty.DoubleProperty);
            Func<string> cacheKeyGetter = () => CacheKey.GetKey((Service1 s) => s.MethodToCache1(testObject.TestObjProperty.DoubleProperty));

            AssertCacheWithClear(cache1, cachedAction, cacheKeyGetter);
        }

        private void AssertCacheWithClear(MockCache cache, Action cachedAction, Func<string> cacheKeyGetter)
        {
            Assert.AreEqual(0, cache.Hits);
            Assert.AreEqual(0, cache.Misses);

            cachedAction();
            Assert.AreEqual(0, cache.Hits);
            Assert.AreEqual(2, cache.Misses);

            cachedAction();
            Assert.AreEqual(1, cache.Hits);
            Assert.AreEqual(2, cache.Misses);

            var key = cacheKeyGetter();
            cache1.Clear(key);

            cachedAction();
            Assert.AreEqual(1, cache.Hits);
            Assert.AreEqual(4, cache.Misses);

            cachedAction();
            Assert.AreEqual(2, cache.Hits);
            Assert.AreEqual(4, cache.Misses);
        }

        private class TestObj
        {
            public double DoubleProperty { get; set; }
            public double DoubleField;
            public static double StaticDoubleProperty { get; set; }
            public static double StaticDoubleField;

            public TestObj TestObjProperty { get; set; }
        }
    }
}
