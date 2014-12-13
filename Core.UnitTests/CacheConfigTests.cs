using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PubComp.Caching.Core.UnitTests
{
    [TestClass]
    public class CacheConfigTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            CacheManager.RemoveAllCaches();
        }

        [TestMethod]
        public void TestNoCacheFound2()
        {
            CacheManager.SetCache("1", new NoCache("1"));
            var cache = CacheManager.GetCache("noCacheFound");
            Assert.IsNull(cache);
        }

        [TestMethod]
        public void TestNoCacheFound3()
        {
            CacheManager.SetCache("noCache", new NoCache("noCache"));
            var cache = CacheManager.GetCache("noCacheFound");
            Assert.IsNull(cache);
        }

        [TestMethod]
        public void TestGetCacheByName()
        {
            const string cacheName = "cache1";
            CacheManager.SetCache(cacheName, new NoCache(cacheName));
            var cache = CacheManager.GetCache(cacheName);
            Assert.IsNotNull(cache);
            Assert.AreEqual(cacheName, cache.Name);
        }

        [TestMethod]
        public void TestGetCacheByNamespace1()
        {
            var cacheName = "cache123*";
            var requestedName = "cache1234";

            CacheManager.SetCache(cacheName, new NoCache(cacheName));
            var cache = CacheManager.GetCache(requestedName);
            Assert.IsNotNull(cache);
            Assert.AreEqual(cacheName, cache.Name);
        }

        [TestMethod]
        public void TestGetCacheByNamespace2()
        {
            var cacheName = this.GetType().Namespace + ".*";
            var requestedName = this.GetType().FullName;
            
            CacheManager.SetCache(cacheName, new NoCache(cacheName));
            var cache = CacheManager.GetCache(requestedName);
            Assert.IsNotNull(cache);
            Assert.AreEqual(cacheName, cache.Name);
        }

        [TestMethod]
        public void TestGetCacheByNamespace3()
        {
            var cacheName = this.GetType().Namespace + ".*";

            CacheManager.SetCache(cacheName, new NoCache(cacheName));
            var cache = CacheManager.GetCache(this.GetType());
            Assert.IsNotNull(cache);
            Assert.AreEqual(cacheName, cache.Name);
        }

        [TestMethod]
        public void TestGetCacheByNamespace4()
        {
            var cacheName = this.GetType().Namespace + ".*";

            CacheManager.SetCache(cacheName, new NoCache(cacheName));
            var cache = CacheManager.GetCache<CacheManagerTests>();
            Assert.IsNotNull(cache);
            Assert.AreEqual(cacheName, cache.Name);
        }

        [TestMethod]
        public void TestGetCacheByNamespace5()
        {
            var cacheName = this.GetType().Namespace + ".*";

            CacheManager.SetCache(cacheName, new NoCache(cacheName));
            var cache = CacheManager.GetCurrentClassCache();
            Assert.IsNotNull(cache);
            Assert.AreEqual(cacheName, cache.Name);
        }
    }
}
