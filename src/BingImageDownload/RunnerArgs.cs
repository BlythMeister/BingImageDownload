using McMaster.Extensions.CommandLineUtils;
using System.Threading;

namespace BingImageDownload
{
    internal class RunnerArgs
    {
        [Option("-p|--path <PATH>", "The path to where images should be saved (Default: C:\\Temp\\BingImageDownload)", CommandOptionType.SingleValue)]
        public string Path { get; }

        [Option("-a|--archive <VALUE>", "The number of months to archive after (Default: 1)", CommandOptionType.SingleValue)]
        public int? ArchiveMonths { get; }

        private int OnExecute(CancellationToken cancellationToken) => Runner.Start(this, cancellationToken);
    }
}
