using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncBasics
{
    [TestFixture]
    public class AsyncBasicsTests
    {
        #region Pausing for a period of time

        [Test]
        public async Task DelayResultTestAsync()
        {
            int result = await DelayResult(42, TimeSpan.FromSeconds(1));
            Assert.AreEqual(42, result);
        }

        private async Task<T> DelayResult<T>(T result, TimeSpan delay)
        {
            await Task.Delay(delay);
            return result;
        }

        [Test]
        public async Task DownloadWithRetriesTestAsync()
        {
            string result = await DownloadWithRetriesAsync("http://google.com");
            Assert.IsNotEmpty(result);
        }

        private async Task<string> DownloadWithRetriesAsync(string url)
        {
            using(var client = new HttpClient())
            {
                TimeSpan nextDelay = TimeSpan.FromSeconds(1);
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        return await client.GetStringAsync(url);
                    }
                    catch (Exception)
                    {
                    }

                    await Task.Delay(nextDelay);
                    nextDelay = nextDelay + nextDelay;
                }

                return await client.GetStringAsync(url);
            }
        }

        [Test]
        public async Task DownloadWithTimeoutTest()
        {
            string result = await DownloadWithTimeoutAsync("http://google.com", TimeSpan.FromSeconds(2));
            Assert.IsNotEmpty(result);
        }

        private async Task<string> DownloadWithTimeoutAsync(string url, TimeSpan timeout)
        {
            using (var client = new HttpClient())
            {
                var downloadTask = client.GetStringAsync(url);
                var timeoutTask = Task.Delay(timeout);
                var firstCompleted = await Task.WhenAny(downloadTask, timeoutTask);
                if(ReferenceEquals(firstCompleted, timeoutTask))
                {
                    return null;
                }

                return await downloadTask;
            }
        }

        #endregion

        #region Returning completed Task

        public interface IMyAsyncInterface
        {
            Task<int> GetValueAsync();
        }

        public class MySyncImplementation : IMyAsyncInterface
        {
            public Task<int> GetValueAsync()
            {
                return Task.FromResult(42);
            }
        }

        [Test]
        public async Task CompletedTaskTestAsync()
        {
            var originalThreadId = Thread.CurrentThread.ManagedThreadId;
            IMyAsyncInterface itf = new MySyncImplementation();
            var result = await itf.GetValueAsync().ConfigureAwait(false);
            Assert.AreEqual(originalThreadId, Thread.CurrentThread.ManagedThreadId);
        }

        #endregion

        #region Reporting progress

        private async Task<string> MethodMakingProgressAsync(IProgress<int> progress)
        {
            int completed = 0;
            while(completed < 100)
            {
                completed++;
                if (progress != null)
                {
                    progress.Report(completed);
                }

                await Task.Delay(10);
            }

            return "Work is completed";
        }

        [Test]
        public async Task RepotingProgressTestAsync()
        {
            Progress<int> progress = new Progress<int>(p => Debug.WriteLine("[" + Thread.CurrentThread.ManagedThreadId + "] progress = " + p));
            var result = await MethodMakingProgressAsync(progress);
            Assert.IsNotEmpty(result);
        }

        #endregion

        #region Waiting for a set of Tasks to complete

        [Test]
        public async Task DownloadFromTwoUrlsTestAsync()
        {
            string url1 = "http://google.com";
            string url2 = "http://gmail.com";

            string[] contents;

            using (var client = new HttpClient())
            {
                var downloadTask1 = client.GetStringAsync(url1);
                var downloadTask2 = client.GetStringAsync(url2);
                contents = await Task.WhenAll(downloadTask1, downloadTask2);
            }

            Assert.AreEqual(2, contents.Length);
        }

        [Test]
        public async Task OnlyOneExceptionIsThrownWhenAwaitedMultipleTasksTest()
        {
            Func<Task> method1 = async () =>
            {
                await Task.Delay(1);
                throw new NotImplementedException();
            };

            Func<Task> method2 = async () =>
            {
                await Task.Delay(1);
                throw new InvalidOperationException();
            };

            try
            {
                Task allDone = Task.WhenAll(method1(), method2());
                await allDone;
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is NotImplementedException || e is InvalidOperationException);
            }
        }

        [Test]
        public async Task ErrorPropertyContainsAllExceptionsFromAwaitedTasksTest()
        {
            Func<Task> method1 = async () =>
            {
                await Task.Delay(1);
                throw new NotImplementedException();
            };

            Func<Task> method2 = async () =>
            {
                await Task.Delay(1);
                throw new InvalidOperationException();
            };

            Task allDone = null;
            try
            {
                allDone = Task.WhenAll(method1(), method2());
                await allDone;
            }
            catch (Exception)
            {
                Assert.IsInstanceOf<AggregateException>(allDone.Exception);
                Assert.AreEqual(2, allDone.Exception.InnerExceptions.Count);
            }
        }

        #endregion

        #region Waiting for Any Task to complete

        [Test]
        public async Task GetResultFromFastestSourceTestAsync()
        {
            Func<Task<int>> method1 = async () =>
            {
                await Task.Delay(100);
                return 1;
            };

            Func<Task<int>> method2 = async () =>
            {
                await Task.Delay(1);
                return 2;
            };

            var firstCompletedTask = await Task.WhenAny(method1(), method2());
            int result = await firstCompletedTask;

            Assert.AreEqual(2, result);
        }

        #endregion
    }
}
