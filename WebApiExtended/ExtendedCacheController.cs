using System.Web.Http;

namespace PubComp.Caching.WebApiExtended
{
    public class ExtendedCacheController : SimpleCacheController
    {
        /// <summary>
        /// Gets names of all registered cache instances
        /// </summary>
        [HttpGet]
        [Route("")]
        public void GetRegisteredCacheNames()
        {
            this.Util.GetRegisteredCacheNames();
        }

        /// <summary>
        /// Gets keys of all registered cache items a specific in a named cache instance
        /// </summary>
        /// <param name="cacheName"></param>
        [HttpGet]
        [Route("{cacheName}")]
        public void GetRegisteredCacheNames(string cacheName)
        {
            this.Util.GetRegisteredCacheItemKeys(cacheName);
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
            this.Util.RefreshCacheItem(cacheName, itemKey);
        }
    }
}
