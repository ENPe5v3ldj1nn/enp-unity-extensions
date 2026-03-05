using System;

namespace enp_unity_extensions.Scripts.Language
{
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
        Vietnamese
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
    }
}