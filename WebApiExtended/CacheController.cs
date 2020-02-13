using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using PubComp.Caching.Core;
using System.Collections.Generic;
using System.Web.Http;

namespace PubComp.Caching.WebApiExtended
{
    [RoutePrefix("api/cache/v1")]
    public class CacheController : ApiController
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
        /// Gets names and configuration of all registered cache instances
        /// </summary>
        [HttpGet]
        [Route("")]
        public object GetRegisteredCacheNames(bool includeConfig = false)
        {
            try
            {
                if (!includeConfig)
                    return this.Util.GetRegisteredCacheNames();

                var cacheList = new List<object>();
                foreach (var cacheName in this.Util.GetRegisteredCacheNames())
                {
                    var cache = CacheManager.GetCache(cacheName);
                    cacheList.Add(new CacheDetails(cache));
                }

                return JArray.FromObject(cacheList, JsonSerializer.CreateDefault(new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                }));
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
