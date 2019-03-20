using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PubComp.Caching.Core.CacheUtils;

namespace PubComp.Caching.Core.UnitTests
{
    [TestClass]
    public class MultiLockTests
    {
        private MultiLock multiLock;
        private readonly Random random = new Random();

        [TestMethod]
        public void TakeAndRelease_CheckLock()
        {
            const int numberOfLocks = 13;
            const string key = "theKey";

            multiLock = new MultiLock(numberOfLocks);

            var lockNumber = GetLockNumber(key);
            var lck = GetLock(lockNumber);

            multiLock.Take(key);
            try
            {
                Assert.IsTrue(lck.CurrentCount == 0, "Lock should have been taken");
            }
            finally
            {
                multiLock.Release(key);
            }

            Assert.IsTrue(lck.CurrentCount > 0, "Lock should have been released");
        }

        [TestMethod]
        public void LockAndLoad_WaitsForLock()
        {
            const int numberOfLocks = 512;
            const string key = "key1";

            multiLock = new MultiLock(numberOfLocks);

            long[] entered = {0L};
            long[] locked = {0L};

            multiLock.Take(key);

            var runResult = Task.Run(action: () => multiLock.LockAndLoad(key, () =>
            {
                var result = Interlocked.Increment(ref entered[0]);
                var ln = GetLockNumber(key);
                var l = GetLock(ln);
                if (l.CurrentCount > 0) Interlocked.Increment(ref locked[0]);
                return result;
            }));

            // Give the task time to start
            Thread.Sleep(100);

            try
            {
                Assert.IsTrue(Interlocked.Read(ref entered[0]) == 0L, "Should wait for lock");
            }
            finally
            {
                multiLock.Release(key);
            }

            runResult.Wait(100);
            Assert.IsTrue(Interlocked.Read(ref entered[0]) != 0L, "Should have gotten lock");
            Assert.IsTrue(Interlocked.Read(ref locked[0]) == 0L, "Lock should have been taken");

            var lockNumber = GetLockNumber(key);
            var lck = GetLock(lockNumber);
            Assert.IsTrue(lck.CurrentCount > 0, "Lock should have been released");
        }

        [TestMethod]
        public void LockAndLoadAsync_WaitsForLock()
        {
            const int numberOfLocks = 512;
            const string key = "key1";

            multiLock = new MultiLock(numberOfLocks);

            long[] entered = { 0L };
            long[] locked = { 0L };

            multiLock.Take(key);

            var runResult = Task.Run(() =>
            {
                #pragma warning disable 1998
                var _ = multiLock.LockAndLoadAsync(key, async () =>
                {
                    var result = Interlocked.Increment(ref entered[0]);
                    var ln = GetLockNumber(key);
                    var l = GetLock(ln);
                    if (l.CurrentCount > 0) Interlocked.Increment(ref locked[0]);
                    return result;
                });
                _.Wait();
                #pragma warning restore 1998
            });

            // Give the task time to start
            Thread.Sleep(100);

            try
            {
                Assert.IsTrue(Interlocked.Read(ref entered[0]) == 0L, "Should wait for lock");
            }
            finally
            {
                multiLock.Release(key);
            }

            runResult.Wait(100);
            Assert.IsTrue(Interlocked.Read(ref entered[0]) != 0L, "Should have gotten lock");
            Assert.IsTrue(Interlocked.Read(ref locked[0]) == 0L, "Lock should have been taken");

            var lockNumber = GetLockNumber(key);
            var lck = GetLock(lockNumber);
            Assert.IsTrue(lck.CurrentCount > 0, "Lock should have been released");
        }

        [TestMethod]
        public void GetLockNumber_Deterministic_Random()
        {
            const int numberOfLocks = 1000;
            const int numberOfKeys = 100000;
            const int maxKeyLength = 100;
            const int iterations = 3;

            multiLock = new MultiLock(numberOfLocks);

            var keys = GetRandomStrings(numberOfKeys, maxKeyLength).ToList();
            Print("keys (first 10): ", keys.Select(k => $"\"{k}\"").Take(10));

            var lockNumbers0 = keys.Select(GetLockNumber).ToList();
            Print("lockNumbers0 (first 10): ", lockNumbers0.Take(10));

            for (int cnt = 1; cnt < iterations; cnt++)
            {
                var lockNumbersN = keys.Select(GetLockNumber).ToList();
                Print($"lockNumbers{cnt} (first 10): ", lockNumbersN.Take(10));

                CollectionAssert.AreEqual(lockNumbers0, lockNumbersN);
            }
        }

        [TestMethod]
        public void GetLockNumber_Distribution_100_Random()
        {
            ushort numberOfLocks = 100;
            RunLockNumberDistributionTest(numberOfLocks, GetRandomStrings);
        }

        [TestMethod]
        public void GetLockNumber_Distribution_100_Mutations()
        {
            ushort numberOfLocks = 100;
            RunLockNumberDistributionTest(numberOfLocks, GetStringWithRandomMutations);
        }

        [TestMethod]
        public void GetLockNumber_Distribution_21_Random()
        {
            ushort numberOfLocks = 21;
            RunLockNumberDistributionTest(numberOfLocks, GetRandomStrings);
        }

        [TestMethod]
        public void GetLockNumber_Distribution_21_Mutations()
        {
            ushort numberOfLocks = 21;
            RunLockNumberDistributionTest(numberOfLocks, GetStringWithRandomMutations);
        }

        [TestMethod]
        public void GetLockNumber_Distribution_513_Random()
        {
            ushort numberOfLocks = 513;
            RunLockNumberDistributionTest(numberOfLocks, GetRandomStrings);
        }

        [TestMethod]
        public void GetLockNumber_Distribution_513_Mutations()
        {
            ushort numberOfLocks = 513;
            RunLockNumberDistributionTest(numberOfLocks, GetStringWithRandomMutations);
        }

        private void RunLockNumberDistributionTest(
            ushort numberOfLocks, Func<int, int, IEnumerable<String>> keysGenerator)
        {
            const int iterations = 100;
            int[] maxKeyLengths = { 10, 31, 42, 101, 292 };

            RunLockNumberDistributionTest(numberOfLocks, keysGenerator, 1, 0.40, maxKeyLengths, iterations);
            RunLockNumberDistributionTest(numberOfLocks, keysGenerator, 3, 0.70, maxKeyLengths, iterations);
            RunLockNumberDistributionTest(numberOfLocks, keysGenerator, 5, 0.80, maxKeyLengths, iterations);
            RunLockNumberDistributionTest(numberOfLocks, keysGenerator, 9, 0.88, maxKeyLengths, iterations);
        }

        private void RunLockNumberDistributionTest(
            ushort numberOfLocks, Func<int, int, IEnumerable<String>> keysGenerator,
            int numberOfKeysPerLock, double minDistribution, int[] maxKeyLengths, int iterations)
        {
            foreach (var maxKeyLength in maxKeyLengths)
            {
                Console.WriteLine($"{nameof(numberOfLocks)} = {numberOfLocks}");
                Console.WriteLine($"{nameof(maxKeyLength)} = {maxKeyLength}");
                Console.WriteLine($"numberOfKeys = {numberOfLocks * numberOfKeysPerLock}");

                int totalUsage = 0;
                double totalError = 0.0;

                for (int cnt = 0; cnt < iterations; cnt++)
                {
                    (int usage, double error) = RunLockNumberDistributionTest(
                        numberOfLocks, keysGenerator, numberOfKeysPerLock, maxKeyLength);

                    totalUsage += usage;
                    totalError += error;
                }

                var averageUsage = totalUsage / (double)iterations;
                var averageError = totalError / iterations;

                Console.WriteLine($"Number of distinct lockNumbers {averageUsage}");

                var test1Msg = $"distribution test #1, expected {averageUsage} >= {numberOfLocks * minDistribution}";
                Console.WriteLine(test1Msg);
                Assert.IsTrue(
                    averageUsage >= (int)(numberOfLocks * minDistribution),
                    test1Msg);

                // 5.7 >= ln(maxMaxKeyLength), 6.3 >= ln(maxNumberOfLocks)
                var expected = 0.3 + 0.2 * (5.7 - Math.Log(maxKeyLength)) + 0.4 * (6.3 - Math.Log(numberOfLocks));
                var test2Msg = $"distribution test #2, expected {averageError} <= {expected}";
                Console.WriteLine(test2Msg);
                Assert.IsTrue(
                    averageError <= expected,
                    test2Msg);
            }
        }

        private (int, double) RunLockNumberDistributionTest(
            ushort numberOfLocks, Func<int, int, IEnumerable<String>> keysGenerator,
            int numberOfKeysPerLock, int maxKeyLength)
        {
            int numberOfKeys = numberOfLocks * numberOfKeysPerLock;

            multiLock = new MultiLock(numberOfLocks);

            var keys = keysGenerator(numberOfKeys, maxKeyLength).ToList();
            Print("keys (first 10): ", keys.Select(k => $"\"{k}\"").Take(10));

            var lockNumbers = keys.Select(GetLockNumber).ToList();
            Print("lockNumbers (first 10): ", lockNumbers.Take(10));

            Assert.AreEqual(numberOfKeys, lockNumbers.Count, "number of locks");

            Assert.IsTrue(lockNumbers.All(l => l < numberOfLocks), "range check");

            var usedLocks = lockNumbers.Distinct().ToList();

            var zeros = Enumerable.Repeat(0, numberOfLocks - usedLocks.Count);

            var usageCount = lockNumbers.GroupBy(ln => ln).Select(g => g.Count()).ToList();
            usageCount.AddRange(zeros);

            var distributionVsExpected = Math.Sqrt(
                usageCount
                    .Select(x => Math.Pow(numberOfKeysPerLock - x, 2))
                    .Sum()
                ) / usageCount.Count;

            return (usedLocks.Count, distributionVsExpected);
        }

        private void Print<T>(string prefix, IEnumerable<T> items)
        {
            Console.WriteLine(string.Concat(prefix, "(", string.Join(", ", items), ")"));
        }

        private IEnumerable<string> GetRandomStrings(int numberOfKeys, int maxKeyLength)
        {
            for (int cnt = 0; cnt < numberOfKeys; cnt++)
                yield return GetRandomString(random.Next(maxKeyLength));
        }

        private IEnumerable<string> GetStringWithRandomMutations(int numberOfKeys, int baseKeyLength)
        {
            var baseKey = GetRandomString(baseKeyLength);

            for (int cnt = 0; cnt < numberOfKeys; cnt++)
                yield return GetRandomMutation(baseKey);
        }

        private uint GetLockNumber(string key)
        {
            const string methodName = "GetLockNumber";
            var method = typeof(MultiLock)
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"{nameof(methodName)} != {methodName}");
            var result = method.Invoke(multiLock, new object[] {key});
            Assert.IsTrue(result is uint, $"{methodName} no longer returns a {typeof(uint).Name}");
            return (uint)result;
        }

        private SemaphoreSlim GetLock(uint lockNumber)
        {
            const string fieldName = "locks";
            var field = typeof(MultiLock)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"{nameof(fieldName)} != {fieldName}");
            var result = field.GetValue(multiLock);
            var array = result as SemaphoreSlim[];
            Assert.IsTrue(array != null, $"{fieldName} no longer returns a {typeof(SemaphoreSlim[]).Name}");
            return array[lockNumber];
        }

        private string GetRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789._()<>[]`";
            var result = new char[length];
            for (int cnt = 0; cnt < result.Length; cnt++)
                result[cnt] = chars[random.Next(chars.Length)];
            return new string(result);
        }

        enum MutationType { Add, Remove, Replace }

        private string GetRandomMutation(string baseString)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789._()<>[]`";

            var mutationType = (MutationType)random.Next(3);

            StringBuilder result;

            switch (mutationType)
            {
                case MutationType.Add:
                {
                    result = new StringBuilder(baseString.Length + 1);
                    var pos = random.Next(baseString.Length + 1);
                    result.Append(SafeSubString(baseString, 0, pos));
                    result.Append(chars[random.Next(chars.Length)]);
                    result.Append(SafeSubString(baseString, pos, baseString.Length));
                }
                break;
                case MutationType.Remove:
                {
                    result = new StringBuilder(baseString.Length - 1);
                    var pos = random.Next(baseString.Length);
                    result.Append(SafeSubString(baseString, 0, pos - 1));
                    result.Append(SafeSubString(baseString, pos, baseString.Length));
                }
                break;
                case MutationType.Replace:
                {
                    result = new StringBuilder(baseString.Length);
                    var pos = random.Next(baseString.Length);
                    result.Append(SafeSubString(baseString, 0, pos - 1));
                    result.Append(chars[random.Next(chars.Length)]);
                    result.Append(SafeSubString(baseString, pos, baseString.Length));
                    }
                break;
                default:
                    throw new NotSupportedException($"{nameof(MutationType)}.{mutationType} is not supported");
            }

            return result.ToString();
        }

        private string SafeSubString(string source, int start, int length)
        {
            length = Math.Min(length, source.Length - start);

            if (start < 0 || length <= 0)
                return string.Empty;

            return source.Substring(start, length);
        }
    }
}
