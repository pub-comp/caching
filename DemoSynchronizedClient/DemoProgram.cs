using NLog;
using PubComp.Caching.Core;
using PubComp.Caching.Core.Notifications;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading;

namespace PubComp.Caching.DemoSynchronizedClient
{
    public class DemoProgram
    {
        const string NotifierName = "reDisNotifier";
        const string LocalCacheWithNotifier = "MyApp.LocalCacheWithNotifier";
        const string LayeredCache = "MyApp.LayeredCache";

        private static object config;

        static void Main(string[] args)
        {
            try
            {
                Run(args);
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(typeof(DemoProgram).FullName).Error(ex);
                throw;
            }
        }

        static void Run(string[] args)
        {
            Console.WriteLine($"{nameof(DemoProgram)} has started...");

            //general-invalidation
            if (args.Any(a => a.ToLowerInvariant() == "general-invalidation"))
            {
                var pattern = string.Join(" ", args.Skip(1));
                Console.WriteLine($"general-invalidation: {pattern}");

                var connectionString = CacheManager.GetConnectionString("localRedis").ConnectionString;
                using (var connection = ConnectionMultiplexer.Connect(connectionString))
                    connection.GetSubscriber().Publish("+general-invalidation", args.Last());
            }
            //invalidate-on-upsert layered
            else if (args.Any(a => a.ToLowerInvariant() == "layered-invalidate-on-upsert"))
            {
                using (CacheDirectives.SetScope(CacheMethod.GetOrSet, DateTimeOffset.UtcNow.AddMinutes(-1)))
                {
                    var layeredCacheNameToTest = string.Join(" ", args.Skip(1));
                    Console.WriteLine($"testing: layered-invalidate-on-upsert");
                    Console.WriteLine($"layeredCacheNameToTest={layeredCacheNameToTest}");

                    var layeredCache = CacheManager.GetCache(LayeredCache);
                    var layeredCacheWithAutomaticInvalidation = CacheManager.GetCache(layeredCacheNameToTest);

                    if (layeredCache.TryGet("keyA2", out string a2) && a2 == "valueA2" &&
                        layeredCacheWithAutomaticInvalidation.TryGet("keyB2", out string b2) && b2 == "valueB2")
                    {
                        layeredCache.Set("keyA2", "demoProgram.valueA2");
                        layeredCacheWithAutomaticInvalidation.Set("keyB2", "demoProgram.valueB2");
                        Console.WriteLine("Completed!");
                    }
                    else
                        Console.WriteLine("invalid!");
                }

                return;
            }
            // general tests
            else
            {
                var cache = CacheManager.GetCache(LocalCacheWithNotifier);

                // Put values in cache
                Console.WriteLine("Caching values for key1 & key2.");
                cache.Get("key1", () => { return "value1"; });
                cache.Get("key2", () => { return "value2"; });

                // Clear keys if passed as parameters

                if (args.Any(a => a.ToLowerInvariant() == "key1"))
                {
                    Console.WriteLine("Clearing key1 from cache.");
                    ClearCache(cache, "key1");
                }

                if (args.Any(a => a.ToLowerInvariant() == "key2"))
                {
                    Console.WriteLine("Clearing key2 from cache.");
                    ClearCache(cache, "key2");
                }

                // Otherwise clear all

                if (!args.Any())
                {
                    Console.WriteLine("Clearing entire cache.");
                    ClearCache(cache, null);
                }
            }

            Console.WriteLine($"{nameof(DemoProgram)} has ended.");

            Thread.Sleep(1000);

            //Console.ReadKey();
        }

        private static void ClearCache(ICache cache, string key)
        {
            if (key != null)
            {
                // Send clear key event
                CacheManager.GetNotifier(NotifierName).Publish(
                    cache.Name, key, CacheItemActionTypes.Removed);

                cache.Clear(key);
            }
            else
            {
                // Send clear all event
                CacheManager.GetNotifier(NotifierName).Publish(
                    cache.Name, null, CacheItemActionTypes.RemoveAll);

                cache.ClearAll();
            }
        }
    }
}
