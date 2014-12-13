using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core.UnitTests.Mocks;
using PubComp.Testing.TestingUtils;

namespace PubComp.Caching.Core.UnitTests
{
    [TestClass]
    public class CacheConfigFileTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            CacheManager.RemoveAllCaches();
        }

        [TestMethod]
        public void TestReadAppConfig()
        {
            var config = ConfigurationManager.GetSection("PubComp/CacheConfig") as IList<CacheConfig>;
            
            Assert.IsNotNull(config);
            Assert.AreEqual(7, config.Count);

            LinqAssert.Any(config, c =>
                c.Action == ConfigAction.Add
                && c.Name == "cacheFromConfig1"
                && c is NoCacheConfig);

            LinqAssert.Any(config, c =>
                c.Action == ConfigAction.Add
                && c.Name == "cacheFromConfig2"
                && c is NoCacheConfig);

            LinqAssert.Any(config, c =>
                c.Action == ConfigAction.Remove
                && c.Name == "cacheFromConfig2"
                && c is NoCacheConfig);

            LinqAssert.Any(config, c =>
                c.Action == ConfigAction.Add
                && c.Name == "cacheFromConfig2"
                && c is MockCacheConfig
                && ((MockCacheConfig)c).Policy != null
                && ((MockCacheConfig)c).Policy.SlidingExpiration != null
                && ((MockCacheConfig)c).Policy.SlidingExpiration.Minutes == 15);

            LinqAssert.Any(config, c =>
                c.Action == ConfigAction.Add
                && c.Name == "cacheFromConfig3"
                && c is NoCacheConfig);

            LinqAssert.Any(config, c =>
                c.Action == ConfigAction.Remove
                && c.Name == "cacheFromConfig3"
                && c is NoCacheConfig);

            LinqAssert.Any(config, c =>
                c.Action == ConfigAction.Remove
                && c.Name == "cacheFromConfig4"
                && c is NoCacheConfig);
        }

        [TestMethod]
        public void TestCreateCacheFromCacheConfig_NoCacheConfig()
        {
            var config = new NoCacheConfig { Action = ConfigAction.Add, Name = "cacheName1" };
            var cache = config.CreateCache();
            Assert.IsNotNull(cache);
            Assert.IsInstanceOfType(cache, typeof(NoCache));
        }

        [TestMethod]
        public void TestCreateCacheFromCacheConfig_MockCacheConfig()
        {
            var config = new MockCacheConfig
            {
                Action = ConfigAction.Add,
                Name = "cacheName2",
                Policy = new System.Runtime.Caching.CacheItemPolicy
                {
                    SlidingExpiration = new TimeSpan(0, 20, 0),
                }
            };
            var cache = config.CreateCache();
            Assert.IsNotNull(cache);
            Assert.IsInstanceOfType(cache, typeof(MockCache));
            Assert.IsNotNull(((MockCache)cache).Policy);
            Assert.IsNotNull(((MockCache)cache).Policy.SlidingExpiration);
            Assert.AreEqual(new TimeSpan(0, 20, 0), ((MockCache)cache).Policy.SlidingExpiration);
        }

        [TestMethod]
        public void TestCreateCachesFromAppConfig()
        {
            CacheManager.InitializeFromConfig();

            var cache1 = CacheManager.GetCache("cacheFromConfig1");
            Assert.IsNotNull(cache1);
            Assert.IsInstanceOfType(cache1, typeof(NoCache));

            var cache2 = CacheManager.GetCache("cacheFromConfig2");
            Assert.IsNotNull(cache2);
            Assert.IsInstanceOfType(cache2, typeof(MockCache));
            Assert.IsNotNull(((MockCache)cache2).Policy);
            Assert.IsNotNull(((MockCache)cache2).Policy.SlidingExpiration);
            Assert.AreEqual(new TimeSpan(0, 15, 0), ((MockCache)cache2).Policy.SlidingExpiration);

            var cache3 = CacheManager.GetCache("cacheFromConfig3");
            Assert.IsNull(cache3);

            var cache4 = CacheManager.GetCache("cacheFromConfig4");
            Assert.IsNull(cache4);
        }
    }
}
