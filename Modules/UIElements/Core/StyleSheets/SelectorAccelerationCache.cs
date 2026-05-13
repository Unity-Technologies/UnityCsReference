// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using TableType = System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<UnityEngine.UIElements.StyleSelectorLookupEntry>>;

namespace UnityEngine.UIElements;

enum SelectorAccelerationTableType
{
    None = -1,
    Name = 0,
    Type = 1,
    Class = 2,
    Length = 3 // Used to initialize the array
}

// Flattened selector part - leaf level
struct FlattenedSelectorPart
{
    public int uniqueStringId;      // UniqueStyleString.id, -1 for wildcards/pseudo
    public StyleSelectorType type;   // Class, ID, Type, Wildcard, PseudoClass
}

// Flattened selector - middle level
struct FlattenedSelector
{
    public int pseudoStateMask;
    public int negatedPseudoStateMask;
    public StyleSelectorRelationship previousRelationship;  // Child or Descendent

    // Range into parts array
    public ushort startPartIndex;
    public ushort partCount;
}

// Selector range descriptor - top level
struct SelectorRangeDescriptor
{
    // Sorting key (used to sort allDescriptors)
    public SelectorAccelerationTableType tableType;  // Name, Type, Class, or None (for wildcards/root)
    public int tableKey;  // UniqueStyleString.id for the last part

    // Range in flat selectors array
    public ushort startSelectorIndex;
    public ushort selectorCount;

    // Reverse lookup
    public int ruleIndex;
    public int selectorIndexInRule;

    // Cached metadata
    public int orderInStyleSheet;
    public int importedStyleSheetIndex;
    public int specificity;
    public bool isSimple;

    // Bloom filter hashes
    public unsafe fixed int ancestorHashes[Hashes.kSize];
}

struct SelectorAccelerationCacheEntry
{
    // Flattened data structures
    public FlattenedSelectorPart[] allParts;        // Leaf level: individual parts
    public FlattenedSelector[] allSelectors;        // Middle level: groups of parts
    public SelectorRangeDescriptor[] allDescriptors; // Top level: SORTED by (tableType, tableKey, orderInStyleSheet)

    // Range-based acceleration tables (store ranges instead of lists)
    public Dictionary<int, (int startIndex, int count)> nameTable;   // UniqueStyleString.id → range in allDescriptors
    public Dictionary<int, (int startIndex, int count)> typeTable;
    public Dictionary<int, (int startIndex, int count)> classTable;

    public Dictionary<int, (int startIndex, int count)> GetTable(int index)
    {
        switch((SelectorAccelerationTableType)index)
        {
            case SelectorAccelerationTableType.Name:
                return nameTable;
            case SelectorAccelerationTableType.Type:
                return typeTable;
            case SelectorAccelerationTableType.Class:
                return classTable;
        }
        return null;
    }

    // Special selectors (also as ranges in sorted allDescriptors)
    public (int startIndex, int count) rootSelectorRange;      // :root pseudo-class descriptors
    public (int startIndex, int count) wildCardSelectorRange;  // * and standalone pseudo-classes

    // For reverse lookup
    internal StyleSheet ownerStyleSheet;

    // Used for early rejection of lookups
    internal int nonEmptyTablesMask;

    // Only used in tests for now
    public static bool AreSame(SelectorAccelerationCacheEntry firstEntry, SelectorAccelerationCacheEntry secondEntry)
    {
        // Compare flattened data references
        return firstEntry.allParts == secondEntry.allParts
               && firstEntry.allSelectors == secondEntry.allSelectors
               && firstEntry.allDescriptors == secondEntry.allDescriptors;
    }

}

class SelectorAccelerationCache
{
    public static SelectorAccelerationCache shared = new SelectorAccelerationCache();

    private static ProfilerMarker s_MarkerBuild = new ProfilerMarker("UIElements.BuildAccelerateSelectors");
    private static ProfilerMarker s_MarkerClean = new ProfilerMarker("UIElements.CleanAcceleratedSelectors");

    readonly Dictionary<EntityId, SelectorAccelerationCacheEntry> m_Cache = new();
    readonly List<(EntityId dependency, EntityId dependent)> m_DependencyList = new(128);
    readonly DependencyComparer  m_DependencyComparer = new();

    class DependencyComparer : IComparer<(EntityId dependency, EntityId dependent)>
    {
        public int Compare((EntityId dependency, EntityId dependent) x, (EntityId dependency, EntityId dependent) y)
        {
            int diff = x.dependency.CompareTo(y.dependency);
            if (diff != 0)
                return diff;
            return x.dependent.CompareTo(y.dependent);
        }
    }

    readonly List<(string path, EntityId entityId)> m_PathList = new(128);
    readonly Dictionary<EntityId, int> m_CacheForHash = new();
    readonly PathEntityIdComparer m_PathListComparer = new();

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
        // Find the all occurrences of this path
        (int firstIndex, int removeCount) = m_PathList.FindRangeForKey(m_PathListComparer, styleSheetPath, EntityId.None);

        // For each occurence, clean up the cached data
        for (int i = 0; i < removeCount; i++)
        {
            EntityId entityId = m_PathList[firstIndex + i].entityId;
            m_Cache.Remove(entityId);
            m_CacheForHash.Remove(entityId);
            // No need to remove each dependent style sheet from the main cache here
            // This is because they will be removed separately (dependent style sheets get reimported too)
            m_DependencyList.RemoveRangeForKey(m_DependencyComparer, entityId, EntityId.None);
        }

        m_PathList.RemoveRange(firstIndex, removeCount);
    }

    public void Remove(StyleSheet styleSheet)
    {
        s_MarkerClean.Begin();

        EntityId entityId = styleSheet.GetEntityId();

        // First, we try to find if any potential dependent stylesheet registered for this one
        {
            (int firstIndex, int removeCount) = m_DependencyList.FindRangeForKey(m_DependencyComparer, entityId, EntityId.None);

            // Remove each entry for any dependent stylesheet
            for (int i = 0; i < removeCount; i++)
            {
                EntityId dependentEntityId = m_DependencyList[firstIndex + i].dependent;
                m_Cache.Remove(dependentEntityId);
                m_CacheForHash.Remove(dependentEntityId);
                RemovedStyleSheetFromMainCache(dependentEntityId);
            }

            m_DependencyList.RemoveRange(firstIndex, removeCount);
        }

        // Attempt to remove the style sheet itself
        RemovedStyleSheetFromMainCache(entityId, styleSheet);

        s_MarkerClean.End();
    }

    private void RemovedStyleSheetFromMainCache(EntityId entityId, StyleSheet styleSheet = null)
    {
        bool removedFromMainCache = m_Cache.Remove(entityId);

        if (!removedFromMainCache)
            return;

        bool removedFromHashCache = m_CacheForHash.Remove(entityId);
        Debug.Assert(removedFromHashCache, "removedFromMainCache == removedFromHashCache");

        if (styleSheet == null)
        {
            styleSheet = Resources.EntityIdToObject(entityId) as StyleSheet;
        }
        if (styleSheet == null)
        {
            return;
        }

        string path = Panel.GetStyleSheetPath(styleSheet);
        if (!string.IsNullOrEmpty(path) && path != "Library/unity editor resources")
        {
            int index = m_PathList.BinarySearch((path, styleSheet.GetEntityId()), m_PathListComparer);
            if (index >= 0)
                m_PathList.RemoveRange(index, 1);
        }
    }

    public SelectorAccelerationCacheEntry GetOrCreate(StyleSheet styleSheet)
    {
        EntityId entityId = styleSheet.GetEntityId();

        if (!m_Cache.TryGetValue(entityId, out var entry))
        {
            s_MarkerBuild.Begin();
            entry = new SelectorAccelerationCacheEntry();

            // Ensure the main stylesheet has calculated hashes before building cache
            styleSheet.RebuildIfNecessary();

            // Track dependencies for imported stylesheets
            if (styleSheet.flattenedRecursiveImports != null && styleSheet.flattenedRecursiveImports.Count > 0)
            {
                for (var i = styleSheet.flattenedRecursiveImports.Count - 1; i >= 0; i--)
                {
                    var sheet = styleSheet.flattenedRecursiveImports[i];
                    if (sheet == null)
                        continue;
                    // This is very defensive, hopefully we can remove this in the future
                    sheet.RebuildIfNecessary();

                    // Accumulate new keys at the end to avoid repetitive scans for a specific index
                    m_DependencyList.Add( (sheet.GetEntityId(), entityId));
                }
                // Sort after adding values at the end
                m_DependencyList.Sort(m_DependencyComparer);
            }

            // Build flattened cache
            SelectorAccelerationCacheBuilder.BuildFlattenedCache(ref entry, styleSheet);

            m_Cache.Add(entityId, entry);
            s_MarkerBuild.End();
            string path = Panel.GetStyleSheetPath(styleSheet);
            if (!string.IsNullOrEmpty(path) && path != "Library/unity editor resources")
            {
                bool inserted = m_PathList.InsertUniquePair(m_PathListComparer, path, entityId);
                if (!inserted)
                {
                    Debug.Assert(false, $"SelectorAccelerationCache: {path} / {entityId} association already exists");
                }
            }
            if (!m_CacheForHash.TryAdd(entityId, styleSheet.contentHash))
            {
                Debug.LogError($"SelectorAccelerationCache: {styleSheet.name} (entityId={entityId}) already has a hash, previous is {m_CacheForHash[entityId]}, new is {styleSheet.contentHash}");
                m_CacheForHash[entityId] = styleSheet.contentHash;
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
