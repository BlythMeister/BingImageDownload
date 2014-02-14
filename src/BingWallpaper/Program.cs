using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace BingWallpaper
{
    internal class Program
    {
        private const string url = "http://bing.com";
        private static readonly string savePath = ConfigurationManager.AppSettings["ImageSavePath"];
        private static readonly string downloadPath = Path.Combine(savePath, "Temp");
        private static readonly string archivePath = Path.Combine(savePath, "Archive");
        private static readonly string logPath = Path.Combine(savePath, "Logs");
        private static readonly int archiveMonths = int.Parse(ConfigurationManager.AppSettings["ArchiveAfterMonths"]);
        private static readonly string[] countries = ConfigurationManager.AppSettings["Countries"].Split(',');
        private static readonly List<byte[]> hashTable = new List<byte[]>();
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
                if (Directory.Exists(downloadPath)) Directory.Delete(downloadPath, true);
            }
        }

        private static void SetupDirectoriesAndLogging()
        {
            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);
            if (!Directory.Exists(downloadPath)) Directory.CreateDirectory(downloadPath);
            if (!Directory.Exists(archivePath)) Directory.CreateDirectory(archivePath);
            if (!Directory.Exists(logPath)) Directory.CreateDirectory(logPath);

            ConsoleWriter.SetupLogWriter(Path.Combine(logPath,string.Format("Log-{0}.txt", DateTime.UtcNow.ToString("yyyy-MM-dd"))));
            foreach (var file in Directory.GetFiles(logPath))
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastAccessTimeUtc < DateTime.UtcNow.AddDays(-14))
                {
                    fileInfo.Delete();}
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
                hashTable.Add(Sha256HashFile(file));
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
                File.Move(tempfilename, filePath);
                hashTable.Add(Sha256HashFile(filePath));
                urlsRetrieved.Add(fileurl);
                return true;
            }

            File.Delete(tempfilename);
            return false;
        }

        private static bool ImageInHash(string tempfilename)
        {
            return hashTable.Any(bytes => bytes.SequenceEqual(Sha256HashFile(tempfilename)));
        }

        private static byte[] Sha256HashFile(string file)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var input = File.OpenRead(file))
                {
                    return sha256.ComputeHash(input);
                }
            }
        }

        private static string GetFileName(XmlNode xmlNode)
        {
            var name = xmlNode.SelectSingleNode("copyright").InnerText;
            if (name.Contains("("))
            {
                name = name.Substring(0, name.LastIndexOf("("));
            }
            name += ".jpg";
            return name;
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
}
