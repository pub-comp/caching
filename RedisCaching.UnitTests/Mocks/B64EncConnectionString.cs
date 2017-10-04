using System;
using System.Text;
using PubComp.Caching.Core;

namespace PubComp.Caching.RedisCaching.UnitTests.Mocks
{
    /// <summary>
    /// An example of how to write a custom class for decrypting encrypted connection strings.
    /// Note: This is a functionality example only, do not encode/decode using the below algorithm!
    /// </summary>
    public class B64EncConnectionString : ICacheConnectionString
    {
        public string Name { get; }
        private readonly string encConnectionString;

        public B64EncConnectionString(string name, string value)
        {
            this.Name = name;
            this.encConnectionString = value;
        }

        public string ConnectionString { get { return Encoding.UTF8.GetString(Convert.FromBase64String(encConnectionString)); } }
    }
}
