using System;
using System.Drawing;
using System.Text;
using System.Xml;
using BingWallpaper;

static internal class ImagePropertyHandling
{
    internal static void SetTitleOnImage(XmlNode xmlNode, Image image)
    {
        var copyright = xmlNode.SelectSingleNode("copyright").InnerText;
        var title = copyright;
        var author = string.Empty;
        if (copyright.Contains("©"))
        {
            var copySymbolPosition = copyright.LastIndexOf("©");
            title = copyright.Substring(0, copySymbolPosition - 1).Trim();
            var endOfCopyright = copyright.LastIndexOf(")") - copySymbolPosition - 1;
            if (endOfCopyright == -1)
            {
                author = copyright.Substring(copySymbolPosition + 1).Trim();
            }
            else
            {
                author = copyright.Substring(copySymbolPosition + 1, endOfCopyright).Trim();
            }
        }

        SetPropertyItemString(image, ImageMetadataPropertyId.Title, title);
        SetPropertyItemString(image, ImageMetadataPropertyId.Author, author);
        SetPropertyItemString(image, ImageMetadataPropertyId.Comment, String.Format("Bing Paper For {0}-{1}", xmlNode.SelectSingleNode("startdate").InnerText, xmlNode.SelectSingleNode("enddate").InnerText));
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