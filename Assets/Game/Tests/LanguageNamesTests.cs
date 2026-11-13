using HoldIt.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization;

namespace HoldIt.Tests
{
    public class LanguageNamesTests
    {
        private Locale _locale;

        [TearDown]
        public void DestroyLocale()
        {
            if (_locale != null)
                Object.DestroyImmediate(_locale);
        }

        [TestCase("en", "English")]
        [TestCase("tr", "Türkçe")]
        public void Of_AKnownCulture_IsItsNativeNameWithACapital(string code, string expected)
        {
            _locale = Locale.CreateLocale(code);

            Assert.AreEqual(expected, LanguageNames.Of(_locale));
        }

        [Test]
        public void Of_AnUnknownCulture_StillGivesANameWithACapital()
        {
            _locale = Locale.CreateLocale(new LocaleIdentifier("zz-ZZ-xx"));
            _locale.name = "zzish";

            var name = LanguageNames.Of(_locale);

            Assert.IsNotEmpty(name);
            Assert.IsTrue(char.IsUpper(name[0]), name);
        }
    }
}
