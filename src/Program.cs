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
        internal static readonly string savePath = ConfigurationManager.AppSettings["ImageSavePath"];
        internal static readonly string appData = Path.Combine(savePath, "App_Data");
        
        private static void Main(string[] args)
        {
            try
            {
                SetupAndTearDown.Startup();
                SetupAndTearDown.ArchiveOldImages();
                BingInteractionAndParsing.GetBingImages();
            }
            catch (Exception e)
            {
                ConsoleWriter.WriteLine("Error: {0}", e.Message);
                throw;
            }
            finally
            {
                SetupAndTearDown.Finish();
            }
        }
    }
}
