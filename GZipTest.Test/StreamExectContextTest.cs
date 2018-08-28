using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace GZipTest.Test {
    [TestClass]
    public class StreamExectContextTest {

        private byte[] Exect(byte[] buf, Func<BinaryBloc, BinaryBloc> process, int? bLeng, int countThread) {
            var manager = new ReadWriteManager(true);
            using (var read = new MemoryStream(buf)) {
                using (var write = new MemoryStream()) {
                    manager.CreateThreadPool(countThread,
                        () => read.BeginRead(
                            manager.GetReadExectContext(
                                block => write.BeginWrite(process(block), manager.GetWriteExectContext())), bLeng))
                            .ForEach(t => t.Start());
                    manager.WaitOne();
                    manager.IsExceptionHappened();
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " Завершено за " + manager.TotalTime().ToString());
                    return write.ToArray();
                }
            }
        }

        [TestMethod]
        public void ReadWriteTest() {           
            var buf = new byte[10500];
            new Random().NextBytes(buf);
            Assert.IsTrue(Enumerable.SequenceEqual(buf, Exect(buf, b => b, 1024, Environment.ProcessorCount)));
        }

        [TestMethod]
        public void CompressDecompressTest() {
            var buf = new byte[5000];
            new Random().NextBytes(buf);
            var compress = Exect(buf, b => b.GZip(CompressionMode.Compress), 1024, 1);            
            var decompress = Exect(compress, b => b.GZip(CompressionMode.Decompress), null, 2);
            Assert.IsTrue(Enumerable.SequenceEqual(buf, decompress));
        }
    }    
}
