using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace GZipArch.Test {
    [TestClass]
    public class SettingsReaderTest {
        protected string tmpFile;
        protected string resFile;

        [TestInitialize]
        public void Init() {
            tmpFile = Path.GetTempFileName();
            resFile = Path.ChangeExtension(tmpFile, "gz");
        }
        [TestCleanup]
        public void Cleanup() {
            if (File.Exists(tmpFile))
                File.Delete(tmpFile);
        }

        [TestMethod]
        public void NotParamsTest() {
            Assert.ThrowsException<Exception>(() => SettingsReader.Read(null));
            Assert.ThrowsException<Exception>(() => SettingsReader.Read(new string[0]));
        }

        [TestMethod]
        public void ReadCompressionModeTest() {
            Assert.AreEqual(SettingsReader.Read(new string[] { "compress", tmpFile, resFile }).CompressionMode, System.IO.Compression.CompressionMode.Compress);
            Assert.AreEqual(SettingsReader.Read(new string[] { "c", tmpFile, resFile }).CompressionMode, System.IO.Compression.CompressionMode.Compress);
            Assert.AreEqual(SettingsReader.Read(new string[] { " DECOMPRESS ", tmpFile, resFile }).CompressionMode, System.IO.Compression.CompressionMode.Decompress);
            Assert.AreEqual(SettingsReader.Read(new string[] { "d", tmpFile, resFile }).CompressionMode, System.IO.Compression.CompressionMode.Decompress);
            Assert.ThrowsException<Exception>(() => SettingsReader.Read(new string[] { "s", tmpFile, resFile }));
        }

        [TestMethod]
        public void ReadInputFileTest() {
            Assert.ThrowsException<Exception>(() => SettingsReader.Read(new string[] { "compress", " ", resFile }));
            Assert.ThrowsException<Exception>(() => SettingsReader.Read(new string[] { "compress", tmpFile + "0", resFile }));
            Assert.IsNotNull(SettingsReader.Read(new string[] { "c", tmpFile + " ", resFile }));
        }

        [TestMethod]
        public void ReadOutFileTest() {
            Assert.ThrowsException<Exception>(() => SettingsReader.Read(new string[] { "compress", tmpFile, " " }));
            Assert.ThrowsException<Exception>(() => SettingsReader.Read(new string[] { "compress", tmpFile, Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "0")}));
            Assert.ThrowsException<Exception>(() => SettingsReader.Read(new string[] { "compress", tmpFile, tmpFile }));
            Assert.IsNotNull(SettingsReader.Read(new string[] { "c", tmpFile, resFile }));
        }
    }
}
