using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Web.Http;
using PubComp.Caching.Core;
using PubComp.Caching.Core.Notifications;
using TestHost.WebApi.Service;

namespace TestHost.WebApi.Controllers
{
    /// <summary>
    /// Example
    /// </summary>
    [RoutePrefix("api/example/v1")]
    public class ExampleV1Controller : ApiController
    {
        private readonly ExampleService exampleService;

        /// <summary>
        /// Constructor
        /// </summary>
        public ExampleV1Controller()
        {
            this.exampleService = new ExampleService();
        }

        /// <summary>
        /// Gets ...
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <response code="200">OK</response>
        /// <response code= "404">Not Found</response>
        [HttpGet]
        [Route("{id}")]
        public IHttpActionResult Get(int id)
        {
            var result = exampleService.Get(id);
            if(result!=null)
                return Ok(result);
            return NotFound();
        }

        [HttpGet]
        [Route("cache/Numbers/syncclear")]
        public IHttpActionResult InMemorySyncClear()
        {
            var cacheName = "Numbers";

            var cacheManager = CacheManager.GetCache(cacheName);
            var cacheNotifier = CacheManager.GetAssociatedNotifier(cacheManager);

            cacheNotifier.Publish(cacheName, null, CacheItemActionTypes.RemoveAll);

            var notifiers = CacheManager.GetNotifierNames().ToList();
            return Ok(notifiers);
        }

        [HttpGet]
        [Route("cache/redis/timestamp")]
        public IHttpActionResult GetRedisCacheTimestamp()
        {
            var cacheName = "RedisTest";
            var cacheKey = "redis-timestamp";

            var cacheManager = CacheManager.GetCache(cacheName);

            if (!cacheManager.TryGet(cacheKey, out DateTime timestamp))
            {
                timestamp = DateTime.Now;
                cacheManager.Set(cacheKey, timestamp);
            }

            return Ok(timestamp);
        }

        /// <summary>
        /// Gets async...
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <response code="200">OK</response>
        /// <response code= "404">Not Found</response>
        [HttpGet]
        [Route("async/{id}")]
        public async Task<IHttpActionResult> GetAsync(int id)
        {
            var result = await exampleService.GetAsync(id).ConfigureAwait(false);
            if(result!=null)
                return Ok(result);
            return NotFound();
        }

        /// <summary>
        /// Clears the cache ...
        /// </summary>
        /// <returns></returns>
        /// <response code="200">OK</response>
        [HttpDelete]
        [Route("cache/")]
        public IHttpActionResult ClearCache()
        {
            new CacheControllerUtil().ClearCache(ExampleService.CacheName);
            return Ok();
        }
    }
}
