using PubComp.Caching.Core;

namespace PubComp.Caching.RedisCaching
{
    public class RedisConnectionString : ICacheConnectionString
    {
        public string Name { get; }
        public string ConnectionString { get; }

        public RedisClientPolicy Policy { get; set; }

        public RedisConnectionString(string name, string connectionString, RedisClientPolicy policy)
        {
            this.Name = name;
            this.ConnectionString = connectionString;
            this.Policy = policy;
        }
    }
}
