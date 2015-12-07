using System.Web.Http;
using Common.Logging;
using PubComp.Caching.Core;

namespace PubComp.Caching.WebApiExtended
{
    [RoutePrefix("api/cache/v1")]
    public class SimpleCacheController : ApiController
    {
        protected readonly CacheControllerUtil Util;
        protected readonly ILog Log = LogManager.GetLogger<SimpleCacheController>();

        public SimpleCacheController()
        {
            this.Util = new CacheControllerUtil();
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
            try
            {
                this.Util.ClearCache(cacheName);
                Log.Info("Cache cleared: " + cacheName);
            }
            catch (CacheException ex)
            {
                Log.Warn(ex.Message);
                throw;
            }
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
            try
            {
                this.Util.ClearCacheItem(cacheName, itemKey);
                Log.Info(string.Concat("Cache item cleared: ", cacheName, '/', itemKey));
            }
            catch (CacheException ex)
            {
                Log.Warn(ex.Message);
                throw;
            }
        }
    }
}
