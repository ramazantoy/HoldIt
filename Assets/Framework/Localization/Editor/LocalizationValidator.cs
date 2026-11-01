using System;
using System.Collections.Generic;
using System.Linq;
using AF.Core.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace AF.Localization.Editor
{
    /// <summary>
    /// Reports a Localization setup the framework cannot work with, references to tables or entries that do not exist
    /// (errors), and strings missing a translation (warnings: a key is usually added before it is translated).
    /// </summary>
    public sealed class LocalizationValidator : IValidator
    {
        private const string ASSETS_FOLDER = "Assets";

        public void ValidateProject(ValidationReport report)
        {
            CheckSetup(LocalizationEditorSettings.ActiveLocalizationSettings, LocalizationEditorSettings.GetLocales(),
                AddressableAssetSettingsDefaultObject.Settings != null, report);

            var rootPrefab = ValidationTargets.FindRootPrefab();
            if (rootPrefab != null)
                CheckObjects(new[] { rootPrefab }, AssetDatabase.GetAssetPath(rootPrefab), FindTable, report);
        }

        public void ValidateScene(Scene scene, ValidationReport report) => CheckObjects(scene.GetRootGameObjects(), scene.path, FindTable, report);

        // Loads every table, prefab and ScriptableObject: too slow for every Play.
        public void ValidateAssets(ValidationReport report)
        {
            foreach (var collection in LocalizationEditorSettings.GetStringTableCollections())
                CheckTranslations(collection.TableCollectionName, collection.SharedData, collection.StringTables, report);

            var rootPrefab = ValidationTargets.FindRootPrefab();
            var rootPath = rootPrefab != null ? AssetDatabase.GetAssetPath(rootPrefab) : null;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { ASSETS_FOLDER }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path == rootPath)
                    continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                    CheckObjects(new[] { prefab }, path, FindTable, report);
            }
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { ASSETS_FOLDER }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path).OfType<ScriptableObject>())
                    CheckReferences(asset, path, FindTable, report);
            }
        }

        internal static void CheckSetup(LocalizationSettings settings, IReadOnlyList<Locale> locales, bool hasAddressables, ValidationReport report)
        {
            if (settings == null)
            {
                report.Error("No active Localization Settings: create or assign one in Project Settings > Localization.");
                return;
            }
            if (!hasAddressables)
                report.Error("No Addressables settings: Unity Localization loads its tables through Addressables. Create them in " +
                             "Window > Asset Management > Addressables > Groups.", settings);
            if (locales.Count == 0)
                report.Error("The project has no locales: add them in Project Settings > Localization.", settings);

            var selectors = settings.GetStartupLocaleSelectors();
            if (selectors.Any(selector => selector is PlayerPrefLocaleSelector))
            {
                report.Error("The Player Pref Locale Selector keeps a second copy of the language in PlayerPrefs, on this device only, " +
                             "and wins over the saved one. ILocaleService saves the language with the global data; remove the " +
                             "selector from Startup Locale Selectors.", settings);
            }
            var last = selectors.LastOrDefault() as SpecificLocaleSelector;
            if (last == null || locales.Count > 0 && locales.All(locale => locale.Identifier != last.LocaleId))
            {
                report.Error("The last Startup Locale Selector must be a Specific Locale Selector with one of the project's locales, so " +
                             "a first launch in a system language the game does not have still finds one.", settings);
            }
        }

        internal static void CheckTranslations(string table, SharedTableData shared, IEnumerable<StringTable> tables, ValidationReport report)
        {
            var localeTables = tables.Where(localeTable => localeTable != null).ToList();
            foreach (var entry in shared.Entries)
            {
                var missing = localeTables
                    .Where(localeTable => string.IsNullOrWhiteSpace(localeTable.GetEntry(entry.Id)?.Value))
                    .Select(localeTable => localeTable.LocaleIdentifier.Code)
                    .ToList();
                if (missing.Count > 0)
                {
                    report.Warning($"{table}/{entry.Key} has no translation for {string.Join(", ", missing)}: the game shows the " +
                                   "fallback locale's text there, or a \"No translation found\" message.", shared);
                }
            }
        }

        internal static void CheckObjects(IEnumerable<GameObject> roots, string location, Func<LocalizedReference, SharedTableData> findTable,
            ValidationReport report)
        {
            foreach (var root in roots)
            {
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component != null)
                        CheckReferences(component, ValidationTargets.Describe(location, component), findTable, report);
                }
            }
        }

        /// <summary>Finds every localized string or asset field of <paramref name="target"/> and checks that it resolves.</summary>
        internal static void CheckReferences(Object target, string location, Func<LocalizedReference, SharedTableData> findTable,
            ValidationReport report)
        {
            using var serialized = new SerializedObject(target);
            var property = serialized.GetIterator();
            while (property.Next(true))
            {
                // Every localized reference type of the package is named Localized...; reading boxedValue is the costly part.
                if (property.propertyType != SerializedPropertyType.Generic || !property.type.StartsWith("Localized", StringComparison.Ordinal))
                    continue;
                if (!(TryBox(property) is LocalizedReference reference))
                    continue;

                var tableSet = reference.TableReference.ReferenceType != TableReference.Type.Empty;
                var entrySet = reference.TableEntryReference.ReferenceType != TableEntryReference.Type.Empty;
                if (!tableSet && !entrySet)
                    continue;
                var where = $"{location}: {property.propertyPath}";
                if (!tableSet || !entrySet)
                {
                    report.Error($"{where} names {(tableSet ? "a table but no entry" : "an entry but no table")}.", target);
                    continue;
                }
                var shared = findTable(reference);
                if (shared == null)
                    report.Error($"{where} refers to the table {reference.TableReference}, which does not exist.", target);
                else if (shared.GetEntryFromReference(reference.TableEntryReference) == null)
                    report.Error($"{where} refers to {reference.TableEntryReference} in {shared.TableCollectionName}, which has no such entry.", target);
            }
        }

        private static object TryBox(SerializedProperty property)
        {
            try
            {
                return property.boxedValue;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static SharedTableData FindTable(LocalizedReference reference) =>
            reference is LocalizedString
                ? LocalizationEditorSettings.GetStringTableCollection(reference.TableReference)?.SharedData
                : LocalizationEditorSettings.GetAssetTableCollection(reference.TableReference)?.SharedData;
    }
}
