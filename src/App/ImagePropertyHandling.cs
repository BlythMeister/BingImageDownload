using System;
using System.Drawing;
using System.Text;
using System.Xml;

namespace BingWallpaper
{
    internal static class ImagePropertyHandling
    {
        internal static void SetTitleOnImage(XmlNode xmlNode, Image image)
        {
            var copyright = xmlNode.SelectSingleNode("copyright")?.InnerText;
            var title = copyright;
            var author = string.Empty;
            var headline = xmlNode.SelectSingleNode("headline")?.InnerText;

            if (copyright != null && copyright.Contains("©"))
            {
                var copySymbolPosition = copyright.LastIndexOf("©", StringComparison.Ordinal);
                title = copyright.Substring(0, copySymbolPosition - 1).Trim();
                var endOfCopyright = copyright.LastIndexOf(")", StringComparison.Ordinal) - copySymbolPosition - 1;
                author = endOfCopyright == -1 ? copyright.Substring(copySymbolPosition + 1).Trim() : copyright.Substring(copySymbolPosition + 1, endOfCopyright).Trim();
            }

            SetPropertyItemString(image, ImageMetadataPropertyId.Title, title);
            SetPropertyItemString(image, ImageMetadataPropertyId.Author, author);
            SetPropertyItemString(image, ImageMetadataPropertyId.Comment, $"Bing Image '{headline}' For {xmlNode.SelectSingleNode("startdate")?.InnerText}-{xmlNode.SelectSingleNode("enddate")?.InnerText}");
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
    }
}
