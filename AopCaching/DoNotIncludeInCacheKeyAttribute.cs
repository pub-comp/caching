using System;

namespace PubComp.Caching.AopCaching
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class DoNotIncludeInCacheKeyAttribute : Attribute
    {
    }
}
