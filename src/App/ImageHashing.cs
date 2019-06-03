using AForge.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BingWallpaper
{
    internal static class ImageHashing
    {
        internal static readonly List<HistogramHash> HistogramHashTable = new List<HistogramHash>();
        internal static readonly string HistogramPath = Path.Combine(Program.AppData, "TempHistogram");

        internal static bool ImageInHash(string tempFilename)
        {
            var testHash = GetRgbHistogram(tempFilename);
            return HistogramHashTable.Any(hash => hash.Equal(testHash));
        }

        internal static bool HaveFilePathInHashTable(string filePath)
        {
            return HistogramHashTable.Any(x => x.FilePath == filePath);
        }

        internal static void AddHash(string filePath)
        {
            if (HaveFilePathInHashTable(filePath)) return;

            HistogramHashTable.Add(GetRgbHistogram(filePath));
        }

        internal static void ClearHash()
        {
            HistogramHashTable.RemoveAll(x => string.IsNullOrWhiteSpace(x.FilePath));
            HistogramHashTable.RemoveAll(x => !File.Exists(x.FilePath));
            HistogramHashTable.RemoveAll(x => x.HashValue == null || !x.HashValue.Any());
        }

        private static HistogramHash GetRgbHistogram(string file)
        {
            var values = new List<int>();
            var histogramFile = Path.Combine(HistogramPath, Guid.NewGuid() + ".jpg");
            File.Copy(file, histogramFile);
            using (var bmp = new System.Drawing.Bitmap(histogramFile))
            {
                // Luminance
                var hslStatistics = new ImageStatisticsHSL(bmp);
                values.AddRange(hslStatistics.Luminance.Values.ToList());

                // RGB
                var rgbStatistics = new ImageStatistics(bmp);
                values.AddRange(rgbStatistics.Red.Values.ToList());
                values.AddRange(rgbStatistics.Green.Values.ToList());
                values.AddRange(rgbStatistics.Blue.Values.ToList());
            }

            File.Delete(histogramFile);

            return new HistogramHash(file, values);
        }
    }
}
