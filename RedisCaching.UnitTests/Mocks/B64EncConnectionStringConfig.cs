using PubComp.Caching.Core;
using PubComp.Caching.Core.Config;

namespace PubComp.Caching.RedisCaching.UnitTests.Mocks
{
    /// <summary>
    /// An example of how to write a custom class for decrypting encrypted connection strings.
    /// Note: This is a functionality example only, do not encode/decode using the below algorithm!
    /// </summary>
    public class B64EncConnectionStringConfig : ConnectionStringConfig
    {
        public string EncConnectionString { get; set; }

        public override ICacheConnectionString CreateConnectionString()
        {
            return new B64EncConnectionString(this.Name, this.EncConnectionString);
        }
    }
}