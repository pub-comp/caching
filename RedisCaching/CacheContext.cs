using System;
using System.Linq;

namespace PubComp.Caching.RedisCaching
{
    public class CacheContext : IDisposable
    {
        private readonly String connectionString;
        protected readonly ServiceStack.Redis.RedisClient innerContext;

        public CacheContext(String connectionString)
        {
            this.connectionString = connectionString;
            this.innerContext = new ServiceStack.Redis.RedisClient(new Uri(connectionString));
        }

        internal String ToString<TValue>(CacheItem<TValue> cacheItem)
        {
            if (cacheItem == null)
                return null;

            var cacheItemString = Newtonsoft.Json.JsonConvert.SerializeObject(
                cacheItem,
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                    TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple,
                });

            return cacheItemString;
        }

        internal CacheItem<TValue> FromString<TValue>(String cacheItemString)
        {
            if (string.IsNullOrEmpty(cacheItemString))
                return null;

            var cacheItem = Newtonsoft.Json.JsonConvert.DeserializeObject<CacheItem<TValue>>(
                cacheItemString,
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                    TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple,
                });

            return cacheItem;
        }

        internal CacheItem<TValue> GetItem<TValue>(String cacheName, String key)
        {
            var id = CacheItem<TValue>.GetId(cacheName, key);
            var cacheItemString = this.innerContext.GetValue(id);
            return FromString<TValue>(cacheItemString);
        }

        internal void SetItem<TValue>(CacheItem<TValue> cacheItem)
        {
            this.innerContext.SetEntry(cacheItem.Id, ToString(cacheItem));
        }

        internal bool SetIfNotExists<TValue>(CacheItem<TValue> cacheItem)
        {
            return this.innerContext.SetEntryIfNotExists(cacheItem.Id, ToString(cacheItem));
        }

        internal void SetExpirationTime<TValue>(CacheItem<TValue> cacheItem)
        {
            if (cacheItem.ExpireIn.HasValue)
                this.innerContext.ExpireEntryIn(cacheItem.Id, cacheItem.ExpireIn.Value);
        }

        internal void ExpireItemIn<TValue>(String cacheName, String key, TimeSpan timeSpan)
        {
            var id = CacheItem<TValue>.GetId(cacheName, key);
            this.innerContext.ExpireEntryIn(id, timeSpan);
        }

        internal void RemoveItem(String cacheName, String key)
        {
            var id = CacheItem<object>.GetId(cacheName, key);
            this.innerContext.RemoveEntry(key);
        }

        internal void ClearItems(String cacheName)
        {
            var keyPrefix = CacheItem<object>.GetId(cacheName, string.Empty);
            var keysToClear = this.innerContext.GetAllKeys().AsQueryable()
                .Where(k => k.StartsWith(keyPrefix)).ToArray();
            this.innerContext.RemoveEntry(keysToClear);
        }

        public void Dispose()
        {
            this.innerContext.Dispose();
        }
    }
}
