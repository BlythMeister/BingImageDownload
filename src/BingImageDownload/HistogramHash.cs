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

                var difference = Math.Abs(val.RGB - otherVal.RGB);
                if (difference > 3)
                {
                    differencesOverTolerance++;
                }
            }

            var differencePercent = differencesOverTolerance / Rgb.Count * 100;

            return differencePercent < 1f;
        }
    }
}
