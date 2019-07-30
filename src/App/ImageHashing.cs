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

        internal static bool ImageInHash(string tempFilename, string realFileName)
        {
            var testHash = GetRgbHistogram(tempFilename);
            return HaveFilePathInHashTable(realFileName) || HistogramHashTable.Any(hash => hash.Equal(testHash));
        }

        internal static bool HaveFilePathInHashTable(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            return HistogramHashTable.Any(x => x.FileName.Equals(fileName, StringComparison.InvariantCultureIgnoreCase));
        }

        internal static void AddHash(string filePath)
        {
            if (HaveFilePathInHashTable(filePath)) return;

            HistogramHashTable.Add(GetRgbHistogram(filePath));
        }

        internal static void ClearHash()
        {
            HistogramHashTable.RemoveAll(x => x.IsInvalid);
        }

        private static HistogramHash GetRgbHistogram(string filePath)
        {
            var histogramFile = Path.Combine(HistogramPath, Guid.NewGuid() + ".jpg");
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
