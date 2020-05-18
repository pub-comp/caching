using System;

namespace PubComp.Caching.Core
{
    public class CacheDetails : ICacheDetails
    {
        public string Name { get; set; }
        public string Type { get; set; }

        public bool IsActive { get; set; }
        public dynamic Policy { get; set; }

        public CacheDetails(ICache cache)
        {
            Name = cache?.Name ?? throw new ArgumentNullException(nameof(cache));
            Type = cache.GetType().Name;

            if (cache is ICacheV2 cacheV2)
            {
                IsActive = cacheV2.IsActive;
                Policy = cacheV2.GetDetails();
            }
            else
            {
                IsActive = true;
            }
        }
    }
}