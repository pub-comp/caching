using System;

namespace PubComp.Caching.Core
{
    [Flags]
    public enum CacheMethod
    {
        None = 0b_0000_0000,// 0
        Set = 0b_0000_0001, // 1
        Get = 0b_0000_0010, // 2
        IgnoreMinimumValueTimestamp = 0b_0001_0000, // 16
        GetOrSet = Get | Set // 3
    }
}