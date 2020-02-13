using System;

namespace PubComp.Caching.Core.Events
{
    public class ProviderStateChangedEventArgs : EventArgs
    {
        public bool IsAvailable { get; }

        public ProviderStateChangedEventArgs(bool isAvailable)
        {
            IsAvailable = isAvailable;
        }
    }
}
