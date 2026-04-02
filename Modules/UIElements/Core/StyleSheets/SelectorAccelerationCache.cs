// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using TableType = System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<UnityEngine.UIElements.StyleComplexSelector>>;

namespace UnityEngine.UIElements;

enum SelectorAccelerationTableType
{
    None = -1,
    Name = 0,
    Type = 1,
    Class = 2,
    Length = 3 // Used to initialize the array
}

struct SelectorAccelerationCacheEntry
{
    public TableType[] tables;

    internal int nonEmptyTablesMask;

    internal List<StyleComplexSelector> rootSelectors;

    internal List<StyleComplexSelector> wildCardSelectors;

    public static SelectorAccelerationCacheEntry Create(StyleSheet styleSheet)
    {
        var entry = new SelectorAccelerationCacheEntry();
        entry.tables = new[]
        {
            // Name
            new TableType(StringComparer.Ordinal),
            // Type
            new TableType(StringComparer.Ordinal),
            // Class
            new TableType(StringComparer.Ordinal)
        };

        styleSheet.RebuildIfNecessary();

        for (var index = 0; index < styleSheet.rules.Length; index++)
        {
            var rule = styleSheet.rules[index];
            if (rule.complexSelectors == null)
                continue;

            foreach (var complexSelector in rule.complexSelectors)
            {
                var lastSelector = complexSelector.selectors[^1];
                var part = lastSelector.parts[0];

                var key = part.value;

                var tableToUse = SelectorAccelerationTableType.None;

                switch (part.type)
                {
                    case StyleSelectorType.Class:
                        tableToUse = SelectorAccelerationTableType.Class;
                        break;
                    case StyleSelectorType.ID:
                        tableToUse = SelectorAccelerationTableType.Name;
                        break;
                    case StyleSelectorType.Type:
                        tableToUse = SelectorAccelerationTableType.Type;
                        break;

                    case StyleSelectorType.Wildcard:
                        if (entry.wildCardSelectors == null)
                            entry.wildCardSelectors = new List<StyleComplexSelector>();
                        entry.wildCardSelectors.Add(complexSelector);
                        break;

                    case StyleSelectorType.PseudoClass:
                        // :root selector are put separately because they apply to very few elements
                        if ((lastSelector.pseudoStateMask & (int)PseudoStates.Root) != 0)
                        {
                            if (entry.rootSelectors == null)
                                entry.rootSelectors = new List<StyleComplexSelector>();
                            entry.rootSelectors.Add(complexSelector);
                        }
                        // in this case we assume a wildcard selector
                        // since a selector such as ":selected" applies to all elements
                        else
                        {
                            if (entry.wildCardSelectors == null)
                                entry.wildCardSelectors = new List<StyleComplexSelector>();
                            entry.wildCardSelectors.Add(complexSelector);
                        }

                        break;
                    default:
                        Debug.LogError($"Invalid first part type {part.type}", styleSheet);
                        break;
                }

                if (tableToUse != SelectorAccelerationTableType.None)
                {
                    var table = entry.tables[(int)tableToUse];
                    if (!table.TryGetValue(key, out var list))
                    {
                        list = new List<StyleComplexSelector>();
                        table.Add(key, list);
                    }
                    entry.nonEmptyTablesMask |= (1 << (int)tableToUse);
                    list.Add(complexSelector);
                }
            }
        }

        return entry;
    }

    // Only used in tests for now
    public static bool AreSame(SelectorAccelerationCacheEntry firstEntry, SelectorAccelerationCacheEntry secondEntry)
    {
        // Take a shortcut by comparing all internal references
        return firstEntry.tables == secondEntry.tables
               && firstEntry.rootSelectors == secondEntry.rootSelectors
               && firstEntry.wildCardSelectors == secondEntry.wildCardSelectors;
    }

}

class SelectorAccelerationCache
{
    public static SelectorAccelerationCache shared = new SelectorAccelerationCache();

    private static ProfilerMarker s_Marker = new ProfilerMarker("UIElements.AccelerateSelectors");

    readonly Dictionary<EntityId, SelectorAccelerationCacheEntry> m_Cache = new();
    readonly List<(string path, EntityId entityId)> m_PathToEntityId = new(128);
    readonly Dictionary<EntityId, int> m_CacheForHash = new();
    readonly PathEntityIdComparer m_Comparer = new();

    class PathEntityIdComparer : IComparer<(string path, EntityId entityId)>
    {
        public int Compare((string path, EntityId entityId) x, (string path, EntityId entityId) y)
        {
            int diff = string.CompareOrdinal(x.path, y.path);
            if (diff != 0)
                return diff;
            return x.entityId.CompareTo(y.entityId);
        }
    }

    public int Count => m_Cache.Count;

    public void Remove(string styleSheetPath)
    {
        // We try to find an appropriate index to expand our search from
        int index = m_PathToEntityId.BinarySearch((styleSheetPath, EntityId.None), m_Comparer);

        Debug.Assert(index < 0, "EntityId.None should not exist");

        index = ~index;

        // Find the first occurrence of this path
        int firstIndex = index;
        while (firstIndex > 0 && m_PathToEntityId[firstIndex - 1].path == styleSheetPath)
            firstIndex--;

        // Remove all entries with this path
        int removeCount = 0;
        for (int i = firstIndex; i < m_PathToEntityId.Count && m_PathToEntityId[i].path == styleSheetPath; i++)
        {
            m_Cache.Remove(m_PathToEntityId[i].entityId);
            m_CacheForHash.Remove(m_PathToEntityId[i].entityId);
            removeCount++;
        }

        m_PathToEntityId.RemoveRange(firstIndex, removeCount);
    }

    public void Remove(StyleSheet styleSheet)
    {
        bool removedFromMainCache = m_Cache.Remove(styleSheet.GetEntityId());

        if (!removedFromMainCache)
            return;

        bool removedFromHashCache = m_CacheForHash.Remove(styleSheet.GetEntityId());
        Debug.Assert(removedFromHashCache, "removedFromMainCache == removedFromHashCache");

        string path = Panel.GetStyleSheetPath(styleSheet);
        if (!string.IsNullOrEmpty(path) && path != "Library/unity editor resources")
        {
            int index = m_PathToEntityId.BinarySearch((path, styleSheet.GetEntityId()), m_Comparer);
            if (index >= 0)
                m_PathToEntityId.RemoveRange(index, 1);
        }
    }

    public SelectorAccelerationCacheEntry GetOrCreate(StyleSheet styleSheet)
    {
        if (!m_Cache.TryGetValue(styleSheet.GetEntityId(), out var entry))
        {
            s_Marker.Begin();
            entry = SelectorAccelerationCacheEntry.Create(styleSheet);
            m_Cache.Add(styleSheet.GetEntityId(), entry);
            s_Marker.End();
            string path = Panel.GetStyleSheetPath(styleSheet);
            if (!string.IsNullOrEmpty(path) && path != "Library/unity editor resources")
            {
                int index = m_PathToEntityId.BinarySearch((path, styleSheet.GetEntityId()), m_Comparer);
                Debug.Assert(index < 0, "Entry should not exist");
                index = ~index;
                m_PathToEntityId.Insert(index, (path, styleSheet.GetEntityId()));
            }
            if (!m_CacheForHash.TryAdd(styleSheet.GetEntityId(), styleSheet.contentHash))
            {
                Debug.LogError($"SelectorAccelerationCache: {styleSheet.name} (entityId={styleSheet.GetEntityId()}) already has a hash, previous is {m_CacheForHash[styleSheet.GetEntityId()]}, new is {styleSheet.contentHash}");
                m_CacheForHash[styleSheet.GetEntityId()] = styleSheet.contentHash;
            }
        }
        if (m_CacheForHash[styleSheet.GetEntityId()] != styleSheet.contentHash)
        {
            Debug.LogError($"SelectorAccelerationCache: {styleSheet.name} (entityId={styleSheet.GetEntityId()}) has changed but change was not notified to SelectorAccelerationCache, please report a bug. Previous hash {m_CacheForHash[styleSheet.GetEntityId()]}, new hash={styleSheet.contentHash}");
            m_CacheForHash[styleSheet.GetEntityId()] = styleSheet.contentHash;
        }
        return entry;
    }
}
