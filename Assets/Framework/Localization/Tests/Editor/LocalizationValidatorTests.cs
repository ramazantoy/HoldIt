using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AF.Core.Editor;
using AF.Localization.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using Object = UnityEngine.Object;

namespace AF.Localization.Tests
{
    public class LocalizationValidatorTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
                Object.DestroyImmediate(created);
            _created.Clear();
        }

        [Test]
        public void Setup_WithTheFrameworksSelectors_HasNoProblems()
        {
            var report = new ValidationReport();
            LocalizationValidator.CheckSetup(Settings(new CommandLineLocaleSelector(), new SystemLocaleSelector(), Specific("en")),
                Locales("en", "tr"), hasAddressables: true, report);

            Assert.AreEqual(0, report.Issues.Count, report.ToString());
        }

        [Test]
        public void Setup_PlayerPrefSelector_IsReported()
        {
            var report = new ValidationReport();
            LocalizationValidator.CheckSetup(Settings(new PlayerPrefLocaleSelector(), new SystemLocaleSelector(), Specific("en")),
                Locales("en"), hasAddressables: true, report);

            Assert.AreEqual(1, report.ErrorCount, report.ToString());
            StringAssert.Contains("Player Pref Locale Selector", report.ToString());
        }

        [Test]
        public void Setup_WithoutAFinalSpecificSelector_IsReported()
        {
            var noFallback = new ValidationReport();
            LocalizationValidator.CheckSetup(Settings(new SystemLocaleSelector()), Locales("en"), true, noFallback);
            var unknownFallback = new ValidationReport();
            LocalizationValidator.CheckSetup(Settings(new SystemLocaleSelector(), Specific("de")), Locales("en"), true, unknownFallback);

            Assert.AreEqual(1, noFallback.ErrorCount, noFallback.ToString());
            Assert.AreEqual(1, unknownFallback.ErrorCount, unknownFallback.ToString());
        }

        [Test]
        public void Setup_MissingPieces_AreReported()
        {
            var noSettings = new ValidationReport();
            LocalizationValidator.CheckSetup(null, Locales("en"), true, noSettings);
            var empty = new ValidationReport();
            LocalizationValidator.CheckSetup(Settings(Specific("en")), Locales(), hasAddressables: false, empty);

            Assert.AreEqual(1, noSettings.ErrorCount);
            Assert.AreEqual(2, empty.ErrorCount, empty.ToString());
        }

        [Test]
        public void MissingOrEmptyTranslations_AreWarnings()
        {
            var shared = Track(ScriptableObject.CreateInstance<SharedTableData>());
            shared.TableCollectionName = "UI";
            var en = Table(shared, "en", ("play", "Play"), ("quit", "Quit"), ("back", "Back"));
            var tr = Table(shared, "tr", ("play", "Oyna"), ("quit", "  "));

            var report = new ValidationReport();
            LocalizationValidator.CheckTranslations("UI", shared, new[] { en, tr }, report);

            Assert.AreEqual(0, report.ErrorCount);
            Assert.AreEqual(2, report.WarningCount, report.ToString());
            StringAssert.Contains("UI/quit has no translation for tr", report.ToString());
            StringAssert.Contains("UI/back has no translation for tr", report.ToString());
        }

        [Test]
        public void References_ToMissingTablesOrEntries_AreErrors()
        {
            var shared = Track(ScriptableObject.CreateInstance<SharedTableData>());
            shared.TableCollectionName = "UI";
            shared.AddKey("play");
            var fixture = Track(ScriptableObject.CreateInstance<LocalizedReferenceFixture>());

            var report = new ValidationReport();
            LocalizationValidator.CheckReferences(fixture, "fixture", reference => reference.TableReference.TableCollectionName == "UI" ? shared : null,
                report);

            var messages = string.Join("\n", report.Issues.Select(issue => issue.Message));
            Assert.AreEqual(3, report.ErrorCount, messages);
            StringAssert.Contains("fixture: missingEntry refers to", messages);
            StringAssert.Contains("fixture: missingTable refers to the table", messages);
            StringAssert.Contains("fixture: list.Array.data[0] names a table but no entry", messages);
        }

        [Test]
        public void Components_AndAssetReferences_AreChecked()
        {
            var root = Track(new GameObject("Text"));
            root.SetActive(false); // the components are [ExecuteAlways]: inactive, they never load a table
            root.AddComponent<LocalizeStringEvent>().StringReference = new LocalizedString { TableEntryReference = "play" };
            root.AddComponent<LocalizeTextureEvent>().AssetReference.SetReference("Logos", "logo");

            var report = new ValidationReport();
            LocalizationValidator.CheckObjects(new[] { root }, "Scene", _ => null, report);

            var messages = string.Join("\n", report.Issues.Select(issue => issue.Message));
            Assert.AreEqual(2, report.ErrorCount, messages);
            StringAssert.Contains("names an entry but no table", messages);
            StringAssert.Contains("refers to the table TableReference(Logos), which does not exist", messages);
        }

        [Test]
        public void DirectLocaleChanges_AreFoundInCompiledCode()
        {
            var report = new ValidationReport();
            DirectLocaleChangeValidator.Check(WithNestedTypes(typeof(DirectChanges), typeof(ExemptChanges), typeof(Reads)), report);

            var messages = string.Join("\n", report.Issues.Select(issue => issue.Message));
            Assert.AreEqual(2, report.ErrorCount, messages);
            StringAssert.Contains("DirectChanges.Select calls LocalizationSettings.SelectedLocale", messages);
            StringAssert.Contains("DirectChanges.SelectLater calls LocalizationSettings.SetSelectedLocale()", messages);
        }

        private LocalizationSettings Settings(params IStartupLocaleSelector[] selectors)
        {
            // Creating settings sets the static instance when none is loaded yet; load the project's first.
            _ = LocalizationSettings.HasSettings;
            var settings = Track(ScriptableObject.CreateInstance<LocalizationSettings>());
            settings.GetStartupLocaleSelectors().Clear();
            settings.GetStartupLocaleSelectors().AddRange(selectors);
            return settings;
        }

        private static SpecificLocaleSelector Specific(string code) => new SpecificLocaleSelector { LocaleId = new LocaleIdentifier(code) };

        private IReadOnlyList<Locale> Locales(params string[] codes) => codes.Select(code => Track(Locale.CreateLocale(code))).ToList();

        private StringTable Table(SharedTableData shared, string code, params (string key, string value)[] entries)
        {
            var table = Track(ScriptableObject.CreateInstance<StringTable>());
            table.SharedData = shared;
            table.LocaleIdentifier = new LocaleIdentifier(code);
            foreach (var (key, value) in entries)
                table.AddEntry(key, value);
            return table;
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }

        private static IEnumerable<Type> WithNestedTypes(params Type[] types) =>
            types.SelectMany(type => new[] { type }.Concat(WithNestedTypes(type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))));

        private static class DirectChanges
        {
            public static void Select(Locale locale) => LocalizationSettings.SelectedLocale = locale;

            public static Action SelectLater(Locale locale) => () => LocalizationSettings.Instance.SetSelectedLocale(locale);
        }

        [LocaleChangeExempt("test: a language debug menu")]
        private static class ExemptChanges
        {
            public static void Select(Locale locale) => LocalizationSettings.SelectedLocale = locale;
        }

        private static class Reads
        {
            public static Locale Selected() => LocalizationSettings.SelectedLocale;
        }
    }
}
