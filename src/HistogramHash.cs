using System.Collections.Generic;
using System.Linq;

namespace BingWallpaper
{
    public class HistogramHash
    {
        public readonly string FilePath;
        public readonly int[] HashValue;

        public HistogramHash(string filePath, int[] values)
        {
            FilePath = filePath;
            HashValue = values;
        }

        public bool Equal(HistogramHash other)
        {
            return HashValue.SequenceEqual(other.HashValue);
        }
    }
}