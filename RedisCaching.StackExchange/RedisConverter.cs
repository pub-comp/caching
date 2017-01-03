using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
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
    }

    
    internal class RedisConverterJson : IRedisConverter
    {
        public string Type {
            get { return "json"; }
        }

        public RedisValue ToRedis<TValue>(CacheItem<TValue> cacheItem)
        {
            if (cacheItem == null)
                return RedisValue.Null;

            var cacheItemString = Newtonsoft.Json.JsonConvert.SerializeObject(
                cacheItem,
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

            var cacheItem = Newtonsoft.Json.JsonConvert.DeserializeObject<CacheItem<TValue>>(
                cacheItemString,
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
            if (cacheItem == null)
                return RedisValue.Null;

            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, cacheItem);
                stream.Flush();
                stream.Position = 0;
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        public CacheItem<TValue> FromRedis<TValue>(RedisValue cacheItemString)
        {
            if (string.IsNullOrEmpty(cacheItemString))
                return null;

            byte[] b = Convert.FromBase64String(cacheItemString);
            using (var stream = new MemoryStream(b))
            {
                var formatter = new BinaryFormatter();
                stream.Seek(0, SeekOrigin.Begin);
                return (CacheItem<TValue>) formatter.Deserialize(stream);
            }
        }

    }
}
