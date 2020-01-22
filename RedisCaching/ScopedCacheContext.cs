using PubComp.Caching.RedisCaching.Converters;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PubComp.Caching.RedisCaching
{
    public class ScopedCacheContext : IDisposable
    {
        private readonly RedisClient client;
        private readonly IRedisConverter convert;

        public ScopedCacheContext(String connectionString, String converterType, String clusterType, int monitorPort, int monitorIntervalMilliseconds)
        {
            this.convert = RedisConverterFactory.CreateConverter(converterType);
            this.client = new RedisClient(connectionString, clusterType, monitorPort, monitorIntervalMilliseconds);
        }

        public ScopedCacheContext(String connectionName, String converterType)
        {
            this.convert = RedisConverterFactory.CreateConverter(converterType);
            this.client = RedisClient.GetNamedRedisClient(connectionName);
        }

        internal ScopedCacheItem<TValue> GetItem<TValue>(String cacheName, String key)
        {
            var id = ScopedCacheItem<TValue>.GetId(cacheName, key);
            var scopedCacheItemString = client.Database.StringGet(id);
            return convert.FromScopedRedis<TValue>(scopedCacheItemString);
        }

        internal async Task<ScopedCacheItem<TValue>> GetItemAsync<TValue>(String cacheName, String key)
        {
            var id = ScopedCacheItem<TValue>.GetId(cacheName, key);
            var scopedCacheItemString = await client.Database.StringGetAsync(id).ConfigureAwait(false);
            return convert.FromScopedRedis<TValue>(scopedCacheItemString);
        }

        internal void SetItem<TValue>(ScopedCacheItem<TValue> scopedCacheItem)
        {
            TimeSpan? expiry = null;
            if (scopedCacheItem.ExpireIn.HasValue)
            {
                expiry = scopedCacheItem.ExpireIn;

                if (expiry.Value.TotalSeconds <= 0)
                {
                    client.Database.KeyDelete(scopedCacheItem.Id, CommandFlags.None);
                    return;
                }
            }

            client.Database.StringSet(scopedCacheItem.Id, convert.ToScopedRedis(scopedCacheItem), expiry, When.Always, CommandFlags.None);
        }

        internal Task SetItemAsync<TValue>(ScopedCacheItem<TValue> scopedCacheItem)
        {
            TimeSpan? expiry = null;
            if (scopedCacheItem.ExpireIn.HasValue)
            {
                expiry = scopedCacheItem.ExpireIn;

                if (expiry.Value.TotalSeconds <= 0)
                    return client.Database.KeyDeleteAsync(scopedCacheItem.Id, CommandFlags.None);
            }

            return client.Database
                .StringSetAsync(scopedCacheItem.Id, convert.ToScopedRedis(scopedCacheItem), expiry, When.Always, CommandFlags.None);
        }

        internal bool SetIfNotExists<TValue>(ScopedCacheItem<TValue> scopedCacheItem)
        {
            if (Contains(scopedCacheItem.Id))
            {
                return false;
            }
            SetItem(scopedCacheItem);
            return true;
        }

        internal async Task<bool> SetIfNotExistsAsync<TValue>(ScopedCacheItem<TValue> scopedCacheItem)
        {
            if (await ContainsAsync(scopedCacheItem.Id).ConfigureAwait(false))
            {
                return false;
            }
            await SetItemAsync(scopedCacheItem).ConfigureAwait(false);
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

        internal void SetExpirationTime<TValue>(ScopedCacheItem<TValue> scopedCacheItem)
        {
            if (scopedCacheItem.ExpireIn.HasValue)
                ExpireById(scopedCacheItem.Id, scopedCacheItem.ExpireIn.Value);
        }

        internal async Task SetExpirationTimeAsync<TValue>(ScopedCacheItem<TValue> scopedCacheItem)
        {
            if (scopedCacheItem.ExpireIn.HasValue)
                await ExpireByIdAsync(scopedCacheItem.Id, scopedCacheItem.ExpireIn.Value).ConfigureAwait(false);
        }

        internal void ExpireItemIn<TValue>(String cacheName, String key, TimeSpan timeSpan)
        {
            var id = ScopedCacheItem<TValue>.GetId(cacheName, key);
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
            var id = ScopedCacheItem<object>.GetId(cacheName, key);
            client.Database.KeyDelete(id, CommandFlags.None);
        }

        internal Task RemoveItemAsync(String cacheName, String key)
        {
            var id = ScopedCacheItem<object>.GetId(cacheName, key);
            return client.Database.KeyDeleteAsync(id, CommandFlags.None);
        }

        internal void ClearItems(String cacheName)
        {
            var keyPrefix = ScopedCacheItem<object>.GetId(cacheName, string.Empty);
            var keys = client.MasterServer.Keys(0, string.Format("*{0}*", keyPrefix), 1000, CommandFlags.None).ToArray();
            client.Database.KeyDelete(keys, CommandFlags.None);
        }

        internal Task ClearItemsAsync(String cacheName)
        {
            var keyPrefix = ScopedCacheItem<object>.GetId(cacheName, string.Empty);
            var keys = client.MasterServer.Keys(0, string.Format("*{0}*", keyPrefix), 1000, CommandFlags.None).ToArray();
            return client.Database.KeyDeleteAsync(keys, CommandFlags.None);
        }

        public void Dispose()
        {
            this.client.Dispose();
        }
    }
}
