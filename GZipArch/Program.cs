using System;
using System.IO;

namespace GZipArch {
    class Program {
        static void Exect(Stream read
            , Stream write
            , ReadWriteManager manager
            , System.IO.Compression.CompressionMode mode
            , int? readBufferSize, int tCount) {
            manager.CreateThreadPool(
                tCount
                , () => read.BeginRead(
                        manager.GetReadExectContext(
                            block => write.BeginWrite(
                                block.GZip(mode)
                                , manager.GetWriteExectContext()))
                            , readBufferSize))
                        .ForEach(t => t.Start());

            long res = 0, old = 0;
            while (!manager.WaitOne(100)) 
                if (old < (res = (100 * read.Position) / read.Length)) 
                    Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " Прогресс: " + (old = res) + "%");

            manager.IsExceptionHappened();

            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " Завершено за " + manager.TotalTime().ToString());
        }

        static void Main(string[] args) {
            try {
                var settings = SettingsReader.Read(args);

                Console.WriteLine(string.Join(" ", new[] { DateTime.Now.ToString("HH:mm:ss.fff"), "Запущен GZipArch:",
                    Environment.NewLine + "Программа предназначенна для поблочного сжатия и расжатия файлов с помощью System.IO.Compression.GzipStream.",
                    Environment.NewLine + "Для компрессии исходный файл делится на блоки в" , settings.BlockSize.GetSizeReadable(), ". Каждый блок компрессится и записывается в выходной файл независимо от остальных блоков.",
                    Environment.NewLine + "Программа в", Environment.ProcessorCount.ToString(), "потока", settings.IsCompression ? "сожмёт" : "распакует",
                    Environment.NewLine + settings.InputFile.FullName, "("+settings.InputFile.Length.GetSizeReadable()+")", Environment.NewLine + "в", settings.Output
                }));

                var manager = new ReadWriteManager();

                using (var read = new FileStream(
                    settings.InputFile.FullName
                    , FileMode.Open
                    , FileAccess.Read
                    , FileShare.Read
                    , settings.BlockSize
                    , FileOptions.Asynchronous))
                    try {
                        using (var write = new FileStream(
                            settings.Output
                            , FileMode.CreateNew
                            , FileAccess.Write
                            , FileShare.Write
                            , settings.BlockSize
                            , FileOptions.Asynchronous))
                            Exect(read
                                , write
                                , manager
                                , settings.CompressionMode
                                , settings.ReadBufferSize
                                , settings.ThreadCount);
                    }
                    catch (Exception e) {
                        if (File.Exists(settings.Output))
                            File.Delete(settings.Output);
                        throw e;
                    }
            }
            catch (Exception e) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(e);
                Console.ResetColor();
            }
            Console.WriteLine("Нажмите любую клавишу для завершения");
            Console.Read();
        }
    }
}
