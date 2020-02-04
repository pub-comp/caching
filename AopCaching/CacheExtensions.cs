using PubComp.Caching.Core;

namespace PubComp.Caching.AopCaching
{
    public static class CacheExtensions
    {
        public static bool IsUseable(this ICache cacheToCheck)
        {
            if (cacheToCheck == null)
                return false;

            if (cacheToCheck is IScopedCache scopedCache)
                return scopedCache.IsActive;

            return true;
        }

    }
}