// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine.Pool;

namespace Unity.Localization.Editor;

class TableSearch
{
    readonly ResourceTableCollection m_Collection;
    readonly QueryEngine<long> m_Engine;

    public TableSearch(ResourceTableCollection collection)
    {
        m_Collection = collection;
        m_Engine = new QueryEngine<long> { validateFilters = false };
        m_Engine.SetSearchDataCallback(SearchWords);
        m_Engine.AddFilter<string>("k", KeyName);
        m_Engine.AddFilter<string>("key", KeyName);
        m_Engine.AddFilter<string>("v", JoinedValues);
        m_Engine.AddFilter<string>("value", JoinedValues);
        m_Engine.AddFilter<string>("type", EntryTypeLabel);
        m_Engine.AddFilter<bool>("smart", IsSmart);
        m_Engine.AddFilter<string>("loc", LocalesWithValue);
        m_Engine.AddFilter<string>("locale", LocalesWithValue);
    }

    public HashSet<long> Filter(string query)
    {
        if (m_Collection?.SharedData == null || string.IsNullOrWhiteSpace(query))
            return null;
        var parsed = m_Engine.ParseQuery(query);
        if (!parsed.valid)
            return null;
        var allKeys = ListPool<long>.Get();
        try
        {
            foreach (var sharedEntry in m_Collection.SharedData.Entries)
                allKeys.Add(sharedEntry.Id);
            return new HashSet<long>(parsed.Apply(allKeys));
        }
        finally
        {
            ListPool<long>.Release(allKeys);
        }
    }

    IEnumerable<string> SearchWords(long keyId)
    {
        yield return KeyName(keyId);
        if (m_Collection == null)
            yield break;
        var tables = m_Collection.Tables;
        for (var i = 0; i < tables.Count; i++)
        {
            if (tables[i]?.GetEntry(keyId) is not IStringEntry stringEntry)
                continue;
            foreach (var value in Values(stringEntry))
                yield return value;
        }
    }

    string JoinedValues(long keyId)
    {
        var words = ListPool<string>.Get();
        try
        {
            if (m_Collection != null)
            {
                var tables = m_Collection.Tables;
                for (var i = 0; i < tables.Count; i++)
                {
                    if (tables[i]?.GetEntry(keyId) is IStringEntry stringEntry)
                        foreach (var value in Values(stringEntry))
                            words.Add(value);
                }
            }
            return string.Join(" ", words);
        }
        finally
        {
            ListPool<string>.Release(words);
        }
    }

    string LocalesWithValue(long keyId)
    {
        var codes = ListPool<string>.Get();
        try
        {
            if (m_Collection != null)
            {
                var tables = m_Collection.Tables;
                for (var i = 0; i < tables.Count; i++)
                {
                    if (tables[i]?.GetEntry(keyId) is IStringEntry stringEntry && HasValue(stringEntry))
                        codes.Add(tables[i].LocaleIdentifier.Code);
                }
            }
            return string.Join(" ", codes);
        }
        finally
        {
            ListPool<string>.Release(codes);
        }
    }

    static IEnumerable<string> Values(IStringEntry entry)
    {
        if (!string.IsNullOrEmpty(entry.Value))
            yield return entry.Value;
        if (entry is VariantStringEntry variant)
        {
            foreach (var v in variant.Variants)
                if (!string.IsNullOrEmpty(v.Value))
                    yield return v.Value;
        }
    }

    static bool HasValue(IStringEntry entry)
    {
        if (!string.IsNullOrEmpty(entry.Value))
            return true;
        if (entry is VariantStringEntry variant)
        {
            foreach (var v in variant.Variants)
                if (!string.IsNullOrEmpty(v.Value))
                    return true;
        }
        return false;
    }

    string EntryTypeLabel(long keyId)
    {
        var variant = IsVariant(keyId);
        return FirstEntry(keyId) switch
        {
            IStringEntry => variant ? "variant-string" : "string",
            IAssetEntry => variant ? "variant-asset" : "asset",
            _ => "string"
        };
    }

    string KeyName(long keyId) => m_Collection?.SharedData?.GetKey(keyId) ?? keyId.ToString();
    bool IsSmart(long keyId) => m_Collection?.SharedData != null && m_Collection.SharedData.IsSmart(keyId);
    bool IsVariant(long keyId) => m_Collection?.SharedData != null && m_Collection.SharedData.IsVariant(keyId);

    IResourceEntry FirstEntry(long keyId)
    {
        if (m_Collection == null)
            return null;
        var tables = m_Collection.Tables;
        for (var i = 0; i < tables.Count; i++)
        {
            var entry = tables[i]?.GetEntry(keyId);
            if (entry != null)
                return entry;
        }
        return null;
    }
}
