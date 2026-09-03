// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Search;

namespace Unity.Localization.Editor.Search;

class LocalizedReferenceSearchItem
{
    internal LocalizedReferenceSearchItem(ResourceTableCollection collection, SharedTableData.SharedTableEntry entry)
    {
        Collection = collection;
        Entry = entry;
    }

    internal ResourceTableCollection Collection { get; }
    internal SharedTableData.SharedTableEntry Entry { get; }
}

class LocalizedReferenceSearchProvider : SearchProvider
{
    internal const string ProviderId = "loc-reference";

    readonly Type m_EntryInterface;
    readonly Type m_AssetType;

    internal LocalizedReferenceSearchProvider(Type entryInterface, Type assetType = null)
        : base(ProviderId, L10n.Tr("Localized References", null))
    {
        m_EntryInterface = entryInterface ?? typeof(IResourceEntry);
        m_AssetType = assetType;
        fetchItems = FetchItems;
        fetchPropositions = FetchPropositions;
        showDetails = false;
        // Newly added or moved Resources assets must be visible the next time the picker opens.
        AssetEntryTypeResolver.InvalidateResourcesIndex();
    }

    IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
    {
        var entry = L10n.Tr("Entry", null);
        yield return new SearchProposition(category: entry, label: L10n.Tr("Key", null), replacement: "k:",
            help: L10n.Tr("Entries whose key contains the text.", null));
        yield return new SearchProposition(category: entry, label: L10n.Tr("Value", null), replacement: "v:",
            help: L10n.Tr("Entries with a translation containing the text.", null));

        var kind = L10n.Tr("Kind", null);
        if (AllowsStrings)
        {
            yield return new SearchProposition(category: kind, label: L10n.Tr("String", null), replacement: "type:string",
                help: L10n.Tr("Plain string entries.", null));
            yield return new SearchProposition(category: kind, label: L10n.Tr("String Variant", null), replacement: "type:variant-string",
                help: L10n.Tr("String entries that vary by plural or gender.", null));
            yield return new SearchProposition(category: kind, label: L10n.Tr("Smart String", null), replacement: "smart:true",
                help: L10n.Tr("Entries formatted with Smart Strings.", null));
        }

        if (AllowsAssets)
        {
            yield return new SearchProposition(category: kind, label: L10n.Tr("Asset", null), replacement: "type:asset",
                help: L10n.Tr("Plain asset entries.", null));
            yield return new SearchProposition(category: kind, label: L10n.Tr("Asset Variant", null), replacement: "type:variant-asset",
                help: L10n.Tr("Asset entries that vary by plural or gender.", null));
        }

        // loc counts locales that hold a string value, so it says nothing about an asset field.
        if (!AllowsStrings)
            yield break;

        var translated = L10n.Tr("Translated In", null);
        var settings = LocalizationEditorSettings.ActiveSettings;
        if (settings == null)
            yield break;
        var locales = settings.AvailableLocales;
        for (var i = 0; i < locales.Count; i++)
        {
            var locale = locales[i];
            if (locale == null || string.IsNullOrEmpty(locale.Code))
                continue;
            var name = string.IsNullOrEmpty(locale.LocaleName) ? locale.Code : locale.LocaleName;
            yield return new SearchProposition(category: translated, label: name, replacement: $"loc:{locale.Code}",
                help: string.Format(L10n.Tr("Entries with a translation for {0}.", null), locale.Code));
        }
    }

    // IResourceEntry allows both; the sibling interfaces allow only their own side.
    bool AllowsStrings => !typeof(IAssetEntry).IsAssignableFrom(m_EntryInterface);
    bool AllowsAssets => !typeof(IStringEntry).IsAssignableFrom(m_EntryInterface);

    object FetchItems(SearchContext context, List<SearchItem> items, SearchProvider provider)
    {
        foreach (var collection in AssetProviderEditors.GetKnownCollections())
        {
            if (collection?.SharedData == null)
                continue;

            // Null means "no usable query", which is show-all rather than show-none.
            var matched = new TableSearch(collection).Filter(context.searchQuery);

            foreach (var entry in collection.SharedData.Entries)
            {
                if (entry == null || (matched != null && !matched.Contains(entry.Id)))
                    continue;
                if (!KindMatches(collection, entry.Id))
                    continue;
                items.Add(provider.CreateItem(
                    context,
                    $"{collection.TableCollectionName}/{entry.Id}",
                    0,
                    entry.Key,
                    collection.TableCollectionName,
                    null,
                    new LocalizedReferenceSearchItem(collection, entry)));
            }
        }
        return null;
    }

    bool KindMatches(ResourceTableCollection collection, long keyId)
    {
        var tables = collection.Tables;
        var sawKindEntry = false;
        for (var i = 0; i < tables.Count; i++)
        {
            var entry = tables[i]?.GetEntry(keyId);
            if (entry == null || !m_EntryInterface.IsInstanceOfType(entry))
                continue;
            sawKindEntry = true;
            if (m_AssetType == null || entry is not IAssetEntry assetEntry)
                return true;
            var assetType = AssetEntryTypeResolver.Resolve(assetEntry);
            if (assetType == null || m_AssetType.IsAssignableFrom(assetType))
                return true;
        }
        return !sawKindEntry;
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
