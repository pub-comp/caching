using System;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using PubComp.Caching.Core;
using System.Collections.Generic;

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

        /// <summary>
        /// Clears all data from a named cache instance only once, by owner identifier {clientId + batchId}
        /// </summary>
        /// <param name="cacheName"></param>
        /// <param name="clientId"></param>
        /// <param name="batchId"></param>
        [HttpPut]
        [HttpGet] 
        [Route("clear/{cacheName}/client/{clientId}/batch/{batchId}")]
        public void Clear(string cacheName, string clientId, string batchId)
        {
            try
            {
                var isCleared = this.Util.TryClearCache(cacheName, clientId, batchId);
                Log.Info($"Cache cleared: {isCleared} cacheName: {cacheName}, clientId: {clientId}, batchId: {batchId}");
            }
            catch (CacheException ex)
            {
                Log.Warn(ex.Message);
                throw;
            }
        }
    }
}
