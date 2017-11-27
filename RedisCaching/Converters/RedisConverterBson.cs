using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters;
using PubComp.Caching.Core.Notifications;
using StackExchange.Redis;

namespace PubComp.Caching.RedisCaching.Converters
{
    internal class RedisConverterBson : IRedisConverter
    {
        public const string Type = "bson";

        string IRedisConverter.Type
        {
            get { return Type; }
        }

        public RedisValue ToRedis<TValue>(CacheItem<TValue> cacheItem)
        {
            return To(cacheItem);
        }

        public CacheItem<TValue> FromRedis<TValue>(RedisValue cacheItemString)
        {
            if (string.IsNullOrEmpty(cacheItemString))
                return null;

            return From<CacheItem<TValue>>(cacheItemString);
        }

        internal static byte[] ToBson<TValue>(TValue value)
        {
            byte[] buffer;

            using (MemoryStream ms = new MemoryStream())
            {
                using (var writer = new Newtonsoft.Json.Bson.BsonWriter(ms))
                {
                    var serializer = new Newtonsoft.Json.JsonSerializer
                    {
                        Formatting = Newtonsoft.Json.Formatting.None,
                        Culture = CultureInfo.InvariantCulture,
                        TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                        TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
                    };
                    serializer.Serialize(writer, value);
                }

                buffer = ms.ToArray();
            }

            return buffer;
        }

        internal static TValue FromBson<TValue>(byte[] buffer)
        {
            TValue value;

            using (MemoryStream ms = new MemoryStream(buffer))
            {
                using (var reader = new Newtonsoft.Json.Bson.BsonReader(ms))
                {
                    var serializer = new Newtonsoft.Json.JsonSerializer
                    {
                        Formatting = Newtonsoft.Json.Formatting.None,
                        Culture = CultureInfo.InvariantCulture,
                        TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                        TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
                    };
                    value = serializer.Deserialize<TValue>(reader);
                }
            }

            return value;
        }

        private RedisValue To<TValue>(TValue data)
        {
            if (data == null)
                return RedisValue.Null;

            return ToBson(data);
        }

        private TValue From<TValue>(RedisValue stringValue)
        {
            if (!stringValue.HasValue)
                return default(TValue);

            byte[] buffer = stringValue;
            return FromBson<TValue>(buffer);
        }

        public RedisValue ToRedis(CacheItemNotification notification)
        {
            return To(notification);
        }

        public CacheItemNotification FromRedis(RedisValue cacheNotificationString)
        {
            return From<CacheItemNotification>(cacheNotificationString);
        }
    }
}