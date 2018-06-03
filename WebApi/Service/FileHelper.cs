using System.IO;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable 1591

namespace TestHost.WebApi.Service
{
    public static class FileHelper
    {
        private const int DefaultBufferSize = 4096;

        private const FileOptions DefaultOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;

        public static string ReadLine(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, DefaultOptions))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadLine();
            }
        }

        public static async Task<string> ReadLineAsync(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, DefaultOptions))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return await reader.ReadLineAsync();
            }
        }
    }
}