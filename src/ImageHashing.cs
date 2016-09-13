using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AForge.Imaging;

namespace BingWallpaper
{
    internal static class ImageHashing
    {
        internal static readonly List<HistogramHash> HistogramHashTable = new List<HistogramHash>();
        internal static readonly string HitogramPath = Path.Combine(Program.AppData, "TempHistogram");

        internal static bool ImageInHash(string tempfilename)
        {
            var testHash = GetRGBHistogram(tempfilename);
            return HistogramHashTable.Any(hash => hash.Equal(testHash));
        }

        internal static bool HaveFilePathInHashTable(string filePath)
        {
            return HistogramHashTable.Any(x => x.FilePath == filePath);
        }

        internal static void AddHash(string filePath)
        {
            if(HaveFilePathInHashTable(filePath)) return;

            HistogramHashTable.Add(GetRGBHistogram(filePath));
        }

        internal static void ClearHash()
        {
            HistogramHashTable.RemoveAll(x => string.IsNullOrWhiteSpace(x.FilePath));
            HistogramHashTable.RemoveAll(x => !File.Exists(x.FilePath));
            HistogramHashTable.RemoveAll(x => x.HashValue == null || !x.HashValue.Any());
        }

        private static HistogramHash GetRGBHistogram(string file)
        {
            var values = new List<int>();
            var histogramfile = Path.Combine(HitogramPath, Guid.NewGuid() + ".jpg");
            File.Copy(file, histogramfile);
            using (var bmp = new System.Drawing.Bitmap(histogramfile))
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

            File.Delete(histogramfile);

            return new HistogramHash(file, values);
        }
    }
}