using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core.Config;
using PubComp.Caching.Core.Config.Loaders;
using PubComp.Caching.Core.UnitTests.Mocks;

namespace PubComp.Caching.Core.UnitTests
{
    [TestClass]
    public class CacheExplicitJsonConfigFileTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            CacheManager.CacheManagerLogic = null;

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("unittests_pubcompcachesettings.json", false, false)
                .Build();
            // The json loading is not part of the test, it's just a convenient helper.

            CacheManager.ConfigLoader = new MicrosoftExtensionsCacheConfigLoader(configuration);
        }

        [TestMethod]
        public void TestMECConfigLoader()
        {
            Assert.IsNotNull(CacheManager.CacheManagerLogic);
            Assert.IsInstanceOfType(CacheManager.CacheManagerLogic.ConfigLoader, typeof(MicrosoftExtensionsCacheConfigLoader));
        }

        [TestMethod]
        public void TestReadInMemConfig()
        {
            var cacheNames = CacheManager.GetCacheNames();
            var connectionStringNames = CacheManager.GetConnectionStringNames();
            var notifierNames = CacheManager.GetNotifierNames();

            Assert.AreEqual(2, cacheNames.Count());
            Assert.AreEqual(3, connectionStringNames.Count());
            Assert.AreEqual(1, notifierNames.Count());
        }

        [TestMethod]
        public void TestCreateCachesFromInMemConfig()
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

            var notifier1 = CacheManager.GetNotifier("noNotifier");
            Assert.IsNotNull(notifier1);
            Assert.IsInstanceOfType(notifier1, typeof(NoNotifier));

            var connectionString1 = CacheManager.GetConnectionString("localRedisB64Enc");
            Assert.IsNotNull(connectionString1);
            Assert.IsInstanceOfType(connectionString1, typeof(B64EncConnectionString));
            Assert.AreEqual("127.0.0.1:6379,serviceName=mymaster,allowAdmin=true",
                ((B64EncConnectionString) connectionString1).ConnectionString);
        }
    }
}
