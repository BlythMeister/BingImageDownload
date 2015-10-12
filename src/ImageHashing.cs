using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AForge.Imaging;
using BingWallpaper;

static internal class ImageHashing
{
    private static readonly List<int[]> histogramHashTable = new List<int[]>();
    internal static readonly string hitogramPath = Path.Combine(Program.appData, "TempHistogram");

    internal static bool ImageInHash(string tempfilename)
    {
        return histogramHashTable.Any(ints => ints.SequenceEqual(GetRGBHistogram(tempfilename)));
    }

    internal static void AddHash(string filePath)
    {
        histogramHashTable.Add(GetRGBHistogram(filePath));
    }

    internal static int[] GetRGBHistogram(string file)
    {
        var values = new List<int>();
        var histogramfile = Path.Combine(hitogramPath, Guid.NewGuid() + ".jpg");
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

        return values.ToArray();
    }
}