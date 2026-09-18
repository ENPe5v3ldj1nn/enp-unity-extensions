using System;
using Newtonsoft.Json;

namespace ENP.UnityExtensions.Runtime
{
    // Newtonsoft persists this as its ToCode() string (see LanguageIdJsonConverter), but Unity
    // serialization and saves written before the converter still store the integer value —
    // append new languages at the end only.
    [JsonConverter(typeof(LanguageIdJsonConverter))]
    public enum LanguageId
    {
        Afrikaans,
        Amharic,
        Bulgarian,
        Catalan,
        Croatian,
        Czech,
        Danish,
        German,
        Greek,
        EnglishUS,
        EnglishUK,
        SpanishSpain,
        SpanishLatinAmerica,
        Estonian,
        Finnish,
        Filipino,
        FrenchCanada,
        FrenchFrance,
        Hebrew,
        Hindi,
        Hungarian,
        Icelandic,
        Indonesian,
        Italian,
        Japanese,
        Korean,
        Lithuanian,
        Latvian,
        Malay,
        Dutch,
        Norwegian,
        Polish,
        PortugueseBrazil,
        PortuguesePortugal,
        Romanian,
        Russian,
        Slovak,
        Slovenian,
        Serbian,
        Swedish,
        Swahili,
        Thai,
        Turkish,
        Ukrainian,
        ChinesePRC,
        ChineseTaiwan,
        ChineseHongKong,
        Zulu,
        Vietnamese,
        Belarusian
    }

    public static class LanguageIdExtensions
    {
        public static string ToCode(this LanguageId id)
        {
            switch (id)
            {
                case LanguageId.Afrikaans: return "af";
                case LanguageId.Amharic: return "am";
                case LanguageId.Bulgarian: return "bg";
                case LanguageId.Belarusian: return "be";
                case LanguageId.Catalan: return "ca";
                case LanguageId.Croatian: return "hr";
                case LanguageId.Czech: return "cs";
                case LanguageId.Danish: return "da";
                case LanguageId.German: return "de";
                case LanguageId.Greek: return "el";
                case LanguageId.EnglishUS: return "en-US";
                case LanguageId.EnglishUK: return "en-GB";
                case LanguageId.SpanishSpain: return "es-ES";
                case LanguageId.SpanishLatinAmerica: return "es-419";
                case LanguageId.Estonian: return "et";
                case LanguageId.Finnish: return "fi";
                case LanguageId.Filipino: return "fil";
                case LanguageId.FrenchCanada: return "fr-CA";
                case LanguageId.FrenchFrance: return "fr-FR";
                case LanguageId.Hebrew: return "he";
                case LanguageId.Hindi: return "hi";
                case LanguageId.Hungarian: return "hu";
                case LanguageId.Icelandic: return "is";
                case LanguageId.Indonesian: return "id";
                case LanguageId.Italian: return "it";
                case LanguageId.Japanese: return "ja";
                case LanguageId.Korean: return "ko";
                case LanguageId.Lithuanian: return "lt";
                case LanguageId.Latvian: return "lv";
                case LanguageId.Malay: return "ms";
                case LanguageId.Dutch: return "nl";
                case LanguageId.Norwegian: return "no";
                case LanguageId.Polish: return "pl";
                case LanguageId.PortugueseBrazil: return "pt-BR";
                case LanguageId.PortuguesePortugal: return "pt-PT";
                case LanguageId.Romanian: return "ro";
                case LanguageId.Russian: return "ru";
                case LanguageId.Slovak: return "sk";
                case LanguageId.Slovenian: return "sl";
                case LanguageId.Serbian: return "sr";
                case LanguageId.Swedish: return "sv";
                case LanguageId.Swahili: return "sw";
                case LanguageId.Thai: return "th";
                case LanguageId.Turkish: return "tr";
                case LanguageId.Ukrainian: return "uk";
                case LanguageId.ChinesePRC: return "zh-CN";
                case LanguageId.ChineseTaiwan: return "zh-TW";
                case LanguageId.ChineseHongKong: return "zh-HK";
                case LanguageId.Zulu: return "zu";
                case LanguageId.Vietnamese: return "vi";
                default: throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }
        }

        public static string ToFolderName(this LanguageId id)
        {
            switch (id)
            {
                case LanguageId.Afrikaans: return "af_afrikaans";
                case LanguageId.Amharic: return "am_amharic";
                case LanguageId.Bulgarian: return "bg_bulgarian";
                case LanguageId.Belarusian: return "be_belarusian";
                case LanguageId.Catalan: return "ca_catalan";
                case LanguageId.Croatian: return "hr_croatian";
                case LanguageId.Czech: return "cs_czech";
                case LanguageId.Danish: return "da_danish";
                case LanguageId.German: return "de_german";
                case LanguageId.Greek: return "el_greek";
                case LanguageId.EnglishUS: return "en_us_english_united_states";
                case LanguageId.EnglishUK: return "en_gb_english_united_kingdom";
                case LanguageId.SpanishSpain: return "es_es_spanish_spain";
                case LanguageId.SpanishLatinAmerica: return "es_419_spanish_latin_america";
                case LanguageId.Estonian: return "et_estonian";
                case LanguageId.Finnish: return "fi_finnish";
                case LanguageId.Filipino: return "fil_filipino";
                case LanguageId.FrenchCanada: return "fr_ca_french_canada";
                case LanguageId.FrenchFrance: return "fr_fr_french_france";
                case LanguageId.Hebrew: return "he_hebrew";
                case LanguageId.Hindi: return "hi_hindi";
                case LanguageId.Hungarian: return "hu_hungarian";
                case LanguageId.Icelandic: return "is_icelandic";
                case LanguageId.Indonesian: return "id_indonesian";
                case LanguageId.Italian: return "it_italian";
                case LanguageId.Japanese: return "ja_japanese";
                case LanguageId.Korean: return "ko_korean";
                case LanguageId.Lithuanian: return "lt_lithuanian";
                case LanguageId.Latvian: return "lv_latvian";
                case LanguageId.Malay: return "ms_malay";
                case LanguageId.Dutch: return "nl_dutch";
                case LanguageId.Norwegian: return "no_norwegian";
                case LanguageId.Polish: return "pl_polish";
                case LanguageId.PortugueseBrazil: return "pt_br_portuguese_brazil";
                case LanguageId.PortuguesePortugal: return "pt_pt_portuguese_portugal";
                case LanguageId.Romanian: return "ro_romanian";
                case LanguageId.Russian: return "ru_russian";
                case LanguageId.Slovak: return "sk_slovak";
                case LanguageId.Slovenian: return "sl_slovenian";
                case LanguageId.Serbian: return "sr_serbian";
                case LanguageId.Swedish: return "sv_swedish";
                case LanguageId.Swahili: return "sw_swahili";
                case LanguageId.Thai: return "th_thai";
                case LanguageId.Turkish: return "tr_turkish";
                case LanguageId.Ukrainian: return "uk_ukrainian";
                case LanguageId.ChinesePRC: return "zh_cn_chinese_simplified_china";
                case LanguageId.ChineseTaiwan: return "zh_tw_chinese_traditional_taiwan";
                case LanguageId.ChineseHongKong: return "zh_hk_chinese_traditional_hong_kong";
                case LanguageId.Zulu: return "zu_zulu";
                case LanguageId.Vietnamese: return "vi_vietnamese";
                default: throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }
        }

        // Name of the language in that language itself — what a language picker should show, so a
        // player finds their own language regardless of which language the UI is currently in.
        public static string ToNativeName(this LanguageId id)
        {
            switch (id)
            {
                case LanguageId.Afrikaans: return "Afrikaans";
                case LanguageId.Amharic: return "አማርኛ";
                case LanguageId.Bulgarian: return "Български";
                case LanguageId.Belarusian: return "Беларуская";
                case LanguageId.Catalan: return "Català";
                case LanguageId.Croatian: return "Hrvatski";
                case LanguageId.Czech: return "Čeština";
                case LanguageId.Danish: return "Dansk";
                case LanguageId.German: return "Deutsch";
                case LanguageId.Greek: return "Ελληνικά";
                case LanguageId.EnglishUS: return "English (US)";
                case LanguageId.EnglishUK: return "English (UK)";
                case LanguageId.SpanishSpain: return "Español (España)";
                case LanguageId.SpanishLatinAmerica: return "Español (Latinoamérica)";
                case LanguageId.Estonian: return "Eesti";
                case LanguageId.Finnish: return "Suomi";
                case LanguageId.Filipino: return "Filipino";
                case LanguageId.FrenchCanada: return "Français (Canada)";
                case LanguageId.FrenchFrance: return "Français";
                case LanguageId.Hebrew: return "עברית";
                case LanguageId.Hindi: return "हिन्दी";
                case LanguageId.Hungarian: return "Magyar";
                case LanguageId.Icelandic: return "Íslenska";
                case LanguageId.Indonesian: return "Bahasa Indonesia";
                case LanguageId.Italian: return "Italiano";
                case LanguageId.Japanese: return "日本語";
                case LanguageId.Korean: return "한국어";
                case LanguageId.Lithuanian: return "Lietuvių";
                case LanguageId.Latvian: return "Latviešu";
                case LanguageId.Malay: return "Bahasa Melayu";
                case LanguageId.Dutch: return "Nederlands";
                case LanguageId.Norwegian: return "Norsk";
                case LanguageId.Polish: return "Polski";
                case LanguageId.PortugueseBrazil: return "Português (Brasil)";
                case LanguageId.PortuguesePortugal: return "Português (Portugal)";
                case LanguageId.Romanian: return "Română";
                case LanguageId.Russian: return "Русский";
                case LanguageId.Slovak: return "Slovenčina";
                case LanguageId.Slovenian: return "Slovenščina";
                case LanguageId.Serbian: return "Српски";
                case LanguageId.Swedish: return "Svenska";
                case LanguageId.Swahili: return "Kiswahili";
                case LanguageId.Thai: return "ไทย";
                case LanguageId.Turkish: return "Türkçe";
                case LanguageId.Ukrainian: return "Українська";
                case LanguageId.ChinesePRC: return "简体中文";
                case LanguageId.ChineseTaiwan: return "繁體中文 (台灣)";
                case LanguageId.ChineseHongKong: return "繁體中文 (香港)";
                case LanguageId.Zulu: return "isiZulu";
                case LanguageId.Vietnamese: return "Tiếng Việt";
                default: throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }
        }

        // "pt" for PortugueseBrazil, "zh" for ChineseTaiwan — the part two regional variants share.
        public static string ToPrimaryCode(this LanguageId id)
        {
            var code = id.ToCode();
            var dash = code.IndexOf('-');
            return dash < 0 ? code : code.Substring(0, dash);
        }

        // Exact inverse of ToCode (case-insensitive). Unlike TryFromLocaleCode it never guesses a
        // variant, so "pt" is rejected rather than mapped to one of the Portuguese variants.
        public static bool TryFromCode(string code, out LanguageId id)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                foreach (LanguageId candidate in Enum.GetValues(typeof(LanguageId)))
                {
                    if (string.Equals(candidate.ToCode(), code.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        id = candidate;
                        return true;
                    }
                }
            }

            id = LanguageId.EnglishUS;
            return false;
        }

        public static bool TryFromLocaleCode(string localeCode, out LanguageId id)
        {
            if (string.IsNullOrWhiteSpace(localeCode))
            {
                id = LanguageId.EnglishUS;
                return false;
            }

            var s = localeCode.Trim().Replace('_', '-');
            var dash = s.IndexOf('-');
            string primary;
            string region;

            if (dash < 0)
            {
                primary = s;
                region = null;
            }
            else
            {
                primary = s.Substring(0, dash);
                region = dash + 1 < s.Length ? s.Substring(dash + 1) : null;
            }

            primary = primary.ToLowerInvariant();
            if (!string.IsNullOrEmpty(region)) region = region.ToUpperInvariant();

            if (primary == "en")
            {
                id = region == "GB" ? LanguageId.EnglishUK : LanguageId.EnglishUS;
                return true;
            }

            if (primary == "fr")
            {
                id = region == "CA" ? LanguageId.FrenchCanada : LanguageId.FrenchFrance;
                return true;
            }

            if (primary == "pt")
            {
                id = region == "BR" ? LanguageId.PortugueseBrazil : LanguageId.PortuguesePortugal;
                return true;
            }

            if (primary == "es")
            {
                id = region == "ES" ? LanguageId.SpanishSpain : LanguageId.SpanishLatinAmerica;
                return true;
            }

            if (primary == "zh")
            {
                if (region == "HK" || region == "MO")
                {
                    id = LanguageId.ChineseHongKong;
                    return true;
                }

                if (region == "TW")
                {
                    id = LanguageId.ChineseTaiwan;
                    return true;
                }

                id = LanguageId.ChinesePRC;
                return true;
            }

            switch (primary)
            {
                case "af": id = LanguageId.Afrikaans; return true;
                case "am": id = LanguageId.Amharic; return true;
                case "bg": id = LanguageId.Bulgarian; return true;
                case "be": id = LanguageId.Belarusian; return true;
                case "ca": id = LanguageId.Catalan; return true;
                case "hr": id = LanguageId.Croatian; return true;
                case "cs": id = LanguageId.Czech; return true;
                case "da": id = LanguageId.Danish; return true;
                case "de": id = LanguageId.German; return true;
                case "el": id = LanguageId.Greek; return true;
                case "et": id = LanguageId.Estonian; return true;
                case "fi": id = LanguageId.Finnish; return true;
                case "fil":
                case "tl": id = LanguageId.Filipino; return true;
                case "he":
                case "iw": id = LanguageId.Hebrew; return true;
                case "hi": id = LanguageId.Hindi; return true;
                case "hu": id = LanguageId.Hungarian; return true;
                case "is": id = LanguageId.Icelandic; return true;
                case "id":
                case "in": id = LanguageId.Indonesian; return true;
                case "it": id = LanguageId.Italian; return true;
                case "ja": id = LanguageId.Japanese; return true;
                case "ko": id = LanguageId.Korean; return true;
                case "lt": id = LanguageId.Lithuanian; return true;
                case "lv": id = LanguageId.Latvian; return true;
                case "ms": id = LanguageId.Malay; return true;
                case "nl": id = LanguageId.Dutch; return true;
                case "no":
                case "nb":
                case "nn": id = LanguageId.Norwegian; return true;
                case "pl": id = LanguageId.Polish; return true;
                case "ro": id = LanguageId.Romanian; return true;
                case "ru": id = LanguageId.Russian; return true;
                case "sk": id = LanguageId.Slovak; return true;
                case "sl": id = LanguageId.Slovenian; return true;
                case "sr": id = LanguageId.Serbian; return true;
                case "sv": id = LanguageId.Swedish; return true;
                case "sw": id = LanguageId.Swahili; return true;
                case "th": id = LanguageId.Thai; return true;
                case "tr": id = LanguageId.Turkish; return true;
                case "uk": id = LanguageId.Ukrainian; return true;
                case "zu": id = LanguageId.Zulu; return true;
                case "vi": id = LanguageId.Vietnamese; return true;
                default:
                    id = LanguageId.EnglishUS;
                    return false;
            }
        }
    }
}