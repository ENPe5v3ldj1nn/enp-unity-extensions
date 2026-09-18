using System;
using Newtonsoft.Json;

namespace ENP.UnityExtensions.Runtime
{
    // Writes LanguageId as its stable code ("uk", "pt-BR") so saves no longer depend on enum order.
    // Reads that code, the enum name, and the legacy integer written before this converter existed —
    // old saves migrate transparently on their next save. An unrecognised value falls back to
    // EnglishUS instead of throwing: one bad field must not make a whole settings file unloadable.
    public sealed class LanguageIdJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(LanguageId) || objectType == typeof(LanguageId?);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteValue(((LanguageId)value).ToCode());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return objectType == typeof(LanguageId?) ? (object)null : LanguageId.EnglishUS;
                case JsonToken.Integer:
                    return FromLegacyInteger(Convert.ToInt64(reader.Value));
                case JsonToken.String:
                    return FromString((string)reader.Value);
                default:
                    return LanguageId.EnglishUS;
            }
        }

        private static LanguageId FromLegacyInteger(long value)
        {
            if (value < int.MinValue || value > int.MaxValue)
                return LanguageId.EnglishUS;

            var id = (LanguageId)(int)value;
            return Enum.IsDefined(typeof(LanguageId), id) ? id : LanguageId.EnglishUS;
        }

        private static LanguageId FromString(string value)
        {
            if (LanguageIdExtensions.TryFromCode(value, out var byCode))
                return byCode;

            if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse(value.Trim(), true, out LanguageId byName) &&
                Enum.IsDefined(typeof(LanguageId), byName))
                return byName;

            return LanguageId.EnglishUS;
        }
    }
}
