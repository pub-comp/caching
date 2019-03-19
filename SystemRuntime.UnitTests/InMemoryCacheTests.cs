using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core.UnitTests;

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
        public async Task TestInMemoryCacheObjectAsync()
        {
            var cache = new InMemoryCache("cache1", new TimeSpan(0, 2, 0));

            int hits = 0;

            Func<Task<string>> getter = async () => { hits++; return hits.ToString(); };

            string result;

            result = await cache.GetAsync("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual("1", result);

            result = await cache.GetAsync("key", getter);
            Assert.AreEqual(1, hits);
            Assert.AreEqual("1", result);
        }

        [TestMethod]
        public void TestInMemoryCacheNull()
        {
            var cache = new InMemoryCache("cache1", new TimeSpan(0, 2, 0));

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
        public void TestInMemoryCacheObjectMutated()
        {
            var cache = new InMemoryCache("cache1", new TimeSpan(0, 2, 0));

            List<string> value = new List<string> { "1" };

            Func<IEnumerable<object>> getter = () => { return value; };

            IEnumerable<object> result;

            result = cache.Get("key", getter);
            CollectionAssert.AreEqual(new object[] { "1" }, result.ToArray());

            value.Add("2");

            result = cache.Get("key", getter);
            CollectionAssert.AreEqual(new object[] { "1", "2" }, result.ToArray());
        }

        [TestMethod]
        public void TestInMemoryCacheTimeToLive_FromInsert()
        {
            var ttl = 3;
            int misses = 0;
            string result;
            var stopwatch = new Stopwatch();
            Func<string> getter = () => { misses++; return misses.ToString(); };

            var cache = new InMemoryCache(
                "insert-expire-cache",
                new InMemoryPolicy
                {
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

            CacheTestTools.AssertValueDoesntChangeWithin(cache, "key", "1", getter, stopwatch, ttl);

            // Should expire within TTL+60sec from insert
            CacheTestTools.AssertValueDoesChangeWithin(cache, "key", "1", getter, stopwatch, 60.1);

            result = cache.Get("key", getter);
            Assert.AreNotEqual(1, misses);
            Assert.AreNotEqual("1", result);
        }

        [TestMethod]
        public void TestInMemoryCacheTimeToLive_Sliding()
        {
            var ttl = 3;
            int misses = 0;
            string result;
            var stopwatch = new Stopwatch();
            Func<string> getter = () => { misses++; return misses.ToString(); };

            var cache = new InMemoryCache(
                "sliding-expire-cache",
                new InMemoryPolicy
                {
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

            CacheTestTools.AssertValueDoesntChangeWithin(cache, "key", "1", getter, stopwatch, ttl + 60);

            // Should expire within TTL+60sec from last access
            CacheTestTools.AssertValueDoesChangeAfter(cache, "key", "1", getter, stopwatch, ttl + 60.1);

            result = cache.Get("key", getter);
            Assert.AreNotEqual(1, misses);
            Assert.AreNotEqual("1", result);
        }

        [TestMethod]
        public void TestInMemoryCacheTimeToLive_Constant()
        {
            var ttl = 3;
            int misses = 0;
            string result;
            var stopwatch = new Stopwatch();
            Func<string> getter = () => { misses++; return misses.ToString(); };

            var expireAt = DateTime.Now.AddSeconds(ttl);
            stopwatch.Start();

            var cache = new InMemoryCache(
                "constant-expire",
                new InMemoryPolicy
                {
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

            CacheTestTools.AssertValueDoesntChangeWithin(cache, "key", "1", getter, stopwatch, ttl);

            // Should expire within TTL+60sec from insert
            CacheTestTools.AssertValueDoesChangeWithin(cache, "key", "1", getter, stopwatch, 60.1);

            result = cache.Get("key", getter);
            Assert.AreNotEqual(1, misses);
            Assert.AreNotEqual("1", result);
        }

        [TestMethod]
        public void TestInMemoryCacheGetTwice()
        {
            var cache = new InMemoryCache("cache1", new InMemoryPolicy());
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
        public void TestInMemoryCacheSetTwice()
        {
            var cache = new InMemoryCache("cache1", new InMemoryPolicy());
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

        [TestMethod]
        public void Get_NestedGetWithoutLock_2Hits()
        {
            //Arrange
            var cache = new InMemoryCache("cache1",
                new InMemoryPolicy {SlidingExpiration = new TimeSpan(0, 2, 0), DoNotLock = true, NumberOfLocks = 1});
            int hits = 0;

            int Get()
            {
                cache.Get("key2", () => 0);
                hits++;
                return hits;
            }

            //Act
            var result1 = cache.Get("key", (Func<int>)Get);
            var hits1 = hits;

            var result2 = cache.Get("key", (Func<int>)Get);
            var hits2 = hits;

            //Assert
            Assert.AreEqual(1, hits1);
            Assert.AreEqual(1, result1);

            Assert.AreEqual(1, hits2);
            Assert.AreEqual(1, result2);
        }

        [TestMethod]
        public void Get_NestedGetWithLock_Deadlock()
        {
            //Arrange
            var cache = new InMemoryCache("cache1",
                new InMemoryPolicy {SlidingExpiration = new TimeSpan(0, 2, 0), DoNotLock = false, NumberOfLocks = 1});
            int hits = 0;

            int Get()
            {
                cache.Get("key2", () => 0);
                hits++;
                return hits;
            }

            var tokenSource = new CancellationTokenSource();

            //Act
            var t = Task.Factory.StartNew(() => cache.Get("key", Get), tokenSource.Token);
            var finished = t.Wait(TimeSpan.FromSeconds(2));
            
            //Assert
            Assert.AreEqual(false, finished);
            Assert.AreEqual(0, hits);
            tokenSource.Cancel();
        }

        [TestMethod]
        public void LoadTest_LockingStrategiesComparison()
        {
            const int numberOfThreads = 16;
            const int numberOfIterations = 1000; // With less iterations times vary too much

            var keys = new[] { "key1", "key2222", "key333333", "key4444444", "keyV", "vi", "7777777", "ate", "IX", "/o" };

            var rand = new Random();

            var threadKeys = Enumerable.Range(0, numberOfThreads)
                .Select(threadNumber => keys[rand.Next(keys.Length)])
                .ToList();

            var noLockTime = LoadTest(null, numberOfIterations, threadKeys);
            var oneLockTime = LoadTest(1, numberOfIterations, threadKeys);
            var onehundredLocksTime = LoadTest(100, numberOfIterations, threadKeys);

            Console.WriteLine($"{nameof(noLockTime)} = {noLockTime}");
            Console.WriteLine($"{nameof(oneLockTime)} = {oneLockTime}");
            Console.WriteLine($"{nameof(onehundredLocksTime)} = {onehundredLocksTime}");

            Assert.IsTrue(noLockTime < oneLockTime,
                $"a. {nameof(noLockTime)} < {nameof(oneLockTime)}");
            Assert.IsTrue(onehundredLocksTime < oneLockTime,
                $"b. {nameof(onehundredLocksTime)} < {nameof(oneLockTime)}");

            // Should be almost the same, using 1.5 in case of different HW changing stats
            Assert.IsTrue(onehundredLocksTime < noLockTime * 1.5,
                $"c. {nameof(onehundredLocksTime)} < {nameof(noLockTime)} * 1.5");

            // Should be more than 1/3 * difference, using 1/2 in case of different HW changing stats
            Assert.IsTrue(noLockTime < oneLockTime * 0.5,
                $"d. {nameof(noLockTime)} < {nameof(oneLockTime)} * 0.5");
            Assert.IsTrue(onehundredLocksTime < oneLockTime * 0.5,
                $"e. {nameof(onehundredLocksTime)} < {nameof(oneLockTime)} * 0.5");
        }

        private double LoadTest(ushort? numberOfLocks, int numberOfIterations, List<string> threadKeys)
        {
            double time = 0.0;
            for (int cnt = 0; cnt < numberOfIterations; cnt++)
            {
                time += LoadTest(numberOfLocks, threadKeys);
            }

            return time;
        }

        private double LoadTest(ushort? numberOfLocks, List<string> threadKeys)
        {
            var cache = new InMemoryCache("cache1",
                new InMemoryPolicy
                {
                    SlidingExpiration = new TimeSpan(0, 2, 0),
                    DoNotLock = !numberOfLocks.HasValue,
                    NumberOfLocks = numberOfLocks
                });

            Action<string> action = (string key) =>
            {
                cache.Get(key, () =>
                {
                    Task.Delay(10).Wait();
                    return $"***{key}***";
                });
            };

            var tasks = threadKeys.Select(k => new Task(() => action(k))).ToArray();

            double elapsedTime;
            var sw = new Stopwatch();
            sw.Start();

            try
            {
                foreach (var task in tasks) task.Start();
                Task.WaitAll(tasks);
            }
            finally
            {
                elapsedTime = sw.Elapsed.TotalMilliseconds;
                sw.Stop();
            }

            return elapsedTime;
        }
    }
}
