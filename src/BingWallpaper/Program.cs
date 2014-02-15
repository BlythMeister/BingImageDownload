using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using AForge.Imaging;
using Image = System.Drawing.Image;

namespace BingWallpaper
{
    internal class Program
    {
        private const string url = "http://bing.com";
        private static readonly string savePath = ConfigurationManager.AppSettings["ImageSavePath"];
        private static readonly string downloadPath = Path.Combine(savePath, "Temp");
        private static readonly string hitogramPath = Path.Combine(savePath, "TempHistogram");
        private static readonly string archivePath = Path.Combine(savePath, "Archive");
        private static readonly string logPath = Path.Combine(savePath, "Logs");
        private static readonly int archiveMonths = int.Parse(ConfigurationManager.AppSettings["ArchiveAfterMonths"]);
        private static readonly string[] countries = ConfigurationManager.AppSettings["Countries"].Split(',');
        private static readonly List<int[]> histogramHashTable = new List<int[]>();
        private static readonly List<string> urlsRetrieved = new List<string>();

        private static void Main(string[] args)
        {
            try
            {
                SetupDirectoriesAndLogging();
                ArchiveOldImages();
                GetBingImages();
            }
            catch (Exception e)
            {
                ConsoleWriter.WriteLine("Error: {0}", e.Message);
                throw;
            }
            finally
            {
                TidyTempFolders();
            }
        }

        private static void TidyTempFolders()
        {
            if (Directory.Exists(downloadPath)) Directory.Delete(downloadPath, true);
            if (Directory.Exists(hitogramPath)) Directory.Delete(hitogramPath, true);
        }

        private static void SetupDirectoriesAndLogging()
        {
            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);
            if (!Directory.Exists(downloadPath)) Directory.CreateDirectory(downloadPath);
            if (!Directory.Exists(hitogramPath)) Directory.CreateDirectory(hitogramPath);
            if (!Directory.Exists(archivePath)) Directory.CreateDirectory(archivePath);
            if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);

            ConsoleWriter.SetupLogWriter(Path.Combine(logPath, string.Format("Log-{0}.txt", DateTime.UtcNow.ToString("yyyy-MM-dd"))));
            foreach (var file in Directory.GetFiles(logPath))
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastAccessTimeUtc < DateTime.UtcNow.AddDays(-14))
                {
                    fileInfo.Delete();
                }
            }

        }

        private static void ArchiveOldImages()
        {
            if (archiveMonths <= 0) return;

            foreach (var file in Directory.GetFiles(savePath))
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastAccessTimeUtc < DateTime.UtcNow.AddMonths(archiveMonths * -1))
                {
                    fileInfo.MoveTo(Path.Combine(archivePath, fileInfo.Name));
                }
            }
        }

        private static void GetBingImages()
        {
            var downloadedImages = 0;

            AddImagesToHash();

            foreach (var country in countries)
            {
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
                            ConsoleWriter.WriteLine("Image for: '{0}' on {1}-{2} using index {3}", country, xmlNode.SelectSingleNode("startdate").InnerText, xmlNode.SelectSingleNode("enddate").InnerText, currentIndex);
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
            }

            ConsoleWriter.WriteLine("Found {0} new images", downloadedImages);
        }

        private static void AddImagesToHash()
        {
            foreach (var file in Directory.GetFiles(savePath, "*.jpg"))
            {
                histogramHashTable.Add(GetRGBHistogram(file));
            }
        }

        private static bool DownloadAndSaveImage(XmlNode xmlNode)
        {
            var fileurl = string.Format("{0}{1}_1920x1080.jpg", url, xmlNode.SelectSingleNode("urlBase").InnerText);
            if (urlsRetrieved.Contains(fileurl)) return false;

            var filePath = Path.Combine(savePath, GetFileName(xmlNode));
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

            if (!ImageInHash(tempfilename) && !File.Exists(filePath))
            {
                ConsoleWriter.WriteLine("Found Image For {0}-{1}", xmlNode.SelectSingleNode("startdate").InnerText, xmlNode.SelectSingleNode("enddate").InnerText);
                using (var srcImg = Image.FromFile(tempfilename))
                {
                    SetTitleOnImage(xmlNode, srcImg);
                    srcImg.Save(filePath);
                }
                histogramHashTable.Add(GetRGBHistogram(filePath));
                urlsRetrieved.Add(fileurl);
                return true;
            }

            File.Delete(tempfilename);
            return false;
        }

        private static bool ImageInHash(string tempfilename)
        {
            return histogramHashTable.Any(ints => ints.SequenceEqual(GetRGBHistogram(tempfilename)));
        }

        private static int[] GetRGBHistogram(string file)
        {
            var values = new List<int>();
            var histogramfile = Path.Combine(hitogramPath, Guid.NewGuid() + ".jpg");
            File.Copy(file, histogramfile);
            using(var bmp = new System.Drawing.Bitmap(histogramfile))
            {
                // Luminance
                var hslStatistics = new ImageStatisticsHSL(bmp);
                values.AddRange(hslStatistics.Luminance.Values.ToList());
            
                // RGB
                var rgbStatistics = new ImageStatistics(bmp);
                values.AddRange(rgbStatistics.Red.Values.ToList());
                values.AddRange(rgbStatistics.Green.Values.ToList());
                values.AddRange(rgbStatistics.Blue.Values.ToList());
            }

            File.Delete(histogramfile);

            return values.ToArray();
        }

        private static string GetFileName(XmlNode xmlNode)
        {
            var name = string.Format("{0}.jpg", xmlNode.SelectSingleNode("urlBase").InnerText.Split('/').Last());
            return Path.GetInvalidFileNameChars().Aggregate(name, (current, invalidChar) => current.Replace(invalidChar, '-'));
        }

        private static void SetTitleOnImage(XmlNode xmlNode, Image image)
        {
            var copyright = xmlNode.SelectSingleNode("copyright").InnerText;
            var title = copyright;
            var author = string.Empty;
            if (copyright.Contains("©"))
            {
                var copySymbolPosition = copyright.LastIndexOf("©");
                title = copyright.Substring(0, copySymbolPosition - 1).Trim();
                author = copyright.Substring(copySymbolPosition + 1, copyright.LastIndexOf(")") - copySymbolPosition - 1).Trim();
            }

            SetPropertyItemString(image, ImageMetadataPropertyId.Title, title);
            SetPropertyItemString(image, ImageMetadataPropertyId.Author, author);
            SetPropertyItemString(image, ImageMetadataPropertyId.Comment, string.Format("Bing Paper For {0}-{1}", xmlNode.SelectSingleNode("startdate").InnerText, xmlNode.SelectSingleNode("enddate").InnerText));
            SetPropertyItemString(image, ImageMetadataPropertyId.Keywords, DateTime.Now.ToShortDateString());
        }

        private static void SetPropertyItemString(Image srcImg, ImageMetadataPropertyId id, string value)
        {
            var buffer = Encoding.Unicode.GetBytes(value);
            var propItem = srcImg.GetPropertyItem(srcImg.PropertyItems[0].Id);
            propItem.Id = (int)id;
            propItem.Type = 1;
            propItem.Len = buffer.Length;
            propItem.Value = buffer;
            srcImg.SetPropertyItem(propItem);
        }

        private static XmlNodeList GetImages(int currentIndex, string country)
        {
            var webRequest = WebRequest.Create(string.Format("{0}/HPImageArchive.aspx?format=xml&idx={1}&n=1&mkt={2}", url, currentIndex, country));
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

    public enum ImageMetadataPropertyId
    {
        Title = 40091,
        Comment = 40092,
        Author = 40093,
        Keywords = 40094,
        Subject = 40095
    }
}
