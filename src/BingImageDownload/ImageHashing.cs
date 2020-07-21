using AForge.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BingImageDownload
{
    internal class ImageHashing
    {
        private readonly ConsoleWriter consoleWriter;
        private readonly Paths paths;
        private readonly List<HistogramHash> histogramHashTable;
        private readonly string histogramBinFile;

        internal ImageHashing(ConsoleWriter consoleWriter, Paths paths)
        {
            this.consoleWriter = consoleWriter;
            this.paths = paths;
            histogramBinFile = Path.Combine(paths.AppData, "imageHistogram.bin");
            histogramHashTable = Serializer.Deserialize<List<HistogramHash>>(histogramBinFile);

            consoleWriter.WriteLine($"Have loaded {histogramHashTable.Count} previous hashes");

            RemoveInvalidHashEntries();

            consoleWriter.WriteLine($"Have {histogramHashTable.Count} previous hashes after removing invalid");

            SaveHashTableBin();

            HashExistingImages();

            consoleWriter.WriteLine($"Have {histogramHashTable.Count} hashed images total");
        }

        internal bool ImageInHash(string tempFilename, string realFileName)
        {
            var testHash = GetRgbHistogram(tempFilename);
            return HaveFilePathInHashTable(realFileName) || histogramHashTable.Any(hash => hash.Equal(testHash));
        }

        internal bool HaveFilePathInHashTable(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            return histogramHashTable.Any(x => x.FileName.Equals(fileName, StringComparison.InvariantCultureIgnoreCase));
        }

        internal void AddHash(string filePath)
        {
            if (HaveFilePathInHashTable(filePath)) return;

            histogramHashTable.Add(GetRgbHistogram(filePath));
        }

        internal void SaveHashTableBin()
        {
            Serializer.Serialize(histogramHashTable, histogramBinFile);
        }

        private void RemoveInvalidHashEntries()
        {
            histogramHashTable.RemoveAll(x => x.IsInvalid(paths));
        }

        private void HashExistingImages(int retryCount = 0)
        {
            consoleWriter.WriteLine($"Hashing images missing from hash (attempt: {retryCount + 1})");

            try
            {
                foreach (var file in Directory.GetFiles(paths.SavePath, "*.jpg").Where(x => !HaveFilePathInHashTable(x)))
                {
                    consoleWriter.WriteLine($"Hashing file: {file}");
                    AddHash(file);
                }

                foreach (var file in Directory.GetFiles(paths.ArchivePath, "*.jpg").Where(x => !HaveFilePathInHashTable(x)))
                {
                    consoleWriter.WriteLine($"Hashing file: {file}");
                    AddHash(file);
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
        }

        private HistogramHash GetRgbHistogram(string filePath)
        {
            var histogramFile = Path.Combine(paths.HistogramPath, Guid.NewGuid() + ".jpg");
            File.Copy(filePath, histogramFile);
            var blue = new List<int>();
            var green = new List<int>();
            var red = new List<int>();
            var y = new List<int>();
            var cb = new List<int>();
            var cr = new List<int>();
            var saturation = new List<int>();
            var luminance = new List<int>();
            var fileName = Path.GetFileName(filePath);

            using (var bmp = new System.Drawing.Bitmap(histogramFile))
            {
                // RGB
                var rgbStatistics = new ImageStatistics(bmp);
                blue.AddRange(rgbStatistics.Blue.Values);
                green.AddRange(rgbStatistics.Green.Values);
                red.AddRange(rgbStatistics.Red.Values);

                // YCbCr
                var yCbCrStatistics = new ImageStatisticsYCbCr(bmp);
                y.AddRange(yCbCrStatistics.Y.Values);
                cb.AddRange(yCbCrStatistics.Cb.Values);
                cr.AddRange(yCbCrStatistics.Cr.Values);

                // HSL
                var hslStatistics = new ImageStatisticsHSL(bmp);
                saturation.AddRange(hslStatistics.Saturation.Values);
                luminance.AddRange(hslStatistics.Luminance.Values);
            }

            File.Delete(histogramFile);

            return new HistogramHash(fileName, blue, green, red, y, cb, cr, saturation, luminance);
        }
    }
}
