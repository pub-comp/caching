using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace PubComp.Caching.Core
{
    public class CacheNotificationsConfigurationHandler : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {
            var configuration = new List<CacheNotificationsConfig>();

            foreach (XmlNode child in section.ChildNodes)
            {
                if (child.NodeType != XmlNodeType.Element)
                    continue;

                var actionName = child.Name;
                ConfigAction action;
                if (!Enum.TryParse<ConfigAction>(actionName, true, out action))
                    continue;

                var nameNode = child.Attributes["name"];
                if (nameNode == null || string.IsNullOrWhiteSpace(nameNode.Value))
                    continue;

                if (action == ConfigAction.Remove)
                {
                    continue;
                }

                var assemblyNode = child.Attributes["assembly"];
                if (assemblyNode == null || string.IsNullOrWhiteSpace(assemblyNode.Value))
                    continue;

                var typeNode = child.Attributes["type"];
                if (typeNode == null || string.IsNullOrWhiteSpace(typeNode.Value))
                    continue;

                var typeName = typeNode.Value + "Config";

                Assembly assembly;

                try
                {
                    assembly = Assembly.Load(assemblyNode.Value);
                }
                catch (System.IO.FileLoadException)
                {
                    continue;
                }
                catch (System.BadImageFormatException)
                {
                    continue;
                }

                var configType = assembly.GetType(typeName, false, false);
                if (configType == null)
                {
                    configType = assembly.GetType(assemblyNode.Value + "." + typeName, false, false);
                }
                if (configType.IsSubclassOf(typeof(CacheNotificationsConfig)) == false)
                    continue;

                var cacheConfig = configType.GetConstructor(new Type[0]).Invoke(new object[0]) as CacheNotificationsConfig;
                if (cacheConfig == null)
                    continue;

                cacheConfig.Action = action;

                foreach (XmlAttribute childAttrib in child.Attributes)
                {
                    if (childAttrib.NodeType != XmlNodeType.Attribute)
                        continue;

                    if (string.Compare(childAttrib.Name, "name", true) == 0)
                    {
                        cacheConfig.Name = childAttrib.Value;
                        continue;
                    }

                    var configProperty = configType.GetProperties()
                        .Where(p => string.Compare(childAttrib.Name, p.Name, true) == 0).FirstOrDefault();
                    if (configProperty == null || !configProperty.CanWrite)
                        continue;

                    object value;

                    try
                    {
                        value = Newtonsoft.Json.JsonConvert.DeserializeObject(childAttrib.Value, configProperty.PropertyType);
                    }
                    catch (Newtonsoft.Json.JsonSerializationException)
                    {
                        continue;
                    }

                    configProperty.SetValue(cacheConfig, value);
                }

                configuration.Add(cacheConfig);
            }

            return configuration;
        }
    }
}
