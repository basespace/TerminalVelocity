using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Illumina.TerminalVelocity.Tests
{
    [TestFixture]
    public class BufferManagerTests
    {
        public static IEnumerable<BufferQueueSetting> CreateSettings(uint howMany, uint chunkSizeStart, uint queueDepth)
        {
        var settings = new List<BufferQueueSetting>(4);
            for (int i = 0; i < howMany; i++)
            {
                settings.Add(new BufferQueueSetting((uint) (chunkSizeStart * (i+1)), queueDepth));
            }
            return settings;
        }

        [Test]
        public void CanWeAllocateBuffers()
        {
            var settings = CreateSettings(1, 10, 1);
            var manager = new BufferManager(settings);
            byte[] test = manager.GetBuffer(10);
            test[0] = 0x01;
            Assert.NotNull(test);
            Assert.AreEqual(test.Length, 10); //we get the same length
          
            manager.FreeBuffer(ref test);

            Assert.True(test == BufferManager.EmptyBuffer); //we aren't referencing it anymore

            test = manager.GetBuffer(10);
            Assert.True(test[0] == 0x01);//we got the same one back
        }

        [Test]
        public void CanWeClearBufferContent()
        {
            var settings = CreateSettings(1, 10, 1);
            var manager = new BufferManager(settings);
            byte[] test = manager.GetBuffer(10);
            test[0] = 0x01;
            Assert.NotNull(test);
            Assert.AreEqual(test.Length, 10); //we get the same length

            manager.FreeBuffer(ref test, true);

            test = manager.GetBuffer(10);
            Assert.True(test[0] == 0x00);//we got a cleared one
        }

        [Test]
        public void GettingMoreThanInitiallyAllocatedWorks()
        {
            var settings = CreateSettings(1, 10, 1);
            var manager = new BufferManager(settings);
            byte[] test = manager.GetBuffer(10);
            Assert.NotNull(test);
            test = manager.GetBuffer(10);
            Assert.NotNull(test);
        }

        [Test]
        public void GettingADifferentSizeThenConfigured()
        {
            var settings = CreateSettings(1, 10, 1);
            var manager = new BufferManager(settings);
            byte[] test = manager.GetBuffer(11);
            Assert.NotNull(test);
            test = manager.GetBuffer(10);
            Assert.NotNull(test);
        }

        [Test]
        public void DisposeWorks()
        {
            var settings = CreateSettings(1, 10, 1);
            var manager = new BufferManager(settings);
            manager.Dispose();
        }
    }
}
