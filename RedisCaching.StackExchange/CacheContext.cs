using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using StackExchange.Redis;

namespace PubComp.Caching.RedisCaching.StackExchange
{
    public class CacheContext : IDisposable
    {
        private RedisClient _redis;
        private IRedisConverter convert;
        
        public CacheContext(String connectionString, String converterType, String clusterType, int monitorPort, int monitorIntervalMilliseconds)
        {
            this.convert = RedisConverterFactory.CreateConverter(converterType);
            _redis = new RedisClient(connectionString, clusterType, monitorPort,monitorIntervalMilliseconds);
        }
        internal CacheItem<TValue> GetItem<TValue>(String cacheName, String key)
        {
            var id = CacheItem<TValue>.GetId(cacheName, key);
            var cacheItemString = _redis.Database.StringGet(id);
            return convert.FromRedis<TValue>(cacheItemString);
        }

        internal void SetItem<TValue>(CacheItem<TValue> cacheItem)
        {
            TimeSpan? expiry = null;
            if (cacheItem.ExpireIn.HasValue)
                expiry = cacheItem.ExpireIn;

            _redis.Database.StringSet(cacheItem.Id, convert.ToRedis(cacheItem), expiry, When.Always, CommandFlags.FireAndForget);
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

        private bool Contains(string key)
        {
            return _redis.Database.KeyExists(key);
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
            _redis.Database.KeyExpire(id, timeSpan, CommandFlags.FireAndForget);
        }

        internal void RemoveItem(String cacheName, String key)
        {
            var id = CacheItem<object>.GetId(cacheName, key);
            _redis.Database.KeyDelete(id, CommandFlags.FireAndForget);
        }

        internal void ClearItems(String cacheName)
        {
            var keyPrefix = CacheItem<object>.GetId(cacheName, string.Empty);
            var keys = _redis.MasterServer.Keys(0, string.Format("*{0}*", keyPrefix), 1000, CommandFlags.None).ToArray();
            _redis.Database.KeyDelete(keys, CommandFlags.FireAndForget);
        }

        public void Dispose()
        {
            this._redis.Dispose();
        }
    }
}
