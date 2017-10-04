using System.Net;

namespace PubComp.Caching.Core.UnitTests.Mocks
{
    /// <summary>
    /// An example of how to write a custom class for decrypting encrypted connection strings.
    /// Note: This is a functionality example only, do not encode/decode using the below algorithm!
    /// </summary>
    public class UrlEncConnectionString : ICacheConnectionString
    {
        public string Name { get; }
        private readonly string encConnectionString;

        public UrlEncConnectionString(string name, string value)
        {
            this.Name = name;
            this.encConnectionString = value;
        }

        public string ConnectionString { get { return WebUtility.UrlDecode(encConnectionString); } }
    }
}
