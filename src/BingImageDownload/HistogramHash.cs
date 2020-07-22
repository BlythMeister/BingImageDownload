using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BingImageDownload
{
    public class HistogramHash
    {
        public string FileName { get; }
        public List<RgbaPixelData> Rgba { get; }

        public HistogramHash(string fileName, List<RgbaPixelData> rgba)
        {
            FileName = fileName;
            Rgba = rgba;
        }

        internal bool IsInvalid(Paths paths)
        {
            if (string.IsNullOrWhiteSpace(FileName)) return true;
            if (!File.Exists(Path.Combine(paths.SavePath, FileName)) && !File.Exists(Path.Combine(paths.ArchivePath, FileName))) return true;
            if (Rgba == null || !Rgba.Any()) return true;
            return false;
        }

        internal bool Equal(HistogramHash other)
        {
            foreach (var val in Rgba)
            {
                var otherVal = other.Rgba.FirstOrDefault(x => x.X.Equals(val.X) && x.Y.Equals(val.Y));
                if (otherVal == null) return false;
                if (!val.RgbaValue.Equals(otherVal.RgbaValue)) return false;
            }

            return true;
        }
    }
}
