using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PubComp.Caching.Core
{
    public class JsonIgnorePropertiesContractResolver : DefaultContractResolver
    {
        public Type[] AttributeTypes { get; }

        public JsonIgnorePropertiesContractResolver(params Type[] attributeTypes)
        {
            if (attributeTypes == null || attributeTypes.Length == 0)
                throw new ArgumentException($"{nameof(attributeTypes)} must contain at least one value");

            if (attributeTypes.Any(t => !typeof(Attribute).IsAssignableFrom(t)))
                throw new ArgumentException($"{nameof(attributeTypes)} must contain only {nameof(Attribute)}s");

            AttributeTypes = attributeTypes;
        }

        protected override JsonProperty CreateProperty(
            MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            var doNotSerialize = member.CustomAttributes.Any(attr =>
                AttributeTypes.Contains(attr.AttributeType));

            if (doNotSerialize)
            {
                property.ShouldSerialize = obj => false;
            }

            return property;
        }
    }
}
