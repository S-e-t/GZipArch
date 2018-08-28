using System;
using System.IO;
using System.IO.Compression;

namespace GZipTest {

    public static class SettingsReader {
        private static CompressionMode ReadMode(string mode) {
            switch ((mode ?? string.Empty).Trim().ToLower()) {
                case "compress":
                case "c":
                    return CompressionMode.Compress;
                case "decompress":
                case "d":
                    return CompressionMode.Decompress;
                default:
                    throw new Exception("Неверный формат направления сжатия, параметр должен задаваться в командной строке следующим образом: \n\r GZipTest.exe compress/decompress [имя исходного файла] [имя результирующего файла]");
            }
        }

        private static FileInfo GetInfoInputFile(string path) {
            path = (path ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(path))
                throw new Exception("Путь исходного файла не задан");

            if (!File.Exists(path))
                throw new Exception("Не найден исходный файл " + path);

            return new FileInfo(path);
        }

        private static string GetInfoOutFile(string path) {
            path = (path ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(path))
                throw new Exception("Путь результирующего фаила не задан");

            var dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                throw new Exception("Директория результирующего файла не найдена " + dir);

            if (File.Exists(path))
                throw new Exception("Результирующий файл " + path + " существует");

            return path;
        }

        public static Settings Read(string[] args) {
            if (args == null || args.Length < 3)
                throw new Exception("Параметры программы, имена исходного и результирующего файлов должны задаваться в командной строке следующим образом: \n\r GZipTest.exe compress/decompress [имя исходного файла] [имя результирующего файла]");

            return new Settings {
                CompressionMode = ReadMode(args[0]),
                InputFile = GetInfoInputFile(args[1]),
                Output = GetInfoOutFile(args[2]),
                BlockSize = 1024 * 1024
            };
        }
    }
}
