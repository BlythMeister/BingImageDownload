using Newtonsoft.Json;
using System;
using System.IO;

namespace BingWallpaper
{
    internal static class Serializer
    {
        internal static T Deserialize<T>(string path) where T : new()
        {
            try
            {
                if (File.Exists(path))
                {
                    var text = File.ReadAllText(path);
                    return JsonConvert.DeserializeObject<T>(text);
                }
            }
            catch (Exception)
            {
                // ignored
            }

            return new T();
        }

        internal static void Serialize<T>(T collection, string path) where T : new()
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(collection, Formatting.Indented));
        }
    }
}
