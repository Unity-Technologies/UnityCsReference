// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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

// Flattened selector part - leaf level.
// Layout must remain in sync with the C++ mirror in
// Modules/UIElements/Core/Native/StyleSheets/FlattenedSelectorStructs.h
struct FlattenedSelectorPart
{
    public int uniqueStringId;      // UniqueStyleString.id, -1 for wildcards/pseudo
    public StyleSelectorType type;   // Class, ID, Type, Wildcard, PseudoClass
}

// Flattened selector - middle level. Parts resolve via (partsStart, partCount) into the
// entry's allParts region.
// Layout must remain in sync with the C++ mirror in
// Modules/UIElements/Core/Native/StyleSheets/FlattenedSelectorStructs.h
struct FlattenedSelector
{
    public int partsStart;  // index into the owning entry's allParts buffer
    public int pseudoStateMask;
    public int negatedPseudoStateMask;
    public StyleSelectorRelationship previousRelationship;  // Child or Descendent
    public ushort partCount;
}

// Selector range descriptor - top level. Resolve selectors via SelectorAccelerationCacheEntry.SelectorsFor.
unsafe struct SelectorRangeDescriptor
{
    public int selectorsStart;  // index into the owning entry's allSelectors buffer

    // Sorting key (used to sort allDescriptors)
    public SelectorAccelerationTableType tableType;  // Name, Type, Class, or None (for wildcards/root)
    public int tableKey;  // UniqueStyleString.id for the last part

    // Reverse lookup
    public int ruleIndex;
    public int selectorIndexInRule;

    // Cached metadata
    public int orderInStyleSheet;
    public int importedStyleSheetIndex;
    public int specificity;

    // Bloom filter hashes
    public fixed int ancestorHashes[Hashes.kSize];

    // selectorCount > 1 doubles as the "is complex" check (matches StyleComplexSelector.isSimple
    // semantics) — used by the matcher to gate the bloom-filter prefilter.
    public ushort selectorCount;
}

// Range into the owning entry's allDescriptors buffer. Resolve via SelectorAccelerationCacheEntry.DescriptorsFor.
// Layout must remain in sync with the C++ mirror in
// Modules/UIElements/Core/Native/StyleSheets/FlattenedSelectorStructs.h
struct DescriptorRange
{
    public int start;
    public int count;
}

// One distinct table key and the descriptor range it maps to. The entry's key index holds
// these sorted by (tableType, key) so a table's block is binary-searchable by key — the
// compact 12-byte stride keeps the search cache-friendly, unlike probing the 52-byte
// descriptors directly. Layout must remain in sync with the C++ mirror in
// Modules/UIElements/Core/Native/StyleSheets/FlattenedSelectorStructs.h
struct SelectorKeyIndexEntry
{
    public int key;               // UniqueStyleString.id
    public DescriptorRange range; // descriptors for this key, in allDescriptors
}

// Argument block for the native per-sheet matcher (NativeSelectorMatcher.MatchSheetFlat).
// The table regions are ranges into the entry's key index; the root/wildcard ranges index
// allDescriptors directly. Layout must remain in sync with the C++ mirror in
// Modules/UIElements/Core/Native/StyleSheets/FlattenedSelectorStructs.h
struct SelectorMatcherRanges
{
    public DescriptorRange nameTableRegion;
    public DescriptorRange typeTableRegion;
    public DescriptorRange classTableRegion;
    public DescriptorRange rootSelectorRange;
    public DescriptorRange wildCardSelectorRange;
}

// Unmanaged core of a cache entry: everything the matcher reads, stored as the header of
// the entry's single tracked allocation so native code reads it in place through one
// pointer. The region pointers point into the same allocation, right after the header.
// Layout must remain in sync with the C++ mirror in
// Modules/UIElements/Core/Native/StyleSheets/FlattenedSelectorStructs.h
[StructLayout(LayoutKind.Sequential)]
unsafe struct SelectorAccelerationCacheCore
{
    // Pointers first so padding is identical on 32- and 64-bit platforms.
    public FlattenedSelectorPart* allParts;
    public FlattenedSelector* allSelectors;
    public SelectorRangeDescriptor* allDescriptors;
    // Compact key index: one SelectorKeyIndexEntry per distinct (tableType, tableKey) pair,
    // in the same order the sorted allDescriptors buffer introduces them, so each table is
    // one key-sorted block — a key lookup is a binary search over 12-byte entries
    // (TryGetDescriptorRange in C#, FindKeyRange in C++). Sized for one entry per descriptor
    // (the exact distinct-key count is only known after the sort); keyIndexCount is the
    // filled length, set by the builder.
    public SelectorKeyIndexEntry* keyIndex;
    public int allPartsCount;
    public int allSelectorsCount;
    public int allDescriptorsCount;
    public int keyIndexCount;

    // The native matcher's argument block: the table regions are ranges into the key index,
    // the root/wildcard ranges index allDescriptors directly. Matching reads it in place
    // instead of rebuilding it per call.
    public SelectorMatcherRanges matcherRanges;

    // Used for early rejection of lookups
    public int nonEmptyTablesMask;
}

// Owns a single tracked native allocation whose header is the entry's unmanaged core;
// m_Core doubles as the allocation's base address and is null for sheets with no selectors.
// Not IDisposable on purpose: entries are copied by value out of the cache, so a `using`
// block on a copy would free the buffer the cache still references. Free() is called
// exactly once, by the owner.
unsafe struct SelectorAccelerationCacheEntry
{
    // Monotonically increasing identifier stamped by the builder on each successful build.
    // Used by AreSame (test helper) to distinguish entries even when the allocator hands the
    // same backing address back after a Free + Malloc cycle.
    internal long m_BuildId;

    // Freed via UnsafeUtility.FreeTracked under the cache's MemoryLabel.
    internal SelectorAccelerationCacheCore* m_Core;

    public readonly ReadOnlySpan<FlattenedSelectorPart> allParts => m_Core == null ? default : new(m_Core->allParts, m_Core->allPartsCount);
    public readonly ReadOnlySpan<FlattenedSelector> allSelectors => m_Core == null ? default : new(m_Core->allSelectors, m_Core->allSelectorsCount);
    public readonly ReadOnlySpan<SelectorRangeDescriptor> allDescriptors => m_Core == null ? default : new(m_Core->allDescriptors, m_Core->allDescriptorsCount);

    // Getters legitimately see null cores (empty or unaccelerated sheets); setters are
    // builder-only and must run after Allocate, so a null core there is a bug.
    readonly SelectorAccelerationCacheCore* writableCore
    {
        get
        {
            Debug.Assert(m_Core != null, "Writing to a SelectorAccelerationCacheEntry that has no core (Allocate was not called)");
            return m_Core;
        }
    }

    // Writable views over the same regions, for the builder to populate without reaching
    // through the core pointer. Consumers use the ReadOnlySpan views above.
    internal readonly Span<FlattenedSelectorPart> allPartsWritable => new(m_Core->allParts, m_Core->allPartsCount);
    internal readonly Span<FlattenedSelector> allSelectorsWritable => new(m_Core->allSelectors, m_Core->allSelectorsCount);
    internal readonly Span<SelectorRangeDescriptor> allDescriptorsWritable => new(m_Core->allDescriptors, m_Core->allDescriptorsCount);

    // Resolve a child range stored as (start, count) on SelectorRangeDescriptor /
    // DescriptorRange against the matching base region of this entry.
    public readonly ReadOnlySpan<FlattenedSelector> SelectorsFor(in SelectorRangeDescriptor descriptor)
        => new(m_Core->allSelectors + descriptor.selectorsStart, descriptor.selectorCount);
    public readonly ReadOnlySpan<SelectorRangeDescriptor> DescriptorsFor(in DescriptorRange range)
        => new(m_Core->allDescriptors + range.start, range.count);

    // Reverse lookup: descriptor → the authoring StyleComplexSelector it was flattened from.
    public readonly StyleComplexSelector GetComplexSelector(in SelectorRangeDescriptor descriptor)
    {
        var styleSheet = descriptor.importedStyleSheetIndex == -1
            ? ownerStyleSheet
            : ownerStyleSheet.flattenedRecursiveImports[descriptor.importedStyleSheetIndex];

        return styleSheet.rules[descriptor.ruleIndex].complexSelectors[descriptor.selectorIndexInRule];
    }

    public DescriptorRange nameTableRegion   // keys are UniqueStyleString.ids
    {
        readonly get => m_Core == null ? default : m_Core->matcherRanges.nameTableRegion;
        set => writableCore->matcherRanges.nameTableRegion = value;
    }
    public DescriptorRange typeTableRegion
    {
        readonly get => m_Core == null ? default : m_Core->matcherRanges.typeTableRegion;
        set => writableCore->matcherRanges.typeTableRegion = value;
    }
    public DescriptorRange classTableRegion
    {
        readonly get => m_Core == null ? default : m_Core->matcherRanges.classTableRegion;
        set => writableCore->matcherRanges.classTableRegion = value;
    }

    // Test helper: the matcher reads the index through the core directly.
    internal readonly ReadOnlySpan<SelectorKeyIndexEntry> keyIndex => m_Core == null ? default : new(m_Core->keyIndex, m_Core->keyIndexCount);

    public readonly DescriptorRange GetTableRegion(SelectorAccelerationTableType type)
    {
        switch (type)
        {
            case SelectorAccelerationTableType.Name:
                return nameTableRegion;
            case SelectorAccelerationTableType.Type:
                return typeTableRegion;
            case SelectorAccelerationTableType.Class:
                return classTableRegion;
        }
        return default;
    }

    // Binary-searches a table's block of the key index for tableKey. Returns false when the
    // key has no selectors in this sheet.
    public readonly bool TryGetDescriptorRange(SelectorAccelerationTableType tableType, int tableKey, out DescriptorRange range)
    {
        var region = GetTableRegion(tableType);
        int lo = region.start;
        int hi = region.start + region.count;
        while (lo < hi)
        {
            int mid = (int)(((uint)lo + (uint)hi) >> 1);
            if (m_Core->keyIndex[mid].key < tableKey)
                lo = mid + 1;
            else
                hi = mid;
        }

        if (lo == region.start + region.count || m_Core->keyIndex[lo].key != tableKey)
        {
            range = default;
            return false;
        }

        range = m_Core->keyIndex[lo].range;
        return true;
    }

    // Test helper: number of distinct keys in a table's block.
    internal readonly int GetTableKeyCount(SelectorAccelerationTableType type) => GetTableRegion(type).count;

    // Special selectors (direct ranges in sorted allDescriptors)
    public DescriptorRange rootSelectorRange       // :root pseudo-class descriptors
    {
        readonly get => m_Core == null ? default : m_Core->matcherRanges.rootSelectorRange;
        set => writableCore->matcherRanges.rootSelectorRange = value;
    }
    public DescriptorRange wildCardSelectorRange   // * and standalone pseudo-classes
    {
        readonly get => m_Core == null ? default : m_Core->matcherRanges.wildCardSelectorRange;
        set => writableCore->matcherRanges.wildCardSelectorRange = value;
    }

    // For reverse lookup
    internal StyleSheet ownerStyleSheet;

    // Used for early rejection of lookups
    internal int nonEmptyTablesMask
    {
        readonly get => m_Core == null ? 0 : m_Core->nonEmptyTablesMask;
        set => writableCore->nonEmptyTablesMask = value;
    }

    public void Free()
    {
        if (m_Core != null)
        {
            UnsafeUtility.FreeTracked(m_Core, SelectorAccelerationCache.s_MemoryLabel);
            m_Core = null;
        }
    }

    // Monotonically increasing id stamped onto each freshly allocated entry, so AreSame can
    // distinguish entries even when the tracked allocator reuses an address after Free + Malloc.
    [NoAutoStaticsCleanup] // monotonic counter; safe to persist
    static long s_NextBuildId;

    // Lay the core header and the four regions out contiguously in one tracked allocation.
    // Region order matches the matcher's access funnel (descriptors → key index → selectors
    // → parts in the native matcher): descriptors are the most-touched region and live right
    // after the header; the key index, selectors and parts follow. 8-byte alignment between
    // regions covers the pointer fields inside the core header.
    public static SelectorAccelerationCacheEntry Allocate(int totalParts, int totalSelectors, int totalDescriptors, int totalKeyIndexEntries)
    {
        const int kAlign = 8;
        long coreBytes        = AlignUp(sizeof(SelectorAccelerationCacheCore), kAlign);
        long descriptorsBytes = AlignUp((long)totalDescriptors * sizeof(SelectorRangeDescriptor), kAlign);
        // Exact capacity: the builder pre-counts distinct (tableType, tableKey) pairs; the
        // filled length (keyIndexCount) is set by BuildRangeTables after sorting.
        long keyIndexBytes    = AlignUp((long)totalKeyIndexEntries * sizeof(SelectorKeyIndexEntry), kAlign);
        long selectorsBytes   = AlignUp((long)totalSelectors   * sizeof(FlattenedSelector),       kAlign);
        long partsBytes       = (long)totalParts               * sizeof(FlattenedSelectorPart);
        long totalBytes       = coreBytes + descriptorsBytes + keyIndexBytes + selectorsBytes + partsBytes;

        byte* basePtr = (byte*)UnsafeUtility.MallocTracked(totalBytes, kAlign, SelectorAccelerationCache.s_MemoryLabel, 0);
        UnsafeUtility.MemClear(basePtr, totalBytes);

        var core = (SelectorAccelerationCacheCore*)basePtr;
        core->allDescriptors      = (SelectorRangeDescriptor*)(basePtr + coreBytes);
        core->allDescriptorsCount = totalDescriptors;
        core->keyIndex            = (SelectorKeyIndexEntry*)(basePtr + coreBytes + descriptorsBytes);
        core->allSelectors        = (FlattenedSelector*)(basePtr + coreBytes + descriptorsBytes + keyIndexBytes);
        core->allSelectorsCount   = totalSelectors;
        core->allParts            = (FlattenedSelectorPart*)(basePtr + coreBytes + descriptorsBytes + keyIndexBytes + selectorsBytes);
        core->allPartsCount       = totalParts;

        return new SelectorAccelerationCacheEntry
        {
            m_BuildId = System.Threading.Interlocked.Increment(ref s_NextBuildId),
            m_Core    = core,
        };

        static long AlignUp(long value, int alignment) => (value + alignment - 1) & ~((long)alignment - 1);
    }

    // Only used in tests for now. Build ids are unique per builder invocation, so two entries
    // are "the same" iff they came from the same BuildFlattenedCache call.
    public static bool AreSame(SelectorAccelerationCacheEntry firstEntry, SelectorAccelerationCacheEntry secondEntry)
    {
        return firstEntry.m_BuildId == secondEntry.m_BuildId;
    }
}

class SelectorAccelerationCache
{
    [NoAutoStaticsCleanup] // Shutdown (registered on UnloadingUtility below) frees the native cache entries
    public static SelectorAccelerationCache shared = new SelectorAccelerationCache();

    internal static readonly MemoryLabel s_MemoryLabel =
        new MemoryLabel(nameof(UIElements), "StyleSheets.SelectorAccelerationCache");

    static SelectorAccelerationCache()
    {
        UnloadingUtility.SubscribeToUnloading(UnloadingSubscriber.SelectorAccelerationCache, Shutdown);
    }

    static void Shutdown()
    {
        shared.Clear();
    }

    internal void Clear()
    {
        foreach (var kv in m_Cache)
            kv.Value.Free();
        m_Cache.Clear();
        m_DependencyList.Clear();
        m_PathList.Clear();
        m_CacheForHash.Clear();
    }

    private static readonly ProfilerMarker s_MarkerBuild = new ProfilerMarker("UIElements.BuildAccelerateSelectors");
    private static readonly ProfilerMarker s_MarkerClean = new ProfilerMarker("UIElements.CleanAcceleratedSelectors");

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
    internal int dependencyCount => m_DependencyList.Count;

    bool TryRemoveAndDisposeEntry(EntityId entityId)
    {
        if (!m_Cache.Remove(entityId, out var entry))
            return false;
        entry.Free();
        return true;
    }

    // Rows naming an evicted sheet as the dependent go with its entry: rebuilding through
    // GetOrCreate re-registers every import relation, so stale rows would duplicate on rebuild.
    void RemoveDependencyRowsForDependents(HashSet<EntityId> evicted)
    {
        if (evicted.Count == 0)
            return;

        int write = 0;
        for (int read = 0; read < m_DependencyList.Count; read++)
        {
            if (!evicted.Contains(m_DependencyList[read].dependent))
                m_DependencyList[write++] = m_DependencyList[read];
        }
        m_DependencyList.RemoveRange(write, m_DependencyList.Count - write);
    }

    public void Remove(string styleSheetPath)
    {
        // Find the all occurrences of this path
        (int firstIndex, int removeCount) = m_PathList.FindRangeForKey(m_PathListComparer, styleSheetPath, EntityId.None);

        using var _ = UnityEngine.Pool.HashSetPool<EntityId>.Get(out var evicted);

        // For each occurence, clean up the cached data
        for (int i = 0; i < removeCount; i++)
        {
            EntityId entityId = m_PathList[firstIndex + i].entityId;
            TryRemoveAndDisposeEntry(entityId);
            m_CacheForHash.Remove(entityId);
            // Dependent style sheets are not evicted from the main cache here: they get reimported
            // too, each arriving with its own Remove call. Only this sheet's dependency rows are
            // cleaned up — the rows it keys now, and the rows naming it as the dependent below.
            m_DependencyList.RemoveRangeForKey(m_DependencyComparer, entityId, EntityId.None);
            evicted.Add(entityId);
        }

        m_PathList.RemoveRange(firstIndex, removeCount);
        RemoveDependencyRowsForDependents(evicted);
    }

    public void Remove(StyleSheet styleSheet)
    {
        using (s_MarkerClean.Auto())
        {

            EntityId entityId = styleSheet.GetEntityId();

            using var _ = UnityEngine.Pool.HashSetPool<EntityId>.Get(out var evicted);
            evicted.Add(entityId);

            // First, we try to find if any potential dependent stylesheet registered for this one
            {
                (int firstIndex, int removeCount) = m_DependencyList.FindRangeForKey(m_DependencyComparer, entityId, EntityId.None);

                // Remove each entry for any dependent stylesheet, including its path-list association:
                // an in-memory edit of an imported sheet (e.g. the UI Builder editing it) has no reimport
                // following it, so the dependent rebuilds through GetOrCreate and re-inserting its path
                // would collide with a stale association. A real reimport's path-keyed Remove tolerates
                // the association already being gone.
                for (int i = 0; i < removeCount; i++)
                {
                    var dependent = m_DependencyList[firstIndex + i].dependent;
                    if (evicted.Add(dependent))
                        RemovedStyleSheetFromMainCache(dependent);
                }
            }

            // Attempt to remove the style sheet itself
            RemovedStyleSheetFromMainCache(entityId, styleSheet);

            // This also drops the rows keyed by this sheet, which named the dependents just evicted.
            RemoveDependencyRowsForDependents(evicted);

        }
    }

    private void RemovedStyleSheetFromMainCache(EntityId entityId, StyleSheet styleSheet = null)
    {
        bool removedFromMainCache = TryRemoveAndDisposeEntry(entityId);

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
            using (s_MarkerBuild.Auto())
            {
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
            }
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
