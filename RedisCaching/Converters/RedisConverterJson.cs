using System.Globalization;
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

            return ToJson(data);
        }

        private static string ToJson<TValue>(TValue data)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(
                data,
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    Formatting = Newtonsoft.Json.Formatting.None,
                    Culture = CultureInfo.InvariantCulture,
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = Newtonsoft.Json.TypeNameAssemblyFormatHandling.Simple,
                });

            return json;
        }

        private static TValue FromJson<TValue>(string json)
        {
            var cacheItem = Newtonsoft.Json.JsonConvert.DeserializeObject<TValue>(
                json,
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    Formatting = Newtonsoft.Json.Formatting.None,
                    Culture = CultureInfo.InvariantCulture,
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                    TypeNameAssemblyFormatHandling = Newtonsoft.Json.TypeNameAssemblyFormatHandling.Simple,
                });

            return cacheItem;
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
            return FromJson<TValue>(stringValue.ToString());
        }

        internal static byte[] ToJsonBytes<TValue>(TValue value)
        {
            var json = ToJson(value);
            var buffer = System.Text.Encoding.Unicode.GetBytes(json);
            return buffer;
        }

        internal static TValue FromJsonBytes<TValue>(byte[] buffer)
        {
            var json = System.Text.Encoding.Unicode.GetString(buffer);
            var value = FromJson<TValue>(json);
            return value;
        }
    }
}