using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core.Config;
using PubComp.Caching.Core.UnitTests.Mocks;

namespace PubComp.Caching.Core.UnitTests
{
    [TestClass]
    public class CacheImplicitAppConfigFileTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            CacheManager.CacheManagerLogic = null;
        }

        [TestMethod]
        public void TestReadAppConfig()
        {
            var cacheNames = CacheManager.GetCacheNames();
            var connectionStringNames = CacheManager.GetConnectionStringNames();
            var notifierNames = CacheManager.GetNotifierNames();

            Assert.AreEqual(2, cacheNames.Count());
            Assert.AreEqual(3, connectionStringNames.Count());
            Assert.AreEqual(1, notifierNames.Count());
        }

        [TestMethod]
        public void TestCreateCachesFromAppConfig()
        {
            var cache1 = CacheManager.GetCache("cacheFromConfig1");
            Assert.IsNotNull(cache1);
            Assert.IsInstanceOfType(cache1, typeof(NoCache));

            var cache2 = CacheManager.GetCache("cacheFromConfig2");
            Assert.IsNotNull(cache2);
            Assert.IsInstanceOfType(cache2, typeof(MockNoCache));
            Assert.IsNotNull(((MockNoCache)cache2).Policy);
            Assert.IsNotNull(((MockNoCache)cache2).Policy.SlidingExpiration);
            Assert.AreEqual(new TimeSpan(0, 15, 0), ((MockNoCache)cache2).Policy.SlidingExpiration);

            var cache3 = CacheManager.GetCache("cacheFromConfig3");
            Assert.IsNull(cache3);

            var cache4 = CacheManager.GetCache("cacheFromConfig4");
            Assert.IsNull(cache4);
        }
    }
}
