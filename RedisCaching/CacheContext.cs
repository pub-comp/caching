using PubComp.Caching.RedisCaching.Converters;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PubComp.Caching.RedisCaching
{
    public class CacheContext : IDisposable
    {
        private readonly RedisClient client;
        private readonly IRedisConverter convert;

        public bool IsActive { get; private set; }

        public CacheContext(String connectionString, String converterType, String clusterType, int monitorPort, int monitorIntervalMilliseconds)
        {
            this.convert = RedisConverterFactory.CreateConverter(converterType);
            this.client = new RedisClient(connectionString, clusterType, monitorPort, monitorIntervalMilliseconds);
            RegisterToRedisConnectionStateChangeEvent();
        }

        public CacheContext(String connectionName, String converterType)
        {
            this.convert = RedisConverterFactory.CreateConverter(converterType);
            this.client = RedisClient.GetNamedRedisClient(connectionName);
            RegisterToRedisConnectionStateChangeEvent();
        }

        private void RegisterToRedisConnectionStateChangeEvent()
        {
            this.client.OnRedisConnectionStateChanged += (sender, args) => this.IsActive = args.NewState;
            this.IsActive = this.client.IsConnected;
        }

        internal CacheItem<TValue> GetItem<TValue>(String cacheName, String key)
        {
            var id = CacheItem<TValue>.GetId(cacheName, key);
            var cacheItemString = client.Database.StringGet(id);
            return convert.FromRedis<TValue>(cacheItemString);
        }

        internal async Task<CacheItem<TValue>> GetItemAsync<TValue>(String cacheName, String key)
        {
            var id = CacheItem<TValue>.GetId(cacheName, key);
            var cacheItemString = await client.Database.StringGetAsync(id).ConfigureAwait(false);
            return convert.FromRedis<TValue>(cacheItemString);
        }

        internal void SetItem<TValue>(CacheItem<TValue> cacheItem)
        {
            TimeSpan? expiry = null;
            if (cacheItem.ExpireIn.HasValue)
            {
                expiry = cacheItem.ExpireIn;

                if (expiry.Value.TotalSeconds <= 0)
                {
                    client.Database.KeyDelete(cacheItem.Id, CommandFlags.None);
                    return;
                }
            }

            client.Database.StringSet(cacheItem.Id, convert.ToRedis(cacheItem), expiry, When.Always, CommandFlags.None);
        }

        internal Task SetItemAsync<TValue>(CacheItem<TValue> cacheItem)
        {
            TimeSpan? expiry = null;
            if (cacheItem.ExpireIn.HasValue)
            {
                expiry = cacheItem.ExpireIn;

                if (expiry.Value.TotalSeconds <= 0)
                    return client.Database.KeyDeleteAsync(cacheItem.Id, CommandFlags.None);
            }

            return client.Database
                .StringSetAsync(cacheItem.Id, convert.ToRedis(cacheItem), expiry, When.Always, CommandFlags.None);
        }

        internal bool SetIfNotExists<TValue>(CacheItem<TValue> cacheItem)
        {
            if (Contains(cacheItem.Id))
            {
                return false;
            }
            SetItem(cacheItem);
            return true;
        }

        internal async Task<bool> SetIfNotExistsAsync<TValue>(CacheItem<TValue> cacheItem)
        {
            if (await ContainsAsync(cacheItem.Id).ConfigureAwait(false))
            {
                return false;
            }
            await SetItemAsync(cacheItem).ConfigureAwait(false);
            return true;
        }

        private bool Contains(string key)
        {
            return client.Database.KeyExists(key);
        }

        private Task<bool> ContainsAsync(string key)
        {
            return client.Database.KeyExistsAsync(key);
        }

        internal void SetExpirationTime<TValue>(CacheItem<TValue> cacheItem)
        {
            if (cacheItem.ExpireIn.HasValue)
                ExpireById(cacheItem.Id, cacheItem.ExpireIn.Value);
        }

        internal async Task SetExpirationTimeAsync<TValue>(CacheItem<TValue> cacheItem)
        {
            if (cacheItem.ExpireIn.HasValue)
                await ExpireByIdAsync(cacheItem.Id, cacheItem.ExpireIn.Value).ConfigureAwait(false);
        }

        internal void ExpireItemIn<TValue>(String cacheName, String key, TimeSpan timeSpan)
        {
            var id = CacheItem<TValue>.GetId(cacheName, key);
            ExpireById(id, timeSpan);
        }

        private void ExpireById(string id, TimeSpan timeSpan)
        {
            client.Database.KeyExpire(id, timeSpan, CommandFlags.None);
        }

        private Task ExpireByIdAsync(string id, TimeSpan timeSpan)
        {
            return client.Database.KeyExpireAsync(id, timeSpan, CommandFlags.None);
        }

        internal void RemoveItem(String cacheName, String key)
        {
            var id = CacheItem<object>.GetId(cacheName, key);
            client.Database.KeyDelete(id, CommandFlags.None);
        }

        internal Task RemoveItemAsync(String cacheName, String key)
        {
            var id = CacheItem<object>.GetId(cacheName, key);
            return client.Database.KeyDeleteAsync(id, CommandFlags.None);
        }

        internal void ClearItems(String cacheName)
        {
            var keyPrefix = CacheItem<object>.GetId(cacheName, string.Empty);
            var keys = client.MasterServer.Keys(0, string.Format("*{0}*", keyPrefix), 1000, CommandFlags.None).ToArray();
            client.Database.KeyDelete(keys, CommandFlags.None);
        }

        internal Task ClearItemsAsync(String cacheName)
        {
            var keyPrefix = CacheItem<object>.GetId(cacheName, string.Empty);
            var keys = client.MasterServer.Keys(0, string.Format("*{0}*", keyPrefix), 1000, CommandFlags.None).ToArray();
            return client.Database.KeyDeleteAsync(keys, CommandFlags.None);
        }

        public void Dispose()
        {
            this.client.Dispose();
        }
    }
}
