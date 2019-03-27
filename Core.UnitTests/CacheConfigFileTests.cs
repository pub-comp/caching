using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core.Config;
using PubComp.Caching.Core.Config.Loaders;
using PubComp.Caching.Core.UnitTests.Mocks;

namespace PubComp.Caching.Core.UnitTests
{
    [TestClass]
    public class CacheConfigFileTests
    {
        private CacheManagerInternals cacheManagerInternals;

        [TestInitialize]
        public void TestInitialize()
        {
            CacheManager.CacheManagerInternals = null;
            CacheManager.Settings = new CacheManagerSettings
                {ConfigLoader = new SystemConfigurationManagerCacheConfigLoader(), ShouldRegisterAllCaches = false};
            CacheManager.InitializeFromConfig();
            cacheManagerInternals = CacheManager.CacheManagerInternals;

        }

        [TestMethod]
        public void TestReadAppConfig()
        {
            var config = ConfigurationManager.GetSection("PubComp/CacheConfig") as IList<ConfigNode>;
            
            Assert.IsNotNull(config);

            Assert.AreEqual(15, config.Count);
            Assert.AreEqual(config.OfType<CacheConfig>().Count(), 4);
            Assert.AreEqual(config.OfType<RemoveConfig>().Count(), 5);
            Assert.AreEqual(config.OfType<ConnectionStringConfig>().Count(), 4);
            Assert.AreEqual(config.OfType<NotifierConfig>().Count(), 2);

            Assert.IsTrue(config.Any(c =>
                c.Action == ConfigAction.Add
                && c.Name == "cacheFromConfig1"
                && c is CacheConfig));

            Assert.IsTrue(config.Any(c =>
                c.Action == ConfigAction.Add
                && c.Name == "cacheFromConfig2"
                && c is NoCacheConfig));

            Assert.IsTrue(config.Any(c =>
                c.Action == ConfigAction.Add
                && c.Name == "cacheFromConfig2"
                && c is MockNoCacheConfig
                && ((MockNoCacheConfig)c).Policy != null
                && ((MockNoCacheConfig)c).Policy.SlidingExpiration.HasValue
                && ((MockNoCacheConfig)c).Policy.SlidingExpiration?.Minutes == 15
                && ((MockNoCacheConfig)c).Policy.AbsoluteExpiration.HasValue == false
                && ((MockNoCacheConfig)c).Policy.ExpirationFromAdd.HasValue == false));

            Assert.IsTrue(config.Any(c =>
                c.Action == ConfigAction.Remove
                && c.Name == "cacheFromConfig2"
                && c is RemoveConfig));

            Assert.IsTrue(config.Any(c =>
                c.Action == ConfigAction.Add
                && c.Name == "cacheFromConfig3"
                && c is CacheConfig
                && ((CacheConfig)c).CreateCache() is NoCache));

            Assert.IsTrue(config.Any(c =>
                c.Action == ConfigAction.Remove
                && c.Name == "cacheFromConfig3"
                && c is RemoveConfig));

            Assert.IsTrue(config.Any(c =>
                c.Action == ConfigAction.Remove
                && c.Name == "cacheFromConfig4"
                && c is RemoveConfig));
        }

        [TestMethod]
        public void TestCreateCacheFromCacheConfig_MockCacheConfig()
        {
            var config = new MockNoCacheConfig
            {
                Action = ConfigAction.Add,
                Name = "cacheName2",
                Policy = new MockCachePolicy
                {
                    SlidingExpiration = new TimeSpan(0, 20, 0),
                }
            };
            var cache = config.CreateCache();
            Assert.IsNotNull(cache);
            Assert.IsInstanceOfType(cache, typeof(MockNoCache));
            Assert.IsNotNull(((MockNoCache)cache).Policy);
            Assert.IsNotNull(((MockNoCache)cache).Policy.SlidingExpiration);
            Assert.AreEqual(new TimeSpan(0, 20, 0), ((MockNoCache)cache).Policy.SlidingExpiration);
        }

        [TestMethod]
        public void TestCreateCachesFromAppConfig()
        {
            var cache1 = cacheManagerInternals.GetCache("cacheFromConfig1");
            Assert.IsNotNull(cache1);
            Assert.IsInstanceOfType(cache1, typeof(NoCache));

            var cache2 = cacheManagerInternals.GetCache("cacheFromConfig2");
            Assert.IsNotNull(cache2);
            Assert.IsInstanceOfType(cache2, typeof(MockNoCache));
            Assert.IsNotNull(((MockNoCache)cache2).Policy);
            Assert.IsNotNull(((MockNoCache)cache2).Policy.SlidingExpiration);
            Assert.AreEqual(new TimeSpan(0, 15, 0), ((MockNoCache)cache2).Policy.SlidingExpiration);

            var cache3 = cacheManagerInternals.GetCache("cacheFromConfig3");
            Assert.IsNull(cache3);

            var cache4 = cacheManagerInternals.GetCache("cacheFromConfig4");
            Assert.IsNull(cache4);
        }

        [TestMethod]
        public void TestReadConnectionStringsFromConfig()
        {
            var connectionString0 = cacheManagerInternals.GetConnectionString("localRedisUrl");
            Assert.IsInstanceOfType(connectionString0, typeof(PlainConnectionString));
            Assert.AreEqual("localRedisUrl", connectionString0.Name);
            Assert.AreEqual("127.0.0.1:6379,serviceName=mymaster", connectionString0.ConnectionString);

            var connectionString1 = cacheManagerInternals.GetConnectionString("localRedisUrlEnc");
            Assert.IsInstanceOfType(connectionString1, typeof(UrlEncConnectionString));
            Assert.AreEqual("localRedisUrlEnc", connectionString1.Name);
            Assert.AreEqual("127.0.0.1:6379,serviceName=mymaster", connectionString1.ConnectionString);

            var connectionString2 = cacheManagerInternals.GetConnectionString("localRedisB64Enc");
            Assert.IsInstanceOfType(connectionString2, typeof(B64EncConnectionString));
            Assert.AreEqual("localRedisB64Enc", connectionString2.Name);
            Assert.AreEqual("127.0.0.1:6379,serviceName=mymaster,allowAdmin=true", connectionString2.ConnectionString);
        }

        [TestMethod]
        public void TestReadNotifierFromConfig()
        {
            var notifier = cacheManagerInternals.GetNotifier("noNotifier");
            Assert.IsInstanceOfType(notifier, typeof(NoNotifier));
            Assert.AreEqual("noNotifier", notifier.Name);
            Assert.AreEqual("127.0.0.1:6379,serviceName=mymaster,allowAdmin=true", GetFieldValue(notifier));
        }

        private static string GetFieldValue(object obj, string fieldName = "connectionString")
        {
            return GetFieldValue(obj.GetType(), obj, fieldName);
        }

        private static string GetFieldValue(Type type, object obj, string fieldName = "connectionString")
        {
            var field = type.GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (field == null && type != typeof(object))
                return GetFieldValue(type.BaseType, obj, fieldName);

            Assert.IsNotNull(field);
            Assert.AreEqual(typeof(string), field.FieldType);

            return field.GetValue(obj) as string;
        }
    }
}
