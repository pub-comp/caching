using System.Globalization;
using System.Runtime.Serialization.Formatters;
using PubComp.Caching.Core.Notifications;
using StackExchange.Redis;

namespace PubComp.Caching.RedisCaching.Converters
{
    internal class RedisConverterJson : IRedisConverter
    {
        public const string Type = "json";

        string IRedisConverter.Type
        {
            get { return Type; }
        }

        public RedisValue ToRedis<TValue>(CacheItem<TValue> cacheItem)
        {
            return To(cacheItem);
        }

        public RedisValue ToRedis(CacheItemNotification notification)
        {
            return To(notification);
        }
        
        private RedisValue To<TValue>(TValue data)
        {
            if (data == null)
                return RedisValue.Null;

            var cacheItemString = Newtonsoft.Json.JsonConvert.SerializeObject(
                data,
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    Formatting = Newtonsoft.Json.Formatting.None,
                    Culture = CultureInfo.InvariantCulture,
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                    TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
                });

            return cacheItemString;
        }

        public CacheItem<TValue> FromRedis<TValue>(RedisValue cacheItemString)
        {
            if (string.IsNullOrEmpty(cacheItemString))
                return null;

            return From<CacheItem<TValue>>(cacheItemString);
        }

        public CacheItemNotification FromRedis(RedisValue cacheNotificationString)
        {
            if (string.IsNullOrEmpty(cacheNotificationString))
                return null;

            return From<CacheItemNotification>(cacheNotificationString);
        }

        public TValue From<TValue>(RedisValue stringValue)
        {
            var cacheItem = Newtonsoft.Json.JsonConvert.DeserializeObject<TValue>(
                stringValue,
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    Formatting = Newtonsoft.Json.Formatting.None,
                    Culture = CultureInfo.InvariantCulture,
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                    TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
                });

            return cacheItem;
        }
    }
}