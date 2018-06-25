using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.AopCaching;
using PubComp.Caching.Core;
using PubComp.Caching.Core.UnitTests;
using PubComp.Caching.DemoSynchronizedClient;
using PubComp.Caching.RedisCaching.UnitTests.Mocks;
using PubComp.Caching.SystemRuntime;
using PubComp.Testing.TestingUtils;

namespace PubComp.Caching.RedisCaching.UnitTests
{
    [TestClass]
    public class RedisCacheTests
    {
        private readonly string connectionString = @"127.0.0.1:6379,serviceName=mymaster";
        
        [TestMethod]
        public void TestRedisCacheBasic()
        {
            var cache1 = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                    ConnectionString = connectionString,
                });

            cache1.ClearAll();
            
            int misses1 = 0;
            Func<string> getter1 = () => { misses1++; return misses1.ToString(); };

            int misses2 = 0;
            Func<string> getter2 = () => { misses2++; return misses2.ToString(); };

            string result;

            result = cache1.Get("key1", getter1);
            Assert.AreEqual(1, misses1);
            Assert.AreEqual("1", result);

            result = cache1.Get("key2", getter1);
            Assert.AreEqual(2, misses1);
            Assert.AreEqual("2", result);
            
            cache1.ClearAll();

            result = cache1.Get("key1", getter1);
            Assert.AreEqual(3, misses1);
            Assert.AreEqual("3", result);

            result = cache1.Get("key2", getter1);
            Assert.AreEqual(4, misses1);
            Assert.AreEqual("4", result);
        }

        [TestMethod]
        public void TestRedisCacheTwoCaches()
        {
            var cache1 = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                    ConnectionString = connectionString,
                });
            cache1.ClearAll();


            var cache2 = new RedisCache(
                "cache2",
                new RedisCachePolicy
                {
                    ConnectionString = connectionString,
                });
            cache2.ClearAll();

            int misses1 = 0;
            Func<string> getter1 = () => { misses1++; return misses1.ToString(); };

            int misses2 = 0;
            Func<string> getter2 = () => { misses2++; return misses2.ToString(); };

            string result;

            result = cache1.Get("key1", getter1);
            Assert.AreEqual(1, misses1);
            Assert.AreEqual("1", result);

            result = cache1.Get("key2", getter1);
            Assert.AreEqual(2, misses1);
            Assert.AreEqual("2", result);

            result = cache2.Get("key1", getter2);
            Assert.AreEqual(1, misses2);
            Assert.AreEqual("1", result);

            result = cache2.Get("key2", getter2);
            Assert.AreEqual(2, misses2);
            Assert.AreEqual("2", result);

            cache1.ClearAll();

            result = cache1.Get("key1", getter1);
            Assert.AreEqual(3, misses1);
            Assert.AreEqual("3", result);

            result = cache1.Get("key2", getter1);
            Assert.AreEqual(4, misses1);
            Assert.AreEqual("4", result);

            result = cache2.Get("key1", getter2);
            Assert.AreEqual(2, misses2);
            Assert.AreEqual("1", result);

            result = cache2.Get("key2", getter2);
            Assert.AreEqual(2, misses2);
            Assert.AreEqual("2", result);
        }

        [TestMethod]
        public void TestRedisCacheStruct()
        {
            var cache = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                    ConnectionString = connectionString,
                });
            cache.ClearAll();

            int misses = 0;

            Func<int> getter = () => { misses++; return misses; };

            int result;

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual(1, result);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void TestRedisCacheObject()
        {
            var cache = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                    ConnectionString = connectionString,
                });
            cache.ClearAll();

            int misses = 0;

            Func<string> getter = () => { misses++; return misses.ToString(); };

            string result;

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);
        }

        [TestMethod]
        public void TestRedisCacheNull()
        {
            var cache = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                    ConnectionString = connectionString,
                });
            cache.ClearAll();

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
        public void TestRedisCacheObjectMutated()
        {
            var cache = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                    ConnectionString = connectionString,
                });
            cache.ClearAll();

            List<string> value = new List<string> { "1" };

            Func<IEnumerable<object>> getter = () => { return value; };

            IEnumerable<object> result;

            result = cache.Get("key", getter);
            LinqAssert.AreSame(new object[] { "1" }, result);

            value.Add("2");

            result = cache.Get("key", getter);
            LinqAssert.AreSame(new object[] { "1" }, result);
        }

        [TestMethod]
        public void TestRedisCacheTimeToLive_FromInsert()
        {
            var ttl = 10;
            int misses = 0;
            string result;
            var stopwatch = new Stopwatch();
            Func<string> getter = () => { misses++; return misses.ToString(); };

            var cache = new RedisCache(
                "insert-expire-cache",
                new RedisCachePolicy
                {
                    ConnectionString = connectionString,
                    ExpirationFromAdd = TimeSpan.FromSeconds(ttl),
                });
            cache.ClearAll();

            stopwatch.Start();
            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            CacheTestTools.AssertValueDoesntChangeWithin(cache, "key", "1", getter, stopwatch, ttl - 1);

            // Should expire within TTL+60sec from insert
            CacheTestTools.AssertValueDoesChangeWithin(cache, "key", "1", getter, stopwatch, 60.1);

            result = cache.Get("key", getter);
            Assert.AreNotEqual(1, misses);
            Assert.AreNotEqual("1", result);
        }

        [TestMethod]
        public void TestRedisCacheTimeToLive_Sliding()
        {
            var ttl = 10;
            int misses = 0;
            string result;
            var stopwatch = new Stopwatch();
            Func<string> getter = () => { misses++; return misses.ToString(); };

            var cache = new RedisCache(
                "sliding-expire-cache",
                new RedisCachePolicy
                {
                    ConnectionString = connectionString,
                    SlidingExpiration = TimeSpan.FromSeconds(ttl),
                });
            cache.ClearAll();

            stopwatch.Start();
            result = cache.Get("key", getter);
            DateTime insertTime = DateTime.Now;
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            CacheTestTools.AssertValueDoesntChangeWithin(cache, "key", "1", getter, stopwatch, ttl - 1 + 60);

            // Should expire within TTL+60sec from last access
            CacheTestTools.AssertValueDoesChangeAfter(cache, "key", "1", getter, stopwatch, ttl + 60.1);

            result = cache.Get("key", getter);
            Assert.AreNotEqual(1, misses);
            Assert.AreNotEqual("1", result);
        }

        [TestMethod]
        public void TestRedisCacheTimeToLive_Constant()
        {
            var ttl = 10;
            int misses = 0;
            string result;
            var stopwatch = new Stopwatch();
            Func<string> getter = () => { misses++; return misses.ToString(); };

            var expireAt = DateTime.Now.AddSeconds(ttl);
            stopwatch.Start();

            var cache = new RedisCache(
                "constant-expire",
                new RedisCachePolicy
                {
                    ConnectionString = connectionString,
                    AbsoluteExpiration = expireAt,
                });
            cache.ClearAll();

            result = cache.Get("key", getter);
            DateTime insertTime = DateTime.Now;
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual("1", result);

            CacheTestTools.AssertValueDoesntChangeWithin(cache, "key", "1", getter, stopwatch, ttl - 1);

            // Should expire within TTL+60sec from insert
            CacheTestTools.AssertValueDoesChangeWithin(cache, "key", "1", getter, stopwatch, 60.1);

            result = cache.Get("key", getter);
            Assert.AreNotEqual(1, misses);
            Assert.AreNotEqual("1", result);
        }

        [TestMethod]
        public void TestRedisCacheGetTwice()
        {
            var cache = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                    ConnectionString = connectionString,
                });
            cache.ClearAll();

            int misses = 0;

            Func<string> getter = () => { misses++; return misses.ToString(); };

            string result;

            result = cache.Get("key", getter);
            Assert.AreEqual("1", result);

            result = cache.Get("key", getter);
            Assert.AreEqual("1", result);
        }

        [TestMethod]
        public void TestRedisCacheSetTwice()
        {
            var cache = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                    ConnectionString = connectionString,
                });
            cache.ClearAll();

            int misses = 0;

            Func<string> getter = () => { misses++; return misses.ToString(); };

            string result;
            bool wasFound;

            cache.Set("key", getter());
            wasFound = cache.TryGet("key", out result);
            Assert.AreEqual(true, wasFound);
            Assert.AreEqual("1", result);

            cache.Set("key", getter());
            wasFound = cache.TryGet("key", out result);
            Assert.AreEqual(true, wasFound);
            Assert.AreEqual("2", result);
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

        private static string GetFieldValue(Type type, object obj, string fieldName = "connectionString")
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
        public void TestRedisNotifier()
        {
            const string localCacheWithNotifier = "MyApp.LocalCacheWithNotifier";
            const string localCacheWithNotifier2 = "MyApp.LocalCacheWithNotifier2";

            var cache1 = CacheManager.GetCache(localCacheWithNotifier);
            var cache2 = CacheManager.GetCache(localCacheWithNotifier2);

            // Cache key1, key2 in cache1
            cache1.Get("key1", () => { return "value1"; });
            cache1.Get("key2", () => { return "value2"; });

            // Cache key1, key2 in cache2
            cache2.Get("key1", () => { return "value3"; });
            cache2.Get("key2", () => { return "value4"; });

            // key1, key2 should be in cache1, cache2
            Assert.IsTrue(cache1.TryGet("key1", out string value111));
            Assert.IsTrue(cache1.TryGet("key2", out string value121));
            Assert.IsTrue(cache2.TryGet("key1", out string value211));
            Assert.IsTrue(cache2.TryGet("key2", out string value221));

            var secondProcess = Process.Start(
                new ProcessStartInfo
                {
                    CreateNoWindow = false,
                    FileName = typeof(DemoProgram).Assembly.Location,
                    Arguments = "key1",
                    WindowStyle = ProcessWindowStyle.Normal,
                });
            Assert.IsNotNull(secondProcess);
            secondProcess.WaitForExit();

            // key1 should have been cleared from cache1
            Assert.IsFalse(cache1.TryGet("key1", out string value112));
            Assert.IsTrue(cache1.TryGet("key2", out string value122));
            Assert.IsTrue(cache2.TryGet("key1", out string value212));
            Assert.IsTrue(cache2.TryGet("key2", out string value222));

            // Cache key1 in cache1
            cache1.Get("key1", () => { return "value1"; });

            var secondProcess2 = Process.Start(
                new ProcessStartInfo
                {
                    CreateNoWindow = false,
                    FileName = typeof(DemoProgram).Assembly.Location,
                    WindowStyle = ProcessWindowStyle.Normal,
                });
            Assert.IsNotNull(secondProcess2);
            secondProcess2.WaitForExit();

            // All keys should have been cleared from cache1
            Assert.IsFalse(cache1.TryGet("key1", out string value113));
            Assert.IsFalse(cache1.TryGet("key2", out string value123));
            Assert.IsTrue(cache2.TryGet("key1", out string value213));
            Assert.IsTrue(cache2.TryGet("key2", out string value223));
        }

        [TestMethod]
        public void TestRedisCacheJson()
        {
            var cache1 = new RedisCache(
                "cache1",
                new RedisCachePolicy
                {
                    ConnectionString = connectionString,
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
                    ConnectionString = connectionString,
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
                    ConnectionString = connectionString,
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
                    ConnectionString = connectionString,
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
                    ConnectionString = connectionString,
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
                    ConnectionString = connectionString,
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
                    ConnectionString = connectionString,
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
                    ConnectionString = connectionString,
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
