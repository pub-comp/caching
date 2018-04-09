using System;

namespace PubComp.Caching.Core.Attributes
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
    public class DoNotIncludeInCacheKeyAttribute : Attribute
    {
    }
}
