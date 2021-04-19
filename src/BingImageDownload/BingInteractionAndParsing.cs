using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;

namespace BingImageDownload
{
    internal class BingInteractionAndParsing
    {
        private const string Url = "https://bing.com";

        private readonly ConsoleWriter consoleWriter;
        private readonly ImageFingerprinting imageFingerprinting;
        private readonly ImagePropertyHandling imagePropertyHandling;
        private readonly Paths paths;
        private readonly Serializer serializer;
        private readonly List<string> urlsRetrieved;
        private readonly string urlsRetrievedBinFile;

        public BingInteractionAndParsing(ConsoleWriter consoleWriter, ImageFingerprinting imageFingerprinting, ImagePropertyHandling imagePropertyHandling, Paths paths, Serializer serializer)
        {
            this.consoleWriter = consoleWriter;
            this.imageFingerprinting = imageFingerprinting;
            this.imagePropertyHandling = imagePropertyHandling;
            this.paths = paths;
            this.serializer = serializer;
            urlsRetrievedBinFile = Path.Combine(paths.AppData, "imageUrlsRetrieved.bin");

            urlsRetrieved = serializer.Deserialize<List<string>>(urlsRetrievedBinFile).ToList();

            consoleWriter.WriteLine($"Have loaded {urlsRetrieved.Count} previous URLs");
        }

        internal (int countryDownloadedImages, int countryDuplicateImages, int countrySeenUrls) GetBingImages(CultureInfo country)
        {
            consoleWriter.WriteLine($"Searching for images for {country.Name} - {country.DisplayName}");
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var countryDownloadedImages = 0;
            var countryDuplicateImages = 0;
            var countrySeenUrls = 0;
            var moreImages = true;
            var datePairs = new Dictionary<(string start, string end), XElement>();
            while (moreImages)
            {
                var imageNodes = GetImages(datePairs.Count, country.Name);

                if (imageNodes ==  null || !imageNodes.Any())
                {
                    moreImages = false;
                }
                else
                {
                    foreach (var imageNode in imageNodes)
                    {
                        var startDate = imageNode.Element("startdate")?.Value;
                        var endDate = imageNode.Element("enddate")?.Value;

                        if (datePairs.Any(x => x.Key.start == startDate && x.Key.end == endDate))
                        {
                            moreImages = false;
                            continue;
                        }

                        datePairs.Add((startDate, endDate), imageNode);
                    }
                }
            }

            foreach (var ((startDate, endDate), imageNode) in datePairs.OrderBy(x => x.Key.start))
            {
                var imageUrl = $"{Url}{imageNode.Element("urlBase")?.Value}_1920x1080.jpg";
                var copyright = imageNode.Element("copyright")?.Value;
                var headline = imageNode.Element("headline")?.Value;
                consoleWriter.WriteLine(1, $"Image for: '{country.Name}' on {startDate}-{endDate} was: {imageUrl}");
                try
                {
                    var result = DownloadAndSaveImage(copyright, headline, imageUrl);
                    switch (result)
                    {
                        case DownloadResult.SeenUrl:
                            countrySeenUrls++;
                            break;

                        case DownloadResult.DuplicateImage:
                            countryDuplicateImages++;
                            break;

                        case DownloadResult.NewImage:
                            countryDownloadedImages++;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    consoleWriter.WriteLine("There was an error getting image", ex);
                }
            }

            consoleWriter.WriteLine($"Found {countryDownloadedImages} new images for {country.Name}");
            consoleWriter.WriteLine($"Found {countryDuplicateImages} duplicate images for {country.Name}");
            consoleWriter.WriteLine($"Found {countrySeenUrls} urls already downloaded for {country.Name}");
            consoleWriter.WriteLine($"Duration {Math.Round(stopwatch.Elapsed.TotalSeconds, 2)} seconds for {country.Name}");
            consoleWriter.WriteLine("");

            return (countryDownloadedImages, countryDuplicateImages, countrySeenUrls);
        }

        private DownloadResult DownloadAndSaveImage(string copyright, string headline, string imageUrl)
        {
            if (urlsRetrieved.Contains(imageUrl))
            {
                consoleWriter.WriteLine(2, "Already Downloaded Image URL");
                return DownloadResult.SeenUrl;
            }

            var tempFilename = Path.Combine(paths.DownloadPath, Guid.NewGuid() + ".jpg");

            try
            {
                using var client = new WebClient();
                client.DownloadFile(imageUrl, tempFilename);
            }
            catch (Exception e)
            {
                consoleWriter.WriteLine(2, $"Error downloading image from url: {imageUrl}", e);
                return DownloadResult.Error;
            }

            consoleWriter.WriteLine(2, "Downloaded Image, Checking If Duplicate");
            var newImage = false;
            var haveIdenticalImage = imageFingerprinting.HaveIdenticalImageInFingerprints(tempFilename);

            if (!haveIdenticalImage)
            {
                var filePath = Path.Combine(paths.SavePath, GetFileName(imageUrl));
                var counter = 0;
                while (imageFingerprinting.HaveFileNameInFingerprints(filePath))
                {
                    counter++;
                    filePath = Path.Combine(paths.SavePath, GetFileName(imageUrl, counter));
                }

                newImage = true;
                consoleWriter.WriteLine(3, "Found New Image");
                using (var srcImg = Image.Load(tempFilename))
                {
                    imagePropertyHandling.SetImageExifTags(copyright, headline, srcImg);
                    srcImg.Save(filePath);
                }
                imageFingerprinting.AddFingerprint(filePath);
            }
            else
            {
                consoleWriter.WriteLine(3, "Identical Image Downloaded");
            }

            urlsRetrieved.Add(imageUrl);

            SaveUrlBin();
            File.Delete(tempFilename);
            return newImage ? DownloadResult.NewImage : DownloadResult.DuplicateImage;
        }

        private string GetFileName(string imageUrl, int counter = 0)
        {
            var name = imageUrl.Substring(7 + Url.Length);
            if (name.Contains("_"))
            {
                name = name.Substring(0, name.IndexOf("_", StringComparison.Ordinal));
            }

            if (name.Contains("."))
            {
                name = name.Substring(name.IndexOf(".", StringComparison.Ordinal) + 1);
            }

            name = counter > 0 ? $"{name} ({counter}).jpg" : $"{name}.jpg";

            return Path.GetInvalidFileNameChars().Aggregate(name, (current, invalidChar) => current.Replace(invalidChar, '-'));
        }

        private List<XElement> GetImages(int currentIndex, string country)
        {
            var urlToLoad = $"{Url}/HPImageArchive.aspx?format=xml&idx={currentIndex}&n=8&mkt={country}";

            try
            {
                using var client = new WebClient();
                var output = client.DownloadString(urlToLoad);
                if (output.Length > 0 && output.Contains("<images>"))
                {
                    try
                    {
                        var xDocument = XDocument.Parse(output);

                        return xDocument.Descendants("image").ToList();
                    }
                    catch (Exception e)
                    {
                        consoleWriter.WriteLine("Error getting images from XML response", e);
                        return null;
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                consoleWriter.WriteLine($"Error loading image search URL: {urlToLoad}", e);
                return null;
            }
        }

        private void SaveUrlBin()
        {
            serializer.Serialize(urlsRetrieved, urlsRetrievedBinFile);
        }

        private enum DownloadResult
        {
            Error,
            SeenUrl,
            DuplicateImage,
            NewImage
        }
    }
}
