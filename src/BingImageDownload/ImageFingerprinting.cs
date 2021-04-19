using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BingImageDownload
{
    internal class ImageFingerprinting
    {
        private readonly ConsoleWriter consoleWriter;
        private readonly Serializer serializer;
        private readonly Paths paths;
        private readonly List<ImageFingerprint> imageFingerprints;
        private readonly string fingerprintBinFile;

        internal ImageFingerprinting(ConsoleWriter consoleWriter, Paths paths, Serializer serializer)
        {
            this.consoleWriter = consoleWriter;
            this.serializer = serializer;
            this.paths = paths;
            fingerprintBinFile = Path.Combine(paths.AppData, "imageFingerprints.bin");
            imageFingerprints = serializer.Deserialize<List<ImageFingerprint>>(fingerprintBinFile);

            consoleWriter.WriteLine($"Have loaded {imageFingerprints.Count} previous fingerprints");

            RemoveInvalidFingerprintsEntries();

            consoleWriter.WriteLine($"Have {imageFingerprints.Count} previous fingerprints after removing invalid");

            FingerprintExistingImages();

            consoleWriter.WriteLine($"Have {imageFingerprints.Count} previous fingerprints after adding missing");

            RemoveInvalidFingerprintsEntries();

            consoleWriter.WriteLine($"Have {imageFingerprints.Count} previous fingerprints after removing invalid (again)");

            SaveFingerprintBin();
        }

        internal bool HaveIdenticalImageInFingerprints(string tempFilename)
        {
            var tempFileFingerprint = GetImageFingerprint(tempFilename);
            return imageFingerprints.Any(fingerprint => fingerprint.Equal(tempFileFingerprint));
        }

        internal bool HaveFileNameInFingerprints(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            return imageFingerprints.Any(x => x.FileName.Equals(fileName, StringComparison.InvariantCultureIgnoreCase));
        }

        internal void AddFingerprint(string filePath, bool saveFingerprints = true)
        {
            if (HaveFileNameInFingerprints(filePath))
            {
                return;
            }

            imageFingerprints.Add(GetImageFingerprint(filePath));

            if (saveFingerprints)
            {
                SaveFingerprintBin();
            }
        }

        private void SaveFingerprintBin()
        {
            serializer.Serialize(imageFingerprints, fingerprintBinFile);
        }

        private void RemoveInvalidFingerprintsEntries()
        {
            imageFingerprints.RemoveAll(x => x.IsInvalid(paths));
        }

        private void FingerprintExistingImages(int retryCount = 0)
        {
            consoleWriter.WriteLine(retryCount > 0 ? $"Fingerprinting missing images (attempt: {retryCount + 1})" : "Fingerprinting missing images");

            try
            {
                var files = Directory.GetFiles(paths.SavePath, "*.jpg").Concat(Directory.GetFiles(paths.ArchivePath, "*.jpg")).Where(x => !HaveFileNameInFingerprints(x)).ToList();
                for (var i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    consoleWriter.WriteLine($"Fingerprinting file: {file} ({i + 1}/{files.Count})");
                    AddFingerprint(file, false);
                }
            }
            catch (Exception) when (retryCount < 3)
            {
                consoleWriter.WriteLine("Error...Starting over");
                FingerprintExistingImages(retryCount + 1);
            }
        }

        private ImageFingerprint GetImageFingerprint(string filePath)
        {
            var histogramFile = Path.Combine(paths.HistogramPath, Guid.NewGuid() + ".jpg");
            File.Copy(filePath, histogramFile);
            var rgb = new List<ImageFingerprint.RgbPixelData>();
            var fileName = Path.GetFileName(filePath);

            using (var image = Image.Load<Rgb24>(histogramFile))
            {
                //Scale down from 1920*1080 to 96*54 (5% the size) - this will pixelate but enough to tell differences.
                //This means 5,184 total pixels rather than 2,073,600.
                image.Mutate(x => x.Resize(96, 54).Grayscale());

                for (var x = 0; x < image.Width; x++)
                {
                    for (var y = 0; y < image.Height; y++)
                    {
                        var pixel = image[x, y];
                        rgb.Add(new ImageFingerprint.RgbPixelData(x, y, pixel.R + pixel.G + pixel.B));
                    }
                }
            }

            File.Delete(histogramFile);

            return new ImageFingerprint(fileName, rgb);
        }
    }
}
