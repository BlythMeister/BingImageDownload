namespace BingImageDownload
{
    public class RgbaPixelData
    {
        public int X { get; }
        public int Y { get; }
        public uint RgbaValue { get; }

        public RgbaPixelData(int x, int y, uint rgbaValue)
        {
            X = x;
            Y = y;
            RgbaValue = rgbaValue;
        }
    }
}
