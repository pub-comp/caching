using System;
using System.Web.Http;
using Common.Logging;
using PubComp.Caching.Core;

namespace PubComp.Caching.WebApiExtended
{
    public class CacheController : ApiController
    {
        protected readonly ILog Logger = LogManager.GetLogger<CacheController>();

        /// <summary>
        /// Clears all data from a named cache instance
        /// </summary>
        /// <param name="cacheName"></param>
        [HttpPut]
        [HttpGet]
        [Route("clear/{cacheName}")]
        public void Clear(string cacheName)
        {
            if (string.IsNullOrEmpty(cacheName))
            {
                Logger.Warn("Cache not cleared due to empty requested cache name.");
            }

            var cache = CacheManager.GetCache(cacheName);

            if (cache == null)
            {
                Logger.Warn("Cache not defined: " + cacheName);
                return;
            }

            if (string.Equals(cache.Name, cacheName, StringComparison.InvariantCultureIgnoreCase))
            {
                cache.ClearAll();
                Logger.Info("Cache cleared: " + cacheName);
                return;
            }

            Logger.Warn("Cache not cleared due to fallback to general cache: " + cacheName);
        }

        /// <summary>
        /// Clears all data from a specific cache key in a named cache instance
        /// </summary>
        /// <param name="cacheName"></param>
        /// <param name="cacheKey"></param>
        [HttpPut]
        [HttpGet]
        [Route("clear/{cacheName}/{cacheKey}")]
        public void Clear(string cacheName, string cacheKey)
        {
            if (string.IsNullOrEmpty(cacheName))
            {
                Logger.Warn("Cache not cleared due to empty requested cache name.");
            }

            var cache = CacheManager.GetCache(cacheName);

            if (cache == null)
            {
                Logger.Warn("Cache not defined: " + cacheName);
                return;
            }

            cache.Clear(cacheKey);
            Logger.InfoFormat("Cache item cleared: {0} - {1}", cacheName, cacheKey);
        }
    }
}
