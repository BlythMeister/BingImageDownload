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

        internal bool ImageInHash(string tempFilename, string realFileName)
        {
            if (HaveFilePathInHashTable(realFileName)) return true;

            var testHash = GetRgbaHistogramHash(tempFilename);
            return histogramHashTable.Any(hash => hash.Equal(testHash));
        }

        internal bool HaveFilePathInHashTable(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            return histogramHashTable.Any(x => x.FileName.Equals(fileName, StringComparison.InvariantCultureIgnoreCase));
        }

        internal void AddHash(string filePath)
        {
            if (HaveFilePathInHashTable(filePath)) return;

            histogramHashTable.Add(GetRgbaHistogramHash(filePath));
        }

        internal void SaveHashTableBin()
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
                foreach (var file in Directory.GetFiles(paths.SavePath, "*.jpg").Where(x => !HaveFilePathInHashTable(x)))
                {
                    consoleWriter.WriteLine($"Hashing file: {file}");
                    AddHash(file);
                }

                foreach (var file in Directory.GetFiles(paths.ArchivePath, "*.jpg").Where(x => !HaveFilePathInHashTable(x)))
                {
                    consoleWriter.WriteLine($"Hashing file: {file}");
                    AddHash(file);
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

        private HistogramHash GetRgbaHistogramHash(string filePath)
        {
            var histogramFile = Path.Combine(paths.HistogramPath, Guid.NewGuid() + ".jpg");
            File.Copy(filePath, histogramFile);
            var rgba = new List<RgbaPixelData>();
            var fileName = Path.GetFileName(filePath);

            using (var image = Image.Load<Rgba32>(histogramFile))
            {
                image.Mutate(x => x.Resize(new Size(32)));

                for (var x = 0; x < image.Width; x++)
                {
                    for (var y = 0; y < image.Height; y++)
                    {
                        var pixel = image[x, y];
                        rgba.Add(new RgbaPixelData(x, y, pixel.Rgba));
                    }
                }
            }

            File.Delete(histogramFile);

            return new HistogramHash(fileName, rgba);
        }
    }
}
