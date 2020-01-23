using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;

namespace PubComp.Caching.Core.UnitTests
{
    // http://softwareonastring.com/502/testing-every-implementer-of-an-interface-with-the-same-tests-using-mstest

    [TestClass]
    public abstract class ScopedCacheInterfaceTests
    {
        protected abstract IScopedCache GetScopedCache(string name);
        protected abstract IScopedCache GetScopedCacheWithSlidingExpiration(string name, TimeSpan slidingExpiration);
        protected abstract IScopedCache GetScopedCacheWithExpirationFromAdd(string name, TimeSpan expirationFromAdd);
        protected abstract IScopedCache GetScopedCacheWithAbsoluteExpiration(string name, DateTimeOffset expireAt);

        [TestMethod]
        public void TestWithoutCacheDirectives()
        {
            var cache = GetScopedCacheWithSlidingExpiration("cache1", TimeSpan.FromMinutes(2));
            cache.ClearAll();

            int misses = 0;

            Func<int> getter = () => ++misses;

            var result = cache.Get("key", getter);
            Assert.AreEqual(1, misses);
            Assert.AreEqual(1, result);

            result = cache.Get("key", getter);
            Assert.AreEqual(2, misses);
            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public void TestWithCacheDirectives_GetOrSet()
        {
            var cache = GetScopedCacheWithSlidingExpiration("cache1", TimeSpan.FromMinutes(2));
            cache.ClearAll();

            var valueTimestamp = DateTimeOffset.UtcNow;

            int getterInvocations = 0;
            ScopedValue<int> Getter() => new ScopedValue<int>(++getterInvocations, valueTimestamp);

            using (CacheDirectives.SetScope(CacheMethod.GetOrSet, valueTimestamp))
            {
                var result = cache.GetScoped("key", Getter);
                Assert.AreEqual(1, getterInvocations);
                Assert.AreEqual(CacheMethodTaken.Set | CacheMethodTaken.GetMiss, result.MethodTaken);
                Assert.AreEqual(1, result.ScopedValue.Value);
                Assert.AreEqual(valueTimestamp, result.ScopedValue.ValueTimestamp);

                result = cache.GetScoped("key", Getter);
                Assert.AreEqual(CacheMethodTaken.Get, result.MethodTaken);
                Assert.AreEqual(1, result.ScopedValue.Value);
                Assert.AreEqual(valueTimestamp, result.ScopedValue.ValueTimestamp);
            }
        }

        [TestMethod]
        public void TestWithCacheDirectives_Set()
        {
            var cache = GetScopedCacheWithSlidingExpiration("cache1", TimeSpan.FromMinutes(2));
            cache.ClearAll();

            int getterInvocations = 0;
            ScopedValue<int> Getter() => new ScopedValue<int>(++getterInvocations, DateTimeOffset.UtcNow);

            using (CacheDirectives.SetScope(CacheMethod.Set, DateTimeOffset.UtcNow))
            {
                Assert.AreEqual(CacheMethodTaken.None, cache.TryGetScoped<int>("key", out _));
                Assert.AreEqual(CacheMethodTaken.Set, cache.GetScoped("key", Getter).MethodTaken);
                Assert.AreEqual(CacheMethodTaken.None, cache.TryGetScoped<int>("key", out _));
                Assert.AreEqual(1, getterInvocations);
            }
        }

        [TestMethod]
        public void TestWithCacheDirectives_Get()
        {
            var cache = GetScopedCacheWithSlidingExpiration("cache1", TimeSpan.FromMinutes(2));
            cache.ClearAll();

            int getterInvocations = 0;
            ScopedValue<int> Getter() => new ScopedValue<int>(++getterInvocations, DateTimeOffset.UtcNow);

            using (CacheDirectives.SetScope(CacheMethod.Get, DateTimeOffset.UtcNow))
            {
                Assert.AreEqual(CacheMethodTaken.GetMiss, cache.GetScoped("key", Getter).MethodTaken);
                Assert.AreEqual(CacheMethodTaken.GetMiss, cache.TryGetScoped<int>("key", out _));
                Assert.AreEqual(CacheMethodTaken.GetMiss, cache.GetScoped("key", Getter).MethodTaken);
                Assert.AreEqual(2, getterInvocations);
            }
        }

        [TestMethod]
        public void TestWithCacheDirectives_MinimumValueTimestamp_UpToDate()
        {
            var cache = GetScopedCacheWithSlidingExpiration("cache1", TimeSpan.FromMinutes(2));
            cache.ClearAll();

            var testTimestamp = DateTimeOffset.UtcNow;

            int getterInvocations = 0;
            ScopedValue<int> Getter() => new ScopedValue<int>(++getterInvocations, testTimestamp.AddHours(-1));

            using (CacheDirectives.SetScope(CacheMethod.GetOrSet, testTimestamp.AddHours(-2)))
            {
                var result = cache.GetScoped("key", Getter);
                Assert.AreEqual(CacheMethodTaken.Set | CacheMethodTaken.GetMiss, result.MethodTaken);

                var cacheMethodTaken = cache.TryGetScoped<int>("key", out var scopedValue);
                Assert.AreEqual(CacheMethodTaken.Get, cacheMethodTaken);
                Assert.AreEqual(getterInvocations, scopedValue.Value);
                Assert.AreEqual(testTimestamp.AddHours(-1), scopedValue.ValueTimestamp);
            }
        }

        [TestMethod]
        public void TestWithCacheDirectives_MinimumValueTimestamp_NotUpToDate()
        {
            var cache = GetScopedCacheWithSlidingExpiration("cache1", TimeSpan.FromMinutes(2));
            cache.ClearAll();

            var testTimestamp = DateTimeOffset.UtcNow;

            int getterInvocations = 0;
            ScopedValue<int> Getter() => new ScopedValue<int>(++getterInvocations, testTimestamp.AddHours(-1));

            using (CacheDirectives.SetScope(CacheMethod.GetOrSet, testTimestamp))
            {
                var result = cache.GetScoped("key", Getter);
                Assert.AreEqual(CacheMethodTaken.Set | CacheMethodTaken.GetMiss, result.MethodTaken);

                var cacheMethodTaken = cache.TryGetScoped<int>("key", out _);
                Assert.AreEqual(CacheMethodTaken.GetMiss, cacheMethodTaken);
            }
        }

        [TestMethod]
        public void TestWithCacheDirectives_MultipleScopes()
        {
            var cache = GetScopedCacheWithSlidingExpiration("cache1", TimeSpan.FromMinutes(2));
            cache.ClearAll();

            var testTimestamp = DateTimeOffset.UtcNow;

            int getterInvocations = 0;
            ScopedValue<int> Getter() => new ScopedValue<int>(++getterInvocations, DateTimeOffset.UtcNow);

            using (CacheDirectives.SetScope(CacheMethod.None, testTimestamp))
            {
                var result = cache.GetScoped("key", Getter);
                Assert.AreEqual(CacheMethodTaken.None, result.MethodTaken);
                Assert.AreEqual(1, getterInvocations);
            }

            using (CacheDirectives.SetScope(CacheMethod.Get, testTimestamp))
            {
                var result = cache.GetScoped("key", Getter);
                Assert.AreEqual(CacheMethodTaken.GetMiss, result.MethodTaken);
                Assert.AreEqual(2, getterInvocations);
            }

            using (CacheDirectives.SetScope(CacheMethod.Set, testTimestamp))
            {
                var result = cache.GetScoped("key", Getter);
                Assert.AreEqual(CacheMethodTaken.Set, result.MethodTaken);
                Assert.AreEqual(3, getterInvocations);
            }

            using (CacheDirectives.SetScope(CacheMethod.None, testTimestamp))
            {
                var result = cache.GetScoped("key", Getter);
                Assert.AreEqual(CacheMethodTaken.None, result.MethodTaken);
                Assert.AreEqual(4, getterInvocations);
            }

            var resultOutsideOfScope = cache.GetScoped("key", Getter);
            Assert.AreEqual(CacheMethodTaken.None, resultOutsideOfScope.MethodTaken);
            Assert.AreEqual(5, getterInvocations);

            Thread.Sleep(1);
            using (CacheDirectives.SetScope(CacheMethod.Get, DateTimeOffset.UtcNow))
            {
                var result = cache.GetScoped("key", Getter);
                Assert.AreEqual(CacheMethodTaken.GetMiss, result.MethodTaken);
                Assert.AreEqual(6, getterInvocations);
            }

            using (CacheDirectives.SetScope(CacheMethod.Get, testTimestamp))
            {
                var result = cache.GetScoped("key", Getter);
                Assert.AreEqual(CacheMethodTaken.Get, result.MethodTaken);
                Assert.AreEqual(6, getterInvocations);
            }
        }

        [TestMethod]
        public void TestWithCacheDirectives_NestedScopes()
        {
            var cache = GetScopedCacheWithSlidingExpiration("cache1", TimeSpan.FromMinutes(2));
            cache.ClearAll();

            var t2 = DateTimeOffset.UtcNow;
            var testTimestamp = t2;//DateTimeOffset.UtcNow;

            int getterInvocations = 0;
            ScopedValue<int> Getter() => new ScopedValue<int>(++getterInvocations, DateTimeOffset.UtcNow);

            using (CacheDirectives.SetScope(CacheMethod.Get, testTimestamp))
            {
                var result = cache.GetScoped("key", Getter);
                Assert.AreEqual(CacheMethodTaken.GetMiss, result.MethodTaken);
                Assert.AreEqual(1, getterInvocations);

                using (CacheDirectives.SetScope(CacheMethod.Set, testTimestamp))
                {
                    result = cache.GetScoped("key", Getter);
                    Assert.AreEqual(CacheMethodTaken.Set, result.MethodTaken);
                    Assert.AreEqual(2, getterInvocations);

                    using (CacheDirectives.SetScope(CacheMethod.None, testTimestamp))
                    {
                        result = cache.GetScoped("key", Getter);
                        Assert.AreEqual(CacheMethodTaken.None, result.MethodTaken);
                        Assert.AreEqual(3, getterInvocations);

                        using (CacheDirectives.SetScope(CacheMethod.Get, testTimestamp))
                        {
                            result = cache.GetScoped("key", Getter);
                            Assert.AreEqual(CacheMethodTaken.Get, result.MethodTaken);
                            Assert.AreEqual(3, getterInvocations);
                        }

                        Thread.Sleep(1);
                        using (CacheDirectives.SetScope(CacheMethod.Get, DateTimeOffset.UtcNow))
                        {
                            using (CacheDirectives.SetScope(CacheMethod.Get, testTimestamp))
                            {
                                result = cache.GetScoped("key", Getter);
                                Assert.AreEqual(CacheMethodTaken.Get, result.MethodTaken);
                                Assert.AreEqual(3, getterInvocations);
                            }

                            result = cache.GetScoped("key", Getter);
                            Assert.AreEqual(CacheMethodTaken.GetMiss, result.MethodTaken);
                            Assert.AreEqual(4, getterInvocations);
                        }

                        result = cache.GetScoped("key", Getter);
                        Assert.AreEqual(CacheMethodTaken.None, result.MethodTaken);
                        Assert.AreEqual(5, getterInvocations);
                    }

                    result = cache.GetScoped("key", Getter);
                    Assert.AreEqual(CacheMethodTaken.Set, result.MethodTaken);
                    Assert.AreEqual(6, getterInvocations);
                }

                var resultOutsideOfScope = cache.GetScoped("key", Getter);
                Assert.AreEqual(CacheMethodTaken.Get, resultOutsideOfScope.MethodTaken);
                Assert.AreEqual(6, getterInvocations);
            }
        }
    }
}
