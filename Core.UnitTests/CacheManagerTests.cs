using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PubComp.Caching.Core.UnitTests
{
    [TestClass]
    public class CacheManagerTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            CacheManager.RemoveAllCaches();
        }

        [TestMethod]
        public void TestNoCacheFound1()
        {
            var cache = CacheManager.GetCache("noCacheFound");
            Assert.IsNull(cache);
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

        [TestMethod]
        public void TestGetCacheByNamespace6()
        {
            var cacheName1 = "cache567*";
            var cacheName2 = "cache56789*";
            
            var requestedName1 = "cache567";
            var requestedName2 = "cache5678";
            var requestedName3 = "cache56789";
            var requestedName4 = "cache567890";

            CacheManager.SetCache(cacheName1, new NoCache(cacheName1));
            CacheManager.SetCache(cacheName2, new NoCache(cacheName2));

            var cache1 = CacheManager.GetCache(requestedName1);
            Assert.IsNotNull(cache1);
            Assert.AreEqual(cacheName1, cache1.Name);

            var cache2 = CacheManager.GetCache(requestedName2);
            Assert.IsNotNull(cache1);
            Assert.AreEqual(cacheName1, cache2.Name);

            var cache3 = CacheManager.GetCache(requestedName3);
            Assert.IsNotNull(cache3);
            Assert.AreEqual(cacheName2, cache3.Name);

            var cache4 = CacheManager.GetCache(requestedName4);
            Assert.IsNotNull(cache4);
            Assert.AreEqual(cacheName2, cache4.Name);
        }

        [TestMethod]
        public void TestGetCacheByNamespace7()
        {
            var cacheName1 = "cache567*";
            var cacheName2 = "cache56789*";

            var requestedName1 = "cache567";
            var requestedName2 = "cache5678";
            var requestedName3 = "cache56789";
            var requestedName4 = "cache567890";

            CacheManager.SetCache(cacheName1, new NoCache(cacheName1));
            CacheManager.SetCache(cacheName2, new NoCache(cacheName2));
            CacheManager.SetCache(cacheName2, null);

            var cache1 = CacheManager.GetCache(requestedName1);
            Assert.IsNotNull(cache1);
            Assert.AreEqual(cacheName1, cache1.Name);

            var cache2 = CacheManager.GetCache(requestedName2);
            Assert.IsNotNull(cache1);
            Assert.AreEqual(cacheName1, cache2.Name);

            var cache3 = CacheManager.GetCache(requestedName3);
            Assert.IsNotNull(cache3);
            Assert.AreEqual(cacheName1, cache3.Name);

            var cache4 = CacheManager.GetCache(requestedName4);
            Assert.IsNotNull(cache4);
            Assert.AreEqual(cacheName1, cache4.Name);
        }
    }
}
