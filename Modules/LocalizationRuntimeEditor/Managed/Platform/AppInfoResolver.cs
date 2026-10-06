// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.Localization.Editor;

// Resolves app info values straight from the table collection assets rather than through the runtime
// ResourceDatabase, because a build cannot rely on the providers having the tables loaded.
static class AppInfoResolver
{
    public static string ResolveString(LocalizedString reference, LocalizationSettings settings, Locale locale)
    {
        if (reference == null || reference.IsEmpty)
            return null;
        var value = Resolve(reference, settings, locale,
            (IStringEntry entry) => string.IsNullOrWhiteSpace(entry.Value) ? null : entry.Value);
        // A pseudo-locale transforms the values resolved for it, and it is the locale that was asked for that
        // transforms them, not whichever one in its fallback chain happened to hold the text.
        if (value != null && locale is IPostProcessValueLocale postProcess)
            return postProcess.PostProcessValue(value);
        return value;
    }

    // Returns the icon's source asset path rather than the texture, because that is all a build needs and it keeps
    // the loaded asset from escaping: the entry owns it and wants it back.
    public static string ResolveTexturePath(LocalizedTexture reference, LocalizationSettings settings, Locale locale)
    {
        if (reference == null || reference.IsEmpty)
            return null;
        return Resolve(reference, settings, locale, (IAssetEntry entry) =>
        {
            if (entry is not ISynchronousAssetEntry sync || !sync.TryLoadAsset(out var asset) || asset == null)
                return null;
            try
            {
                if (asset is not Texture2D)
                    return null;
                var path = AssetDatabase.GetAssetPath(asset);
                return string.IsNullOrEmpty(path) ? null : path;
            }
            finally
            {
                entry.ReleaseAsset(asset);
            }
        });
    }

    public static bool IsSmart(LocalizedString reference) =>
        SharedEntry(reference, out var shared, out var id) && shared.IsSmart(id);

    // A platform resource is baked once and read without Unity running, so there is no variant to select on the
    // device. Reporting it lets the build say the default value was used rather than silently taking it; resolving
    // the selector here would bake whatever the Editor currently matches, which is the Editor's platform.
    public static bool IsVariantDriven(LocalizedReference reference) =>
        SharedEntry(reference, out var shared, out var id) && shared.GetVariantKey(id) != null;

    static bool SharedEntry(LocalizedReference reference, out SharedTableData shared, out long id)
    {
        shared = null;
        id = 0;
        if (reference == null || reference.IsEmpty)
            return false;
        shared = FindCollection(reference.TableReference)?.SharedData;
        if (shared == null)
            return false;
        id = ResolveKeyId(EntryReference(reference), shared);
        return id != 0;
    }

    static TableEntryReference EntryReference(LocalizedReference reference) => reference switch
    {
        LocalizedEntry<IStringEntry> stringEntry => stringEntry.TableEntryReference,
        LocalizedEntry<IAssetEntry> assetEntry => assetEntry.TableEntryReference,
        _ => default
    };

    // Keeps walking the fallback chain until a locale yields a usable value, so an entry that exists but is empty
    // does not stop the search the way it would if the entry object alone counted as resolved.
    static TResult Resolve<TEntry, TResult>(LocalizedEntry<TEntry> reference, LocalizationSettings settings, Locale locale,
        System.Func<TEntry, TResult> select)
        where TEntry : class, IResourceEntry
        where TResult : class
    {
        var collection = FindCollection(reference.TableReference);
        if (collection == null)
            return null;

        var visited = new HashSet<string>();
        for (var current = locale; current != null && visited.Add(current.Code); current = Fallback(settings, current))
        {
            var table = collection.GetTable(current.Identifier);
            var entry = FindEntry<TEntry>(table, reference.TableEntryReference, collection);
            if (entry != null)
            {
                var result = select(entry);
                if (result != null)
                    return result;
            }
            // The reference decides whether a locale without a value may borrow one from the locale behind it.
            if (!reference.EnableFallback)
                break;
        }
        return null;
    }

    static T FindEntry<T>(ResourceTable table, TableEntryReference entryRef, ResourceTableCollection collection)
        where T : class, IResourceEntry
    {
        if (table == null)
            return null;
        var id = ResolveKeyId(entryRef, collection.SharedData);
        return id != 0 ? table.GetEntry<T>(id) : null;
    }

    static long ResolveKeyId(TableEntryReference entryRef, SharedTableData shared)
    {
        if (entryRef.KeyId != 0)
            return entryRef.KeyId;
        if (shared == null || string.IsNullOrEmpty(entryRef.Key))
            return 0;
        return shared.GetId(entryRef.Key);
    }

    static Locale Fallback(LocalizationSettings settings, Locale locale)
    {
        if (settings == null || string.IsNullOrEmpty(locale.FallbackCode))
            return null;
        return settings.GetLocale(new LocaleIdentifier(locale.FallbackCode));
    }

    static ResourceTableCollection FindCollection(TableReference reference)
    {
        if (reference.IsEmpty)
            return null;
        foreach (var collection in AssetProviderEditors.GetKnownCollections())
        {
            var shared = collection?.SharedData;
            if (shared == null)
                continue;
            if (reference.ReferenceType == TableReference.Type.Guid)
            {
                if (shared.TableCollectionNameGuid == reference.TableCollectionNameGuid)
                    return collection;
            }
            else if (collection.TableCollectionName == reference.TableCollectionName)
            {
                return collection;
            }
        }
        return null;
    }
}
