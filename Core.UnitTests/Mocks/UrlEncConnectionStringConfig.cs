using PubComp.Caching.Core.Config;

namespace PubComp.Caching.Core.UnitTests.Mocks
{
    /// <summary>
    /// An example of how to write a custom class for decrypting encrypted connection strings.
    /// Note: This is a functionality example only, do not encode/decode using the below algorithm!
    /// </summary>
    public class UrlEncConnectionStringConfig : ConnectionStringConfig
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string EncConnectionString { get; set; }

        public override ICacheConnectionString CreateConnectionString()
        {
            return new UrlEncConnectionString(this.Name, this.EncConnectionString);
        }
    }
}