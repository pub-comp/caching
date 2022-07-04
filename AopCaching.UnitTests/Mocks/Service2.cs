using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PubComp.Caching.AopCaching.UnitTests.Mocks
{
    public class Service2
    {
        private int methodToCache0Counter;
        private int methodToCache0NameMissing;

        [Cache("CacheMissing")]
        public int MethodToCache0()
        {
            return ++methodToCache0Counter;
        }

        [Cache("CacheMissing")]
        public async Task<int> MethodToCache0Async()
        {
            await Task.Delay(10);
            return ++methodToCache0Counter;
        }

        [Cache("CacheInitMissing", true)]
        public int MethodToCacheMissing()
        {
            return ++methodToCache0NameMissing;
        }

        [Cache("CacheInitMissingAsync", true)]
        public async Task<int> MethodToCacheMissingAsync()
        {
            await Task.Delay(10);
            return ++methodToCache0NameMissing;
        }

        [Cache("localCache")]
        public IEnumerable<string> MethodToCache1()
        {
            return new string[] { "1", "2", "3", "4", "5" };
        }

        [Cache("localCache")]
        public async Task<IEnumerable<string>> MethodToCache1Async()
        {
            return await Task.FromResult(new string[] { "1", "2", "3", "4", "5" });
        }

        [Cache("localCache")]
        public IEnumerable<string> MethodToCache1(int number)
        {
            for (int cnt = 1; cnt <= number; cnt++)
                yield return cnt.ToString();
        }

        [Cache("localCache")]
        public IEnumerable<string> MethodToCache1(double number)
        {
            for (double cnt = 0.9; cnt <= number; cnt++)
                yield return cnt.ToString();
        }

        [Cache("localCache")]
        public IEnumerable<string> MethodToCache1(object obj)
        {
            Func<object, string> f = o => (o ?? "null").ToString();
            var st = f(obj);

            return new[] { st, st, st };
        }
    }
}
