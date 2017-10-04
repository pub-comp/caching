using PubComp.Caching.Core.Config;
using PubComp.Caching.Core.Notifications;

namespace PubComp.Caching.Core.UnitTests.Mocks
{
    public class NoNotifierConfig : NotifierConfig
    {
        public NoNotifierPolicy Policy { get; set; }

        public override ICacheNotifier CreateCacheNotifier()
        {
            return new NoNotifier(this.Name, this.Policy);
        }
    }
}
