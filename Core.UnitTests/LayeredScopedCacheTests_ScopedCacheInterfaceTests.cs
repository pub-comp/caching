using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.SystemRuntime;
using System;

namespace PubComp.Caching.Core.UnitTests
{
    [TestClass]
    public class LayeredScopedCacheTests_ScopedCacheInterfaceTests : ScopedCacheInterfaceTests
    {
        protected override IScopedCache GetScopedCache(string name)
        {
            var l1 = new InMemoryScopedCache($"{name}__l1", new InMemoryPolicy());
            var l2 = new InMemoryScopedCache($"{name}__l2", new InMemoryPolicy());

            return new LayeredScopedCache(name, l1, l2);
        }

        protected override IScopedCache GetScopedCacheWithSlidingExpiration(string name, TimeSpan slidingExpiration)
        {
            var l1 = new InMemoryScopedCache($"{name}__l1", slidingExpiration);
            var l2 = new InMemoryScopedCache($"{name}__l2", slidingExpiration);

            return new LayeredScopedCache(name, l1, l2);
        }

        protected override IScopedCache GetScopedCacheWithExpirationFromAdd(string name, TimeSpan expirationFromAdd)
        {
            var l1 = new InMemoryScopedCache($"{name}__l1", new InMemoryPolicy { ExpirationFromAdd = expirationFromAdd });
            var l2 = new InMemoryScopedCache($"{name}__l2", new InMemoryPolicy { ExpirationFromAdd = expirationFromAdd });

            return new LayeredScopedCache(name, l1, l2);
        }

        protected override IScopedCache GetScopedCacheWithAbsoluteExpiration(string name, DateTimeOffset expireAt)
        {
            var l1 = new InMemoryScopedCache($"{name}__l1", new InMemoryPolicy { AbsoluteExpiration = expireAt });
            var l2 = new InMemoryScopedCache($"{name}__l2", new InMemoryPolicy { AbsoluteExpiration = expireAt });

            return new LayeredScopedCache(name, l1, l2);
        }
    }
}
