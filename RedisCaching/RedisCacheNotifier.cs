using System;
using PubComp.Caching.Core;
using StackExchange.Redis;

namespace PubComp.Caching.RedisCaching
{
    public class RedisCacheNotifier : ICacheNotifier
    {
        private readonly string cacheName;
        private Func<CacheItemNotification, bool> callback;
        private readonly IRedisConverter convert;
        private RedisCacheNotifierPolicy policy;
        private readonly RedisClient client;
        private readonly string sender;

        public RedisCacheNotifier(string cacheName, RedisCacheNotifierPolicy policy)
        {
            this.policy = policy;
            this.cacheName = cacheName;
            this.sender = Guid.NewGuid().ToString();
            this.convert = RedisConverterFactory.CreateConverter(policy.Converter);

            this.client = new RedisClient(policy.ConnectionString, policy.ClusterType, policy.MonitorPort,
                policy.MonitorIntervalMilliseconds);
        }

        // ReSharper disable once ParameterHidesMember
        public void Subscribe(Func<CacheItemNotification, bool> callback)
        {
            this.callback = callback;

            //subscribe to redis
            client.Subscriber.Subscribe(cacheName, (channel, message) =>
            {
                var notificationInfo = convert.FromRedis(message);
                OnCacheUpdated(notificationInfo);
            });
        }

        public void UnSubscribe()
        {
            callback = null;

            //unsubscribe to redis
            client.Subscriber.Unsubscribe(cacheName, null, CommandFlags.FireAndForget);
        }

        public void Publish(string key, CacheItemActionTypes action)
        {
            var message = new CacheItemNotification(sender, cacheName, key, action);
            var messageToSend = convert.ToRedis(message);

            // publish via redis
            client.Subscriber.Publish(cacheName, messageToSend, CommandFlags.FireAndForget);
        }

        private void OnCacheUpdated(CacheItemNotification notification)
        {
            // callback from redis
            if (callback == null)
            {
                return;
            }

            //ignore my messages
            if (sender == notification.Sender)
            {
                return;
            }

            callback(notification);
        }
    }
}