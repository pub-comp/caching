using PubComp.Caching.Core.Notifications;

namespace PubComp.Caching.Core.Config
{
    public abstract class NotifierConfig : ConfigNode
    {
        public abstract ICacheNotifier CreateCacheNotifier();
    }
}
