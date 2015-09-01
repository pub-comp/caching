using System.Collections.Generic;
using System.Web.Http;
using PubComp.Caching.Core;

namespace PubComp.Caching.WebApiExtended
{
    public class ExtendedCacheController : SimpleCacheController
    {
        /// <summary>
        /// Gets names of all registered cache instances
        /// </summary>
        [HttpGet]
        [Route("")]
        public IEnumerable<string> GetRegisteredCacheNames()
        {
            try
            {
                return this.Util.GetRegisteredCacheNames();
            }
            catch (CacheException ex)
            {
                Log.Warn(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Gets keys of all registered cache items a specific in a named cache instance
        /// </summary>
        /// <param name="cacheName"></param>
        [HttpGet]
        [Route("{cacheName}")]
        public IEnumerable<string> GetRegisteredCacheItemKeys(string cacheName)
        {
            try
            {
                return this.Util.GetRegisteredCacheItemKeys(cacheName);
            }
            catch (CacheException ex)
            {
                Log.Warn(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Refreshes a specific registered cache item in a named cache instance by key
        /// </summary>
        /// <param name="cacheName"></param>
        /// <param name="itemKey"></param>
        [HttpPut]
        [HttpGet]
        [Route("refresh/{cacheName}/{itemKey}")]
        public void RefreshCacheItem(string cacheName, string itemKey)
        {
            try
            {
                this.Util.RefreshCacheItem(cacheName, itemKey);
                Log.Info(string.Concat("Cache item refreshed: ", cacheName, '/', itemKey));
            }
            catch (CacheException ex)
            {
                Log.Warn(ex.Message);
                throw;
            }
        }
    }
}
