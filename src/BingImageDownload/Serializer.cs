using Newtonsoft.Json;
using System;
using System.IO;

namespace BingImageDownload
{
    internal class Serializer
    {
        private readonly ConsoleWriter consoleWriter;

        internal Serializer(ConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        internal T Deserialize<T>(string path) where T : new()
        {
            try
            {
                if (File.Exists(path))
                {
                    var text = File.ReadAllText(path);
                    return JsonConvert.DeserializeObject<T>(text);
                }
            }
            catch (Exception e)
            {
                consoleWriter.WriteLine($"Error Loading {path}", e);
            }

            return new T();
        }

        internal void Serialize<T>(T collection, string path) where T : new()
        {
            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(collection, Formatting.Indented));
            }
            catch (Exception e)
            {
                consoleWriter.WriteLine($"Error Saving {path}", e);
            }
        }
    }
}
