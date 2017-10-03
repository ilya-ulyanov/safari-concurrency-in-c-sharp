using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelBasics
{
    [TestFixture]
    public class ParallelBasicsTests
    {
        private class WorkItem
        {
            private Random random = new Random();

            public WorkItem()
            {
                this.Duration = this.random.Next(100, 500);
            }

            public int Duration { get; }
        }

        [Test]
        public void ProcessWorkInParallelTest()
        {
            var items = Enumerable.Range(1, 100).Select(_ => new WorkItem());
            var result = Parallel.ForEach(items, item => Thread.Sleep(item.Duration));
            Assert.IsTrue(result.IsCompleted);
        }

        [Test]
        public void StopParallelProcessingFromInsideTest()
        {
            var items = Enumerable.Range(1, 100).Select(_ => new WorkItem());
            int processedItemsCount = 0;
            var result = Parallel.ForEach(items, (item, state) =>
            {
                if (item.Duration > 450)
                {
                    state.Stop();
                    return;
                }

                Thread.Sleep(item.Duration);
                Interlocked.Increment(ref processedItemsCount);
            });

            Assert.IsFalse(result.IsCompleted);
            Assert.Greater(processedItemsCount, 0);
        }

        [Test]
        public void CancelParallelProcessingFromOutsideTest()
        {
            var items = Enumerable.Range(1, 100).Select(_ => new WorkItem());
            CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            Assert.Throws<OperationCanceledException>(() =>
            {
                Parallel.ForEach(
                    items, 
                    new ParallelOptions { CancellationToken = cts.Token },
                    item => Thread.Sleep(item.Duration)
                );
            });
        }

        [Test]
        public void ParallelAggregationTest()
        {
            var items = Enumerable.Range(1, 10000);
            int result = 0;
            Parallel.ForEach(
                items, 
                () => 0, 
                (item, state, localSum) => localSum += item, 
                localSum => Interlocked.Add(ref result, localSum));
            Assert.AreEqual(50005000, result);
        }
    }
}
