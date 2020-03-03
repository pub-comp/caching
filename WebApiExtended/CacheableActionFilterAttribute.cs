using Newtonsoft.Json;
using PubComp.Caching.Core;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace PubComp.Caching.WebApiExtended
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CacheableActionFilterAttribute : FilterAttribute, IActionFilter, ICacheable
    {
        private readonly NLog.ILogger logger;

        public CacheMethod DefaultMethod { get; set; } = CacheMethod.None;
        public double DefaultMinimumAgeInMilliseconds { get; set; } = 0;

        public CacheableActionFilterAttribute()
        {
            logger = NLog.LogManager.GetLogger(typeof(CacheableActionFilterAttribute).FullName);
        }

        public async Task<HttpResponseMessage> ExecuteActionFilterAsync(
            HttpActionContext actionContext,
            CancellationToken cancellationToken,
            Func<Task<HttpResponseMessage>> continuation)
        {
            var requestedCacheDirectives = GetCacheDirectivesFromRequest(actionContext);
            using (CacheDirectives.SetScope(requestedCacheDirectives))
            {
                var response = await continuation().ConfigureAwait(false);
                return response;
            }
        }

        private CacheDirectives GetCacheDirectivesFromRequest(HttpActionContext actionContext)
        {
            try
            {
                var cacheDirectivesJson = actionContext.Request.Headers
                    .FirstOrDefault(x => x.Key == CacheDirectives.HeadersKey)
                    .Value?.First();

                if (string.IsNullOrEmpty(cacheDirectivesJson))
                    return new CacheDirectives
                    {
                        Method = DefaultMethod,
                        MinimumValueTimestamp = DateTimeOffset.UtcNow.AddMilliseconds(-Math.Abs(DefaultMinimumAgeInMilliseconds))
                    };

                var cacheDirectives = JsonConvert.DeserializeObject<CacheDirectives>(cacheDirectivesJson);
                if (cacheDirectives.IsValid())
                    return cacheDirectives;

                logger.Warn($"CacheMethod requested: {cacheDirectives.Method} has been demoted to CacheMethod.{nameof(CacheMethod.None)} due to invalid request: {cacheDirectivesJson}");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failed to retrieve/parse CacheDirectives, CacheMethod.None");
            }
            return new CacheDirectives { Method = CacheMethod.None };
        }
    }
}
