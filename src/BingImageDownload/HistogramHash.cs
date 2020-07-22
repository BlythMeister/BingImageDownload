using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BingImageDownload
{
    public class HistogramHash
    {
        public string FileName { get; }
        public Dictionary<(int x, int y), uint> RGBA { get; }

        public HistogramHash(string fileName, Dictionary<(int x, int y), uint> rgba)
        {
            FileName = fileName;
            RGBA = rgba;
        }

        internal bool IsInvalid(Paths paths)
        {
            if (string.IsNullOrWhiteSpace(FileName)) return true;
            if (!File.Exists(Path.Combine(paths.SavePath, FileName)) && !File.Exists(Path.Combine(paths.ArchivePath, FileName))) return true;
            if (RGBA == null || !RGBA.Any()) return true;
            return false;
        }

        internal bool Equal(HistogramHash other)
        {
            foreach (var (pos, val) in RGBA.ToList())
            {
                if (!other.RGBA.ContainsKey(pos)) return false;
                var otherVal = other.RGBA[pos];
                if (!val.Equals(otherVal)) return false;
            }

            return true;
        }
    }
}
