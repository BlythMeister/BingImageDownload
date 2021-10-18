using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BingImageDownload
{
    public class ImageFingerprint
    {
        public string FileName { get; }
        public List<RgbPixelData> Rgb { get; }

        public ImageFingerprint(string fileName, List<RgbPixelData> rgb)
        {
            FileName = fileName;
            Rgb = rgb;
        }

        internal bool IsInvalid(Paths paths)
        {
            if (string.IsNullOrWhiteSpace(FileName))
            {
                return true;
            }

            if (!File.Exists(Path.Combine(paths.SavePath, FileName)) && !File.Exists(Path.Combine(paths.ArchivePath, FileName)))
            {
                return true;
            }

            if (Rgb.All(x => x.Rgb == 0))
            {
                return true;
            }

            return false;
        }

        internal bool Equal(ImageFingerprint other)
        {
            var differencesOverTolerance = 0f;
            foreach (var val in Rgb)
            {
                var otherVal = other.Rgb.FirstOrDefault(x => x.X.Equals(val.X) && x.Y.Equals(val.Y));
                if (otherVal == null)
                {
                    return false;
                }

                var difference = Math.Abs(val.Rgb - otherVal.Rgb);
                if (difference > 3)
                {
                    differencesOverTolerance++;
                    var differencePercent = differencesOverTolerance / Rgb.Count * 100;
                    if (differencePercent > 5f)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public class RgbPixelData
        {
            public int Rgb { get; }
            public int X { get; }
            public int Y { get; }

            public RgbPixelData(int x, int y, int rgb)
            {
                Rgb = rgb;
                X = x;
                Y = y;
            }
        }
    }
}
