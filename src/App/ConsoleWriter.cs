using System;
using System.IO;
using System.Text;

namespace BingWallpaper
{
    internal static class ConsoleWriter
    {
        private static readonly StringBuilder TempBuilder = new StringBuilder();
        private static readonly object LockThis = new object();
        private static StreamWriter logWriter;

        internal static void WriteLine(string text, Exception exception)
        {
            WriteLine($"{text} - {exception}"); ;
        }

        internal static void WriteLine(int indentation, string text)
        {
            for (var i = 0; i < indentation; i++)
            {
                text = $"  {text}";
            }

            WriteLine(text);
        }

        internal static void WriteLine(string text)
        {
            WriteToFile(text);
            Console.WriteLine(text);
        }

        internal static void WriteToFile(string textLine)
        {
            if (logWriter == null)
            {
                TempBuilder.AppendLine(textLine);
                return;
            }

            if (TempBuilder.Length > 0)
            {
                logWriter.Write(TempBuilder.ToString());
                TempBuilder.Clear();
            }

            WriteToStream(textLine);
        }

        internal static void SetupLogWriter(string filePath)
        {
            logWriter = new StreamWriter(filePath, false, Encoding.UTF8);

            if (TempBuilder.Length > 0)
            {
                WriteToStream(TempBuilder.ToString());
                logWriter.Flush();
                TempBuilder.Clear();
            }

            logWriter.AutoFlush = true;
        }

        private static void WriteToStream(string text)
        {
            lock (LockThis)
            {
                logWriter.WriteLine("{0:yyyy-MM-dd HH:mm} - {1}", DateTime.UtcNow, text);
            }
        }
    }
}
