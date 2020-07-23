using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using System;
using System.Text;

namespace BingImageDownload
{
    internal class ImagePropertyHandling
    {
        internal void SetImageExifTags(string copyright, string headline, Image image)
        {
            var title = copyright;
            var author = string.Empty;

            if (copyright != null && copyright.Contains("©"))
            {
                var copySymbolPosition = copyright.LastIndexOf("©", StringComparison.Ordinal);
                title = copyright.Substring(0, copySymbolPosition - 1).Trim();
                var endOfCopyright = copyright.LastIndexOf(")", StringComparison.Ordinal) - copySymbolPosition - 1;
                author = endOfCopyright == -1 ? copyright.Substring(copySymbolPosition + 1).Trim() : copyright.Substring(copySymbolPosition + 1, endOfCopyright).Trim();
            }

            image.Metadata.ExifProfile ??= new ExifProfile();

            void SetPropertyItemString(ExifTag<byte[]> tag, string value)
            {
                var buffer = Encoding.Unicode.GetBytes(value);
                image.Metadata.ExifProfile.SetValue(tag, buffer);
            }

            SetPropertyItemString(ExifTag.XPTitle, title);
            SetPropertyItemString(ExifTag.XPAuthor, author);
            SetPropertyItemString(ExifTag.XPComment, headline);
            SetPropertyItemString(ExifTag.XPKeywords, DateTime.Now.ToShortDateString());
        }
    }
}
