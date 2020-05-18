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
        public void TestPolicyDeserialization()
        {
            var policyString = @"{'SyncProvider':'RedisNotifier', 'SlidingExpiration':'1:02:03:04.567', 'DoNotLock':true, 'NumberOfLocks':10, 'LockTimeoutMilliseconds':10000, 'DoThrowExceptionOnTimeout':false, 'OnSyncProviderFailure': {'InvalidateOnProviderStateChange':false, 'SlidingExpiration':'1:02:03:04.567'} }";
            var policy = Newtonsoft.Json.JsonConvert.DeserializeObject<InMemoryPolicy>(policyString);

            Assert.IsNotNull(policy);
            Assert.AreEqual("RedisNotifier", policy.SyncProvider);
            Assert.AreEqual(new TimeSpan(1, 2, 3, 4, 567), policy.SlidingExpiration);
            Assert.AreEqual(true, policy.DoNotLock);
            Assert.AreEqual((ushort)10, policy.NumberOfLocks);
            Assert.AreEqual(10000, policy.LockTimeoutMilliseconds);
            Assert.AreEqual(false, policy.DoThrowExceptionOnTimeout);

            Assert.IsNotNull(policy.OnSyncProviderFailure);
            Assert.AreEqual(false, policy.OnSyncProviderFailure.InvalidateOnProviderStateChange);
            Assert.AreEqual(new TimeSpan(1, 2, 3, 4, 567), policy.OnSyncProviderFailure.SlidingExpiration);
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
        public void Get_NestedGetWithoutLock_2Hits()
        {
            //Arrange
            var cache = new InMemoryCache("cache1",
                new InMemoryPolicy { SlidingExpiration = new TimeSpan(0, 2, 0), DoNotLock = true, NumberOfLocks = 1 });
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
        [Ignore] // Now reentrant locking is supported
        public void Get_NestedGetWithLock_Deadlock()
        {
            //Arrange
            var cache = new InMemoryCache("cache1",
                new InMemoryPolicy { SlidingExpiration = new TimeSpan(0, 2, 0), DoNotLock = false, NumberOfLocks = 1 });
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
            var tenLocksTime = LoadTest(10, numberOfIterations, threadKeys);
            var onehundredLocksTime = LoadTest(100, numberOfIterations, threadKeys);
            var oneThousandLocksTime = LoadTest(1000, numberOfIterations, threadKeys);

            Console.WriteLine($"{nameof(noLockTime)} = {noLockTime}");
            Console.WriteLine($"{nameof(oneLockTime)} = {oneLockTime}");
            Console.WriteLine($"{nameof(tenLocksTime)} = {tenLocksTime}");
            Console.WriteLine($"{nameof(onehundredLocksTime)} = {onehundredLocksTime}");
            Console.WriteLine($"{nameof(oneThousandLocksTime)} = {oneThousandLocksTime}");

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

        [TestMethod]
        public void Policy_OnSyncProviderFailure_WithOnSyncProvider()
        {
            var cache = new InMemoryCache("t1",
                new InMemoryPolicy
                {
                    ExpirationFromAdd = TimeSpan.FromMinutes(2),
                    SyncProvider = "syncProvider",
                    OnSyncProviderFailure = new InMemoryFallbackPolicy
                    {
                        ExpirationFromAdd = TimeSpan.FromMinutes(1)
                    }
                });
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void Policy_OnSyncProviderFailure_WithoutOnSyncProvider()
        {
            var cache = new InMemoryCache("t1",
                new InMemoryPolicy
                {
                    ExpirationFromAdd = TimeSpan.FromMinutes(2),
                    OnSyncProviderFailure = new InMemoryFallbackPolicy
                    {
                        ExpirationFromAdd = TimeSpan.FromMinutes(1)
                    }
                });
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void Policy_OnSyncProviderFailure_WithOnSyncProvider_GreaterExpiryForFallback()
        {
            var cache = new InMemoryCache("t1",
                new InMemoryPolicy
                {
                    ExpirationFromAdd = TimeSpan.FromMinutes(2),
                    SyncProvider = "syncProvider",
                    OnSyncProviderFailure = new InMemoryFallbackPolicy
                    {
                        ExpirationFromAdd = TimeSpan.FromMinutes(4)
                    }
                });
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
