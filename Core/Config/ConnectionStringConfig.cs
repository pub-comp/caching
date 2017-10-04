namespace PubComp.Caching.Core.Config
{
    public abstract class ConnectionStringConfig : ConfigNode
    {
        public abstract ICacheConnectionString CreateConnectionString();
    }
}
