using UnityEngine.Localization;

namespace AF.Localization
{
    /// <summary>Published after the language changed and its preloaded tables are ready; not when the same one is set again.</summary>
    public readonly struct LocaleChanged
    {
        public readonly Locale Previous;
        public readonly Locale Current;

        public LocaleChanged(Locale previous, Locale current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
