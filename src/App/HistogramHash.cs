using System.Collections.Generic;
using System.Linq;

namespace BingWallpaper
{
    public class HistogramHash
    {
        public readonly string FilePath;
        public readonly List<int> HashValue;

        public HistogramHash(string filePath, List<int> hashValue)
        {
            FilePath = filePath;
            HashValue = hashValue;
        }

        public bool Equal(HistogramHash other)
        {
            return HashValue.SequenceEqual(other.HashValue);
        }
    }
}
