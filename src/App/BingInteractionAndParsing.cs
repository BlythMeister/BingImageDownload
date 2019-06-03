using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;

namespace BingWallpaper
{
    internal static class BingInteractionAndParsing
    {
        private const string Url = "https://bing.com";
        internal static readonly string DownloadPath = Path.Combine(Program.AppData, "Temp");
        internal static readonly List<string> UrlsRetrieved = new List<string>();
        internal static readonly List<CultureInfo> Countries = new List<CultureInfo>();

        internal static void GetBingImages()
        {
            var downloadedImages = 0;

            foreach (var country in Countries)
            {
                ConsoleWriter.WriteLine($"Searching for images for {country.Name} - {country.DisplayName}");
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
                            ConsoleWriter.WriteLine(1, $"Image for: '{country.Name}' on {startDate}-{endDate} index {currentIndex} was: {imageUrl}");
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
                                ConsoleWriter.WriteLine("There was an error getting image", ex);
                            }
                        }

                        currentIndex += 1;
                    }
                }

                downloadedImages += countryImages;
                ConsoleWriter.WriteLine($"Found {countryImages} new images for {country.Name}");
                ConsoleWriter.WriteLine($"Found {countryDuplicateImages} duplicate images for {country.Name}");
                ConsoleWriter.WriteLine("");
            }

            ConsoleWriter.WriteLine($"Found {downloadedImages} new images");
        }

        internal static bool DownloadAndSaveImage(XmlNode xmlNode)
        {
            var fileUrl = $"{Url}{xmlNode.SelectSingleNode("urlBase")?.InnerText}_1920x1080.jpg";
            if (UrlsRetrieved.Contains(fileUrl))
            {
                ConsoleWriter.WriteLine(2, "Already Downloaded Image URL");
                return false;
            }

            var filePath = Path.Combine(Program.SavePath, GetFileName(xmlNode));
            var tempFilename = Path.Combine(DownloadPath, Guid.NewGuid() + ".jpg");

            try
            {
                using (var client = new WebClient())
                {
                    client.DownloadFile(fileUrl, tempFilename);
                }
            }
            catch (Exception e)
            {
                ConsoleWriter.WriteLine(2, $"Error downloading image from url: {fileUrl}", e);
                return false;
            }

            ConsoleWriter.WriteLine(2, "Downloaded Image, Checking If Duplicate");
            var newImage = false;
            if (!ImageHashing.ImageInHash(tempFilename) && !File.Exists(filePath))
            {
                newImage = true;
                ConsoleWriter.WriteLine(3, "Found New Image");
                using (var srcImg = Image.FromFile(tempFilename))
                {
                    ImagePropertyHandling.SetTitleOnImage(xmlNode, srcImg);
                    srcImg.Save(filePath);
                }
                ImageHashing.AddHash(filePath);
            }
            else
            {
                ConsoleWriter.WriteLine(3, "Identical Image Downloaded");
            }

            UrlsRetrieved.Add(fileUrl);
            File.Delete(tempFilename);
            return newImage;
        }

        internal static string GetFileName(XmlNode xmlNode)
        {
            var name = $"{xmlNode.SelectSingleNode("urlBase")?.InnerText.Substring(7)}.jpg";
            return Path.GetInvalidFileNameChars().Aggregate(name, (current, invalidChar) => current.Replace(invalidChar, '-'));
        }

        internal static XmlNodeList GetImages(int currentIndex, string country)
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
                            ConsoleWriter.WriteLine("Error getting images from XML response", e);
                            return null;
                        }
                    }

                    return null;
                }
            }
            catch (Exception e)
            {
                ConsoleWriter.WriteLine($"Error loading image search URL: {urlToLoad}", e);
                return null;
            }
        }
    }
}
