using PubComp.Caching.Core;
using PubComp.Caching.Core.Config;

namespace PubComp.Caching.RedisCaching
{
    public class RedisConnectionStringConfig : ConnectionStringConfig
    {
        public string ConnectionString { get; set; }

        public override ICacheConnectionString CreateConnectionString()
        {
            return new RedisConnectionString(this.Name, this.ConnectionString);
        }
    }
}
