using Newtonsoft.Json;
using PubComp.Caching.Core;
using Swashbuckle.Swagger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Description;

namespace PubComp.Caching.WebApiExtended
{
    public class CacheableSwaggerOperationalFilter : IOperationFilter
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            var cacheableAttribute = apiDescription.ActionDescriptor.GetFilterPipeline()
                .Select(filterInfo => filterInfo.Instance)
                .OfType<ICacheable>()
                .SingleOrDefault();

            if (cacheableAttribute == null)
            {
                return;
            }

            if (operation.parameters == null)
                operation.parameters = new List<Parameter>();
            var parameters = operation.parameters;

            var example = JsonConvert.SerializeObject(
                new Dictionary<string, object>
                {
                    { nameof(CacheDirectives.Method) , cacheableAttribute.DefaultMethod.ToString() },
                    { nameof(CacheDirectives.MinimumValueTimestamp), DateTimeOffset.UtcNow.AddMilliseconds(-Math.Abs(cacheableAttribute.DefaultMinimumAgeInMilliseconds)) }
                });

            var cacheDirectivesParameter = new Parameter()
            {
                @in = "header",
                name = CacheDirectives.HeadersKey,
                description = $"Method (enum: {string.Join(", ", Enum.GetNames(typeof(CacheMethod)))}), MinimumValueTimestamp (DateTimeOffset?)",
                @default = example,
                type = "object",
            };
            parameters.Add(cacheDirectivesParameter);
        }
    }
}