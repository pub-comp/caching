using System;

namespace PubComp.Caching.Core.Events
{
    public class ProviderStateChangedEventArgs : EventArgs
    {
        public bool NewState { get; }

        public ProviderStateChangedEventArgs(bool newState)
        {
            NewState = newState;
        }
    }
}
