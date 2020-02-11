using PubComp.Caching.Core.Notifications;
using System.Threading.Tasks;

namespace PubComp.Caching.Core
{
    public static class CacheExtensions
    {
        /// <summary>
        /// Check if cache is in useable state (not null, is active, ...)
        /// </summary>
        /// <param name="cacheToCheck"></param>
        /// <returns></returns>
        public static bool IsUseable(this ICache cacheToCheck)
        {
            return IsUseable(cacheToCheck as ICacheState);
        }

        /// <summary>
        /// Check if cache is in useable state (not null, is active, ...)
        /// </summary>
        /// <param name="cacheToCheck"></param>
        /// <returns></returns>
        public static bool IsUseable(this ICacheState cacheToCheck)
        {
            return cacheToCheck?.IsActive ?? true;
        }

        public static void NotifySyncProvider(this ICache cache, string key, CacheItemActionTypes action)
        {
            var notifier = CacheManager.GetAssociatedNotifier(cache);
            notifier?.Publish(cache.Name, key, action);
        }

        public static Task NotifySyncProviderAsync(this ICache cache, string key, CacheItemActionTypes action)
        {
            var notifier = CacheManager.GetAssociatedNotifier(cache);
            if (notifier != null)
                return notifier.PublishAsync(cache.Name, key, action);

            return Task.CompletedTask;
        }

        public static bool TryNotifySyncProvider(this ICache cache, string key, CacheItemActionTypes action)
        {
            var notifier = CacheManager.GetAssociatedNotifier(cache);
            return notifier?.TryPublish(cache.Name, key, action) ?? false;
        }

        public static Task<bool> TryNotifySyncProviderAsync(this ICache cache, string key, CacheItemActionTypes action)
        {
            var notifier = CacheManager.GetAssociatedNotifier(cache);
            if (notifier != null)
                return notifier.TryPublishAsync(cache.Name, key, action);

            return Task.FromResult(false);
        }
    }
}
