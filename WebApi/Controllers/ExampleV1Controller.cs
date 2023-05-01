using PubComp.Caching.Core;
using System.Threading.Tasks;
using System.Web.Http;
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
