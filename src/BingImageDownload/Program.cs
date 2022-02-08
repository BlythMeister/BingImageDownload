using McMaster.Extensions.CommandLineUtils;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BingImageDownload
{
    internal class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                Console.ResetColor();
                Console.Clear();
                return await CommandLineApplication.ExecuteAsync<RunnerArgs>(args);
            }
            finally
            {
                if (Debugger.IsAttached)
                {
                    Console.WriteLine("");
                    Console.WriteLine("-----------------------------------------------------");
                    Console.WriteLine("Press Enter To Close Debugger");
                    Console.ReadLine();
                }

                Console.ResetColor();
            }
        }
    }
}
