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
        private static readonly string url = ConfigurationManager.AppSettings["BingUrl"];
        private static readonly string savePath = ConfigurationManager.AppSettings["ImageSavePath"];
        private static readonly string downloadPath = ConfigurationManager.AppSettings["ImageSavePathTemp"];
        private static readonly string[] countries = new[] { "en-US", "en-UK", "en-GB", "en-AU", "en-NZ", "en-CA", "de-DE", "zh-CN", "ja-JP" };
        private static readonly List<byte[]> hashTable = new List<byte[]>();

        private static void Main(string[] args)
        {
            try
            {
                GetBingImages();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                throw;
            }
        }

        private static void GetBingImages()
        {
            if (!Directory.Exists(savePath)) Directory.CreateDirectory(savePath);
            if (!Directory.Exists(downloadPath)) Directory.CreateDirectory(downloadPath);
            var downloadedImages = 0;
            AddImagesToHash();

            foreach (var country in countries)
            {
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
                            downloadedImages += DownloadAndSaveImage(xmlNode);
                        }

                        if (xmlNodeList.Count < 8)
                        {
                            moreImages = false;
                        }
                        else
                        {
                            currentIndex += 8;
                        }
                    }
                }
            }

            Console.WriteLine("Found {0} new images", downloadedImages);
            if (Directory.Exists(downloadPath)) Directory.Delete(downloadPath, true);
        }

        private static void AddImagesToHash()
        {
            foreach (var file in Directory.GetFiles(savePath))
            {
                hashTable.Add(Sha256HashFile(file));
            }
        }

        private static int DownloadAndSaveImage(XmlNode xmlNode)
        {
            var downloadedImages = 0;
            var fileurl = string.Format("{0}{1}", url, xmlNode.SelectSingleNode("url").InnerText);
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
                Console.WriteLine("New File Found, Saving {0}", filePath);
                File.Move(tempfilename, filePath);
                hashTable.Add(Sha256HashFile(filePath));
                downloadedImages++;
            }
            else
            {
                Console.WriteLine("Duplicate Content Found, Will Disacrd {0}", tempfilename);
                File.Delete(tempfilename);
            }
            return downloadedImages;
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
            var webRequest = WebRequest.Create(string.Format("{0}/HPImageArchive.aspx?format=xml&idx={1}&n=8&mkt={2}", url, currentIndex, country));
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
