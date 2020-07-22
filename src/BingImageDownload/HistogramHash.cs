using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BingImageDownload
{
    public class HistogramHash
    {
        public string FileName { get; }
        public List<RgbPixelData> Rgb { get; }

        public HistogramHash(string fileName, List<RgbPixelData> rgb)
        {
            FileName = fileName;
            Rgb = rgb;
        }

        internal bool IsInvalid(Paths paths)
        {
            if (string.IsNullOrWhiteSpace(FileName)) return true;
            if (!File.Exists(Path.Combine(paths.SavePath, FileName)) && !File.Exists(Path.Combine(paths.ArchivePath, FileName))) return true;
            if (Rgb == null || !Rgb.Any()) return true;
            return false;
        }

        internal bool Equal(HistogramHash other)
        {
            var differencesOverTolerance = 0f;

            foreach (var val in Rgb)
            {
                var otherVal = other.Rgb.FirstOrDefault(x => x.X.Equals(val.X) && x.Y.Equals(val.Y));
                if (otherVal == null) return false;

                var differenceR = Math.Abs(val.R - otherVal.R);
                var differenceG = Math.Abs(val.G - otherVal.G);
                var differenceB = Math.Abs(val.B - otherVal.B);

                if (differenceR > 3 || differenceG > 3 || differenceB > 3)
                {
                    differencesOverTolerance++;
                }
            }

            var differencePercent = differencesOverTolerance / Rgb.Count * 100;

            return differencePercent < 1;
        }
    }
}
