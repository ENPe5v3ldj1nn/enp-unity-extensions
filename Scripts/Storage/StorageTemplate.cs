using UnityEngine;

namespace enp_unity_extensions.Scripts.Storage
{
    public class StorageTemplate
    {
        private const string FILE_PATH = "/filePath/";
        private const string FILENAME = "searchFilters.fltr";
        
        public static void SaveFile(Object file)
        {
            Storage.Save(FILE_PATH, FILENAME, file);
        }
        
        public static Object LoadFile()
        {
            return Storage.Load<Object>(FILE_PATH, FILENAME);
        }
    }
}