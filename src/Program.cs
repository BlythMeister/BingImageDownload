using System;
using System.Configuration;
using System.IO;

namespace BingWallpaper
{
    internal class Program
    {
        internal static readonly string SavePath = ConfigurationManager.AppSettings["ImageSavePath"];
        internal static readonly string AppData = Path.Combine(SavePath, "App_Data");
        
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
