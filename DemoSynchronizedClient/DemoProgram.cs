using System;
using System.Linq;
using System.Threading;
using NLog;
using PubComp.Caching.Core;
using PubComp.Caching.Core.Notifications;
using StackExchange.Redis;

namespace PubComp.Caching.DemoSynchronizedClient
{
    public class DemoProgram
    {
        const string NotifierName = "redisNotifier";
        const string LocalCacheWithNotifier = "MyApp.LocalCacheWithNotifier";

        private static readonly string connectionString = @"127.0.0.1:6379,serviceName=mymaster";
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

            if (args.Any(a => a.ToLowerInvariant() == "general-invalidation"))
            {
                using (var connection = ConnectionMultiplexer.Connect(connectionString))
                    connection.GetSubscriber().Publish("+general-invalidation", "test");
            }

            // Otherwise clear all

            if (!args.Any())
            {
                Console.WriteLine("Clearing entire cache.");
                ClearCache(cache, null);
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
