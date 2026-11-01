using System;

namespace AF.Settings
{
    /// <summary>
    /// Allows code to write the screen, quality, frame rate and cursor confinement settings itself, bypassing
    /// <see cref="IDisplaySettings"/>.
    /// Validation reports such writes anywhere else. On a type it covers every member, nested types and lambdas included.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
    public sealed class DisplaySettingsExemptAttribute : Attribute
    {
        public DisplaySettingsExemptAttribute(string reason) => Reason = reason;

        public string Reason { get; }
    }
}
