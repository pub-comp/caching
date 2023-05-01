﻿using PubComp.Caching.Core.Notifications;
using System;
using System.Threading.Tasks;

namespace PubComp.Caching.Core.UnitTests.Mocks
{
    public class NoNotifier : ICacheNotifier
    {
        private readonly string name;
        private readonly NoNotifierPolicy policy;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly string connectionString;

        public NoNotifier(string name, NoNotifierPolicy policy)
        {
            this.name = name;
            this.policy = policy;

            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            if (!string.IsNullOrEmpty(policy.ConnectionName))
            {
                this.connectionString = CacheManager.GetConnectionString(policy.ConnectionName)?.ConnectionString;

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new ArgumentException(
                        $"{nameof(ICacheConnectionString.ConnectionString)} not found for {nameof(policy.ConnectionName)} {policy.ConnectionName}", $"{nameof(policy)}.{nameof(policy.ConnectionName)}");
                }
            }
            else if (!string.IsNullOrEmpty(policy.ConnectionString))
            {
                this.connectionString = policy.ConnectionString;
            }
            else
            {
                throw new ArgumentException(
                    $"{nameof(policy.ConnectionString)} is undefined", $"{nameof(policy)}.{nameof(policy.ConnectionString)}");
            }
        }

        public string Name { get { return this.name; } }
        
        public void Subscribe(string cacheName, Func<CacheItemNotification, bool> callback)
        {
        }

        public void Subscribe(string cacheName, 
            Func<CacheItemNotification, bool> cacheUpdatedCallback,
            EventHandler<Core.Events.ProviderStateChangedEventArgs> notifierProviderStateChangedCallback)
        {
        }


        public void UnSubscribe(string cacheName)
        {
        }

        public async Task PublishAsync(string cacheName, string key, CacheItemActionTypes action)
        {
        }

        public bool TryPublish(string cacheName, string key, CacheItemActionTypes action)
        {
            return true;
        }

        public Task<bool> TryPublishAsync(string cacheName, string key, CacheItemActionTypes action)
        {
            return Task.FromResult(true);
        }

        public void Publish(string cacheName, string key, CacheItemActionTypes action)
        {
        }
    }
}