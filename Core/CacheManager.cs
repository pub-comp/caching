using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PubComp.Caching.Core
{
    public class CacheManager
    {
        private static Func<MethodBase> callingMethodGetter;
        private static readonly object _loadLock = new object();

        private static System.Threading.ReaderWriterLockSlim sync
            = new System.Threading.ReaderWriterLockSlim(System.Threading.LockRecursionPolicy.NoRecursion);

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<CacheName, ICache> caches
            = new System.Collections.Concurrent.ConcurrentDictionary<CacheName, ICache>();

        static CacheManager()
        {
            InitializeFromConfig();
        }

        public static void InitializeFromConfig()
        {
            var config = LoadConfig();
            ApplyConfig(config);
        }

        private static IList<CacheConfig> LoadConfig()
        {
            var config = ConfigurationManager.GetSection("PubComp/CacheConfig") as IList<CacheConfig>;
            return config;
        }

        private static void ApplyConfig(IList<CacheConfig> config)
        {
            if (config == null)
                return;

            foreach (var item in config)
            {
                switch (item.Action)
                {
                    case ConfigAction.Remove:
                        SetCache(item.Name, null);
                        break;

                    case ConfigAction.Add:
                        SetCache(item.Name, item.CreateCache());
                        break;
                }
            }
        }

        /// <summary>Gets a cache instance using full name of calling method's class</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static ICache GetCurrentClassCache()
        {
            var method = GetCallingMethod();
            var declaringType = method.DeclaringType;
            return GetCache(declaringType);
        }

        /// <summary>Gets a cache instance using full name of given class</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static ICache GetCache<TClass>()
        {
            return GetCache(typeof(TClass));
        }

        /// <summary>Gets a cache instance using full name of given class</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static ICache GetCache(Type type)
        {
            return GetCache(type.FullName);
        }

        private static KeyValuePair<CacheName, ICache>[] GetCaches()
        {
            KeyValuePair<CacheName, ICache>[] cachesArray;

            sync.EnterReadLock();
            try
            {
                cachesArray = caches.ToArray();
            }
            finally
            {
                sync.ExitReadLock();
            }

            return cachesArray;
        }

        /// <summary>Gets a cache by name</summary>
        /// <remarks>For better performance, store the result in client class</remarks>
        public static ICache GetCache(string name)
        {
            var cachesArray = GetCaches();
            
            var cachesSorted = cachesArray.OrderByDescending(c => c.Key.GetMatchLevel(name));
            var cache = cachesSorted.FirstOrDefault();

            return (cache.Key.Prefix != null && cache.Key.GetMatchLevel(name) >= cache.Key.Prefix.Length) ? cache.Value : null;
        }

        private class CacheComparer : IEqualityComparer<ICache>
        {
            public bool Equals(ICache x, ICache y)
            {
                if (x == null || y == null)
                    return x == y;
                
                return x.Name == y.Name;
            }

            public int GetHashCode(ICache obj)
            {
                if (obj == null)
                    return 0;

                return (obj.Name ?? string.Empty).GetHashCode();
            }
        }

        /// <summary>Adds or sets a cache by name</summary>
        /// <remarks>Cache name can end with wildcard '*'</remarks>
        public static void SetCache(string name, ICache cache)
        {
            var cacheName = new CacheName(name);

            sync.EnterWriteLock();
            try
            {
                if (cache == null)
                {
                    ICache oldCache;
                    caches.TryRemove(cacheName, out oldCache);
                }
                else
                {
                    caches.AddOrUpdate(cacheName, cache, (n, c) => cache);
                }
            }
            finally
            {
                sync.ExitWriteLock();
            }
        }

        public static void RemoveCache(string name)
        {
            SetCache(name, null);
        }

        public static void RemoveAllCaches()
        {
            sync.EnterWriteLock();
            try
            {
                caches.Clear();
            }
            finally
            {
                sync.ExitWriteLock();
            }
        }

        private static MethodBase GetCallingMethod()
        {
            Func<MethodBase> method = callingMethodGetter;
            if (method == null)
            {
                lock (_loadLock)
                {
                    if (callingMethodGetter == null)
                        callingMethodGetter = CreateGetClassNameFunction();

                    method = callingMethodGetter;
                }
            }
            return method();
        }

        private static Func<MethodBase> CreateGetClassNameFunction()
        {
            var stackFrameType = Type.GetType("System.Diagnostics.StackFrame");
            if (stackFrameType == null)
                throw new PlatformNotSupportedException("CreateGetClassNameFunction is only supported on platforms where System.Diagnostics.StackFrame exist");

            var constructor = stackFrameType.GetConstructor(new[] { typeof(int) });
            var getMethodMethod = stackFrameType.GetMethod("GetMethod");

            if (constructor == null)
                throw new PlatformNotSupportedException("StackFrame(int skipFrames) constructor not present");
            
            if (getMethodMethod == null)
                throw new PlatformNotSupportedException("StackFrame.GetMethod() not present");

            var stackFrame = Expression.New(constructor, Expression.Constant(3));
            var method = Expression.Call(stackFrame, getMethodMethod);
            var lambda = Expression.Lambda<Func<MethodBase>>(method);
            var compileFunction = lambda.GetType().GetMethod("Compile", new Type[0]);
            var function = (Func<MethodBase>)compileFunction.Invoke(lambda, null);

            return function;
        }
    }
}
