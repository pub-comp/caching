using System;
using System.Threading.Tasks;
using PubComp.Caching.AopCaching;

#pragma warning disable 1591

namespace TestHost.WebApi.Service
{
    public class ExampleV2Service
    {
        public const string CacheName = "ScopedNumbers";

        private readonly string dbPath;

        public ExampleV2Service() : this(Startup.DbPath)
        {
        }

        private ExampleV2Service(string dbPath)
        {
            this.dbPath = dbPath;
        }

        public string Get(int id)
        {
            var num = DateTime.Now.Ticks % id;
            return $"{id}>{num} - " + GetInner(num);
        }

        public async Task<string> GetAsync(int id)
        {
            var num = DateTime.Now.Ticks % id;
            return $"{id}>{num} - " + await GetAsyncInner(num).ConfigureAwait(false);
        }

        [Cache(CacheName)]
        private string GetInner(long id)
        {
            return $"{id} - {DateTimeOffset.UtcNow} - " + FileHelper.ReadLine($"{dbPath}\\{id}.txt");
        }
        
        [Cache(CacheName)]
        private async Task<string> GetAsyncInner(long id)
        {
            return $"{id} - {DateTimeOffset.UtcNow} - " + await FileHelper
                       .ReadLineAsync($"{dbPath}\\{id}.txt")
                       .ConfigureAwait(false);
        }
    }
}