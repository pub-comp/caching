using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace PubComp.Caching.Core.Config.Loaders
{
    /// <summary>
    /// Load the cache configuration using .NetStandard's Microsoft.Extenstions.Configuration
    /// </summary>
    public class MicrosoftExtensionsCacheConfigLoader : ICacheConfigLoader
    {
        private readonly Dictionary<string, Assembly> _assemblies = new Dictionary<string, Assembly>();

        private readonly IConfigurationSection pubCompCacheConfigurationSection;

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
        /// "PubComp:CacheConfig:0:Action"->"Add",
        /// "PubComp:CacheConfig:0:Name"->"NoCache1",
        /// "PubComp:CacheConfig:0:Assembly"->"PubComp.Caching.Core",
        /// "PubComp:CacheConfig:0:Type"->"NoCache",
        /// "PubComp:CacheConfig:1:Name"->"NoCache2",
        /// ...
        /// </summary>
        /// <returns>The list of Cache ConfigNode (and its inheriting classes)</returns>
        public IList<ConfigNode> LoadConfig()
        {
            var configNodes = new List<ConfigNode>();
            var cacheConfigs = pubCompCacheConfigurationSection.GetChildren();

            foreach (var cacheConfig in cacheConfigs)
            {
                if (cacheConfig.GetValue<ConfigAction>("action") == ConfigAction.Remove)
                {
                    configNodes.Add(new RemoveConfig { Action = ConfigAction.Remove, Name = cacheConfig["name"] });
                    continue;
                }

                var typeName = cacheConfig["type"] + "Config";
                var assemblyName = cacheConfig["assembly"];
                Assembly assembly;

                if (!_assemblies.TryGetValue(assemblyName, out assembly))
                {
                    try
                    {
                        assembly = Assembly.Load(assemblyName);
                    }
                    catch (Exception ex) when (ex is FileLoadException ||
                                               ex is FileNotFoundException ||
                                               ex is BadImageFormatException)
                    {
                        LogConfigError($"Could not load assembly {assemblyName}", ex);
                        continue;
                    }

                    _assemblies.Add(assemblyName, assembly);
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

                configNodes.Add(cacheConfig.Get(configType) as ConfigNode);
            }

            return configNodes;
        }

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
        }
    }
}