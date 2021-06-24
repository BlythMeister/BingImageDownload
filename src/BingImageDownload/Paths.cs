using System;
using System.IO;

namespace BingImageDownload
{
    internal class Paths
    {
        internal string BasePath { get; }
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
                var homeDir = Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX ? Environment.GetEnvironmentVariable("HOME") : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");

                if (string.IsNullOrWhiteSpace(homeDir))
                {
                    throw new NullReferenceException("No directory passed & unable to locate 'HOME' path");
                }

                BasePath = Path.Combine(homeDir, "BingImageDownload");
            }
            else
            {
                BasePath = basePath;
            }

            SavePath = Path.Combine(BasePath, "Images"); ;
            ArchivePath = Path.Combine(BasePath, "Archive");
            AppData = Path.Combine(BasePath, "App_Data");
            DownloadPath = Path.Combine(BasePath, "App_Data", "Temp");
            HistogramPath = Path.Combine(BasePath, "App_Data", "TempHistogram");
            LogPath = Path.Combine(BasePath, "Logs");

            if (!Directory.Exists(BasePath))
            {
                Directory.CreateDirectory(BasePath);
            }

            if (!Directory.Exists(SavePath))
            {
                Directory.CreateDirectory(SavePath);
            }

            if (!Directory.Exists(ArchivePath))
            {
                Directory.CreateDirectory(ArchivePath);
            }

            if (!Directory.Exists(AppData))
            {
                Directory.CreateDirectory(AppData);
            }

            if (!Directory.Exists(DownloadPath))
            {
                Directory.CreateDirectory(DownloadPath);
            }

            if (!Directory.Exists(HistogramPath))
            {
                Directory.CreateDirectory(HistogramPath);
            }

            if (!Directory.Exists(LogPath))
            {
                Directory.CreateDirectory(LogPath);
            }
        }
    }
}
