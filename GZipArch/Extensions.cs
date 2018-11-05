using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace GZipArch {
    public static class Extensions {

        private const int BUFFER_SIZE = 256 * 1024;

        public static byte[] GZip(this byte[] val, CompressionMode mode, int bufferSize = BUFFER_SIZE) {
            switch (mode) {
                case CompressionMode.Compress:
                    return val.GZipCompress(val == null || val.Length > bufferSize ? bufferSize : val.Length);
                case CompressionMode.Decompress:
                    return val.GZipDecompress(bufferSize);
                default:
                    throw new Exception("Неверный формат направления сжатия");
            }
        }

        public static byte[] GZipCompress(this byte[] val, int bufferSize) {
            if (val == null || val.Length == 0)
                return val;

            using (var output = new MemoryStream()) {
                using (var buf = new BufferedStream(new GZipStream(output,
                 CompressionMode.Compress), bufferSize)) {
                    buf.Write(val, 0, val.Length);
                }
                return output.ToArray();
            }
        }

        public static byte[] GZipDecompress(this byte[] val, int bufferSize) {
            if (val == null || val.Length == 0)
                return val;

            var buf = new byte[bufferSize];
            int n;
            using (var output = new MemoryStream()) {
                using (var input = new MemoryStream(val)) {
                    using (var gzs = new BufferedStream(new GZipStream(input, CompressionMode.Decompress), buf.Length)) {
                        while ((n = gzs.Read(buf, 0, buf.Length)) != 0) {
                            Array.Resize(ref buf, n);
                            output.Write(buf, 0, n);
                        }
                    }
                }
                return output.ToArray();
            }
        }

        public static byte[] InsertLengthToStart(this byte[] val) => val.InsertIntToStart(val.Length);

        public static byte[] InsertIntToStart(this byte[] val, int i) => i.ToBytes().Concat(val).ToArray();

        public static byte[] ToBytes(this int val) => BitConverter.GetBytes(val);

        public static string GetSizeReadable(this int i) => ((long)i).GetSizeReadable();

        public static string GetSizeReadable(this long i) {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = Math.Abs(i);
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1) {
                order++;
                len = len / 1024;
            }
            return string.Format("{0:0.##} {1}", len, sizes[order]);
        }

        public static T[] Resize<T>(this T[] array, int newSize) {
            Array.Resize(ref array, newSize);
            return array;
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action) {
            if (action == null)
                return source;

            foreach (var i in source) action?.Invoke(i);
            return source;
        }

        public static int ToInt(this byte[] val) => BitConverter.ToInt32(val, 0);        
    }
}
