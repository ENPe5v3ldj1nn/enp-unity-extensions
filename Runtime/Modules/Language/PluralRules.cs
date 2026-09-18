using System;

namespace ENP.UnityExtensions.Runtime
{
    // CLDR plural categories. In JSON they are written in lower case: "zero", "one", "two",
    // "few", "many", "other".
    public enum PluralCategory
    {
        Zero,
        One,
        Two,
        Few,
        Many,
        Other
    }

    // CLDR cardinal rules for whole numbers (unicode.org/cldr/charts/latest/supplemental/language_plural_rules.html).
    // Only integer counts are supported — fractional forms are irrelevant to game counters.
    public static class PluralRules
    {
        public static PluralCategory Select(LanguageId language, long count)
        {
            var n = Math.Abs(count);
            var mod10 = n % 10;
            var mod100 = n % 100;

            switch (language)
            {
                case LanguageId.Japanese:
                case LanguageId.Korean:
                case LanguageId.ChinesePRC:
                case LanguageId.ChineseTaiwan:
                case LanguageId.ChineseHongKong:
                case LanguageId.Thai:
                case LanguageId.Vietnamese:
                case LanguageId.Indonesian:
                case LanguageId.Malay:
                    return PluralCategory.Other;

                case LanguageId.Russian:
                case LanguageId.Ukrainian:
                case LanguageId.Belarusian:
                    if (mod10 == 1 && mod100 != 11) return PluralCategory.One;
                    if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return PluralCategory.Few;
                    return PluralCategory.Many;

                case LanguageId.Polish:
                    if (n == 1) return PluralCategory.One;
                    if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return PluralCategory.Few;
                    return PluralCategory.Many;

                case LanguageId.Croatian:
                case LanguageId.Serbian:
                    if (mod10 == 1 && mod100 != 11) return PluralCategory.One;
                    if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return PluralCategory.Few;
                    return PluralCategory.Other;

                case LanguageId.Czech:
                case LanguageId.Slovak:
                    if (n == 1) return PluralCategory.One;
                    if (n >= 2 && n <= 4) return PluralCategory.Few;
                    return PluralCategory.Other;

                case LanguageId.Slovenian:
                    if (mod100 == 1) return PluralCategory.One;
                    if (mod100 == 2) return PluralCategory.Two;
                    if (mod100 == 3 || mod100 == 4) return PluralCategory.Few;
                    return PluralCategory.Other;

                case LanguageId.Lithuanian:
                    if (mod10 == 1 && (mod100 < 11 || mod100 > 19)) return PluralCategory.One;
                    if (mod10 >= 2 && (mod100 < 11 || mod100 > 19)) return PluralCategory.Few;
                    return PluralCategory.Other;

                case LanguageId.Latvian:
                    if (mod10 == 0 || (mod100 >= 11 && mod100 <= 19)) return PluralCategory.Zero;
                    if (mod10 == 1 && mod100 != 11) return PluralCategory.One;
                    return PluralCategory.Other;

                case LanguageId.Romanian:
                    if (n == 1) return PluralCategory.One;
                    if (n == 0 || (mod100 >= 2 && mod100 <= 19)) return PluralCategory.Few;
                    return PluralCategory.Other;

                case LanguageId.Hebrew:
                    if (n == 1) return PluralCategory.One;
                    if (n == 2) return PluralCategory.Two;
                    return PluralCategory.Other;

                case LanguageId.FrenchFrance:
                case LanguageId.FrenchCanada:
                    if (n == 0 || n == 1) return PluralCategory.One;
                    if (n != 0 && n % 1000000 == 0) return PluralCategory.Many;
                    return PluralCategory.Other;

                case LanguageId.PortugueseBrazil:
                    if (n == 0 || n == 1) return PluralCategory.One;
                    if (n != 0 && n % 1000000 == 0) return PluralCategory.Many;
                    return PluralCategory.Other;

                case LanguageId.SpanishSpain:
                case LanguageId.SpanishLatinAmerica:
                case LanguageId.Italian:
                case LanguageId.Catalan:
                case LanguageId.PortuguesePortugal:
                    if (n == 1) return PluralCategory.One;
                    if (n != 0 && n % 1000000 == 0) return PluralCategory.Many;
                    return PluralCategory.Other;

                case LanguageId.Hindi:
                case LanguageId.Amharic:
                case LanguageId.Zulu:
                    return n == 0 || n == 1 ? PluralCategory.One : PluralCategory.Other;

                case LanguageId.Filipino:
                    return mod10 != 4 && mod10 != 6 && mod10 != 9 ? PluralCategory.One : PluralCategory.Other;

                case LanguageId.Icelandic:
                    return mod10 == 1 && mod100 != 11 ? PluralCategory.One : PluralCategory.Other;

                default:
                    return n == 1 ? PluralCategory.One : PluralCategory.Other;
            }
        }

        public static string ToKey(this PluralCategory category)
        {
            switch (category)
            {
                case PluralCategory.Zero: return "zero";
                case PluralCategory.One: return "one";
                case PluralCategory.Two: return "two";
                case PluralCategory.Few: return "few";
                case PluralCategory.Many: return "many";
                case PluralCategory.Other: return "other";
                default: throw new ArgumentOutOfRangeException(nameof(category), category, null);
            }
        }
    }
}
