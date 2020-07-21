using System.IO;

namespace BingImageDownload
{
    internal class Paths
    {
        internal string SavePath { get; }
        internal string ArchivePath { get; }
        internal string AppData { get; }
        internal string DownloadPath { get; }
        internal string HistogramPath { get; }
        internal string LogPath { get; }

        internal Paths(string basePath)
        {
            if (string.IsNullOrWhiteSpace(basePath))
            {
                basePath = @"C:\Temp\BingImageDownload";
            }

            SavePath = basePath;
            ArchivePath = Path.Combine(basePath, "Archive");
            AppData = Path.Combine(basePath, "App_Data");
            DownloadPath = Path.Combine(basePath, "App_Data", "Temp");
            HistogramPath = Path.Combine(basePath, "App_Data", "TempHistogram");
            LogPath = Path.Combine(basePath, "Logs");

            if (!Directory.Exists(SavePath)) Directory.CreateDirectory(SavePath);
            if (!Directory.Exists(ArchivePath)) Directory.CreateDirectory(ArchivePath);
            if (!Directory.Exists(AppData)) Directory.CreateDirectory(AppData);
            if (!Directory.Exists(DownloadPath)) Directory.CreateDirectory(DownloadPath);
            if (!Directory.Exists(HistogramPath)) Directory.CreateDirectory(HistogramPath);
            if (!Directory.Exists(LogPath)) Directory.CreateDirectory(LogPath);
        }
    }
}
