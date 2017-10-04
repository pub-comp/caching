namespace PubComp.Caching.Core.UnitTests.Mocks
{
    /// <summary>
    /// An example of how to write a custom class for connection strings.
    /// </summary>
    public class PlainConnectionString : ICacheConnectionString
    {
        public string Name { get; }
        public string ConnectionString { get; }

        public PlainConnectionString(string name, string value)
        {
            this.Name = name;
            this.ConnectionString = value;
        }
    }
}
