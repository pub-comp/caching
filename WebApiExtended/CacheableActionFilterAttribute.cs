using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using PubComp.Caching.Core;

namespace PubComp.Caching.WebApiExtended
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CacheableActionFilterAttribute : FilterAttribute, IActionFilter, ICacheable
    {
        public async Task<HttpResponseMessage> ExecuteActionFilterAsync(
            HttpActionContext actionContext,
            CancellationToken cancellationToken,
            Func<Task<HttpResponseMessage>> continuation)
        {
            var definedCacheDirectives = GetCacheDirectives(actionContext);
            using (ScopedContext<CacheDirectives>.CreateNewScope(definedCacheDirectives))
            {
                var response = await continuation().ConfigureAwait(false);

                //if (cacheableController.CacheDirectivesOutcome != null)
                //{
                //    var cacheDirectivesOutcomeJson = JsonConvert.SerializeObject(cacheableController.CacheDirectivesOutcome);
                //    response.Headers.Add(CacheDirectivesOutcome.HeadersKey, cacheDirectivesOutcomeJson);
                //}
                return response;
            }
        }

        private CacheDirectives GetCacheDirectives(HttpActionContext actionContext)
        {
            var cacheDirectivesJson = actionContext.Request.Headers
                .SingleOrDefault(x => x.Key == CacheDirectives.HeadersKey)
                .Value?.First();

            if (string.IsNullOrEmpty(cacheDirectivesJson))
                return new CacheDirectives { Method = CacheMethod.None };

            return JsonConvert.DeserializeObject<CacheDirectives>(cacheDirectivesJson);
        }
    }
}
