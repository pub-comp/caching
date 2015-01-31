using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubComp.Caching.AopCaching
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Parameter)]
    public class DoNotIncludeInCacheKeyAttribute : Attribute
    {
    }
}
