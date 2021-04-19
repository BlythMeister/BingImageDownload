using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace BingImageDownload
{
    internal class Runner
    {
        public static int Start(RunnerArgs runnerArgs, in CancellationToken cancellationToken)
        {
            var paths = new Paths(runnerArgs.Path);
            var consoleWriter = new ConsoleWriter(paths);

            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var version = FileVersionInfo.GetVersionInfo(assemblyLocation).ProductVersion;

            consoleWriter.WriteLine("----------------------------");
            consoleWriter.WriteLine("|                          |");
            consoleWriter.WriteLine("|    Bing Image Download   |");
            consoleWriter.WriteLine($"|{version.PadLeft(9 + version.Length).PadRight(26)}|");
            consoleWriter.WriteLine("|                          |");
            consoleWriter.WriteLine("|    Author: Chris Blyth   |");
            consoleWriter.WriteLine("|                          |");
            consoleWriter.WriteLine("----------------------------");

            consoleWriter.WriteLine($"Saving to: {paths.SavePath}");

            var serializer = new Serializer(consoleWriter);
            var imageFingerprinting = new ImageFingerprinting(consoleWriter, paths, serializer);
            var imagePropertyHandling = new ImagePropertyHandling();
            var bingInteractionAndParsing = new BingInteractionAndParsing(consoleWriter, imageFingerprinting, imagePropertyHandling, paths, serializer);
            var fileClearer = new FileClearer(consoleWriter, paths, runnerArgs.ArchiveMonths);

            try
            {
                var downloadedImages = 0;
                var duplicateImages = 0;
                var seenUrls = 0;

                var countries = CultureInfo.GetCultures(CultureTypes.AllCultures).Where(x => x.Name.Contains("-")).ToList();
                consoleWriter.WriteLine($"Have loaded {countries.Count} countries");

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                for (int i = 0; i < countries.Count; i++)
                {
                    var country = countries[i];
                    consoleWriter.WriteLine($"Searching for images for {country.Name} - {country.DisplayName} ({i + 1}/{countries.Count})");

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    var (countryDownloadedImages, countryDuplicateImages, countrySeenUrls) = bingInteractionAndParsing.GetBingImages(country);

                    downloadedImages += countryDownloadedImages;
                    duplicateImages += countryDuplicateImages;
                    seenUrls += countrySeenUrls;
                }

                consoleWriter.WriteLine("Summary:");
                consoleWriter.WriteLine($"Found {downloadedImages} new images");
                consoleWriter.WriteLine($"Found {duplicateImages} duplicate images");
                consoleWriter.WriteLine($"Found {seenUrls} urls already downloaded");
                consoleWriter.WriteLine($"Duration {Math.Round(stopwatch.Elapsed.TotalMinutes, 2)} minutes");

                if (cancellationToken.IsCancellationRequested)
                {
                    return -2;
                }
            }
            catch (Exception e)
            {
                consoleWriter.WriteLine("Error processing", e);
                return -1;
            }
            finally
            {
                consoleWriter.WriteLine("Done, waiting 5 seconds before clearing up");
                Thread.Sleep(TimeSpan.FromSeconds(5));
                consoleWriter.WriteLine("Clearing up");
                fileClearer.ArchiveOldImages();
                fileClearer.ClearLogFiles();
                fileClearer.ClearTempFolders();
            }

            return 0;
        }
    }
}
