using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core;
using PubComp.Caching.Core.UnitTests;
using System;

namespace PubComp.Caching.SystemRuntime.UnitTests
{
    [TestClass]
    public class InMemorySerializedCache_CacheInterfaceTests : CacheInterfaceTests
    {
        protected override ICache GetCache(string name)
            => new InMemorySerializedCache(name, new InMemoryPolicy());

        protected override ICache GetCacheWithSlidingExpiration(string name, TimeSpan slidingExpiration)
            => new InMemorySerializedCache(name, slidingExpiration);

        protected override ICache GetCacheWithExpirationFromAdd(string name, TimeSpan expirationFromAdd)
            => new InMemorySerializedCache(name, new InMemoryPolicy { ExpirationFromAdd = expirationFromAdd });

        protected override ICache GetCacheWithAbsoluteExpiration(string name, DateTimeOffset expireAt)
            => new InMemorySerializedCache(name, new InMemoryPolicy { AbsoluteExpiration = expireAt });
    }
}
