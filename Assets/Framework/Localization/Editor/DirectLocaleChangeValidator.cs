using System;
using System.Collections.Generic;
using System.Reflection;
using AF.Core.Editor;
using UnityEngine.Localization.Settings;

namespace AF.Localization.Editor
{
    /// <summary>
    /// Finds project code that sets Unity Localization's selected locale: that belongs to <see cref="ILocaleService"/>, which
    /// saves the choice. Project assemblies only (Assets/), tests, editor code and this module excluded; code marked
    /// <see cref="LocaleChangeExemptAttribute"/> is skipped.
    /// </summary>
    public sealed class DirectLocaleChangeValidator : IValidator
    {
        private const string ADVICE = "ILocaleService is the only writer of the selected locale: a direct change is never saved and the " +
                                      "next launch shows the old language. Call ILocaleService.Set instead, or mark a deliberate " +
                                      "exception with [LocaleChangeExempt(\"reason\")].";

        public void ValidateProject(ValidationReport report) =>
            Check(DirectCallScan.ProjectTypes(typeof(LocaleService).Assembly), report);

        internal static void Check(IEnumerable<Type> types, ValidationReport report) =>
            DirectCallScan.Check(types, typeof(LocaleChangeExemptAttribute), Describe, ADVICE, report);

        /// <summary>The call as reported, or null when it is allowed.</summary>
        private static string Describe(MethodBase target)
        {
            if (target.DeclaringType != typeof(LocalizationSettings))
                return null;
            return target.Name switch
            {
                "set_SelectedLocale" => "LocalizationSettings.SelectedLocale",
                "SetSelectedLocale" => "LocalizationSettings.SetSelectedLocale()",
                _ => null,
            };
        }
    }
}
