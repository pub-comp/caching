using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PubComp.Caching.Core.UnitTests
{
    public static class CacheTestTools
    {
        public static void AssertValueDoesntChangeWithin<TValue>(
            ICache cache, string key, TValue expected, Func<TValue> getter,
            Stopwatch stopwatch, double minSeconds)
        {
            double elapsedSeconds;

            do
            {
                elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                var actual = cache.Get(key, getter);
                Assert.AreEqual(expected, actual, "Not in cache after " + elapsedSeconds + " sec");
                Thread.Sleep(500);
            } while (stopwatch.Elapsed.TotalSeconds < minSeconds);

            Console.WriteLine("Remained in cached for at least " + elapsedSeconds + " sec");
        }

        public static void AssertValueDoesChangeWithin<TValue>(
            ICache cache, string key, TValue expected, Func<TValue> getter,
            Stopwatch stopwatch, double maxSeconds)
        {
            double elapsedSeconds;
            TValue actual;

            do
            {
                elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                actual = cache.Get(key, getter);
                if (!object.Equals(expected, actual))
                    break;
                Thread.Sleep(500);
            } while (stopwatch.Elapsed.TotalSeconds <= maxSeconds);

            Assert.AreNotEqual(expected, actual, "Still in cache after " + elapsedSeconds + " sec");

            Console.WriteLine("Remained in cached for at most " + elapsedSeconds + " sec");
        }

        public static void AssertValueDoesChangeAfter<TValue>(
            ICache cache, string key, TValue expected, Func<TValue> getter,
            Stopwatch stopwatch, double seconds)
        {
            Thread.Sleep((int)(seconds * 1000));
            var elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            var actual = cache.Get(key, getter);

            Assert.AreNotEqual(expected, actual, "Still in cache after " + elapsedSeconds + " sec");

            Console.WriteLine("Remained in cached for at most " + elapsedSeconds + " sec");
        }
    }
}
