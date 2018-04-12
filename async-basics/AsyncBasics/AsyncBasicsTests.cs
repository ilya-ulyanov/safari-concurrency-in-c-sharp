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
            int result = await AsyncUtils.DelayResult(42, TimeSpan.FromSeconds(1));
            Assert.AreEqual(42, result);
        }

        [Test]
        public async Task DownloadWithRetriesTestAsync()
        {
            string result = await AsyncUtils.DownloadAsStringWithRetriesAsync("http://google.com");
            Assert.IsNotEmpty(result);
        }

        [Test]
        public async Task DownloadWithTimeoutTest()
        {
            string result = await AsyncUtils.DownloadAsStringWithTimeoutAsync("http://google.com");
            Assert.IsNotEmpty(result);
        }

        #endregion

        #region Returning completed Task

        public interface IMyAsyncInterface
        {
            Task<int> GetValueAsync();

            Task<T> NotImplementedAsync<T>();
        }

        public class MySyncImplementation : IMyAsyncInterface
        {
            public Task<int> GetValueAsync()
            {
                return Task.FromResult(42);
            }

            public Task<T> NotImplementedAsync<T>()
            {
                return Task.FromException<T>(new NotImplementedException());
                //var tcs = new TaskCompletionSource<T>();
                //tcs.SetException(new NotImplementedException());
                //return tcs.Task;
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

        [Test]
        public void FailedTaskTestAsync()
        {
            IMyAsyncInterface itf = new MySyncImplementation();
            Assert.ThrowsAsync<NotImplementedException>(async () => await itf.NotImplementedAsync<int>());
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
            string[] contents = await AsyncUtils.DownloadAsStringsWithTimeoutAsync(new string[] { "http://google.com", "http://gmail.com" });
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

        #region Processing Tasks as they complete

        [Test]
        public async Task ProcessTasksResultsAsTheyCompleteTestAsync()
        {
            Func<int, Task<int>> delayAndReturn = async input =>
            {
                await Task.Delay(TimeSpan.FromSeconds(input));
                return input;
            };

            int result = -1;
            Action<Task<int>> handler = async task =>
            {
                var r = await task;
                Debug.WriteLine(r);
                Interlocked.CompareExchange(ref result, r, -1);
            };

            Task taskA = delayAndReturn(2).ContinueWith(handler);
            Task taskB = delayAndReturn(3).ContinueWith(handler);
            Task taskC = delayAndReturn(1).ContinueWith(handler);

            Task[] tasks = { taskA, taskB, taskC };

            await Task.WhenAll(tasks);

            Assert.AreEqual(1, result);
        }

        #endregion
    }
}
