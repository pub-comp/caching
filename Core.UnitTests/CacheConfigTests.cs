using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core.UnitTests.Mocks;

namespace PubComp.Caching.Core.UnitTests
{
    [TestClass]
    public class CacheConfigTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            CacheManager.Settings = null;
            CacheManager.CacheManagerInternals = null;
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
        public void TestCacheManagerSetGetRemoveApi()
        {
            var cm = new CacheManagerInternals(new CacheManagerSettings());

            CollectionAssert.AreEqual(new string[0], cm.GetCacheNames().ToList());
            CollectionAssert.AreEqual(new string[0], cm.GetNotifierNames().ToList());
            CollectionAssert.AreEqual(new string[0], cm.GetConnectionStringNames().ToList());

            var cs1 = new PlainConnectionString("cs1", string.Empty);
            var cs2 = new PlainConnectionString("cs2", string.Empty);
            var cs3 = new PlainConnectionString("cs3", string.Empty);
            cm.SetConnectionString("cs1", cs1);
            cm.SetConnectionString("cs2", cs2);
            cm.SetConnectionString("cs3", cs3);

            var n1 = new NoNotifier("n1", new NoNotifierPolicy { ConnectionString = "1.1.1.1" });
            var n2 = new NoNotifier("n2", new NoNotifierPolicy { ConnectionString = "2.2.2.2" });
            var n3 = new NoNotifier("n3", new NoNotifierPolicy { ConnectionString = "3.3.3.3" });
            cm.SetNotifier("n1", n1);
            cm.SetNotifier("n2", n2);
            cm.SetNotifier("n3", n3);

            var c1 = new NoCache("c1");
            var c2 = new NoCache("c2");
            var c3 = new NoCache("c3");
            cm.SetCache("c1", c1);
            cm.SetCache("c2", c2);
            cm.SetCache("c3", c3);

            cm.Associate(c1, n2);
            cm.Associate(c2, n1);
            cm.Associate(c3, n3);

            CollectionAssert.AreEqual(new[] { "cs1", "cs2", "cs3" }, cm.GetConnectionStringNames().OrderBy(_ => _).ToList());
            Assert.AreSame(cs1, cm.GetConnectionString("cs1"));
            Assert.AreSame(cs2, cm.GetConnectionString("cs2"));
            Assert.AreSame(cs3, cm.GetConnectionString("cs3"));

            CollectionAssert.AreEqual(new[] { "n1", "n2", "n3" }, cm.GetNotifierNames().OrderBy(_ => _).ToList());
            Assert.AreSame(n1, cm.GetNotifier("n1"));
            Assert.AreSame(n2, cm.GetNotifier("n2"));
            Assert.AreSame(n3, cm.GetNotifier("n3"));

            CollectionAssert.AreEqual(new[] { "c1", "c2", "c3" }, cm.GetCacheNames().OrderBy(_ => _).ToList());
            Assert.AreSame(c1, cm.GetCache("c1"));
            Assert.AreSame(c2, cm.GetCache("c2"));
            Assert.AreSame(c3, cm.GetCache("c3"));

            Assert.AreSame(n2, cm.GetAssociatedNotifier(c1));
            Assert.AreSame(n1, cm.GetAssociatedNotifier(c2));
            Assert.AreSame(n3, cm.GetAssociatedNotifier(c3));

            cm.RemoveAssociation(c3);
            Assert.IsNull(cm.GetAssociatedNotifier(c3));

            cm.RemoveCache("c3");
            CollectionAssert.AreEqual(new[] { "c1", "c2" }, cm.GetCacheNames().OrderBy(_ => _).ToList());
            Assert.IsNull(cm.GetCache("c3"));

            cm.RemoveNotifier("n3");
            CollectionAssert.AreEqual(new[] { "n1", "n2" }, cm.GetNotifierNames().OrderBy(_ => _).ToList());
            Assert.IsNull(cm.GetNotifier("n3"));

            cm.RemoveConnectionString("cs3");
            CollectionAssert.AreEqual(new[] { "cs1", "cs2" }, cm.GetConnectionStringNames().OrderBy(_ => _).ToList());
            Assert.IsNull(cm.GetConnectionString("cs3"));

            cm.RemoveAllNotifiers();
            cm.RemoveAllCaches();
            cm.RemoveAllNotifiers();
            cm.RemoveAllConnectionStrings();
            
            Assert.IsNull(cm.GetAssociatedNotifier(c2));
            Assert.IsNull(cm.GetAssociatedNotifier(c1));

            CollectionAssert.AreEqual(new string[0], cm.GetCacheNames().OrderBy(_ => _).ToList());
            Assert.IsNull(cm.GetCache("c2"));
            Assert.IsNull(cm.GetCache("c1"));

            CollectionAssert.AreEqual(new string[0], cm.GetNotifierNames().OrderBy(_ => _).ToList());
            Assert.IsNull(cm.GetNotifier("n2"));
            Assert.IsNull(cm.GetNotifier("n1"));

            CollectionAssert.AreEqual(new string[0], cm.GetConnectionStringNames().OrderBy(_ => _).ToList());
            Assert.IsNull(cm.GetConnectionString("cs2"));
            Assert.IsNull(cm.GetConnectionString("cs1"));
        }
    }
}
