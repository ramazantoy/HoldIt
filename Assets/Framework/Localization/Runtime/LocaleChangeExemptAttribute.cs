using System;

namespace AF.Localization
{
    /// <summary>
    /// Allows code to set Unity Localization's selected locale itself, bypassing <see cref="ILocaleService"/>. Validation
    /// reports such calls anywhere else. On a type it covers every member, nested types and lambdas included.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
    public sealed class LocaleChangeExemptAttribute : Attribute
    {
        public LocaleChangeExemptAttribute(string reason) => Reason = reason;

        public string Reason { get; }
    }
}
