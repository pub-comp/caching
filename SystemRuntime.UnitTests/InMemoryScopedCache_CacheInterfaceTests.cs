using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core;
using PubComp.Caching.Core.UnitTests;
using System;

namespace PubComp.Caching.SystemRuntime.UnitTests
{
    [TestClass]
    public class InMemoryScopedCache_CacheInterfaceTests : CacheInterfaceTests
    {
        protected override ICache GetCache(string name)
            => new InMemoryScopedCache(name, new InMemoryPolicy());

        protected override ICache GetCacheWithSlidingExpiration(string name, TimeSpan slidingExpiration)
            => new InMemoryScopedCache(name, slidingExpiration);

        protected override ICache GetCacheWithExpirationFromAdd(string name, TimeSpan expirationFromAdd)
            => new InMemoryScopedCache(name, new InMemoryPolicy { ExpirationFromAdd = expirationFromAdd });

        protected override ICache GetCacheWithAbsoluteExpiration(string name, DateTimeOffset expireAt)
            => new InMemoryScopedCache(name, new InMemoryPolicy { AbsoluteExpiration = expireAt });

        private IDisposable cacheDirectives;

        [TestInitialize]
        public void TestInitialize()
        {
            cacheDirectives = CacheDirectives.SetScope(CacheMethod.GetOrSet, DateTimeOffset.UtcNow);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            cacheDirectives?.Dispose();
            cacheDirectives = null;
        }
    }
}
