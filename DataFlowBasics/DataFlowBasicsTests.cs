using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace DataFlowBasics
{
    [TestFixture]
    public class DataFlowBasicsTests
    {
        [Test, Timeout(100)]
        public async Task LinkingBlocks_PropagateCompletionTest()
        {
            var multiplyBlock = new TransformBlock<int, int>(i => i * 2);
            var subtractBlock = new TransformBlock<int, int>(i => i - 2);

            multiplyBlock.LinkTo(subtractBlock, new DataflowLinkOptions { PropagateCompletion = true });

            multiplyBlock.Complete();

            await subtractBlock.Completion;
            Assert.Pass();
        }

        [Test]
        public void PropagatingErrorTest()
        {
            var block = new TransformBlock<int, int>(item =>
            {
                if (item == 1)
                {
                    throw new InvalidOperationException("Blech.");
                }

                return item * 2;
            });

            block.Post(1);
            block.Post(2);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await block.Completion);
        }

        [Test]
        public void LinkedBlocks_PropagatingErrorTestAsync()
        {
            var multiplyBlock = new TransformBlock<int, int>(item =>
            {
                if (item == 1)
                {
                    throw new InvalidOperationException("Blech.");
                }

                return item * 2;
            });

            var subtractBlock = new TransformBlock<int, int>(i => i - 2);

            multiplyBlock.LinkTo(subtractBlock, new DataflowLinkOptions { PropagateCompletion = true });

            multiplyBlock.Post(1);
            multiplyBlock.Post(2);

            Assert.ThrowsAsync<AggregateException>(async () => await subtractBlock.Completion);
        }

        [Test]
        public async Task UlinkingBlocksTest()
        {
            var block1 = new TransformBlock<int, int>(i => i * 2);
            var block2 = new ActionBlock<int>(i => Debug.WriteLine(i));
            var link = block1.LinkTo(block2);

            block1.Post(-1);
            block1.Post(2);
            await Task.Delay(2000);
            // unlink
            link.Dispose();

            block1.Post(-2);
            await Task.Delay(2000);
        }
    }
}
