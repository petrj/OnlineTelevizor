using System;
using System.IO;
using Newtonsoft.Json;

namespace SledovaniTVAPI
{
    public class JSONObject
    {
        public string status { get; set; }
        public string error { get; set; }

        public static string AppDataDir
        {
            get
            {
                var sep = Path.DirectorySeparatorChar.ToString();
                var dir = System.AppDomain.CurrentDomain.BaseDirectory;
                if (!dir.EndsWith(sep))
                {
                    dir += sep;
                }

                return dir;
            }
        }

        public void SaveToFile(string name)
        {
            if (!Path.IsPathRooted(name))
            {
                name = AppDataDir + name;
            }

            File.WriteAllText(name, this.ToString());
        }

        public static bool FileExists(string name)
        {
            if (!Path.IsPathRooted(name))
            {
                name = AppDataDir + name;
            }
            return File.Exists(name);
        }

        public static T LoadFromFile<T>(string name)
        {
            if (!Path.IsPathRooted(name))
            {
                name = AppDataDir + name;
            }
            string s = File.ReadAllText(name);

            var obj = JsonConvert.DeserializeObject<T>(s);

            return obj;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
