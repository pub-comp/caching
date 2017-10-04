namespace PubComp.Caching.Core.Config
{
    public abstract class ConfigNode
    {
        public ConfigAction Action { get; set; }

        public string Name { get; set; }
    }
}