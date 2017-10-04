using PubComp.Caching.Core;

namespace PubComp.Caching.RedisCaching
{
    public class RedisConnectionString : ICacheConnectionString
    {
        public string Name { get; }
        public string ConnectionString { get; }

        public RedisConnectionString(string name, string value)
        {
            this.Name = name;
            this.ConnectionString = value;
        }
    }
}
