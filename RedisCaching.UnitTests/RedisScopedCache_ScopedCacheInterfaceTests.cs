using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core;
using PubComp.Caching.Core.UnitTests;
using System;

namespace PubComp.Caching.RedisCaching.UnitTests
{
    [TestClass]
    public class RedisScopedCache_ScopedCacheInterfaceTests : ScopedCacheInterfaceTests
    {
        private readonly string connectionName = "localRedis";

        protected override IScopedCache GetScopedCache(string name)
            => new RedisScopedCache(name, new RedisCachePolicy { ConnectionName = connectionName });

        protected override IScopedCache GetScopedCacheWithSlidingExpiration(string name, TimeSpan slidingExpiration)
            => new RedisScopedCache(name, new RedisCachePolicy { SlidingExpiration = slidingExpiration, ConnectionName = connectionName });

        protected override IScopedCache GetScopedCacheWithExpirationFromAdd(string name, TimeSpan expirationFromAdd)
            => new RedisScopedCache(name, new RedisCachePolicy { ExpirationFromAdd = expirationFromAdd, ConnectionName = connectionName });

        protected override IScopedCache GetScopedCacheWithAbsoluteExpiration(string name, DateTimeOffset expireAt)
            => new RedisScopedCache(name, new RedisCachePolicy { AbsoluteExpiration = expireAt, ConnectionName = connectionName });
    }
}
