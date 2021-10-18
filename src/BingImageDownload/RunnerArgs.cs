using McMaster.Extensions.CommandLineUtils;
using System.Threading;

namespace BingImageDownload
{
    internal class RunnerArgs
    {
        [Option("-p|--path <PATH>", "The path to where images should be saved (Default: Home Path)", CommandOptionType.SingleValue)]
        public string Path { get; }

        [Option("-a|--archive <VALUE>", "The number of months to archive after (Default: 1)", CommandOptionType.SingleValue)]
        public int? ArchiveMonths { get; }

        [Option("-d|--delete <VALUE>", "The number of months to delete after (Default: never)", CommandOptionType.SingleValue)]
        public int? DeleteMonths { get; }

        [Option("-r|--resolution <VALUE>", "The resolution for images (HD,FHD,UHD) (Default: FHD)", CommandOptionType.SingleValue)]
        public string Resolution { get; }

        private int OnExecute(CancellationToken cancellationToken) => Runner.Start(this, cancellationToken);
    }
}
