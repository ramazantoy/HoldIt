using UnityEngine.Localization;

namespace HoldIt.Settings
{
    /// <summary>A language as its own speakers write it ("English", "Türkçe"), starting with a capital letter.</summary>
    internal static class LanguageNames
    {
        public static string Of(Locale locale)
        {
            var culture = locale.Identifier.CultureInfo;
            var name = culture != null ? culture.NativeName : locale.LocaleName;
            if (string.IsNullOrEmpty(name))
                return locale.Identifier.Code;
            var first = culture != null ? char.ToUpper(name[0], culture) : char.ToUpperInvariant(name[0]);
            return first + name.Substring(1);
        }
    }
}
