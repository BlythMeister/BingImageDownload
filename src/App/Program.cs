using System;
using System.Configuration;
using System.IO;
using System.Threading;

namespace BingWallpaper
{
    internal class Program
    {
        internal static readonly string SavePath = ConfigurationManager.AppSettings["ImageSavePath"];
        internal static readonly string ArchivePath = Path.Combine(SavePath, "Archive");
        internal static readonly string AppData = Path.Combine(SavePath, "App_Data");

        private static void Main()
        {
            try
            {
                SetupAndTearDown.Startup();
                BingInteractionAndParsing.GetBingImages();
            }
            catch (Exception e)
            {
                ConsoleWriter.WriteLine("Unhandled Error", e);
                throw;
            }
            finally
            {
                Thread.Sleep(TimeSpan.FromSeconds(30));
                SetupAndTearDown.Finish();
            }
        }
    }
}
