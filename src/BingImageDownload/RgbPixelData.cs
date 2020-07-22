namespace BingImageDownload
{
    public class RgbPixelData
    {
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        public int X { get; }
        public int Y { get; }

        public RgbPixelData(int x, int y, byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
            X = x;
            Y = y;
        }
    }
}
