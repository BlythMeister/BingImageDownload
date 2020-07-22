using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml.Linq;

namespace BingImageDownload
{
    internal class BingInteractionAndParsing
    {
        private const string Url = "https://bing.com";

        private readonly ConsoleWriter consoleWriter;
        private readonly ImageHashing imageHashing;
        private readonly ImagePropertyHandling imagePropertyHandling;
        private readonly Paths paths;
        private readonly Serializer serializer;
        private readonly List<string> urlsRetrieved;
        private readonly List<CultureInfo> countries;
        private readonly string urlsRetrievedBinFile;

        public BingInteractionAndParsing(ConsoleWriter consoleWriter, ImageHashing imageHashing, ImagePropertyHandling imagePropertyHandling, Paths paths, Serializer serializer)
        {
            this.consoleWriter = consoleWriter;
            this.imageHashing = imageHashing;
            this.imagePropertyHandling = imagePropertyHandling;
            this.paths = paths;
            this.serializer = serializer;
            urlsRetrievedBinFile = Path.Combine(paths.AppData, "urlsRetrieved.bin");

            urlsRetrieved = serializer.Deserialize<List<string>>(urlsRetrievedBinFile).ToList();
            countries = CultureInfo.GetCultures(CultureTypes.AllCultures).Where(x => x.Name.Contains("-")).ToList();

            consoleWriter.WriteLine($"Have loaded {urlsRetrieved.Count} previous URLs");
            consoleWriter.WriteLine($"Have loaded {countries.Count} countries");
        }

        internal void GetBingImages(CancellationToken cancellationToken)
        {
            var downloadedImages = 0;

            foreach (var country in countries)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                consoleWriter.WriteLine($"Searching for images for {country.Name} - {country.DisplayName}");
                var countryImages = 0;
                var countryDuplicateImages = 0;
                var moreImages = true;
                var datePairs = new Dictionary<(string start, string end), XElement>();
                while (moreImages)
                {
                    var imageNodes = GetImages(datePairs.Count, country.Name);

                    if (!imageNodes.Any())
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
                    consoleWriter.WriteLine(1, $"Image for: '{country.Name}' on {startDate}-{endDate} was: {imageUrl}");
                    try
                    {
                        if (DownloadAndSaveImage(imageNode, imageUrl))
                        {
                            countryImages++;
                        }
                        else
                        {
                            countryDuplicateImages++;
                        }
                    }
                    catch (Exception ex)
                    {
                        consoleWriter.WriteLine("There was an error getting image", ex);
                    }
                }

                downloadedImages += countryImages;
                consoleWriter.WriteLine($"Found {countryImages} new images for {country.Name}");
                consoleWriter.WriteLine($"Found {countryDuplicateImages} duplicate images for {country.Name}");
                consoleWriter.WriteLine("");
            }

            consoleWriter.WriteLine($"Found {downloadedImages} new images");
        }

        internal bool DownloadAndSaveImage(XElement imageNode, string imageUrl)
        {
            if (urlsRetrieved.Contains(imageUrl))
            {
                consoleWriter.WriteLine(2, "Already Downloaded Image URL");
                return false;
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
                return false;
            }

            consoleWriter.WriteLine(2, "Downloaded Image, Checking If Duplicate");
            var newImage = false;
            var haveIdenticalImage = imageHashing.HaveIdenticalImageInHashTable(tempFilename);

            if (!haveIdenticalImage)
            {
                var filePath = Path.Combine(paths.SavePath, GetFileName(imageUrl));
                var counter = 0;
                while (imageHashing.HaveFileNameInHashTable(filePath))
                {
                    counter++;
                    filePath = Path.Combine(paths.SavePath, GetFileName(imageUrl, counter));
                }

                newImage = true;
                consoleWriter.WriteLine(3, "Found New Image");
                using (var srcImg = Image.Load(tempFilename))
                {
                    imagePropertyHandling.SetImageExifTags(imageNode, srcImg);
                    srcImg.Save(filePath);
                }
                imageHashing.AddHash(filePath);
            }
            else
            {
                consoleWriter.WriteLine(3, "Identical Image Downloaded");
            }

            urlsRetrieved.Add(imageUrl);
            SaveUrlBin();
            File.Delete(tempFilename);
            return newImage;
        }

        internal string GetFileName(string imageUrl, int counter = 0)
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

        internal List<XElement> GetImages(int currentIndex, string country)
        {
            var urlToLoad = $"{Url}/HPImageArchive.aspx?format=xml&idx={currentIndex}&n=5&mkt={country}";

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
    }
}
