namespace PubComp.Caching.Core.Notifications
{
    public class CacheItemNotification
    {
        public CacheItemNotification(string sender, string cacheName, string key, CacheItemActionTypes action)
        {
            this.Sender = sender;
            this.CacheName = cacheName;
            this.Key = key;
            this.Action = action;
        }

        public string Sender { get; }

        public string CacheName { get; }

        public string Key { get; }

        public CacheItemActionTypes Action { get; }
    }
}