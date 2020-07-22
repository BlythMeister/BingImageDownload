namespace BingImageDownload
{
    public class RgbPixelData
    {
        public int RGB { get; }
        public int X { get; }
        public int Y { get; }

        public RgbPixelData(int x, int y, int rgb)
        {
            RGB = rgb;
            X = x;
            Y = y;
        }
    }
}
