using System;
using Newtonsoft.Json;

namespace ENP.UnityExtensions.Runtime
{
    [Serializable]
    public class JsonSerializableBase
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
