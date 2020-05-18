using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core;
using PubComp.Caching.Core.UnitTests;
using System;

namespace PubComp.Caching.SystemRuntime.UnitTests
{
    [TestClass]
    public class InMemoryScopedCache_ScopedCacheInterfaceTests : ScopedCacheInterfaceTests
    {
        protected override IScopedCache GetScopedCache(string name)
            => new InMemoryScopedCache(name, new InMemoryPolicy());

        protected override IScopedCache GetScopedCacheWithSlidingExpiration(string name, TimeSpan slidingExpiration)
            => new InMemoryScopedCache(name, slidingExpiration);

        protected override IScopedCache GetScopedCacheWithExpirationFromAdd(string name, TimeSpan expirationFromAdd)
            => new InMemoryScopedCache(name, new InMemoryPolicy { ExpirationFromAdd = expirationFromAdd });

        protected override IScopedCache GetScopedCacheWithAbsoluteExpiration(string name, DateTimeOffset expireAt)
            => new InMemoryScopedCache(name, new InMemoryPolicy { AbsoluteExpiration = expireAt });
    }
}
