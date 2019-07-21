using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.AopCaching.UnitTests.Mocks;
using PubComp.Caching.Core;
using PubComp.Caching.Core.Config.Loaders;

namespace PubComp.Caching.AopCaching.UnitTests
{
    [TestClass]
    public class AopTestsAssemblyInitializer
    {
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            // 2 cache definitions are from the in-memory configuration and 2 are set programmatically.
            // The reason MicrosoftExtensionsCacheConfigLoader is used instead of a mock is to verify
            // the "name*" convention is working properly with the RL implementation of the loader

            var cb = new ConfigurationBuilder()
                .AddInMemoryCollection(CacheConfiguration())
                .Build();

            CacheManager.Settings = new CacheManagerSettings
            {
                ConfigLoader = new MicrosoftExtensionsCacheConfigLoader(cb)
            };
            CacheManager.InitializeFromConfig();

            const string cache1Name = "PubComp.Caching.AopCaching.UnitTests.Mocks.Service*";
            var cache1 = new MockCache(cache1Name);
            CacheManager.SetCache(cache1.Name, cache1);

            const string cache2Name = "localCache";
            var cache2 = new MockCache(cache2Name);
            CacheManager.SetCache(cache2.Name, cache2);

            //const string cache3Name = "PubComp.Caching.AopCaching.UnitTests.Mocks.Generic*";
            //var cache3 = new MockCache(cache3Name);
            //CacheManager.SetCache(cache3.Name, cache3);

            //const string cacheMulti1Name = "PubComp.Caching.AopCaching.UnitTests.Mocks.*";
            //var cacheMulti1 = new MockCache(cacheMulti1Name);
            //CacheManager.SetCache(cacheMulti1.Name, cacheMulti1);
        }

        private static Dictionary<string, string> CacheConfiguration()
        {
            var result = new Dictionary<string, string>
            {
                {"PubComp:CacheConfig:PubComp.Caching.AopCaching.UnitTests.Mocks.Generic*:Assembly", "PubComp.Caching.AopCaching.UnitTests"},
                {"PubComp:CacheConfig:PubComp.Caching.AopCaching.UnitTests.Mocks.Generic*:Type", "PubComp.Caching.AopCaching.UnitTests.Mocks.MockCache"},
                {"PubComp:CacheConfig:PubComp.Caching.AopCaching.UnitTests.Mocks.*:Assembly", "PubComp.Caching.AopCaching.UnitTests"},
                {"PubComp:CacheConfig:PubComp.Caching.AopCaching.UnitTests.Mocks.*:Type", "PubComp.Caching.AopCaching.UnitTests.Mocks.MockCache"},
            };
            
            return result;
        }
    }
}