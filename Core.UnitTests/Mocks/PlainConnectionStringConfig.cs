using PubComp.Caching.Core.Config;

namespace PubComp.Caching.Core.UnitTests.Mocks
{
    /// <summary>
    /// An example of how to write a custom class for connection strings.
    /// </summary>
    public class PlainConnectionStringConfig : ConnectionStringConfig
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string ConnectionString { get; set; }

        public override ICacheConnectionString CreateConnectionString()
        {
            return new PlainConnectionString(this.Name, this.ConnectionString);
        }
    }
}