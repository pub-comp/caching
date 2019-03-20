using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace PubComp.Caching.Core.Config.Loaders
{
    /// <summary>
    /// Load the cache configuration using .NetStandard's Microsoft.Extensions.Configuration
    /// </summary>
    public class MicrosoftExtensionsCacheConfigLoader : ICacheConfigLoader
    {
        private readonly IConfigurationSection pubCompCacheConfigurationSection;
        private readonly CacheConfigLoadErrorsException cacheConfigLoadErrorsException = new CacheConfigLoadErrorsException();

        public MicrosoftExtensionsCacheConfigLoader(IConfigurationSection pubCompCacheConfigurationSection)
        {
            this.pubCompCacheConfigurationSection = pubCompCacheConfigurationSection;
        }

        public MicrosoftExtensionsCacheConfigLoader(IConfiguration pubCompCacheConfiguration)
        : this(pubCompCacheConfiguration.GetSection("PubComp:CacheConfig"))
        {
        }

        /// <summary>
        /// Load the configuration data from the PubComp CacheConfig ConfigurationSource
        /// and return it as an ordered list of relevant ConfigNode types
        /// e.g.:
        /// "PubComp:CacheConfig:NoCache1:Assembly"->"PubComp.Caching.Core",
        /// "PubComp:CacheConfig:NoCache1:Type"->"NoCache",
        /// "PubComp:CacheConfig:YesCache2:Assembly"->"PubComp.Caching.Core",
        /// "PubComp:CacheConfig:YesCache2:Type"->"JustAnExampleCache",
        /// ...
        /// </summary>
        /// <returns>The list of Cache ConfigNode (and its inheriting classes)</returns>
        public IList<ConfigNode> LoadConfig()
        {
            var assemblies = new Dictionary<string, Assembly>();
            var configNodes = new List<ConfigNode>();
            var cacheConfigs = pubCompCacheConfigurationSection.GetChildren();

            foreach (var cacheConfig in cacheConfigs)
            {
                if (cacheConfig.GetValue<ConfigAction>("action") == ConfigAction.Remove)
                {
                    configNodes.Add(new RemoveConfig { Action = ConfigAction.Remove, Name = cacheConfig.Key });
                    continue;
                }
                var typeName = cacheConfig["type"] + "Config";
                var assemblyName = cacheConfig["assembly"];
                Assembly assembly;

                if (!assemblies.TryGetValue(assemblyName, out assembly))
                {
                    try
                    {
                        assembly = Assembly.Load(assemblyName);
                    }
                    catch (Exception ex) when (ex is FileLoadException ||
                                               ex is FileNotFoundException || // Was FileNotFoundException missing on purpose?
                                               ex is BadImageFormatException)
                    {
                        LogConfigError($"Could not load assembly {assemblyName}", ex);
                        continue;
                    }

                    assemblies.Add(assemblyName, assembly);
                }

                var configType = assembly.GetType(typeName, false, false);
                if (configType == null)
                {
                    configType = assembly.GetType(assemblyName + '.' + typeName, false, false);
                }

                if (configType == null)
                {
                    LogConfigError($"Could not load type {typeName} from assembly {assemblyName}");
                    continue;
                }

                var configNode = cacheConfig.Get(configType) as ConfigNode;
                configNode.Name = cacheConfig.Key;
                configNodes.Add(configNode);
            }

            if (!cacheConfigLoadErrorsException.IsEmpty())
                throw cacheConfigLoadErrorsException;

            return configNodes;
        }

        // TODO: Use real log here - and throw a fatal exception instead of swallowing the missing assembly/type
        private void LogConfigError(string error, Exception ex = null)
        {
            if (ex != null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"{typeof(MicrosoftExtensionsCacheConfigLoader).FullName}: {error}. Exception: {ex}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    $"{typeof(MicrosoftExtensionsCacheConfigLoader).FullName}: {error}");
            }

            cacheConfigLoadErrorsException.Add(new CacheConfigLoadErrorsException.CacheConfigLoadError
                {Error = error, Exception = ex});
        }
    }
}