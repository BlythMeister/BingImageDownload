using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using BingWallpaper;

static internal class BingInteractionAndParsing
{
    private const string url = "http://bing.com";
    internal static readonly string downloadPath = Path.Combine(Program.appData, "Temp");
    internal static readonly List<string> urlsRetrieved = new List<string>();
    internal static readonly string[] countries = ConfigurationManager.AppSettings["Countries"].Split(',');

    internal static void GetBingImages()
    {
        var downloadedImages = 0;

        foreach (var country in countries)
        {
            ConsoleWriter.WriteLine("Searching for images for {0}", country);
            var countryImages = 0;
            var countryDuplicateImages = 0;
            var currentIndex = 0;
            var moreImages = true;
            while (moreImages)
            {
                var xmlNodeList = GetImages(currentIndex, country);
                if (xmlNodeList == null)
                {
                    moreImages = false;
                }
                else
                {
                    foreach (XmlNode xmlNode in xmlNodeList)
                    {
                        ConsoleWriter.WriteLine(1, "Image for: '{0}' on {1}-{2} using index {3}", country, xmlNode.SelectSingleNode("startdate").InnerText, xmlNode.SelectSingleNode("enddate").InnerText, currentIndex);
                        if (DownloadAndSaveImage(xmlNode))
                        {
                            countryImages++;
                        }
                        else
                        {
                            countryDuplicateImages++;
                        }
                    }

                    currentIndex += 1;
                }
            }

            downloadedImages += countryImages;
            ConsoleWriter.WriteLine("Found {0} new images for {1}", countryImages, country);
            ConsoleWriter.WriteLine("Found {0} duplicate images for {1}", countryDuplicateImages, country);
            ConsoleWriter.WriteLine("");
        }

        ConsoleWriter.WriteLine("Found {0} new images", downloadedImages);
    }

    internal static bool DownloadAndSaveImage(XmlNode xmlNode)
    {
        var fileurl = String.Format("{0}{1}_1920x1080.jpg", url, xmlNode.SelectSingleNode("urlBase").InnerText);
        if (urlsRetrieved.Contains(fileurl))
        {
            ConsoleWriter.WriteLine(2, "Already Dowloaded Image URL");
            return false;
        }

        var filePath = Path.Combine(Program.savePath, GetFileName(xmlNode));
        var tempfilename = Path.Combine(downloadPath, Guid.NewGuid() + ".jpg");
        var fileWebRequest = WebRequest.Create(fileurl);

        using (var fileWebResponse = fileWebRequest.GetResponse())
        {
            using (var tempStream = File.Create(tempfilename))
            {
                var buffer = new byte[1024];
                using (var fileStream = fileWebResponse.GetResponseStream())
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

        ConsoleWriter.WriteLine(2, "Downloaded Image, Checking If Duplicate");
            
        var newImage = false;
        if (!ImageHashing.ImageInHash(tempfilename) && !File.Exists(filePath))
        {
            newImage = true;
            ConsoleWriter.WriteLine(3, "Found New Image");
            using (var srcImg = Image.FromFile(tempfilename))
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

        urlsRetrieved.Add(fileurl);
        File.Delete(tempfilename);
        return newImage;
    }

    internal static string GetFileName(XmlNode xmlNode)
    {
        var name = String.Format("{0}.jpg", xmlNode.SelectSingleNode("urlBase").InnerText.Split('/').Last());
        return Path.GetInvalidFileNameChars().Aggregate(name, (current, invalidChar) => current.Replace(invalidChar, '-'));
    }

    internal static XmlNodeList GetImages(int currentIndex, string country)
    {
        var webRequest = WebRequest.Create(String.Format("{0}/HPImageArchive.aspx?format=xml&idx={1}&n=1&mkt={2}", url, currentIndex, country));
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