using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AsyncBasics
{
    public static class AsyncUtils
    {
        public static async Task<T> DelayResult<T>(T result, TimeSpan delay)
        {
            await Task.Delay(delay);
            return result;
        }

        public static async Task<string> DownloadAsStringWithRetriesAsync(string url)
        {
            using (var httpClient = new HttpClient())
            {
                var nextDelay = TimeSpan.FromSeconds(1);
                for (int i = 0; i <= 3; ++i)
                {
                    try
                    {
                        return await httpClient.GetStringAsync(url);
                    }
                    catch
                    {
                    }

                    await Task.Delay(nextDelay);
                    nextDelay = nextDelay + nextDelay;
                }

                return await httpClient.GetStringAsync(url);
            }
        }

        public static async Task<string> DownloadAsStringWithTimeoutAsync(string url, int timeoutInSeconds = 10)
        {
            using (var httpClient = new HttpClient())
            {
                Task<string> downloadTask = httpClient.GetStringAsync(url);
                Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutInSeconds));
                Task firstCompletedTask = await Task.WhenAny(downloadTask, timeoutTask);
                if (ReferenceEquals(timeoutTask, firstCompletedTask))
                {
                    return null;
                }

                return await downloadTask;
            }
        }

        public static async Task<string[]> DownloadAsStringsWithTimeoutAsync(IEnumerable<string> urls, int timeoutInSeconds = 10)
        {
            IEnumerable<Task<string>> downloadTasks = urls.Select(url => DownloadAsStringWithTimeoutAsync(url, timeoutInSeconds));
            string[] result = await Task.WhenAll(downloadTasks.ToArray());
            return result;
        }
    }
}
