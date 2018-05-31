using System;
using System.Linq;
using System.Threading.Tasks;
using PubComp.Caching.RedisCaching.Converters;
using StackExchange.Redis;

namespace PubComp.Caching.RedisCaching
{
    public class CacheContext : IDisposable
    {
        private readonly RedisClient client;
        private readonly IRedisConverter convert;
        
        public CacheContext(String connectionString, String converterType, String clusterType, int monitorPort, int monitorIntervalMilliseconds)
        {
            this.convert = RedisConverterFactory.CreateConverter(converterType);
            this.client = new RedisClient(connectionString, clusterType, monitorPort,monitorIntervalMilliseconds);
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
                expiry = cacheItem.ExpireIn;

            client.Database.StringSet(cacheItem.Id, convert.ToRedis(cacheItem), expiry, When.Always, CommandFlags.FireAndForget);
        }

        internal async Task SetItemAsync<TValue>(CacheItem<TValue> cacheItem)
        {
            TimeSpan? expiry = null;
            if (cacheItem.ExpireIn.HasValue)
                expiry = cacheItem.ExpireIn;

            await client.Database
                .StringSetAsync(cacheItem.Id, convert.ToRedis(cacheItem), expiry, When.Always,
                    CommandFlags.FireAndForget).ConfigureAwait(false);
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
            if (Contains(cacheItem.Id))
            {
                return false;
            }
            await SetItemAsync(cacheItem);
            return true;
        }

        private bool Contains(string key)
        {
            return client.Database.KeyExists(key);
        }

        internal void SetExpirationTime<TValue>(CacheItem<TValue> cacheItem)
        {
            if (cacheItem.ExpireIn.HasValue)
                ExpireById(cacheItem.Id, cacheItem.ExpireIn.Value);
        }

        internal void ExpireItemIn<TValue>(String cacheName, String key, TimeSpan timeSpan)
        {
            var id = CacheItem<TValue>.GetId(cacheName, key);
            ExpireById(id, timeSpan);
        }

        private void ExpireById(string id, TimeSpan timeSpan)
        {
            client.Database.KeyExpire(id, timeSpan, CommandFlags.FireAndForget);
        }

        internal void RemoveItem(String cacheName, String key)
        {
            var id = CacheItem<object>.GetId(cacheName, key);
            client.Database.KeyDelete(id, CommandFlags.FireAndForget);
        }

        internal async Task RemoveItemAsync(String cacheName, String key)
        {
            var id = CacheItem<object>.GetId(cacheName, key);
            await client.Database.KeyDeleteAsync(id, CommandFlags.FireAndForget).ConfigureAwait(false);
        }

        internal void ClearItems(String cacheName)
        {
            var keyPrefix = CacheItem<object>.GetId(cacheName, string.Empty);
            var keys = client.MasterServer.Keys(0, string.Format("*{0}*", keyPrefix), 1000, CommandFlags.None).ToArray();
            client.Database.KeyDelete(keys, CommandFlags.FireAndForget);
        }

        internal async Task ClearItemsAsync(String cacheName)
        {
            var keyPrefix = CacheItem<object>.GetId(cacheName, string.Empty);
            var keys = client.MasterServer.Keys(0, string.Format("*{0}*", keyPrefix), 1000, CommandFlags.None).ToArray();
            await client.Database.KeyDeleteAsync(keys, CommandFlags.FireAndForget).ConfigureAwait(false);
        }

        public void Dispose()
        {
            this.client.Dispose();
        }
    }
}
