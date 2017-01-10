using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PubComp.Caching.Core
{
    public interface ICacheNotifier
    {
        void Subscribe(Func<CacheItemNotification, bool> callback);

        void UnSubscribe();

        void Publish(string key, CacheItemActionTypes action);
    }
    
    public class CacheItemNotification
    {
        public CacheItemNotification(string sender, string cacheName, string key, CacheItemActionTypes action)
        {
            this.Key = key;
            this.CacheName = cacheName;
            this.Action = action;
            this.Sender = sender;

        }
       public string Key { get; }
        
       public string CacheName { get; }

       public string Sender { get; }

       public CacheItemActionTypes Action { get; }
    }

    public enum CacheItemActionTypes
    {
        Added = 1,
        Updated = 2,
        Removed = 3,
        RemoveAll = 100
    }

}
