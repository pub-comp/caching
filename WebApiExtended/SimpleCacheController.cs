using System.Web.Http;
using Common.Logging;
using PubComp.Caching.Core;

namespace PubComp.Caching.WebApiExtended
{
    public class SimpleCacheController : ApiController
    {
        protected readonly CacheControllerUtil Util;
        protected readonly ILog Log = LogManager.GetLogger<SimpleCacheController>();

        public SimpleCacheController()
        {
            this.Util = new CacheControllerUtil(msg => Log.Info(msg), msg => Log.Warn(msg));
        }

        /// <summary>
        /// Clears all data from a named cache instance
        /// </summary>
        /// <param name="cacheName"></param>
        [HttpPut]
        [HttpGet]
        [Route("clear/{cacheName}")]
        public void Clear(string cacheName)
        {
            this.Util.ClearCache(cacheName);
        }

        /// <summary>
        /// Clears a specific cache item in a named cache instance by key
        /// </summary>
        /// <param name="cacheName"></param>
        /// <param name="itemKey"></param>
        [HttpPut]
        [HttpGet]
        [Route("clear/{cacheName}/{itemKey}")]
        public void Clear(string cacheName, string itemKey)
        {
            this.Util.ClearCacheItem(cacheName, itemKey);
        }
    }
}
