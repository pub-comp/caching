using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core;
using PubComp.Caching.DemoSynchronizedClient;
using System;
using System.Diagnostics;
using System.Threading;

namespace PubComp.Caching.RedisCaching.UnitTests
{
    [TestClass]
    public class RedisNotifier_GeneralInvalidationChannelTests
    {
        private readonly string connectionName = "localRedis";

        private IDisposable cacheDirectives;

        [TestInitialize]
        public void TestInitialize()
        {
            cacheDirectives = CacheDirectives.SetScope(CacheMethod.GetOrSet, DateTimeOffset.UtcNow);
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

        [TestCleanup]
        public void TestCleanup()
        {
            cacheDirectives?.Dispose();
            cacheDirectives = null;
        }

        [TestMethod]
        public void TestRedisNotifierGeneralInvalidation_Empty()
        {
            const string localCache = "localCache";
            const string layeredCache = "MyApp.LayeredCache";

            var cache = CacheManager.GetCache(localCache);
            cache.Get("key1", () => "value1");

            var cache2 = CacheManager.GetCache(layeredCache);
            cache2.Get("key2", () => "value2");

            // key1 should be in cache1
            Assert.IsTrue(cache.TryGet("key1", out string value1));
            Assert.IsTrue(cache2.TryGet("key2", out string value2));

            var secondProcess = Process.Start(
                new ProcessStartInfo
                {
                    CreateNoWindow = false,
                    FileName = typeof(DemoProgram).Assembly.Location,
                    Arguments = "general-invalidation",
                    WindowStyle = ProcessWindowStyle.Normal,
                });
            Assert.IsNotNull(secondProcess);
            secondProcess.WaitForExit();

            Thread.Sleep(100);
            Assert.IsTrue(cache.TryGet("key1", out value1));
            Assert.IsTrue(cache2.TryGet("key2", out value2));
        }

        [TestMethod]
        public void TestRedisNotifierGeneralInvalidation_Invalid()
        {
            const string localCache = "localCache";
            const string layeredCache = "MyApp.LayeredCache";

            var cache = CacheManager.GetCache(localCache);
            cache.Get("key1", () => "value1");

            var cache2 = CacheManager.GetCache(layeredCache);
            cache2.Get("key2", () => "value2");

            // key1 should be in cache1
            Assert.IsTrue(cache.TryGet("key1", out string value1));
            Assert.IsTrue(cache2.TryGet("key2", out string value2));

            var secondProcess = Process.Start(
                new ProcessStartInfo
                {
                    CreateNoWindow = false,
                    FileName = typeof(DemoProgram).Assembly.Location,
                    Arguments = "general-invalidation **?[(",
                    WindowStyle = ProcessWindowStyle.Normal,
                });
            Assert.IsNotNull(secondProcess);
            secondProcess.WaitForExit();

            Thread.Sleep(100);
            Assert.IsTrue(cache.TryGet("key1", out value1));
            Assert.IsTrue(cache2.TryGet("key2", out value2));
        }

        [TestMethod]
        public void TestRedisNotifierGeneralInvalidation_All()
        {
            const string localCache = "localCache";
            const string layeredCache = "MyApp.LayeredCache";

            var cache = CacheManager.GetCache(localCache);
            cache.Get("key1", () => "value1");

            var cache2 = CacheManager.GetCache(layeredCache);
            cache2.Get("key2", () => "value2");

            // key1 should be in cache1
            Assert.IsTrue(cache.TryGet("key1", out string value1));
            Assert.IsTrue(cache2.TryGet("key2", out string value2));

            var secondProcess = Process.Start(
                new ProcessStartInfo
                {
                    CreateNoWindow = false,
                    FileName = typeof(DemoProgram).Assembly.Location,
                    Arguments = "general-invalidation .*",
                    WindowStyle = ProcessWindowStyle.Normal,
                });
            Assert.IsNotNull(secondProcess);
            secondProcess.WaitForExit();

            Thread.Sleep(100);
            Assert.IsFalse(cache.TryGet("key1", out value1));
            Assert.IsFalse(cache2.TryGet("key2", out value2));
        }

        [TestMethod]
        public void TestRedisNotifierGeneralInvalidation_Pattern()
        {
            const string localCache = "localCache";
            const string layeredCache = "MyApp.LayeredCache";

            var cache = CacheManager.GetCache(localCache);
            cache.Get("key1", () => "value1");

            var cache2 = CacheManager.GetCache(layeredCache);
            cache2.Get("key2", () => "value2");

            // key1 should be in cache1
            Assert.IsTrue(cache.TryGet("key1", out string value1));
            Assert.IsTrue(cache2.TryGet("key2", out string value2));

            var secondProcess = Process.Start(
                new ProcessStartInfo
                {
                    CreateNoWindow = false,
                    FileName = typeof(DemoProgram).Assembly.Location,
                    Arguments = "general-invalidation lOc.*",
                    WindowStyle = ProcessWindowStyle.Normal,
                });
            Assert.IsNotNull(secondProcess);
            secondProcess.WaitForExit();

            Thread.Sleep(100);
            Assert.IsFalse(cache.TryGet("key1", out value1));
            Assert.IsTrue(cache2.TryGet("key2", out value2));
        }
    }
}
