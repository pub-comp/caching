using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core;
using PubComp.Caching.Core.UnitTests;
using System;

namespace PubComp.Caching.SystemRuntime.UnitTests
{
    [TestClass]
    public class InMemoryCache_CacheInterfaceTests : CacheInterfaceTests
    {
        protected override ICache GetCache(string name)
            => new InMemoryCache(name, new InMemoryPolicy());

        protected override ICache GetCacheWithSlidingExpiration(string name, TimeSpan slidingExpiration)
            => new InMemoryCache(name, slidingExpiration);

        protected override ICache GetCacheWithExpirationFromAdd(string name, TimeSpan expirationFromAdd)
            => new InMemoryCache(name, new InMemoryPolicy { ExpirationFromAdd = expirationFromAdd });

        protected override ICache GetCacheWithAbsoluteExpiration(string name, DateTimeOffset expireAt)
            => new InMemoryCache(name, new InMemoryPolicy { AbsoluteExpiration = expireAt });
    }
}
