using PubComp.Caching.Core;
using PubComp.Caching.WebApiExtended;
using System.Threading.Tasks;
using System.Web.Http;
using Swashbuckle.Swagger.Annotations;
using TestHost.WebApi.Service;

namespace TestHost.WebApi.Controllers
{
    /// <summary>
    /// Example
    /// </summary>
    [RoutePrefix("api/example/v2")]
    [SwaggerOperationFilter(typeof(CacheableSwaggerOperationalFilter))]
    public class ExampleV2Controller : ApiController
    {
        private readonly ExampleV2Service exampleService;

        /// <summary>
        /// Constructor
        /// </summary>
        public ExampleV2Controller()
        {
            this.exampleService = new ExampleV2Service();
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
        [CacheableActionFilter]
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
