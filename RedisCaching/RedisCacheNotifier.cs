using System;
using PubComp.Caching.Core;
using PubComp.Caching.Core.Notifications;
using StackExchange.Redis;

namespace PubComp.Caching.RedisCaching
{
    public class RedisCacheNotifier : ICacheNotifier
    {
        private readonly string name;
        private readonly string connectionString;
        private Func<CacheItemNotification, bool> callback;
        private readonly IRedisConverter convert;
        private readonly RedisClient client;
        private readonly string sender;
        private readonly NLog.ILogger log;

        public RedisCacheNotifier(string name, RedisCacheNotifierPolicy policy)
        {
            this.name = name;
            this.log = NLog.LogManager.GetLogger(typeof(RedisCacheNotifier).FullName);

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

            this.client = new RedisClient(this.connectionString, policy.ClusterType, policy.MonitorPort,
                policy.MonitorIntervalMilliseconds);
        }

        public string Name { get { return this.name; } }

        // ReSharper disable once ParameterHidesMember
        public void Subscribe(Func<CacheItemNotification, bool> callback)
        {
            this.callback = callback;

            //subscribe to redis
            client.Subscriber.Subscribe(name, (channel, message) =>
            {
                var notificationInfo = convert.FromRedis(message);
                OnCacheUpdated(notificationInfo);
            });
        }

        public void UnSubscribe()
        {
            callback = null;

            //unsubscribe to redis
            client.Subscriber.Unsubscribe(name, null, CommandFlags.FireAndForget);
        }

        public void Publish(string cacheName, string key, CacheItemActionTypes action)
        {
            var message = new CacheItemNotification(sender, cacheName, key, action);
            var messageToSend = convert.ToRedis(message);

            // publish via redis
            client.Subscriber.Publish(name, messageToSend, CommandFlags.FireAndForget);
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