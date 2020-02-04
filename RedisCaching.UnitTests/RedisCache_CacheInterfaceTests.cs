using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core;
using PubComp.Caching.Core.UnitTests;
using System;

namespace PubComp.Caching.RedisCaching.UnitTests
{
    [TestClass]
    public class RedisCache_CacheInterfaceTests : CacheInterfaceTests
    {
        private readonly string connectionName = "localRedis";

        protected override ICache GetCache(string name)
            => new RedisCache(name, new RedisCachePolicy { ConnectionName = connectionName });

        protected override ICache GetCacheWithSlidingExpiration(string name, TimeSpan slidingExpiration)
            => new RedisCache(name, new RedisCachePolicy { SlidingExpiration = slidingExpiration, ConnectionName = connectionName });

        protected override ICache GetCacheWithExpirationFromAdd(string name, TimeSpan expirationFromAdd)
            => new RedisCache(name, new RedisCachePolicy { ExpirationFromAdd = expirationFromAdd, ConnectionName = connectionName });

        protected override ICache GetCacheWithAbsoluteExpiration(string name, DateTimeOffset expireAt)
            => new RedisCache(name, new RedisCachePolicy { AbsoluteExpiration = expireAt, ConnectionName = connectionName });
    }
}
