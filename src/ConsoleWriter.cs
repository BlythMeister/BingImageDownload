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
        internal static bool HaveWrittenExeption;

        internal static void WriteLine()
        {
            WriteToFile("");
            Console.WriteLine();
        }

        internal static void WriteLine(string text, Exception exception)
        {
            HaveWrittenExeption = true;
            WriteException(text, exception, 1);
        }

        private static void WriteException(string text, Exception exception, int calls)
        {
            var formattedText = calls > 1 ? $"Inner Exception - {exception.Message}" : $"{text} - {exception.Message}";
            WriteToFile(formattedText);
            WriteToFile(exception.StackTrace);

            Console.WriteLine(formattedText);
            Console.Write(exception.StackTrace);

            if (exception.InnerException != null && calls < 5)
            {
                WriteException(text, exception.InnerException, calls + 1);
            }
        }

        internal static void WriteLine(string text)
        {
            WriteLine(text, new object[] { });
        }

        internal static void WriteLine(string text, params Object[] args)
        {
            var formattedText = string.Format(text, args);

            WriteToFile(formattedText);
            Console.WriteLine(formattedText);
        }

        internal static void WriteLine(int indentation, string text, params Object[] args)
        {
            var formattedText = string.Format(text, args);
            for (var i = 0; i < indentation; i++)
            {
                formattedText = $"  {formattedText}";
            }

            WriteToFile(formattedText);
            Console.WriteLine(formattedText);
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

        internal static void CloseLogWriterStream()
        {
            logWriter.Close();
            logWriter = null;
        }
    }
}
