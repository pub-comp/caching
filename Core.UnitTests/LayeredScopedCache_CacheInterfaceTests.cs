﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.SystemRuntime;
using System;

namespace PubComp.Caching.Core.UnitTests
{
    [TestClass]
    public class LayeredScopedCache_CacheInterfaceTests : CacheInterfaceTests
    {
        protected override ICache GetCache(string name)
        {
            var l1 = new InMemoryScopedCache($"{name}__l1", new InMemoryPolicy());
            var l2 = new InMemoryScopedCache($"{name}__l2", new InMemoryPolicy());

            return new LayeredScopedCache(name, l1, l2);
        }

        protected override ICache GetCacheWithSlidingExpiration(string name, TimeSpan slidingExpiration)
        {
            var l1 = new InMemoryScopedCache($"{name}__l1", slidingExpiration);
            var l2 = new InMemoryScopedCache($"{name}__l2", slidingExpiration);

            return new LayeredScopedCache(name, l1, l2);
        }

        protected override ICache GetCacheWithExpirationFromAdd(string name, TimeSpan expirationFromAdd)
        {
            var l1 = new InMemoryScopedCache($"{name}__l1", new InMemoryPolicy { ExpirationFromAdd = expirationFromAdd });
            var l2 = new InMemoryScopedCache($"{name}__l2", new InMemoryPolicy { ExpirationFromAdd = expirationFromAdd });

            return new LayeredScopedCache(name, l1, l2);
        }

        protected override ICache GetCacheWithAbsoluteExpiration(string name, DateTimeOffset expireAt)
        {
            var l1 = new InMemoryScopedCache($"{name}__l1", new InMemoryPolicy { AbsoluteExpiration = expireAt });
            var l2 = new InMemoryScopedCache($"{name}__l2", new InMemoryPolicy { AbsoluteExpiration = expireAt });

            return new LayeredScopedCache(name, l1, l2);
        }
    }
}
