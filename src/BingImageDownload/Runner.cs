using System;
using System.Threading;

namespace BingImageDownload
{
    internal class Runner
    {
        public static int Start(RunnerArgs runnerArgs, in CancellationToken cancellationToken)
        {
            var paths = new Paths(runnerArgs.Path);
            var consoleWriter = new ConsoleWriter(paths);
            var serializer = new Serializer(consoleWriter);
            var imageHashing = new ImageHashing(consoleWriter, paths, serializer);
            var imagePropertyHandling = new ImagePropertyHandling();
            var bingInteractionAndParsing = new BingInteractionAndParsing(consoleWriter, imageHashing, imagePropertyHandling, paths, serializer);
            var fileClearer = new FileClearer(consoleWriter, paths, runnerArgs.ArchiveMonths);

            try
            {
                bingInteractionAndParsing.GetBingImages(cancellationToken);
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
                Thread.Sleep(TimeSpan.FromSeconds(30));
                fileClearer.ArchiveOldImages();
                fileClearer.ClearLogFiles();
                fileClearer.ClearTempFolders();
            }

            return 0;
        }
    }
}
