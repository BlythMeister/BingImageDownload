using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace BingWallpaper
{
    internal static class Serializer
    {
        internal static IEnumerable<T> Deserialize<T>(string path)
        {
            var endpointCollection = new List<T>();

            if (File.Exists(path))
            {
                var text = File.ReadAllText(path);
                endpointCollection = JsonConvert.DeserializeObject<List<T>>(text);
            }

            return endpointCollection;
        }

        internal static void Serialize<T>(IEnumerable<T> collection, string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(collection));
        }
    }
}
