namespace PubComp.Caching.RedisCaching.Converters
{
    internal class RedisConverterFactory
    {
        public static IRedisConverter CreateConverter(string type)
        {
            type = (type ?? string.Empty).ToLowerInvariant();

            switch (type)
            {
                case RedisConverterBson.Type:
                    return new RedisConverterBson();

                case RedisConverterDeflate.Type:
                    return new RedisConverterDeflate();

                case RedisConverterGZip.Type:
                    return new RedisConverterGZip();

                case RedisConverterJson.Type:
                default:
                    return new RedisConverterJson();
            }
        }
    }
}