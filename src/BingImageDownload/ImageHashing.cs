using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BingImageDownload
{
    internal class ImageHashing
    {
        private readonly ConsoleWriter consoleWriter;
        private readonly Serializer serializer;
        private readonly Paths paths;
        private readonly List<HistogramHash> histogramHashTable;
        private readonly string histogramBinFile;

        internal ImageHashing(ConsoleWriter consoleWriter, Paths paths, Serializer serializer)
        {
            this.consoleWriter = consoleWriter;
            this.serializer = serializer;
            this.paths = paths;
            histogramBinFile = Path.Combine(paths.AppData, "imageHistogram.bin");
            histogramHashTable = serializer.Deserialize<List<HistogramHash>>(histogramBinFile);

            consoleWriter.WriteLine($"Have loaded {histogramHashTable.Count} previous hashes");

            RemoveInvalidHashEntries();

            consoleWriter.WriteLine($"Have {histogramHashTable.Count} previous hashes after removing invalid");

            HashExistingImages();

            consoleWriter.WriteLine($"Have {histogramHashTable.Count} previous hashes after adding missing");

            RemoveInvalidHashEntries();

            consoleWriter.WriteLine($"Have {histogramHashTable.Count} previous hashes after removing invalid (again)");

            SaveHashTableBin();
        }

        internal bool HaveIdenticalImageInHashTable(string tempFilename)
        {
            var testHash = GetRgbHistogramHash(tempFilename);
            return histogramHashTable.Any(hash => hash.Equal(testHash));
        }

        internal bool HaveFileNameInHashTable(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            return histogramHashTable.Any(x => x.FileName.Equals(fileName, StringComparison.InvariantCultureIgnoreCase));
        }

        internal void AddHash(string filePath, bool saveHashTable = true)
        {
            if (HaveFileNameInHashTable(filePath)) return;

            histogramHashTable.Add(GetRgbHistogramHash(filePath));

            if (saveHashTable)
            {
                SaveHashTableBin();
            }
        }

        private void SaveHashTableBin()
        {
            serializer.Serialize(histogramHashTable, histogramBinFile);
        }

        private void RemoveInvalidHashEntries()
        {
            histogramHashTable.RemoveAll(x => x.IsInvalid(paths));
        }

        private void HashExistingImages(int retryCount = 0)
        {
            consoleWriter.WriteLine($"Hashing images missing from hash (attempt: {retryCount + 1})");

            try
            {
                foreach (var file in Directory.GetFiles(paths.SavePath, "*.jpg").Where(x => !HaveFileNameInHashTable(x)))
                {
                    consoleWriter.WriteLine($"Hashing file: {file}");
                    AddHash(file, false);
                }

                foreach (var file in Directory.GetFiles(paths.ArchivePath, "*.jpg").Where(x => !HaveFileNameInHashTable(x)))
                {
                    consoleWriter.WriteLine($"Hashing file: {file}");
                    AddHash(file, false);
                }
            }
            catch (Exception)
            {
                if (retryCount < 5)
                {
                    HashExistingImages(retryCount + 1);
                }
                else
                {
                    throw;
                }
            }
        }

        private HistogramHash GetRgbHistogramHash(string filePath)
        {
            var histogramFile = Path.Combine(paths.HistogramPath, Guid.NewGuid() + ".jpg");
            File.Copy(filePath, histogramFile);
            var Rgb = new List<RgbPixelData>();
            var fileName = Path.GetFileName(filePath);

            using (var image = Image.Load<Rgb24>(histogramFile))
            {
                //Scale down from 1920*1080 to 48*27 - this will pixelate but enough to tell differences.
                //This means 1296 total pixels rather than 2073600.
                image.Mutate(x => x.Resize(48, 27).Grayscale());

                for (var x = 0; x < image.Width; x++)
                {
                    for (var y = 0; y < image.Height; y++)
                    {
                        var pixel = image[x, y];
                        Rgb.Add(new RgbPixelData(x, y, pixel.R, pixel.G, pixel.B));
                    }
                }
            }

            File.Delete(histogramFile);

            return new HistogramHash(fileName, Rgb);
        }
    }
}
