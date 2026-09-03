// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using Unity.Localization.Providers;
using UnityEditor;
using UnityEngine;

namespace Unity.Localization.Editor;

// The public entry points are on LocalizationEditorSettings; these do the work.
static class LocalizationTableAuthoring
{
    internal static bool CanCreateCollection(out string reason)
    {
        var settings = LocalizationEditorSettings.ActiveSettings;
        if (settings == null)
        {
            reason = L10n.Tr("This project has no Localization settings. Create them in Project Settings > Localization first.", null);
            return false;
        }
        if (!HasResolvedLocale(settings))
        {
            reason = L10n.Tr("Add at least one locale in the Localization settings before creating a table collection.", null);
            return false;
        }
        reason = null;
        return true;
    }

    // An entry is null when the script that defined its locale subclass is gone, and a collection built from those
    // alone would have no tables at all.
    static bool HasResolvedLocale(LocalizationSettings settings)
    {
        var locales = settings.AvailableLocales;
        for (var i = 0; i < locales.Count; i++)
        {
            if (locales[i] != null)
                return true;
        }
        return false;
    }

    // Builds the whole collection: a folder holding the collection asset, its shared data, and one table per locale.
    internal static ResourceTableCollection CreateCollection(string parentFolder, string collectionName)
    {
        if (!CanCreateCollection(out _) || string.IsNullOrEmpty(parentFolder) || string.IsNullOrEmpty(collectionName))
            return null;
        var settings = LocalizationEditorSettings.ActiveSettings;

        // CreateFolder appends a number when the name is taken, so read the name back rather than trust the request.
        var folder = parentFolder;
        var folderGuid = AssetDatabase.CreateFolder(parentFolder, collectionName);
        if (!string.IsNullOrEmpty(folderGuid))
        {
            folder = AssetDatabase.GUIDToAssetPath(folderGuid);
            collectionName = Path.GetFileName(folder);
        }
        var collectionPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{collectionName}.asset");
        collectionName = Path.GetFileNameWithoutExtension(collectionPath);

        var shared = ScriptableObject.CreateInstance<SharedTableData>();
        shared.name = collectionName + " Shared Data";
        shared.TableCollectionName = collectionName;

        var collection = ScriptableObject.CreateInstance<ResourceTableCollection>();
        collection.name = collectionName;
        collection.SharedData = shared;

        AssetDatabase.CreateAsset(collection, collectionPath);
        AssetDatabase.CreateAsset(shared, AssetDatabase.GenerateUniqueAssetPath($"{folder}/{collectionName} Shared Data.asset"));

        // Pick the source by save path: a Resources folder forces the Resources source, otherwise the configured default.
        var target = AssetProviderEditors.ResolveForNewCollection(collectionPath);
        if (target == null)
        {
            target = new ReferencedAssetProvider();
            settings.Database.AssetProvider.AddProvider(target);
        }
        collection.ProviderId = target.Id;

        foreach (var locale in settings.AvailableLocales)
        {
            if (locale != null)
                AddLocaleTable(collection, locale);
        }

        AssetProviderEditors.RegisterCollection(collection);

        EditorUtility.SetDirty(collection);
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssetIfDirty(collection);
        AssetDatabase.SaveAssetIfDirty(settings);

        LocalizationEditorSettings.RaiseCollectionsChanged();
        return collection;
    }

    // A raw create with no existence check, so a caller building every locale at once uses it directly.
    internal static ResourceTable AddLocaleTable(ResourceTableCollection collection, Locale locale)
    {
        var table = ScriptableObject.CreateInstance<ResourceTable>();
        table.name = $"{collection.TableCollectionName}_{locale.Code}";
        table.SharedData = collection.SharedData;
        table.LocaleIdentifier = locale.Identifier;
        var dir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(collection))?.Replace('\\', '/');
        var path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{collection.TableCollectionName}_{locale.Code}.asset");
        AssetDatabase.CreateAsset(table, path);
        collection.AddTable(table);
        return table;
    }

    // The idempotent form, used to add one table after creation or to satisfy an import.
    internal static ResourceTable EnsureLocaleTable(ResourceTableCollection collection, Locale locale)
    {
        if (collection == null || locale == null || collection.SharedData == null)
            return null;
        var existing = collection.GetTable(locale.Identifier);
        if (existing != null)
            return existing;
        var table = AddLocaleTable(collection, locale);
        // Join any surrounding undo group (an import registers one) so rolling back does not orphan the new asset.
        Undo.RegisterCreatedObjectUndo(table, "Add Localization Table");
        AssetProviderEditors.RegisterCollection(collection);
        EditorUtility.SetDirty(collection);
        AssetDatabase.SaveAssetIfDirty(collection);
        AssetDatabase.SaveAssetIfDirty(table);
        return table;
    }

    internal static long CreateStringEntry(ResourceTableCollection collection, string key)
        => CreateEntry(collection, key, typeof(StringEntry));

    internal static long CreateEntry(ResourceTableCollection collection, string key, Type entryType)
    {
        var shared = collection != null ? collection.SharedData : null;
        if (shared == null || string.IsNullOrEmpty(key) || entryType == null)
            return 0;

        var sharedEntry = shared.AddKey(key);
        var id = sharedEntry?.Id ?? 0;
        foreach (var table in collection.Tables)
        {
            if (table == null || table.GetEntry(id) != null)
                continue;
            if (entryType == typeof(StringEntry))
                table.AddStringEntry(key, string.Empty);
            else
                table.AddEntry((IResourceEntry)Activator.CreateInstance(entryType, id));
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssetIfDirty(table);
        }
        EditorUtility.SetDirty(shared);
        AssetDatabase.SaveAssetIfDirty(shared);
        LocalizationEditorSettings.RaiseCollectionsChanged();
        return id;
    }
}
