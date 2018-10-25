using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NLog;
using PubComp.Caching.Core;

namespace PubComp.Caching.WebApiExtended.Net.Core
{
    [Route("api/cache/v1")]
    public class CacheController : Controller
    {
        protected readonly CacheControllerUtil Util;
        protected readonly ILogger Log = LogManager.GetLogger(typeof(CacheController).FullName);

        public CacheController()
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
