using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace BingImageDownload
{
    internal class Runner
    {
        public static int Start(RunnerArgs runnerArgs, in CancellationToken cancellationToken)
        {
            var paths = new Paths(runnerArgs.Path);
            var consoleWriter = new ConsoleWriter(paths);

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

                foreach (var country in countries)
                {
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
                consoleWriter.WriteLine($"Duration {stopwatch.Elapsed.TotalMinutes} minutes");

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
