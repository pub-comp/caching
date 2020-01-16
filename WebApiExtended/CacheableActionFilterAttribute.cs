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
        private readonly NLog.ILogger log;

        public CacheableActionFilterAttribute()
        {
            log = NLog.LogManager.GetLogger(typeof(CacheableActionFilterAttribute).FullName);
        }

        public async Task<HttpResponseMessage> ExecuteActionFilterAsync(
            HttpActionContext actionContext,
            CancellationToken cancellationToken,
            Func<Task<HttpResponseMessage>> continuation)
        {
            var definedCacheDirectives = GetCacheDirectives(actionContext);
            using (ScopedContext<CacheDirectives>.CreateNewScope(definedCacheDirectives))
            {
                var response = await continuation().ConfigureAwait(false);
                return response;
            }
        }

        private CacheDirectives GetCacheDirectives(HttpActionContext actionContext)
        {
            var cacheDirectivesJson = actionContext.Request.Headers
                .FirstOrDefault(x => x.Key == CacheDirectives.HeadersKey)
                .Value?.First();

            if (string.IsNullOrEmpty(cacheDirectivesJson))
                return new CacheDirectives { Method = CacheMethod.None };

            var cacheDirectives = JsonConvert.DeserializeObject<CacheDirectives>(cacheDirectivesJson);
            if (cacheDirectives.Method.HasFlag(CacheMethod.Get) &&
                (cacheDirectives.MinimumValueTimestamp == default || 
                 cacheDirectives.MinimumValueTimestamp > DateTimeOffset.UtcNow))
            {
                var newCacheMethod = cacheDirectives.Method ^ CacheMethod.Get;
                log.Warn($"CacheMethod requested: {cacheDirectives.Method} has been demoted to {newCacheMethod} due to invalid {nameof(cacheDirectives.MinimumValueTimestamp)}: {cacheDirectives.MinimumValueTimestamp}");
                cacheDirectives.Method = newCacheMethod;
            }

            return cacheDirectives;
        }
    }
}
