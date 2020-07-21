using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml;

namespace BingImageDownload
{
    internal class BingInteractionAndParsing
    {
        private const string Url = "https://bing.com";

        private readonly ConsoleWriter consoleWriter;
        private readonly ImageHashing imageHashing;
        private readonly ImagePropertyHandling imagePropertyHandling;
        private readonly Paths paths;
        private readonly List<string> urlsRetrieved;
        private readonly List<CultureInfo> countries;
        private readonly string urlsRetrievedBinFile;

        public BingInteractionAndParsing(ConsoleWriter consoleWriter, ImageHashing imageHashing, ImagePropertyHandling imagePropertyHandling, Paths paths)
        {
            this.consoleWriter = consoleWriter;
            this.imageHashing = imageHashing;
            this.imagePropertyHandling = imagePropertyHandling;
            this.paths = paths;
            urlsRetrievedBinFile = Path.Combine(paths.AppData, "urlsRetrieved.bin");

            urlsRetrieved = Serializer.Deserialize<List<string>>(urlsRetrievedBinFile).ToList();
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
                var currentIndex = 0;
                var moreImages = true;
                var startDate = string.Empty;
                var endDate = string.Empty;
                while (moreImages)
                {
                    var xmlNodeList = GetImages(currentIndex, country.Name);

                    if (xmlNodeList == null)
                    {
                        moreImages = false;
                    }
                    else
                    {
                        foreach (XmlNode xmlNode in xmlNodeList)
                        {
                            var nodeStartDate = xmlNode.SelectSingleNode("startdate")?.InnerText;
                            var nodeEndDate = xmlNode.SelectSingleNode("enddate")?.InnerText;

                            if (startDate == nodeStartDate && endDate == nodeEndDate)
                            {
                                moreImages = false;
                                break;
                            }

                            startDate = nodeStartDate;
                            endDate = nodeEndDate;
                            var imageUrl = $"{Url}{xmlNode.SelectSingleNode("urlBase")?.InnerText}_1920x1080.jpg";
                            consoleWriter.WriteLine(1, $"Image for: '{country.Name}' on {startDate}-{endDate} index {currentIndex} was: {imageUrl}");
                            try
                            {
                                if (DownloadAndSaveImage(xmlNode))
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

                        currentIndex += 1;
                    }
                }

                downloadedImages += countryImages;
                consoleWriter.WriteLine($"Found {countryImages} new images for {country.Name}");
                consoleWriter.WriteLine($"Found {countryDuplicateImages} duplicate images for {country.Name}");
                consoleWriter.WriteLine("");
            }

            consoleWriter.WriteLine($"Found {downloadedImages} new images");
        }

        internal bool DownloadAndSaveImage(XmlNode xmlNode)
        {
            var fileUrl = $"{Url}{xmlNode.SelectSingleNode("urlBase")?.InnerText}_1920x1080.jpg";
            if (urlsRetrieved.Contains(fileUrl))
            {
                consoleWriter.WriteLine(2, "Already Downloaded Image URL");
                return false;
            }

            var filePath = Path.Combine(paths.SavePath, GetFileName(xmlNode));
            var tempFilename = Path.Combine(paths.DownloadPath, Guid.NewGuid() + ".jpg");

            try
            {
                using (var client = new WebClient())
                {
                    client.DownloadFile(fileUrl, tempFilename);
                }
            }
            catch (Exception e)
            {
                consoleWriter.WriteLine(2, $"Error downloading image from url: {fileUrl}", e);
                return false;
            }

            consoleWriter.WriteLine(2, "Downloaded Image, Checking If Duplicate");
            var newImage = false;
            if (!imageHashing.ImageInHash(tempFilename, filePath))
            {
                newImage = true;
                consoleWriter.WriteLine(3, "Found New Image");
                using (var srcImg = Image.FromFile(tempFilename))
                {
                    imagePropertyHandling.SetTitleOnImage(xmlNode, srcImg);
                    srcImg.Save(filePath);
                }
                imageHashing.AddHash(filePath);
            }
            else
            {
                consoleWriter.WriteLine(3, "Identical Image Downloaded");
            }

            urlsRetrieved.Add(fileUrl);
            File.Delete(tempFilename);
            return newImage;
        }

        internal string GetFileName(XmlNode xmlNode)
        {
            var nameNode = xmlNode.SelectSingleNode("urlBase");
            if (nameNode == null) throw new Exception("Missing urlBase Node");

            var name = nameNode.InnerText.Substring(7);
            if (name.Contains("_"))
            {
                name = name.Substring(0, name.IndexOf("_", StringComparison.Ordinal));
            }

            if (name.Contains("."))
            {
                name = name.Substring(name.IndexOf(".", StringComparison.Ordinal) + 1);
            }

            name = $"{name}.jpg";

            return Path.GetInvalidFileNameChars().Aggregate(name, (current, invalidChar) => current.Replace(invalidChar, '-'));
        }

        internal XmlNodeList GetImages(int currentIndex, string country)
        {
            var urlToLoad = $"{Url}/HPImageArchive.aspx?format=xml&idx={currentIndex}&n=1&mkt={country}";

            try
            {
                using (var client = new WebClient())
                {
                    var output = client.DownloadString(urlToLoad);
                    if (output.Length > 0 && output.Contains("<images>"))
                    {
                        try
                        {
                            var xmlDocument = new XmlDocument();
                            xmlDocument.LoadXml(output);

                            return xmlDocument.GetElementsByTagName("image");
                        }
                        catch (Exception e)
                        {
                            consoleWriter.WriteLine("Error getting images from XML response", e);
                            return null;
                        }
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                consoleWriter.WriteLine($"Error loading image search URL: {urlToLoad}", e);
                return null;
            }
        }

        internal void SaveUrlBin()
        {
            Serializer.Serialize(urlsRetrieved, urlsRetrievedBinFile);
        }
    }
}
