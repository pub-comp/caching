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
            var loadErrors = new CacheConfigLoadErrorsException();
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

                if (!assemblies.TryGetValue(assemblyName, out var assembly))
                {
                    try
                    {
                        assembly = Assembly.Load(assemblyName);
                    }
                    catch (Exception ex) when (ex is FileLoadException ||
                                               ex is FileNotFoundException ||
                                               ex is BadImageFormatException)
                    {
                        LogConfigError(loadErrors, $"Could not load assembly {assemblyName} for type {typeName}", ex);
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
                    LogConfigError(loadErrors, $"Could not load type {typeName} from assembly {assemblyName}");
                    continue;
                }

                var configNode = cacheConfig.Get(configType) as ConfigNode;
                configNode.Name = cacheConfig["Name"] ?? cacheConfig.Key; // Use explicit name if possible. Key is the fallback name. 
                configNodes.Add(configNode);
            }

            if (!loadErrors.IsEmpty())
                throw loadErrors;

            return configNodes;
        }

        // TODO: Use real log here - and throw a fatal exception instead of swallowing the missing assembly/type
        private void LogConfigError(
            CacheConfigLoadErrorsException loadErrors, string error, Exception ex = null)
        {
            System.Diagnostics.Debug.WriteLine(
                ex != null
                    ? $"{typeof(MicrosoftExtensionsCacheConfigLoader).FullName}: {error}, Exception: {ex}"
                    : $"{typeof(MicrosoftExtensionsCacheConfigLoader).FullName}: {error}");

            loadErrors.Add(
                new CacheConfigLoadErrorsException.CacheConfigLoadError
                {
                    Error = error,
                    Exception = ex
                });
        }
    }
}