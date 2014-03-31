using System;
using System.Configuration;
using System.IO;
using System.Linq;
using BingWallpaper;

static internal class SetupAndTearDown
{
    internal static void Startup()
    {
        if (!Directory.Exists(Program.savePath)) Directory.CreateDirectory(Program.savePath);
        if (!Directory.Exists(Program.appData)) Directory.CreateDirectory(Program.appData);
        if (!Directory.Exists(BingInteractionAndParsing.downloadPath)) Directory.CreateDirectory(BingInteractionAndParsing.downloadPath);
        if (!Directory.Exists(ImageHashing.hitogramPath)) Directory.CreateDirectory(ImageHashing.hitogramPath);

        var logPath = Path.Combine(Program.savePath, "Logs");
        if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);
        ConsoleWriter.SetupLogWriter(Path.Combine(logPath, String.Format("Log-{0}.txt", DateTime.UtcNow.ToString("yyyy-MM-dd"))));


        BingInteractionAndParsing.urlsRetrieved.AddRange(Serializer.Deserialize<string>(Path.Combine(Program.appData, "urlsRetrieved.bin")));
        
        foreach (var file in Directory.GetFiles(Program.savePath, "*.jpg"))
        {
            ImageHashing.AddHash(file);
        }

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
        if (Directory.Exists(BingInteractionAndParsing.downloadPath)) Directory.Delete(BingInteractionAndParsing.downloadPath, true);
        if (Directory.Exists(ImageHashing.hitogramPath)) Directory.Delete(ImageHashing.hitogramPath, true);
        if (BingInteractionAndParsing.urlsRetrieved.Any())
        {
            Serializer.Serialize(BingInteractionAndParsing.urlsRetrieved, Path.Combine(Program.appData, "urlsRetrieved.bin"));    
        }
    }

    internal static void ArchiveOldImages()
    {
        var archiveMonths = int.Parse(ConfigurationManager.AppSettings["ArchiveAfterMonths"]);
        var archivePath = Path.Combine(Program.savePath, "Archive");

        if (archiveMonths <= 0) return;

        foreach (var file in Directory.GetFiles(Program.savePath))
        {
            var fileInfo = new FileInfo(file);
            if (fileInfo.LastAccessTimeUtc < DateTime.UtcNow.AddMonths(archiveMonths * -1))
            {
                if (!Directory.Exists(archivePath)) Directory.CreateDirectory(archivePath);
                fileInfo.MoveTo(Path.Combine(archivePath, fileInfo.Name));
            }
        }
    }
}