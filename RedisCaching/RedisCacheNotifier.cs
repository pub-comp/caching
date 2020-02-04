using PubComp.Caching.Core;
using PubComp.Caching.Core.Events;
using PubComp.Caching.Core.Notifications;
using PubComp.Caching.RedisCaching.Converters;
using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PubComp.Caching.RedisCaching
{
    public class RedisCacheNotifier : ICacheNotifier, IDisposable
    {
        private readonly string name;
        private readonly RedisCacheNotifierPolicy policy;
        private readonly string connectionString;
        private readonly IRedisConverter convert;
        private readonly string sender;
        private readonly NLog.ILogger log;

        private RedisClient generalInvalidationRedisClient = null;
        private ConcurrentDictionary<string, RedisClient> cacheSubClients;
        private ConcurrentDictionary<string, Func<CacheItemNotification, bool>> cacheCallbacks;

        public RedisCacheNotifier(string name, RedisCacheNotifierPolicy policy)
        {
            this.name = name;
            this.policy = policy;

            this.log = NLog.LogManager.GetLogger(typeof(RedisCacheNotifier).FullName);

            this.cacheSubClients = new ConcurrentDictionary<string, RedisClient>();
            this.cacheCallbacks = new ConcurrentDictionary<string, Func<CacheItemNotification, bool>>();

            if (policy == null)
            {
                log.Error("Invalid Policy for Cache {0}", this.name);
                throw new ArgumentNullException(nameof(policy));
            }

            if (!string.IsNullOrEmpty(policy.ConnectionName))
            {
                this.connectionString = CacheManager.GetConnectionString(policy.ConnectionName)?.ConnectionString;

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new ArgumentException(
                        $"{nameof(ICacheConnectionString.ConnectionString)} not found for {nameof(policy.ConnectionName)} {policy.ConnectionName}", $"{nameof(policy)}.{nameof(policy.ConnectionName)}");
                }
            }
            else if (!string.IsNullOrEmpty(policy.ConnectionString))
            {
                this.connectionString = policy.ConnectionString;
            }
            else
            {
                throw new ArgumentException(
                    $"{nameof(policy.ConnectionString)} is undefined", $"{nameof(policy)}.{nameof(policy.ConnectionString)}");
            }

            this.sender = Guid.NewGuid().ToString();
            this.convert = RedisConverterFactory.CreateConverter(policy.Converter);

            SubscribeToGeneralInvalidationMessage(policy.GeneralInvalidationChannel);
        }

        public string Name { get { return this.name; } }

        private RedisClient GetSubClient(string cacheName, Func<CacheItemNotification, bool> cacheUpdatedCallback,
            EventHandler<Core.Events.ProviderStateChangedEventArgs> notifierProviderStateChangedCallback)
        {
            if (cacheUpdatedCallback != null)
                this.cacheCallbacks.AddOrUpdate(cacheName, cacheUpdatedCallback, (k, c) => cacheUpdatedCallback);

            var client = this.cacheSubClients.GetOrAdd(cacheName, cn => CreateClient(notifierProviderStateChangedCallback));
            return client;
        }

        private RedisClient CreateClient(EventHandler<Core.Events.ProviderStateChangedEventArgs> providerStateChangedCallback)
        {
            return RedisClient.GetNamedRedisClient(this.policy.ConnectionName, providerStateChangedCallback);
        }

        public void Subscribe(string cacheName, Func<CacheItemNotification, bool> callback)
        {
            Subscribe(cacheName, callback, null);
        }

        public void SubscribeToGeneralInvalidationMessage(string generalInvalidationChannel)
        {
            if (string.IsNullOrWhiteSpace(generalInvalidationChannel))
                return;

            generalInvalidationRedisClient = CreateClient(null);
            generalInvalidationRedisClient.Subscriber.Subscribe(generalInvalidationChannel, (channel, message) =>
            {
                if (string.IsNullOrEmpty(message))
                {
                    log.Warn("General-Invalidation invoked without a regex pattern (to clear all: .*)");
                    return;
                }
                log.Info($"General-Invalidation has been invoked: '{message}'");

                try
                {
                    var regex = new Regex(message);
                    var cacheNamesToClear = CacheManager.GetCacheNames().Where(cacheName => regex.IsMatch(cacheName));

                    foreach (var cacheName in cacheNamesToClear)
                        CacheManager.GetCache(cacheName).ClearAll();
                }
                catch (Exception ex)
                {
                    log.Error(ex, "General-Invalidation failed !");
                }
            });
        }

        // ReSharper disable once ParameterHidesMember
        public void Subscribe(string cacheName, Func<CacheItemNotification, bool> cacheUpdatedCallback, 
            EventHandler<Core.Events.ProviderStateChangedEventArgs> notifierProviderStateChangedCallback)
        {
            var client = GetSubClient(cacheName, cacheUpdatedCallback, notifierProviderStateChangedCallback);
            // Subscribe to Redis
            client.Subscriber.Subscribe(cacheName, (channel, message) =>
            {
                var notificationInfo = convert.FromRedis(message);
                OnCacheUpdated(notificationInfo);
            });
            notifierProviderStateChangedCallback(this, new ProviderStateChangedEventArgs(client.IsConnected));
        }

        public void UnSubscribe(string cacheName)
        {
            this.cacheCallbacks.TryRemove(cacheName, out _);
            
            // Unsubscribe from Redis
            GetSubClient(cacheName, null, null).Subscriber.Unsubscribe(cacheName, null, CommandFlags.None);
        }

        public bool TryPublish(string cacheName, string key, CacheItemActionTypes action)
        {
            try
            {
                Publish(cacheName, key, action);
                return true;
            }
            catch (Exception ex)
            {
                log.Warn(ex, $"Failed to publish {action} for {cacheName}.{key}");
                return false;
            }
        }

        public async Task<bool> TryPublishAsync(string cacheName, string key, CacheItemActionTypes action)
        {
            try
            {
                await PublishAsync(cacheName, key, action).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                log.Warn(ex, $"Failed to publish {action} for {cacheName}.{key}");
                return false;
            }
        }

        public void Publish(string cacheName, string key, CacheItemActionTypes action)
        {
            var message = new CacheItemNotification(sender, cacheName, key, action);
            var messageToSend = convert.ToRedis(message);
            GetSubClient(cacheName, null, null).Subscriber.Publish(cacheName, messageToSend, CommandFlags.None);
        }

        public async Task PublishAsync(string cacheName, string key, CacheItemActionTypes action)
        {
            var message = new CacheItemNotification(sender, cacheName, key, action);
            var messageToSend = convert.ToRedis(message);
            await GetSubClient(cacheName, null, null).Subscriber
                .PublishAsync(cacheName, messageToSend, CommandFlags.None)
                .ConfigureAwait(false);
        }

        private void OnCacheUpdated(CacheItemNotification notification)
        {
            if (notification.CacheName == null)
                return;

            // Ignore own messages - prevent loops
            if (sender == notification.Sender)
            {
                return;
            }

            if (this.cacheCallbacks.TryGetValue(
                notification.CacheName, out Func<CacheItemNotification, bool> callback))
            {
                log.Debug($"Received {nameof(CacheItemNotification)} for cache={notification.CacheName}, key={notification.Key}");

                // CacheSynchronizer callback
                callback(notification);
            }
        }

        public void Dispose()
        {
            var subClients = this.cacheSubClients.Values.ToList();
            this.cacheSubClients = new ConcurrentDictionary<string, RedisClient>();

            foreach (var redisClient in subClients)
            {
                redisClient.Dispose();
            }

            generalInvalidationRedisClient?.Dispose();

            this.cacheCallbacks = new ConcurrentDictionary<string, Func<CacheItemNotification, bool>>();
        }
    }
}