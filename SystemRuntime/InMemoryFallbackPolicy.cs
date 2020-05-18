using System;

namespace PubComp.Caching.SystemRuntime
{
    public class InMemoryFallbackPolicy : InMemoryExpirationPolicy
    {
        public bool InvalidateOnProviderStateChange { get; set; } = true;
    }
}
