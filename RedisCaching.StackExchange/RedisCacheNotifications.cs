using System;
using PubComp.Caching.Core;
using StackExchange.Redis;

namespace PubComp.Caching.RedisCaching.StackExchange
{
    public class RedisCacheNotifications : ICacheNotifier
    {
        private readonly string _cacheName;
        private Func<CacheItemNotification, bool> _callback;
        private readonly IRedisConverter _convert;
        private RedisCacheNotificationsPolicy _policy;
        private readonly RedisClient _redis;
        private readonly string _sender;

        public RedisCacheNotifications(string cacheName, RedisCacheNotificationsPolicy policy)
        {
            _policy = policy;
            _cacheName = cacheName;
            _sender = Guid.NewGuid().ToString();
            _convert = RedisConverterFactory.CreateConverter(policy.Converter);

            _redis = new RedisClient(policy.ConnectionString, policy.ClusterType, policy.MonitorPort,
                policy.MonitorIntervalMilliseconds);
        }

        public void Subscribe(Func<CacheItemNotification, bool> callback)
        {
            _callback = callback;

            //subscribe to redis
            _redis.Subscriber.Subscribe(_cacheName, (channel, message) =>
            {
                var notificationInfo = _convert.FromRedis(message);
                OnCacheUpdated(notificationInfo);
            });
        }

        public void UnSubscribe()
        {
            _callback = null;

            //unsubscribe to redis
            _redis.Subscriber.Unsubscribe(_cacheName, null, CommandFlags.FireAndForget);
        }

        public void Publish(string key, CacheItemActionTypes action)
        {
            var message = new CacheItemNotification(_sender, _cacheName, key, action);
            var messageToSend = _convert.ToRedis(message);

            // publish via redis
            _redis.Subscriber.Publish(_cacheName, messageToSend, CommandFlags.FireAndForget);
        }

        private void OnCacheUpdated(CacheItemNotification notification)
        {
            // callback from redis
            if (_callback == null)
            {
                return;
            }

            //ignore my messages
            if (_sender == notification.Sender)
            {
                return;
            }

            _callback(notification);
        }
    }
}