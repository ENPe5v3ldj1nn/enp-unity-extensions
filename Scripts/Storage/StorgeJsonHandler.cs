using System;
using Unity.Plastic.Newtonsoft.Json;

namespace enp_unity_extensions.Scripts.Storage
{
    [Serializable]
    public class StorgeJsonHandler
    {
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }
        
        public string ToJson(JsonSerializerSettings settings)
        {
            return JsonConvert.SerializeObject(this, settings);
        }
    }
}