using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace GZipArch.Test {
    [TestClass]
    public class ExtensionsTest {

        protected Random rnd;
        [TestInitialize]
        public void Init() {
            rnd = new Random();
        }

        [TestMethod]
        public void IntByteConvertTest() {
            var i = rnd.Next();
            var bs = i.ToBytes();
            Assert.AreEqual<int>(i, bs.ToInt());
            Assert.AreEqual<int>(1, new byte[] { 1, 0, 0, 0 }.ToInt());
            Assert.AreEqual<int>(256, new byte[] { 0, 1, 0, 0 }.ToInt());
        }

        [TestMethod]
        public void InsertLengthToStartTest() {
            var bs = rnd.Next(Int32.MaxValue).ToBytes();
            var vithLength = bs.InsertLengthToStart();
            Assert.AreEqual<int>(8, vithLength.Length);
            Assert.IsTrue(Enumerable.SequenceEqual(new byte[] { 4, 0, 0, 0 }.Concat(bs), vithLength));
        }
        [TestMethod]
        public void ReadIntTest() {
            var i = rnd.Next();
            using (var m = new MemoryStream(i.ToBytes())) {
                Assert.AreEqual<int>(i, m.ReadInt());
            }
        }

        [TestMethod]
        public void CompressionModeTest() {
            var input = new byte[1024 * 4];
            rnd.NextBytes(input);
            Assert.IsTrue(Enumerable.SequenceEqual(input, input.GZip(CompressionMode.Compress).GZip(CompressionMode.Decompress)));
        }


        [TestMethod]
        public void GetSizeReadableTest() => 
            Assert.AreEqual<string>(4355784704.GetSizeReadable(), "4,06 GB");
        

    }
}
