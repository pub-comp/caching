using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core.UnitTests.Mocks;
using System;

namespace PubComp.Caching.Core.UnitTests
{
    [TestClass]
    public class LayeredScopedCache_LayeredCacheBaseTests : LayeredCacheBaseTests
    {
        protected override IMockCache GetMockCache(string name)
        {
            return new MockMemScopedCache(name);
        }

        protected override ICache GetLayeredCache(string name, ICache level1, ICache level2)
        {
            return new LayeredScopedCache(name, level1, level2);
        }

        protected override ICache GetLayeredCache(string name, string level1CacheName, string level2CacheName)
        {
            return new LayeredScopedCache(name, level1CacheName, level2CacheName);
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestLevel1NotScopedCache()
        {
            GetLayeredCache("cache0", new MockMemCache("l1"), GetMockCache("l2"));
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public void TestLevel2NotScopedCache()
        {
            GetLayeredCache("cache0", GetMockCache("l1"), new MockMemCache("l2"));
        }

    }
}
