using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using PubComp.Caching.Core.Exceptions;

namespace PubComp.Caching.Core.CacheUtils
{
    /// <summary>
    /// An abstraction of a locking mechanism, implemented with an array of <see cref="SemaphoreSlim"/>
    /// where the actual instance of <see cref="SemaphoreSlim"/> in use depends on the cache key
    /// </summary>
    public class MultiLock
    {
        private static readonly ConcurrentDictionary<string, AsyncLocal<bool>> AsyncLocalContext
            = new ConcurrentDictionary<string, AsyncLocal<bool>>();

        private readonly string id = Guid.NewGuid().ToString();
        private readonly SemaphoreSlim[] locks;
        private readonly uint[] scramblers;
        private const int MaxNumberOfLocks = 1000;
        private readonly int? timeoutMilliseconds;
        private readonly bool doThrowExceptionOnTimeout;
        private readonly bool doEnableReentrantLocking;

        /// <summary>
        /// Create an instance of <see cref="MultiLock" />
        /// </summary>
        /// <param name="numberOfLocks">Number of locks</param>
        /// <param name="timeoutMilliseconds">Optional timeout in mSec</param>
        /// <param name="doThrowExceptionOnTimeout">If using timeout, determines if an exception is thrown on timeout</param>
        /// <param name="doEnableReentrantLocking">If true, enables reentrant (recursive) calls without deadlocking</param>
        public MultiLock(
            ushort numberOfLocks, int? timeoutMilliseconds = null, bool doThrowExceptionOnTimeout = true,
            bool doEnableReentrantLocking = true)
        {
            if (numberOfLocks < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfLocks), numberOfLocks, "required >= 1");
            }

            if (numberOfLocks > MaxNumberOfLocks)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfLocks), numberOfLocks, $"required < {MaxNumberOfLocks}");
            }

            var l = new SemaphoreSlim[numberOfLocks];
            for (int cnt = 0; cnt < l.Length; cnt++)
            {
                l[cnt] = new SemaphoreSlim(1, 1);
            }

            // Create a random array of numbers in range [0,numberOfLocks)
            // array size =
            // number of digits of 2^32 (number of possible .NET hash codes)
            // in base <numberOfLocks>        
            var digits = numberOfLocks > 1 ? (int)Math.Ceiling(Math.Log(Math.Pow(2.0, 32.0)) / Math.Log(numberOfLocks)) : 1;
            var s = new uint[digits];
            var rand = new Random();
            for (int cnt = 0; cnt < s.Length; cnt++)
            {
                s[cnt] = (uint)rand.Next(numberOfLocks);
            }

            this.locks = l;
            this.scramblers = s;
            this.timeoutMilliseconds = timeoutMilliseconds;
            this.doThrowExceptionOnTimeout = doThrowExceptionOnTimeout;
            this.doEnableReentrantLocking = doEnableReentrantLocking;
        }

        /// <summary>
        /// Gets the lock number for a given key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private uint GetLockNumber(string key)
        {
            var hash = (uint)key.GetHashCode();

            var numberOfLocks = (uint)this.locks.Length;
            var s = this.scramblers;

            // Convert hash code to base <numberOfLocks> using modulo and divide
            // and XOR the digits

            uint result = 0;

            // ReSharper disable once ForCanBeConvertedToForeach (performance)
            for (int cnt = 0; cnt < s.Length; cnt++)
            {
                var currentDigit = hash % numberOfLocks;

                // currentDigit = modulo, then XOR with scrambler to scatter better
                // result = result XOR currentDigit
                result ^= currentDigit ^ s[cnt];

                hash /= numberOfLocks; // Divide number for calculating next digits
            }

            // Result in range [0,2^k) where 2^k-1 < numberOfLocks <= 2^k
            // Modulo numberOfLocks to limit to range [0,numberOfLocks)
            result %= numberOfLocks;
            return result;
        }

        private string GetContextKey(uint lockNumber)
        {
            return $"{this.id}.{lockNumber}";
        }

        /// <summary>
        /// Mark that flow has passed reentrant check
        /// </summary>
        /// <param name="lockNumber"></param>
        /// <returns></returns>
        private bool MarkFlow(uint lockNumber)
        {
            var contextKey = GetContextKey(lockNumber);
            var variable = AsyncLocalContext.GetOrAdd(contextKey, _ => new AsyncLocal<bool>());
            if (variable.Value == false)
            {
                variable.Value = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Unmark flow
        /// </summary>
        /// <param name="lockNumber"></param>
        private void UnmarkFlow(uint lockNumber)
        {
            var contextKey = GetContextKey(lockNumber);
            AsyncLocalContext.GetOrAdd(contextKey, _ => new AsyncLocal<bool>()).Value = false;
        }

        private bool Take(uint lockNumber, int? timeoutOverride = null)
        {
            var timeout = timeoutOverride ?? this.timeoutMilliseconds;

            bool taken;
            if (timeout.HasValue)
            {
                taken = this.locks[lockNumber].Wait(timeout.Value);
            }
            else
            {
                this.locks[lockNumber].Wait();
                taken = true;
            }

            return taken;
        }

        private async Task<bool> TakeAsync(uint lockNumber, int? timeoutOverride = null)
        {
            var timeout = timeoutOverride ?? this.timeoutMilliseconds;

            bool taken;
            if (timeout.HasValue)
            {
                taken = await this.locks[lockNumber].WaitAsync(timeout.Value).ConfigureAwait(false);
            }
            else
            {
                await this.locks[lockNumber].WaitAsync().ConfigureAwait(false);
                taken = true;
            }

            return taken;
        }

        /// <summary>
        /// Attempts to take a lock, depending on the key.
        /// </summary>
        /// <param name="key">The key to use for choosing the lock</param>
        /// <returns>If case timeoutMilliseconds was defined (non-null),
        /// true if take succeeded, false if timed out.
        /// If timeoutMilliseconds is null returns true.</returns>
        public bool Take(string key)
        {
            var lockNumber = GetLockNumber(key);
            return Take(lockNumber);
        }

        /// <summary>
        /// Attempts to take a lock, depending on the key.
        /// </summary>
        /// <param name="key">The key to use for choosing the lock</param>
        /// <returns>If case timeoutMilliseconds was defined (non-null),
        /// true if take succeeded, false if timed out.
        /// If timeoutMilliseconds is null returns true.</returns>
        public async Task<bool> TakeAsync(string key)
        {
            var lockNumber = GetLockNumber(key);
            return await TakeAsync(lockNumber);
        }

        private int Release(uint lockNumber)
        {
            var count = this.locks[lockNumber].Release();
            return count;
        }

        /// <summary>
        /// Releases a lock, depending on the key.
        /// </summary>
        /// <param name="key">The key to use for choosing the lock</param>
        /// <returns>1 if the lock was already available, 0 if not</returns>
        public int Release(string key)
        {
            var lockNumber = GetLockNumber(key);
            return Release(lockNumber);
        }

        private bool IsFlowAlreadyMarked(uint lockNumber)
        {
            var contextKey = GetContextKey(lockNumber);
            return AsyncLocalContext.TryGetValue(contextKey, out var result) && result.Value;
        }

        /// <summary>
        /// Locks the correct lock, depending on key, then attempts to run the loader.
        /// The lock is released on completion or failure of the loader.
        /// </summary>
        /// <typeparam name="TResult">The type of result, returned by the loader method</typeparam>
        /// <param name="key">The key to use for choosing the lock</param>
        /// <param name="loader">A method to run for loading the result</param>
        /// <returns>The result of the loader</returns>
        public TResult LockAndLoad<TResult>(String key, Func<TResult> loader)
        {
            var lockNumber = GetLockNumber(key);

            // If failed to get lock, check if already have this lock (higher up in call stack)
            if (doEnableReentrantLocking && IsFlowAlreadyMarked(lockNumber))
                return loader();

            // Do this BEFORE first async call, otherwise it won't affect current thread!
            var wasMarked = MarkFlow(lockNumber);
            try
            {
                // Wait for lock with or without timeout (depending on field)
                var gotLock = Take(lockNumber);
                if (!gotLock && doThrowExceptionOnTimeout)
                    throw new CacheLockException($"Failed to obtain lock for {key}");

                try
                {
                    return loader();
                }
                finally
                {
                    if (gotLock)
                        Release(lockNumber);
                }
            }
            finally
            {
                if (wasMarked)
                    UnmarkFlow(lockNumber);
            }
        }

        /// <summary>
        /// Asynchronously locks the correct lock, depending on key, then attempts to run the loader.
        /// The lock is released on completion or failure of the loader.
        /// </summary>
        /// <typeparam name="TResult">The type of result, returned by the loader method</typeparam>
        /// <param name="key">The key to use for choosing the lock</param>
        /// <param name="loader">A method to run for loading the result</param>
        /// <returns>A task that returns the result of the loader</returns>
        public async Task<TResult> LockAndLoadAsync<TResult>(String key, Func<Task<TResult>> loader)
        {
            var lockNumber = GetLockNumber(key);

            // If failed to get lock, check if already have this lock (higher up in call stack)
            if (doEnableReentrantLocking && IsFlowAlreadyMarked(lockNumber))
                return await loader().ConfigureAwait(false);

            // Do this BEFORE first async call, otherwise it won't affect current thread!
            var wasMarked = MarkFlow(lockNumber);
            try
            {
                // Wait for lock with or without timeout (depending on field)
                var gotLock = await TakeAsync(lockNumber);
                if (!gotLock && doThrowExceptionOnTimeout)
                    throw new CacheLockException($"Failed to obtain lock for {key}");

                try
                {
                    return await loader().ConfigureAwait(false);
                }
                finally
                {
                    if (gotLock)
                        Release(lockNumber);
                }
            }
            finally
            {
                if (wasMarked)
                    UnmarkFlow(lockNumber);
            }
        }
    }
}
