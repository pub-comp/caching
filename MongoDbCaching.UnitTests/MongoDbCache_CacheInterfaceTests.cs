using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core;
using PubComp.Caching.Core.UnitTests;
using System;

namespace PubComp.Caching.MongoDbCaching.UnitTests
{
    [TestClass]
    public class MongoDbCache_CacheInterfaceTests : CacheInterfaceTests
    {
        private readonly string connectionName = "localRedis";

        protected override ICache GetCache(string name)
            => new MongoDbCache(name, new MongoDbCachePolicy { DatabaseName = "TestCacheDb" });

        protected override ICache GetCacheWithSlidingExpiration(string name, TimeSpan slidingExpiration)
            => new MongoDbCache(name, new MongoDbCachePolicy { SlidingExpiration = slidingExpiration, DatabaseName = "TestCacheDb" });

        protected override ICache GetCacheWithExpirationFromAdd(string name, TimeSpan expirationFromAdd)
            => new MongoDbCache(name, new MongoDbCachePolicy { ExpirationFromAdd = expirationFromAdd, DatabaseName = "TestCacheDb" });

        protected override ICache GetCacheWithAbsoluteExpiration(string name, DateTimeOffset expireAt)
            => new MongoDbCache(name, new MongoDbCachePolicy { AbsoluteExpiration = expireAt, DatabaseName = "TestCacheDb" });
    }
}
