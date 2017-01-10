using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using PubComp.Caching.Core;
using PubComp.Caching.RedisCaching.StackExchange;
using StackExchange.Redis;

namespace PubComp.Caching.RedisCaching.StackExchange
{

    internal class RedisConverterFactory
    {
        public static IRedisConverter CreateConverter(string type)
        {
            if (type == null || type == "json")
            {
                return new RedisConverterJson();
            }
            else
            {
                return new RedisConverterBinary();
            }
        }
    }

    internal interface IRedisConverter
    {
        string Type { get; } 
        RedisValue ToRedis<TValue>(CacheItem<TValue> cacheItem);
        CacheItem<TValue> FromRedis<TValue>(RedisValue cacheItemString);
        RedisValue ToRedis(CacheItemNotification notification);
        CacheItemNotification FromRedis(RedisValue cacheNotificationString);
    }

    
    internal class RedisConverterJson : IRedisConverter
    {
        public string Type {
            get { return "json"; }
        }

        public RedisValue ToRedis<TValue>(CacheItem<TValue> cacheItem)
        {
            return To(cacheItem);
        }

        public RedisValue ToRedis(CacheItemNotification notification)
        {
            return To(notification);
        }
        
        private RedisValue To<TValue>(TValue item)
        {
            if (item == null)
                return RedisValue.Null;

            var cacheItemString = Newtonsoft.Json.JsonConvert.SerializeObject(
                item,
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                    TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple,
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
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                    TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple,
                });

            return cacheItem;
        }

    }

    internal class RedisConverterBinary : IRedisConverter
    {
        public string Type
        {
            get { return "binary"; }
        }

        public RedisValue ToRedis<TValue>(CacheItem<TValue> cacheItem)
        {
            return To(cacheItem);
        }

        private RedisValue To<TValue>(TValue data)
        {
            if (data == null)
                return RedisValue.Null;

            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, data);
                stream.Flush();
                stream.Position = 0;
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        public CacheItem<TValue> FromRedis<TValue>(RedisValue cacheItemString)
        {
            if (string.IsNullOrEmpty(cacheItemString))
                return null;

            return From<CacheItem<TValue>>(cacheItemString);
        }

        private TValue From<TValue>(RedisValue stringValue)
        {
            byte[] b = Convert.FromBase64String(stringValue);
            using (var stream = new MemoryStream(b))
            {
                var formatter = new BinaryFormatter();
                stream.Seek(0, SeekOrigin.Begin);
                return (TValue)formatter.Deserialize(stream);
            }
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
