using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BingWallpaper
{
    public class HistogramHash
    {
        public string FileName { get; }
        public List<int> Blue { get; }
        public List<int> Green { get; }
        public List<int> Red { get; }
        public List<int> Y { get; }
        public List<int> Cb { get; }
        public List<int> Cr { get; }
        public List<int> Luminance { get; }
        public List<int> Saturation { get; }

        public HistogramHash(string fileName, List<int> blue, List<int> green, List<int> red, List<int> y, List<int> cb, List<int> cr, List<int> saturation, List<int> luminance)
        {
            FileName = fileName;
            Blue = blue;
            Green = green;
            Red = red;
            Y = y;
            Cb = cb;
            Cr = cr;
            Saturation = saturation;
            Luminance = luminance;
        }

        public bool IsInvalid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FileName)) return true;
                if (!File.Exists(FileName)) return true;
                if (Blue == null || !Blue.Any()) return true;
                if (Green == null || !Green.Any()) return true;
                if (Red == null || !Red.Any()) return true;
                if (Y == null || !Y.Any()) return true;
                if (Cb == null || !Cb.Any()) return true;
                if (Cr == null || !Cr.Any()) return true;
                if (Saturation == null || !Saturation.Any()) return true;
                if (Luminance == null || !Luminance.Any()) return true;
                return false;
            }
        }

        public bool Equal(HistogramHash other)
        {
            return Blue.SequenceEqual(other.Blue) &&
                   Green.SequenceEqual(other.Green) &&
                   Red.SequenceEqual(other.Red) &&
                   Y.SequenceEqual(other.Y) &&
                   Cb.SequenceEqual(other.Cb) &&
                   Cr.SequenceEqual(other.Cr) &&
                   Saturation.SequenceEqual(other.Saturation) &&
                   Luminance.SequenceEqual(other.Luminance);
        }
    }
}
