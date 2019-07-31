using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BingWallpaper
{
    internal static class SetupAndTearDown
    {
        internal static void Startup()
        {
            if (!Directory.Exists(Program.SavePath)) Directory.CreateDirectory(Program.SavePath);
            if (!Directory.Exists(Program.ArchivePath)) Directory.CreateDirectory(Program.ArchivePath);
            if (!Directory.Exists(Program.AppData)) Directory.CreateDirectory(Program.AppData);
            if (!Directory.Exists(BingInteractionAndParsing.DownloadPath)) Directory.CreateDirectory(BingInteractionAndParsing.DownloadPath);
            if (!Directory.Exists(ImageHashing.HistogramPath)) Directory.CreateDirectory(ImageHashing.HistogramPath);

            var logPath = Path.Combine(Program.SavePath, "Logs");
            if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);
            ConsoleWriter.SetupLogWriter(Path.Combine(logPath, $"Log-{DateTime.UtcNow:yyyy-MM-dd}.txt"));

            BingInteractionAndParsing.UrlsRetrieved.AddRange(Serializer.Deserialize<List<string>>(Path.Combine(Program.AppData, "urlsRetrieved.bin")));
            BingInteractionAndParsing.Countries.AddRange(CultureInfo.GetCultures(CultureTypes.AllCultures).Where(x => x.Name.Contains("-")));
            ImageHashing.HistogramHashTable.AddRange(Serializer.Deserialize<List<HistogramHash>>(Path.Combine(Program.AppData, "imageHistogram.bin")));

            ConsoleWriter.WriteLine($"Have loaded {BingInteractionAndParsing.UrlsRetrieved.Count} previous URLs");
            ConsoleWriter.WriteLine($"Have loaded {BingInteractionAndParsing.Countries.Count} countries");
            ConsoleWriter.WriteLine($"Have loaded {ImageHashing.HistogramHashTable.Count} previous hashes");

            ArchiveOldImages();

            ImageHashing.RemoveInvalidHashEntries();
            HashExistingImages();

            if (ImageHashing.HistogramHashTable.Any())
            {
                Serializer.Serialize(ImageHashing.HistogramHashTable, Path.Combine(Program.AppData, "imageHistogram.bin"));
            }

            ClearLogFiles(logPath);
        }

        private static void HashExistingImages(int retryCount = 0)
        {
            try
            {
                foreach (var file in Directory.GetFiles(Program.SavePath, "*.jpg").Where(x => !ImageHashing.HaveFilePathInHashTable(x)))
                {
                    ConsoleWriter.WriteLine($"Hashing file: {file}");
                    ImageHashing.AddHash(file);
                }

                var preventArchiveDupes = bool.Parse(ConfigurationManager.AppSettings["PreventDuplicatesInArchive"]);
                if (preventArchiveDupes)
                {
                    foreach (var file in Directory.GetFiles(Program.ArchivePath, "*.jpg").Where(x => !ImageHashing.HaveFilePathInHashTable(x)))
                    {
                        ConsoleWriter.WriteLine($"Hashing file: {file}");
                        ImageHashing.AddHash(file);
                    }
                }
            }
            catch (Exception)
            {
                if (retryCount < 5)
                {
                    HashExistingImages(retryCount + 1);
                }
                else
                {
                    throw;
                }
            }

            ConsoleWriter.WriteLine($"Now have {ImageHashing.HistogramHashTable.Count} hashed images");
        }

        private static void ClearLogFiles(string logPath)
        {
            foreach (var file in Directory.GetFiles(logPath))
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastAccessTimeUtc < DateTime.UtcNow.AddDays(-28))
                {
                    try
                    {
                        fileInfo.Delete();
                    }
                    catch (Exception exception)
                    {
                        ConsoleWriter.WriteLine("Error clearing a log file", exception);
                    }
                }
            }
        }

        internal static void Finish()
        {
            try
            {
                if (Directory.Exists(BingInteractionAndParsing.DownloadPath)) Directory.Delete(BingInteractionAndParsing.DownloadPath, true);
                if (Directory.Exists(ImageHashing.HistogramPath)) Directory.Delete(ImageHashing.HistogramPath, true);
            }
            catch (Exception exception)
            {
                ConsoleWriter.WriteLine("Error cleaning up", exception);
            }

            if (BingInteractionAndParsing.UrlsRetrieved.Any())
            {
                Serializer.Serialize(BingInteractionAndParsing.UrlsRetrieved, Path.Combine(Program.AppData, "urlsRetrieved.bin"));
            }

            if (ImageHashing.HistogramHashTable.Any())
            {
                Serializer.Serialize(ImageHashing.HistogramHashTable, Path.Combine(Program.AppData, "imageHistogram.bin"));
            }
        }

        internal static void ArchiveOldImages()
        {
            try
            {
                var archiveMonths = int.Parse(ConfigurationManager.AppSettings["ArchiveAfterMonths"]);
                var archivePath = Path.Combine(Program.SavePath, "Archive");

                if (archiveMonths <= 0) return;

                foreach (var file in Directory.GetFiles(Program.SavePath))
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTimeUtc < DateTime.UtcNow.AddMonths(archiveMonths * -1))
                    {
                        if (!Directory.Exists(archivePath)) Directory.CreateDirectory(archivePath);
                        fileInfo.MoveTo(Path.Combine(archivePath, fileInfo.Name));
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
