using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace enp_unity_extensions.Scripts.Storage
{
    public static class Storage
    {
        private static string _persistencePath;
        private static string PersistencePath
        {
            get
            {
                if (string.IsNullOrEmpty(_persistencePath))
                {
                    _persistencePath = Application.persistentDataPath;
                }

                return _persistencePath;
            }
        }

        public static T Load<T>(string path, string fileName)
        {
            var directory = PersistencePath + path;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var filePath = Path.Combine(directory, fileName);
            if (!File.Exists(filePath))
                return default;

            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static void Save<T>(string path, string fileName, T data)
        {
            var directory = PersistencePath + path;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var filePath = Path.Combine(directory, fileName);
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }
}