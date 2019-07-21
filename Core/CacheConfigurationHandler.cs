using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using PubComp.Caching.Core.Config;
using PubComp.Caching.Core.Config.Loaders;

namespace PubComp.Caching.Core
{
    public class CacheConfigurationHandler : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {
            var loadErrors = new CacheConfigLoadErrorsException();

            var assemblies = new Dictionary<string, Assembly>();
            var configuration = new List<ConfigNode>();

            foreach (XmlNode child in section.ChildNodes)
            {
                if (child.NodeType != XmlNodeType.Element)
                    continue;

                var actionName = child.Name;

                if (!Enum.TryParse(actionName, true, out ConfigAction action))
                {
                    LogConfigError(loadErrors, $"Unrecognized {typeof(ConfigAction).FullName}: {child.Name}");
                    continue;
                }

                var nameNode = child.Attributes?["name"];
                if (string.IsNullOrWhiteSpace(nameNode?.Value))
                {
                    LogConfigError(loadErrors, "Attribute 'name' is missing for one or more config nodes");
                    continue;
                }

                if (action == ConfigAction.Remove)
                {
                    configuration.Add(new RemoveConfig { Action = ConfigAction.Remove, Name = nameNode.Value });
                    continue;
                }

                var assemblyNode = child.Attributes["assembly"];
                if (string.IsNullOrWhiteSpace(assemblyNode?.Value))
                {
                    LogConfigError(loadErrors, $"Attribute 'assembly' is missing for {child.Name}");
                    continue;
                }

                var typeNode = child.Attributes["type"];
                if (string.IsNullOrWhiteSpace(typeNode?.Value))
                {
                    LogConfigError(loadErrors, $"Attribute 'type' is missing for {child.Name}");
                    continue;
                }

                var typeName = typeNode.Value + "Config";

                if (!assemblies.TryGetValue(assemblyNode.Value, out var assembly))
                {
                    try
                    {
                        assembly = Assembly.Load(assemblyNode.Value);
                    }
                    catch (Exception ex) when (ex is FileLoadException
                                               || ex is FileNotFoundException
                                               || ex is BadImageFormatException)
                    {
                        LogConfigError(loadErrors, $"Could not load assembly {assemblyNode.Value}", ex);
                        continue;
                    }

                    assemblies.Add(assemblyNode.Value, assembly);
                }

                var configType = assembly.GetType(typeName, false, false);
                if (configType == null)
                {
                    configType = assembly.GetType(assemblyNode.Value + '.' + typeName, false, false);
                }

                if (configType == null)
                {
                    LogConfigError(loadErrors, $"Could not load type {typeName} from assembly {assemblyNode.Value}");
                    continue;
                }

                if (configType.IsSubclassOf(typeof(ConfigNode)) == false)
                {
                    LogConfigError(loadErrors, $"{configType.FullName} is not a sub class of {typeof(ConfigNode).FullName}");
                    continue;
                }

                var node = configType.GetConstructor(new Type[0])?.Invoke(new object[0]);
                if (node == null)
                {
                    LogConfigError(loadErrors, $"Failed to create an instance of type {configType.FullName} using default constructor");
                    continue;
                }

                if (!(node is ConfigNode configNode))
                {
                    LogConfigError(loadErrors, $"{node.GetType().FullName} is not a sub class of {typeof(ConfigNode).FullName}");
                    continue;
                }

                ApplyConfig(configuration, child.Attributes, configType, action, configNode, loadErrors);
            }

            if (!loadErrors.IsEmpty())
                throw loadErrors;

            return configuration;
        }

        private void ApplyConfig(
            List<ConfigNode> configuration, XmlAttributeCollection attributes,
            Type configType, ConfigAction action, ConfigNode cacheConfig,
            CacheConfigLoadErrorsException loadErrors)
        {
            cacheConfig.Action = action;

            // ReSharper disable once PossibleNullReferenceException
            foreach (XmlAttribute childAttrib in attributes)
            {
                if (childAttrib.NodeType != XmlNodeType.Attribute)
                    continue;

                if (string.Compare(childAttrib.Name, "name", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    cacheConfig.Name = childAttrib.Value;
                    continue;
                }

                if (string.Compare(childAttrib.Name, "type", StringComparison.InvariantCultureIgnoreCase) == 0
                    || string.Compare(childAttrib.Name, "assembly", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    continue;
                }

                var configProperty = configType.GetProperties()
                    .FirstOrDefault(p =>
                        string.Compare(childAttrib.Name, p.Name, StringComparison.InvariantCultureIgnoreCase) == 0);

                if (configProperty == null || !configProperty.CanWrite)
                {
                    LogConfigError(loadErrors, $"No writable property named {childAttrib.Name} found on type {configType.FullName}");
                    continue;
                }

                object value;
                try
                {
                    if (configProperty.PropertyType == typeof(string))
                    {
                        value = childAttrib.Value;
                    }
                    else
                    {
                        value = Newtonsoft.Json.JsonConvert.DeserializeObject(
                            childAttrib.Value, configProperty.PropertyType);
                    }
                }
                catch (Newtonsoft.Json.JsonSerializationException ex)
                {
                    LogConfigError(loadErrors, $"Could not deserialize {childAttrib.Value} to {configProperty.PropertyType.FullName}", ex);
                    continue;
                }

                configProperty.SetValue(cacheConfig, value);
            }

            configuration.Add(cacheConfig);
        }

        // TODO: Use real log here - and throw a fatal exception instead of swallowing the missing assembly/type
        private void LogConfigError(
            CacheConfigLoadErrorsException loadErrors, string error, Exception ex = null)
        {
            System.Diagnostics.Debug.WriteLine(
                ex != null
                    ? $"{typeof(CacheConfigurationHandler).FullName}: {error}, Exception: {ex}"
                    : $"{typeof(CacheConfigurationHandler).FullName}: {error}");

            loadErrors.Add(
                new CacheConfigLoadErrorsException.CacheConfigLoadError
                {
                    Error = error, Exception = ex
                });
        }
    }
}
