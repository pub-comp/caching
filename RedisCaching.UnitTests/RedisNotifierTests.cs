﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core;
using PubComp.Caching.DemoSynchronizedClient;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace PubComp.Caching.RedisCaching.UnitTests
{
    [TestClass]
    public class RedisNotifierTests
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
        public void TestRedisNotifierFallback()
        {
            const string localCacheWithNotifierAndFallback = "MyApp.LocalCacheWithNotifierAndFallback";
            const string localCacheWithNotifierAndFallbackInvalidConnection = "MyApp.LocalCacheWithNotifierAndFallbackInvalidConnection";

            var cache = CacheManager.GetCache(localCacheWithNotifierAndFallback);
            var cacheInvalidConnection = CacheManager.GetCache(localCacheWithNotifierAndFallbackInvalidConnection);

            cache.Get("key1", () => "value1");
            cacheInvalidConnection.Get("key2", () => "value2");

            // key1, key2 should be in cache1, cache2
            Assert.IsTrue(cache.TryGet("key1", out string value1));
            Assert.IsTrue(cacheInvalidConnection.TryGet("key2", out value1));

            Thread.Sleep(4000);

            Assert.IsTrue(cache.TryGet("key1", out value1));
            Assert.IsFalse(cacheInvalidConnection.TryGet("key2", out value1));
        }

        [TestMethod]
        public void TestRedisNotifierFallbackInvalidationOnConnectionFailure()
        {
            const string localCacheWithNotifierAndFallback = "MyApp.LocalCacheWithNotifierAndFallback";

            var cache = CacheManager.GetCache(localCacheWithNotifierAndFallback);
            cache.Get("key1", () => "value1");

            Assert.IsTrue(cache.TryGet("key1", out string value1));

            FakeRedisClientsNewState(isConnected: false);

            // OnRedisConnectionStateChanged should invalidate cache items
            Assert.IsFalse(cache.TryGet("key1", out value1));

            FakeRedisClientsNewState(isConnected: true);
        }

        [TestMethod]
        public void TestRedisNotifierFallbackWithoutInvalidationOnConnectionFailure()
        {
            const string localCacheWithNotifierAndFallback = "MyApp.LocalCacheWithNotifierAndFallback.WithoutInvalidation";

            var cache = CacheManager.GetCache(localCacheWithNotifierAndFallback);
            cache.Get("key1", () => "value1");

            Assert.IsTrue(cache.TryGet("key1", out string value1));

            FakeRedisClientsNewState(isConnected: false);

            // OnRedisConnectionStateChanged should invalidate cache items
            Assert.IsTrue(cache.TryGet("key1", out value1));

            FakeRedisClientsNewState(isConnected: true);
        }

        [TestMethod]
        public void TestRedisNotifierFallbackInvalidationOnConnectionResumed()
        {
            const string localCacheWithNotifierAndFallback = "MyApp.LocalCacheWithNotifierAndFallback";

            var cache = CacheManager.GetCache(localCacheWithNotifierAndFallback);

            FakeRedisClientsNewState(isConnected: false);
            FakeRedisClientsNewState(isConnected: true);

            cache.Get("key1", () => "value1");
            Assert.IsTrue(cache.TryGet<string>("key1", out _));

            Thread.Sleep(4000);
            Assert.IsTrue(cache.TryGet<string>("key1", out _));
        }

        [TestMethod]
        public void TestRedisNotifierFallbackExpirationOnConnectionFailure()
        {
            const string localCacheWithNotifierAndFallback = "MyApp.LocalCacheWithNotifierAndFallback";

            var cache = CacheManager.GetCache(localCacheWithNotifierAndFallback);

            FakeRedisClientsNewState(isConnected: false);

            cache.Get("key1", () => "value1");
            Assert.IsTrue(cache.TryGet("key1", out string value1));

            Thread.Sleep(4000);

            // Fallback expiry policy should be 3s
            Assert.IsFalse(cache.TryGet("key1", out value1));

            FakeRedisClientsNewState(isConnected: true);
        }

        [TestMethod]
        public void TestRedisNotifierFallbackExpirationOnConnectionResumed()
        {
            const string localCacheWithNotifierAndFallback = "MyApp.LocalCacheWithNotifierAndFallback";

            var cache = CacheManager.GetCache(localCacheWithNotifierAndFallback);

            FakeRedisClientsNewState(isConnected: false);
            FakeRedisClientsNewState(isConnected: true);

            cache.Get("key1", () => "value1");
            Assert.IsTrue(cache.TryGet("key1", out string value1));

            Thread.Sleep(4000);

            // Default expiry policy should be 10m
            Assert.IsTrue(cache.TryGet("key1", out value1));
        }

        private void FakeRedisClientsNewState(bool isConnected)
        {
            foreach (var client in RedisClient.ActiveRedisClients.Values.Where(x => x.IsValueCreated && x.Value.IsConnected))
            {
                var providerStateChangedEventArgs = new Core.Events.ProviderStateChangedEventArgs(isConnected);
                Raise(client.Value, nameof(client.Value.OnRedisConnectionStateChanged), providerStateChangedEventArgs);
            }
        }

        internal static void Raise<TEventArgs>(object source, string eventName, TEventArgs eventArgs) where TEventArgs : EventArgs
        {
            var eventDelegate = (MulticastDelegate)source.GetType().GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(source);
            if (eventDelegate != null)
            {
                foreach (var handler in eventDelegate.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, new object[] { source, eventArgs });
                }
            }
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
    }
}
