using System;
using System.IO;
using System.IO.Compression;

namespace GZipTest {
    /// <summary>
    /// Обрабатываемые блоки 
    /// </summary>
    public sealed class BinaryBloc {
        /// <summary>
        /// Идентификатор блока
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// Буфер
        /// </summary>
        public byte[] Buffer { get; set; }

        public BinaryBloc GZip(CompressionMode mode) {
            Buffer = Buffer.GZip(mode);
            /* Из-за ограничения в 4GB на размер входного потока для GZipStream при сжатии будем явно указывать длину блоков в первых 4 байтах каждого блока*/
            Buffer = CompressionMode.Compress.Equals(mode) ? Buffer.InsertLengthToStart() : Buffer;
            return this;
        }
    }

    public sealed class StreamState {
        public BinaryBloc Bloc;
        public Stream Stream;
        public int? BufSize;
        public IStreamExectContext Context;

        public IAsyncResult BeginRead(AsyncCallback endRead) =>
            Stream.BeginRead(Bloc.Buffer, 0, Bloc.Buffer.Length, endRead, this);

        public BinaryBloc EndRead(IAsyncResult ar) {
            var countRead = Stream.EndRead(ar);
            if (countRead != Bloc.Buffer.Length)
                Bloc.Buffer = Bloc.Buffer.Resize(countRead);
            return countRead == 0 ? null : Bloc;
        }

        public IAsyncResult BeginWrite(AsyncCallback endWrite) =>
            Stream.BeginWrite(Bloc.Buffer, 0, Bloc.Buffer.Length, endWrite, this);

        public BinaryBloc EndWrite(IAsyncResult ar) {
            Stream.EndWrite(ar);
            return Bloc;
        }
    }


    public sealed class Settings {
        public CompressionMode CompressionMode { get; set; }
        public FileInfo InputFile { get; set; }
        public string Output { get; set; }
        public int BlockSize { get; set; }
        public int ThreadCount => Environment.ProcessorCount * 2;
        public int? ReadBufferSize => IsCompression ? BlockSize : null as int?;
        public bool IsCompression => CompressionMode.Compress.Equals(CompressionMode);
        public bool IsDecompress => CompressionMode.Decompress.Equals(CompressionMode);

        
    }
}
