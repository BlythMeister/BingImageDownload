using System;
using System.IO;

namespace BingImageDownload
{
    internal class FileClearer
    {
        private readonly ConsoleWriter consoleWriter;
        private readonly Paths paths;
        private readonly int archiveMonths;
        private readonly int deleteMonths;

        public FileClearer(ConsoleWriter consoleWriter, Paths paths, int? archiveMonths, int? deleteMonths)
        {
            this.consoleWriter = consoleWriter;
            this.paths = paths;
            this.archiveMonths = archiveMonths ?? 1;
            this.deleteMonths = deleteMonths ?? 0;
        }

        internal void ArchiveOldImages()
        {
            try
            {
                if (archiveMonths <= 0)
                {
                    return;
                }

                foreach (var file in Directory.GetFiles(paths.SavePath))
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTimeUtc < DateTime.UtcNow.AddMonths(archiveMonths * -1))
                    {
                        consoleWriter.WriteLine($"Archiving image {fileInfo.Name}");
                        fileInfo.MoveTo(Path.Combine(paths.ArchivePath, fileInfo.Name));
                    }
                }
            }
            catch (Exception exception)
            {
                consoleWriter.WriteLine("Error archiving image", exception);
            }
        }

        internal void DeleteOldImages()
        {
            try
            {
                if (deleteMonths <= 0)
                {
                    return;
                }

                foreach (var file in Directory.GetFiles(paths.ArchivePath))
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTimeUtc < DateTime.UtcNow.AddMonths(deleteMonths * -1))
                    {
                        consoleWriter.WriteLine($"Deleting image {fileInfo.Name}");
                        fileInfo.Delete();
                    }
                }
            }
            catch (Exception exception)
            {
                consoleWriter.WriteLine("Error deleting image", exception);
            }
        }

        public void ClearLogFiles()
        {
            foreach (var file in Directory.GetFiles(paths.LogPath))
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
                        consoleWriter.WriteLine("Error clearing a log file", exception);
                    }
                }
            }
        }

        public void ClearTempFolders()
        {
            try
            {
                if (Directory.Exists(paths.DownloadPath))
                {
                    Directory.Delete(paths.DownloadPath, true);
                }

                if (Directory.Exists(paths.HistogramPath))
                {
                    Directory.Delete(paths.HistogramPath, true);
                }
            }
            catch (Exception exception)
            {
                consoleWriter.WriteLine("Error cleaning up", exception);
            }
        }
    }
}
