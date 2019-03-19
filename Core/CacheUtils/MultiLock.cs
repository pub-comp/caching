using System;
using System.Threading;
using System.Threading.Tasks;

namespace PubComp.Caching.Core.CacheUtils
{
    public class MultiLock
    {
        private readonly SemaphoreSlim[] locks;
        private readonly uint[] scramblers;
        private const int MaxNumberOfLocks = 1000;

        public MultiLock(ushort numberOfLocks)
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
            var digits = numberOfLocks > 1 ? (int)Math.Ceiling(Math.Log(Math.Pow(2.0, 32.0)) / Math.Log(numberOfLocks)) : 1;
            var s = new uint[digits];
            var rand = new Random();

            for (int cnt = 0; cnt < l.Length; cnt++)
            {
                l[cnt] = new SemaphoreSlim(1, 1);
            }

            for (int cnt = 0; cnt < s.Length; cnt++)
            {
                s[cnt] = (uint)rand.Next(numberOfLocks);
            }

            this.locks = l;
            this.scramblers = s;
        }

        private uint GetLockNumber(string key)
        {
            var hash = (uint)key.GetHashCode();
            uint pos = 0;

            var numberOfLocks = (uint)this.locks.Length;
            var s = this.scramblers;

            // ReSharper disable once ForCanBeConvertedToForeach (performance)
            for (int cnt = 0; cnt < s.Length; cnt++)
            {
                var rem = hash % numberOfLocks;
                hash /= numberOfLocks;
                pos ^= rem ^ s[cnt];
            }

            pos %= numberOfLocks;
            return pos;
        }

        public void Take(string key)
        {
            var lockNumber = GetLockNumber(key);
            this.locks[lockNumber].Wait();
        }

        public void Release(string key)
        {
            var lockNumber = GetLockNumber(key);
            this.locks[lockNumber].Release();
        }

        public TResult LockAndLoad<TResult>(String key, Func<TResult> loader)
        {
            var lockNumber = GetLockNumber(key);
            this.locks[lockNumber].Wait();
            try
            {
                return loader();
            }
            finally
            {
                this.locks[lockNumber].Release();
            }
        }

        public async Task<TResult> LockAndLoadAsync<TResult>(String key, Func<Task<TResult>> loader)
        {
            var lockNumber = GetLockNumber(key);
            this.locks[lockNumber].Wait();
            try
            {
                return await loader().ConfigureAwait(false);
            }
            finally
            {
                this.locks[lockNumber].Release();
            }
        }
    }
}
