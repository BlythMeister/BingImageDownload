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

        internal static void AddHash(string filePath)
        {
            if(HistogramHashTable.Any(x=>x.FilePath == filePath)) return;

            HistogramHashTable.Add(GetRGBHistogram(filePath));
        }

        internal static HistogramHash GetRGBHistogram(string file)
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

            return new HistogramHash(file, values.ToArray());
        }
    }
}