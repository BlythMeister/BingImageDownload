using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;

namespace BingWallpaper
{
    internal static class BingInteractionAndParsing
    {
        private const string Url = "http://bing.com";
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
                    XmlNodeList xmlNodeList;

                    try
                    {
                        xmlNodeList = GetImages(currentIndex, country.Name);
                    }
                    catch (Exception e)
                    {
                        ConsoleWriter.WriteLine($"Error getting images for {country.Name} - {country.DisplayName}", e);
                        continue;
                    }

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
            var fileurl = $"{Url}{xmlNode.SelectSingleNode("urlBase")?.InnerText}_1920x1080.jpg";
            if (UrlsRetrieved.Contains(fileurl))
            {
                ConsoleWriter.WriteLine(2, "Already Dowloaded Image URL");
                return false;
            }

            var filePath = Path.Combine(Program.SavePath, GetFileName(xmlNode));
            var tempFilename = Path.Combine(DownloadPath, Guid.NewGuid() + ".jpg");
            var fileWebRequest = WebRequest.Create(fileurl);

            using (var fileWebResponse = fileWebRequest.GetResponse())
            {
                using (var tempStream = File.Create(tempFilename))
                {
                    var buffer = new byte[1024];
                    using (var fileStream = fileWebResponse.GetResponseStream())
                    {
                        if (fileStream != null)
                        {
                            int bytesRead;
                            do
                            {
                                bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                                tempStream.Write(buffer, 0, bytesRead);
                            } while (bytesRead > 0);
                        }
                    }
                }
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

            UrlsRetrieved.Add(fileurl);
            File.Delete(tempFilename);
            return newImage;
        }

        internal static string GetFileName(XmlNode xmlNode)
        {
            var name = $"{xmlNode.SelectSingleNode("urlBase")?.InnerText.Substring(11)}.jpg";
            return Path.GetInvalidFileNameChars().Aggregate(name, (current, invalidChar) => current.Replace(invalidChar, '-'));
        }

        internal static XmlNodeList GetImages(int currentIndex, string country)
        {
            var webRequest = WebRequest.Create($"{Url}/HPImageArchive.aspx?format=xml&idx={currentIndex}&n=1&mkt={country}");
            using (var webResponse = webRequest.GetResponse())
            {
                using (var streamReader = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8))
                {
                    var output = streamReader.ReadToEnd();

                    if (output.Length > 0 && output.Contains("<images>"))
                    {
                        try
                        {
                            var xmlDocument = new XmlDocument();
                            xmlDocument.LoadXml(output);

                            return xmlDocument.GetElementsByTagName("image");
                        }
                        catch (Exception)
                        {
                            return null;
                        }
                    }
                }
                return null;
            }
        }
    }
}
