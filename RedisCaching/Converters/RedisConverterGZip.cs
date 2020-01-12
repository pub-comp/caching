using System.IO;
using PubComp.Caching.Core.Notifications;
using StackExchange.Redis;

namespace PubComp.Caching.RedisCaching.Converters
{
    internal class RedisConverterGZip : IRedisConverter
    {
        public const string Type = "gzip";

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

        public RedisValue ToScopedRedis<TValue>(ScopedCacheItem<TValue> scopedCacheItem)
        {
            return To(scopedCacheItem);
        }

        public ScopedCacheItem<TValue> FromScopedRedis<TValue>(RedisValue scopedCacheItemString)
        {
            if (string.IsNullOrEmpty(scopedCacheItemString))
                return null;

            return From<ScopedCacheItem<TValue>>(scopedCacheItemString);
        }

        private byte[] Compress(byte[] inputBuffer)
        {
            byte[] output;
            using (var outputstream = new MemoryStream())
            {
                using (var algostream = new System.IO.Compression.GZipStream(outputstream, System.IO.Compression.CompressionLevel.Fastest, true))
                {
                    algostream.Write(inputBuffer, 0, inputBuffer.Length);
                }
                outputstream.Seek(0L, SeekOrigin.Begin);
                output = outputstream.ToArray();
            }
            return output;
        }

        private byte[] Decompress(byte[] inputBuffer)
        {
            byte[] output;
            using (var inputstream = new MemoryStream(inputBuffer))
            {
                using (var outputstream = new MemoryStream())
                {
                    using (var algostream = new System.IO.Compression.GZipStream(inputstream, System.IO.Compression.CompressionMode.Decompress, true))
                    {
                        algostream.CopyTo(outputstream);
                    }
                    outputstream.Seek(0L, SeekOrigin.Begin);
                    output = outputstream.ToArray();
                }
            }
            return output;
        }

        private RedisValue To<TValue>(TValue data)
        {
            if (data == null)
                return RedisValue.Null;

            return Compress(RedisConverterBson.ToBson(data));
        }

        private TValue From<TValue>(RedisValue stringValue)
        {
            if (!stringValue.HasValue)
                return default(TValue);

            byte[] buffer = Decompress(stringValue);
            return RedisConverterBson.FromBson<TValue>(buffer);
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