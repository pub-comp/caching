using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core;
using PubComp.Caching.DemoSynchronizedClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PubComp.Caching.RedisCaching.UnitTests
{
    [TestClass]
    public class RedisNotifier_LayeredAutomaticInvalidationTests
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
        [DataRow("MyApp.LayeredCache", "MyApp.LayeredCache.ForAutomaticInvalidationTest")]
        [DataRow("MyApp.LayeredCache", "MYApp.LayeredScopedCache.ForAutomaticInvalidationTest")]
        public async Task TestRedisNotifierLayeredInvalidationOnUpsert(string layeredCache, string layeredCacheWithAutomaticInvalidation)
        {
            var cache = CacheManager.GetCache(layeredCache);
            var cacheWithAutomaticInvalidationOnUpdate = CacheManager.GetCache(layeredCacheWithAutomaticInvalidation);

            cache.ClearAll();
            cacheWithAutomaticInvalidationOnUpdate.ClearAll();

            Assert.AreEqual("valueA1", cache.Get("keyA1", () => "valueA1"));
            Assert.AreEqual("valueA2", cache.Get("keyA2", () => "valueA2"));
            Assert.AreEqual("valueB1", cacheWithAutomaticInvalidationOnUpdate.Get("keyB1", () => "valueB1"));
            Assert.AreEqual("valueB2", cacheWithAutomaticInvalidationOnUpdate.Get("keyB2", () => "valueB2"));

            var secondProcessResult =
                await ExecuteInSecondProcess("layered-invalidate-on-upsert", layeredCacheWithAutomaticInvalidation);

            Assert.AreEqual("valueA1", cache.Get("keyA1", () => "valueA1.g"));
            Assert.AreEqual("valueA2", cache.Get("keyA2", () => "valueA2.g"));
            Assert.AreEqual("valueB1", cacheWithAutomaticInvalidationOnUpdate.Get("keyB1", () => "valueB1.g"));
            Assert.AreEqual("demoProgram.valueB2", cacheWithAutomaticInvalidationOnUpdate.Get("keyB2", () => "valueB2.g"));

            Assert.IsTrue(secondProcessResult.Contains("testing: layered-invalidate-on-upsert"));
            Assert.IsTrue(secondProcessResult.Contains($"layeredCacheNameToTest={layeredCacheWithAutomaticInvalidation}"));
            Assert.IsTrue(secondProcessResult.Contains("Completed!"));
        }

        private static async Task<List<string>> ExecuteInSecondProcess(params string[] arguments)
        {
            var result = new List<string>();

            var processStartInfo = new ProcessStartInfo
            {
                FileName = typeof(DemoProgram).Assembly.Location,
                Arguments = string.Join(" ", arguments),
                WindowStyle = ProcessWindowStyle.Normal,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            var secondProcess = new Process {StartInfo = processStartInfo, EnableRaisingEvents = true};
            secondProcess.OutputDataReceived += (sender, args) => result.Add(args.Data);
            
            secondProcess.Start();

            secondProcess.BeginOutputReadLine();
            secondProcess.WaitForExit();

            var sw = new Stopwatch();
            sw.Start();
            while (!secondProcess.HasExited && sw.ElapsedMilliseconds < 30000)
                await Task.Delay(200);

            return result;
        }
    }
}
