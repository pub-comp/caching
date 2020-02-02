using PubComp.Caching.AopCaching;
using PubComp.Caching.Core;
using PubComp.Caching.Core.UnitTests;
using PubComp.Caching.DemoSynchronizedClient;
using PubComp.Caching.RedisCaching.UnitTests.Mocks;
using PubComp.Caching.SystemRuntime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PubComp.Caching.RedisCaching.UnitTests
{
    [TestClass]
    public class RedisCacheTests
    {
        private readonly string connectionName = "localRedis";

        [TestInitialize]
        public void TestInitialize()
        {
            CacheManager.InitializeFromConfig();
            foreach (var cacheName in CacheManager.GetCacheNames())
                try
                {
                    CacheManager.GetCache(cacheName).ClearAll();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to clear cache [{cacheName}] due to: {ex.Message}");
                }
        }

        [TestMethod]
        public void TestRedisCacheObjectMutated()
        {
            var cache = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                    ConnectionName = connectionName,
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

        [TestMethod][Ignore]
        public void TestRedisCacheMassSetDataLoadTest()
        {
            var redisCache = CacheManager.GetCache("redisCache");
            redisCache.ClearAll();
                
            int keycount = 0;
            List<MockCacheItem> list = new List<MockCacheItem>();
            while (keycount++ < 100000)
            {
                list.Add(MockCacheItem.GetNewMockInstance(keycount.ToString()));
            }

            var stopWatch = Stopwatch.StartNew();
            Parallel.ForEach(list, (item =>
            {
                redisCache.Set(item.Key, item);
            }));

            TimeSpan elapsed = stopWatch.Elapsed;
            System.Diagnostics.Debug.WriteLine("TestRedisCacheLoadTest::Finished, Elapsed " + elapsed.Seconds.ToString());

            bool isFast = elapsed.Seconds < 20;//took less then 20 seconds
            Assert.IsTrue(isFast, "Redis SET load test was too slow, took: " + elapsed.Seconds);
        }

        [TestMethod][Ignore]
        public void TestRedisCacheMassGetDataLoadTest()
        {
            var redisCache = CacheManager.GetCache("redisCache");

            int keycount = 0;
            List<string> list = new List<string>();
            while (keycount++ < 100000)
            {
                list.Add(MockCacheItem.GetKey(keycount.ToString()));
            }

            var stopWatch = Stopwatch.StartNew();
            Parallel.ForEach(list, (key =>
            {
                MockCacheItem item;
                redisCache.TryGet(key, out item);
                Assert.IsNotNull(item);
            }));

            TimeSpan elapsed = stopWatch.Elapsed;
            System.Diagnostics.Debug.WriteLine("TestRedisCacheLoadTest::Finished, Elapsed " + elapsed.Seconds.ToString());

            bool isFast = elapsed.Seconds < 20;//took less then 20 seconds
            Assert.IsTrue(isFast, "Redis GET load test was too slow, took: " + elapsed.Seconds);
        }

        [TestMethod]
        public void TestRedisReadConfig()
        {
            var redisCache = CacheManager.GetCache("redisCache");
            Assert.IsInstanceOfType(redisCache, typeof(RedisCache));
            Assert.AreEqual("redisCache", redisCache.Name);
            Assert.AreEqual("127.0.0.1:6379,serviceName=mymaster,allowAdmin=true", GetFieldValue(redisCache));

            var notifier = CacheManager.GetNotifier("redisNotifier");
            Assert.IsInstanceOfType(notifier, typeof(RedisCacheNotifier));
            Assert.AreEqual("redisNotifier", notifier.Name);
            Assert.AreEqual("127.0.0.1:6379,serviceName=mymaster", GetFieldValue(notifier));

            var connectionString = CacheManager.GetConnectionString("localRedis");
            Assert.IsInstanceOfType(connectionString, typeof(RedisConnectionString));
            Assert.AreEqual("127.0.0.1:6379,serviceName=mymaster", connectionString.ConnectionString);

            var connectionStringAdmin = CacheManager.GetConnectionString("localRedisAdmin");
            Assert.IsInstanceOfType(connectionStringAdmin, typeof(B64EncConnectionString));
            Assert.AreEqual("127.0.0.1:6379,serviceName=mymaster,allowAdmin=true", connectionStringAdmin.ConnectionString);

            var localCache = CacheManager.GetCache("localCache");
            Assert.IsInstanceOfType(localCache, typeof(InMemoryCache));
            Assert.AreEqual("localCache", localCache.Name);
            Assert.AreEqual("redisNotifier", GetFieldValue(localCache, "notiferName"));
        }

        private static string GetFieldValue(object obj, string fieldName = "connectionString")
        {
            return GetFieldValue(obj.GetType(), obj, fieldName);
        }

        private static string GetFieldValue(Type type, object obj, string fieldName)
        {
            var field = type.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field == null && type != typeof(object))
                return GetFieldValue(type.BaseType, obj, fieldName);

            Assert.IsNotNull(field);
            Assert.AreEqual(typeof(string), field.FieldType);

            return field.GetValue(obj) as string;
        }

        [TestMethod]
        public void TestRedisCacheJson()
        {
            var cache1 = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                    ConnectionName = connectionName,
                    Converter = "json",
                });

            cache1.ClearAll();

            var expected = new List<object> { false, 2L, 3.0, "four" };

            cache1.Set("key1", expected);
            var result = cache1.Get("key1", () => (List<object>)null);

            CollectionAssert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestRedisCacheBson()
        {
            var cache1 = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                    ConnectionName = connectionName,
                    Converter = "bson",
                });

            cache1.ClearAll();

            var expected = new List<object> { false, 2L, 3.0, "four" };

            cache1.Set("key1Bson", expected);
            var result = cache1.Get("key1Bson", () => (List<object>)null);

            CollectionAssert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestRedisCacheDeflate()
        {
            var cache1 = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                    ConnectionName = connectionName,
                    Converter = "deflate",
                });

            cache1.ClearAll();

            var expected = new List<object> { false, 2L, 3.0, "four" };

            cache1.Set("key1Deflate", expected);
            var result = cache1.Get("key1Deflate", () => (List<object>)null);

            CollectionAssert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestRedisCacheGzip()
        {
            var cache1 = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                    ConnectionName = connectionName,
                    Converter = "gzip",
                });

            cache1.ClearAll();

            var expected = new List<object> { false, 2L, 3.0, "four" };

            cache1.Set("key1Gzip", expected);
            var result = cache1.Get("key1Gzip", () => (List<object>)null);

            CollectionAssert.AreEqual(expected, result);
        }

        [TestMethod]
        public void TestRedisCacheWithAOP_Int32()
        {
            var redisCacheForAop = new RedisCache(
                "redisCache",
                new RedisCachePolicy
                {
                    ConnectionName = connectionName,
                });

            redisCacheForAop.ClearAll();

            var expected = GetInt32Value(5);
            var fromCache = GetInt32Value(5);

            Assert.AreEqual(expected, fromCache);
        }

        [TestMethod]
        public void TestRedisCacheWithAOP_Int64()
        {
            var redisCacheForAop = new RedisCache(
                "redisCache",
                new RedisCachePolicy
                {
                    ConnectionName = connectionName,
                });

            redisCacheForAop.ClearAll();

            var expected = GetInt64Value(5);
            var fromCache = GetInt64Value(5);

            Assert.AreEqual(expected, fromCache);
        }

        [TestMethod]
        public void TestRedisCacheWithAOP_Enum()
        {
            var redisCacheForAop = new RedisCache(
                "redisCache",
                new RedisCachePolicy
                {
                    ConnectionName = connectionName,
                });

            redisCacheForAop.ClearAll();

            var expected = GetEnumValue(5);
            var fromCache = GetEnumValue(5);

            Assert.AreEqual(expected, fromCache);
        }

        [TestMethod]
        public void TestRedisCacheWithAOP_Struct()
        {
            var redisCacheForAop = new RedisCache(
                "redisCache",
                new RedisCachePolicy
                {
                    ConnectionName = connectionName,
                });
                
            redisCacheForAop.ClearAll();

            var expected = GeStructValue(5);
            var fromCache = GeStructValue(5);

            Assert.AreEqual(expected, fromCache);
        }


        #region Methods with AOP redis caching

        [Cache("redisCache")]
        public int GetInt32Value(int value)
        {
            return value;
        }

        [Cache("redisCache")]
        public long GetInt64Value(int value)
        {
            return value;
        }

        [Cache("redisCache")]
        public MyEnum GetEnumValue(int value)
        {
            return (MyEnum)value;
        }

        [Cache("redisCache")]
        public MyStruct GeStructValue(int value)
        {
            return value;
        }

        public enum MyEnum : long
        {
            A, B, C, D
        }

        public struct MyStruct
        {
            public long Value;

            public MyStruct(long value)
            {
                Value = value;
            }

            public static implicit operator long(MyStruct x)
            {
                return x.Value;
            }

            public static implicit operator MyStruct(long x)
            {
                return new MyStruct(x);
            }

            public override string ToString()
            {
                return Value.ToString();
            }
        }

        #endregion
    }
}
