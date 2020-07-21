using System;
using System.IO;
using System.Text;

namespace BingImageDownload
{
    internal class ConsoleWriter
    {
        private readonly StringBuilder tempBuilder = new StringBuilder();
        private readonly object lockThis = new object();
        private readonly StreamWriter logWriter;

        internal ConsoleWriter(Paths paths)
        {
            logWriter = new StreamWriter(Path.Combine(paths.LogPath, $"Log-{DateTime.UtcNow:yyyy-MM-dd}.txt"), false, Encoding.UTF8);

            if (tempBuilder.Length > 0)
            {
                WriteToStream(tempBuilder.ToString());
                logWriter.Flush();
                tempBuilder.Clear();
            }

            logWriter.AutoFlush = true;
        }

        internal void WriteLine(string text, Exception exception)
        {
            WriteLine($"{text} - {exception}");
        }

        internal void WriteLine(int indentation, string text, Exception exception)
        {
            for (var i = 0; i < indentation; i++)
            {
                text = $"  {text}";
            }

            WriteLine($"{text} - {exception}");
        }

        internal void WriteLine(int indentation, string text)
        {
            for (var i = 0; i < indentation; i++)
            {
                text = $"  {text}";
            }

            WriteLine(text);
        }

        internal void WriteLine(string text)
        {
            WriteToFile(text);
            Console.WriteLine(text);
        }

        internal void WriteToFile(string textLine)
        {
            if (logWriter == null)
            {
                tempBuilder.AppendLine(textLine);
                return;
            }

            if (tempBuilder.Length > 0)
            {
                logWriter.Write(tempBuilder.ToString());
                tempBuilder.Clear();
            }

            WriteToStream(textLine);
        }

        private void WriteToStream(string text)
        {
            lock (lockThis)
            {
                logWriter.WriteLine("{0:yyyy-MM-dd HH:mm} - {1}", DateTime.UtcNow, text);
            }
        }
    }
}
