namespace PubComp.Caching.Core
{
    public interface ICacheConnectionString
    {
        string Name { get; }

        string ConnectionString { get; }
    }
}
