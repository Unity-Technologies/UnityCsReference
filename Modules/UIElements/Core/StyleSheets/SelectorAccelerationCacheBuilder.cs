// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    // Builder for flattened selector acceleration cache
    //
    // This builder converts StyleSheet selector data into a flattened, cache-friendly format
    // optimized for fast matching with reduced memory allocations and better cache locality.
    //
    // LIMITS (imposed by data structure sizes for memory efficiency):
    //   - Maximum 65,535 total parts across entire stylesheet (including imports)
    //   - Maximum 65,535 total selectors across entire stylesheet (including imports)
    //
    // These limits are extremely generous for typical stylesheets. If exceeded, the builder will:
    //   - Log clear error messages indicating which limit was exceeded
    //   - Return empty acceleration cache
    //
    // For reference, a typical UI stylesheet has:
    //   - 50-500 total selectors
    //   - 1-5 parts per selector
    //   - 1-3 selectors per complex selector (descendant/child relationships)
    //
    // Note: Using ushort instead of byte for counts eliminates struct padding while
    // providing more than enough capacity for any realistic stylesheet.
    internal static partial class SelectorAccelerationCacheBuilder
    {
        // Limits imposed by data structure sizes (ushort.MaxValue -> 65,535)
        private const int MaxTotalParts = ushort.MaxValue;
        private const int MaxTotalSelectors = ushort.MaxValue;

        // Cached delegate so the per-build sort doesn't allocate one.
        [NoAutoStaticsCleanup] // static method reference; safe to persist
        private static readonly RefComparison<SelectorRangeDescriptor> s_DescriptorRefComparison = CompareDescriptors;

        // Public API
        public static unsafe void BuildFlattenedCache(ref SelectorAccelerationCacheEntry entry, StyleSheet styleSheet)
        {
            // Count total parts, selectors, and complex selectors (including imported sheets)
            int totalParts = 0;
            int totalSelectors = 0;
            int totalComplexSelectors = 0;

            if (!CountSelectorsInStyleSheet(styleSheet, ref totalParts, ref totalSelectors, ref totalComplexSelectors))
            {
                entry.ownerStyleSheet = styleSheet;
                return;
            }

            if (styleSheet.flattenedRecursiveImports != null)
            {
                foreach (var sheet in styleSheet.flattenedRecursiveImports)
                {
                    if (sheet == null) continue;
                    if (!CountSelectorsInStyleSheet(sheet, ref totalParts, ref totalSelectors, ref totalComplexSelectors))
                    {
                        entry.ownerStyleSheet = styleSheet;
                        return;
                    }
                }
            }

            // Nothing to allocate - leave m_BackingBuffer null and all pointer/count fields zero.
            if (totalParts == 0 && totalSelectors == 0 && totalComplexSelectors == 0)
            {
                entry.ownerStyleSheet = styleSheet;
                return;
            }

            // Pre-count the distinct (tableType, tableKey) pairs so the key index is allocated
            // at its exact size (a fixed per-descriptor capacity wastes up to 12B per
            // descriptor on sheets with many rules per key). Build-time only; pooled scratch.
            int keyIndexCount = 0;
            var packedKeys = System.Buffers.ArrayPool<long>.Shared.Rent(totalComplexSelectors);
            try
            {
                int packedCount = 0;
                CollectTableKeys(styleSheet, packedKeys, ref packedCount);
                if (styleSheet.flattenedRecursiveImports != null)
                {
                    foreach (var sheet in styleSheet.flattenedRecursiveImports)
                    {
                        if (sheet == null) continue;
                        CollectTableKeys(sheet, packedKeys, ref packedCount);
                    }
                }

                Array.Sort(packedKeys, 0, packedCount);
                for (int i = 0; i < packedCount; i++)
                {
                    if (i == 0 || packedKeys[i] != packedKeys[i - 1])
                        keyIndexCount++;
                }
            }
            finally
            {
                System.Buffers.ArrayPool<long>.Shared.Return(packedKeys);
            }

            entry = SelectorAccelerationCacheEntry.Allocate(totalParts, totalSelectors, totalComplexSelectors, keyIndexCount);
            entry.ownerStyleSheet = styleSheet;

            // The entry isn't published to the cache yet, so a throw past this point would
            // leak the tracked allocation if we didn't free it ourselves.
            try
            {
                var allParts       = entry.allPartsWritable;
                var allSelectors   = entry.allSelectorsWritable;
                var allDescriptors = entry.allDescriptorsWritable;

                int partIdx = 0;
                int selectorIdx = 0;
                int descriptorIdx = 0;

                FlattenStyleSheet(allParts, allSelectors, allDescriptors, styleSheet, -1, ref partIdx, ref selectorIdx, ref descriptorIdx);

                if (styleSheet.flattenedRecursiveImports != null)
                {
                    for (int i = 0; i < styleSheet.flattenedRecursiveImports.Count; i++)
                    {
                        var sheet = styleSheet.flattenedRecursiveImports[i];
                        if (sheet == null) continue;
                        FlattenStyleSheet(allParts, allSelectors, allDescriptors, sheet, i, ref partIdx, ref selectorIdx, ref descriptorIdx);
                    }
                }

                // Sort descriptors by (tableType, tableKey, orderInStyleSheet) in place over the
                // backing buffer - SpanSort takes a ref-comparison so the descriptor (a large
                // struct) is not copied per comparison.
                if (totalComplexSelectors > 1)
                    SpanSort.Sort(allDescriptors, s_DescriptorRefComparison);

                BuildRangeTables(ref entry, allDescriptors);
                Debug.Assert(entry.m_KeyIndexCount == keyIndexCount,
                    "Key-index pre-count disagrees with the built index; GetTableSlot and BuildRangeTables are out of sync");
            }
            catch (Exception)
            {
                entry.Free();
                throw;
            }
        }

        // Count selectors in a stylesheet and validate limits
        private static bool CountSelectorsInStyleSheet(StyleSheet styleSheet, ref int totalParts, ref int totalSelectors, ref int totalComplexSelectors)
        {
            for (int ruleIdx = 0; ruleIdx < styleSheet.rules.Length; ruleIdx++)
            {
                var rule = styleSheet.rules[ruleIdx];
                if (rule.complexSelectors == null) continue;

                foreach (var complexSelector in rule.complexSelectors)
                {
                    totalComplexSelectors++;
                    foreach (var selector in complexSelector.selectors)
                    {
                        totalSelectors++;
                        totalParts += selector.parts.Length;
                    }
                }
            }

            // Validate limits after counting
            if (totalParts > MaxTotalParts)
            {
                Debug.LogError($"StyleSheet '{styleSheet.name}' causes total parts ({totalParts}) to exceed " +
                    $"the maximum of {MaxTotalParts}. This stylesheet and its imports will not be accelerated.", styleSheet);
                return false;
            }

            if (totalSelectors > MaxTotalSelectors)
            {
                Debug.LogError($"StyleSheet '{styleSheet.name}' causes total selectors ({totalSelectors}) to exceed " +
                    $"the maximum of {MaxTotalSelectors}. This stylesheet and its imports will not be accelerated.", styleSheet);
                return false;
            }

            return true;
        }

        // Maps a complex selector's rightmost part to its acceleration-table slot. The single
        // source of truth for the key scheme: the descriptor fill and the key-count pre-pass
        // (CollectTableKeys) both go through here.
        private static void GetTableSlot(StyleComplexSelector complexSelector, out SelectorAccelerationTableType tableType, out int tableKey)
        {
            var lastSelector = complexSelector.selectors[^1];
            var lastPart = lastSelector.parts[0];

            switch (lastPart.type)
            {
                case StyleSelectorType.Class:
                    tableType = SelectorAccelerationTableType.Class;
                    tableKey = lastPart.cachedUniqueStyleStringId;
                    break;
                case StyleSelectorType.ID:
                    tableType = SelectorAccelerationTableType.Name;
                    tableKey = lastPart.cachedUniqueStyleStringId;
                    break;
                case StyleSelectorType.Type:
                    tableType = SelectorAccelerationTableType.Type;
                    tableKey = lastPart.cachedUniqueStyleStringId;
                    break;
                case StyleSelectorType.Wildcard:
                    tableType = SelectorAccelerationTableType.None;
                    tableKey = 1; // 1 for wildcard
                    break;
                case StyleSelectorType.PseudoClass:
                    tableType = SelectorAccelerationTableType.None;
                    tableKey = ((lastSelector.pseudoStateMask & (int)PseudoStates.Root) != 0) ? 0 : 1;
                    break;
                default:
                    // Predicates never reach the cache builder; zeros match the previous
                    // cleared-memory behavior.
                    tableType = (SelectorAccelerationTableType)0;
                    tableKey = 0;
                    break;
            }
        }

        // Packs every Name/Type/Class table slot of the sheet into keys (one entry per
        // descriptor; None slots are skipped — root/wildcard don't enter the key index).
        private static void CollectTableKeys(StyleSheet styleSheet, long[] keys, ref int count)
        {
            for (int ruleIdx = 0; ruleIdx < styleSheet.rules.Length; ruleIdx++)
            {
                var rule = styleSheet.rules[ruleIdx];
                if (rule.complexSelectors == null) continue;

                foreach (var complexSelector in rule.complexSelectors)
                {
                    GetTableSlot(complexSelector, out var tableType, out var tableKey);
                    if (tableType == SelectorAccelerationTableType.None)
                        continue;

                    keys[count++] = ((long)tableType << 32) | (uint)tableKey;
                }
            }
        }

        // Flatten a stylesheet into the writable region spans.
        private static unsafe void FlattenStyleSheet(
            Span<FlattenedSelectorPart> allParts,
            Span<FlattenedSelector> allSelectors,
            Span<SelectorRangeDescriptor> allDescriptors,
            StyleSheet styleSheet,
            int importedStyleSheetIndex,
            ref int partIdx,
            ref int selectorIdx,
            ref int descriptorIdx)
        {
            for (int ruleIdx = 0; ruleIdx < styleSheet.rules.Length; ruleIdx++)
            {
                var rule = styleSheet.rules[ruleIdx];
                if (rule.complexSelectors == null) continue;

                for (int selIdx = 0; selIdx < rule.complexSelectors.Length; selIdx++)
                {
                    var complexSelector = rule.complexSelectors[selIdx];
                    int startSelectorIndex = selectorIdx;

                    // Flatten all selectors in this complex selector
                    foreach (var selector in complexSelector.selectors)
                    {
                        int startPartIndex = partIdx;

                        // Flatten all parts in this selector
                        foreach (var part in selector.parts)
                        {
                            allParts[partIdx] = FlattenPart(part);
                            partIdx++;
                        }

                        int partCount = partIdx - startPartIndex;

                        // Create flattened selector with an index range into the parts buffer.
                        allSelectors[selectorIdx] = new FlattenedSelector
                        {
                            pseudoStateMask = selector.pseudoStateMask,
                            negatedPseudoStateMask = selector.negatedPseudoStateMask,
                            previousRelationship = selector.previousRelationship,
                            partsStart = startPartIndex,
                            partCount = (ushort)partCount
                        };
                        selectorIdx++;
                    }

                    int selectorCount = selectorIdx - startSelectorIndex;

                    Debug.Assert(selectorCount > 0, "Complex selector with empty selectors[] reached the cache builder");

                    // Write the range descriptor by ref to avoid copying the 52B struct.
                    ref var pDesc = ref allDescriptors[descriptorIdx];
                    pDesc.selectorsStart = startSelectorIndex;
                    pDesc.selectorCount = (ushort)selectorCount;
                    pDesc.ruleIndex = ruleIdx;
                    pDesc.selectorIndexInRule = selIdx;
                    pDesc.orderInStyleSheet = complexSelector.orderInStyleSheet;
                    pDesc.importedStyleSheetIndex = importedStyleSheetIndex;
                    pDesc.specificity = complexSelector.specificity;

                    // Copy ancestor hashes
                    for (int i = 0; i < 4; i++)
                        pDesc.ancestorHashes[i] = complexSelector.ancestorHashes.hashes[i];

                    // Set tableType and tableKey based on last part
                    GetTableSlot(complexSelector, out pDesc.tableType, out pDesc.tableKey);

                    descriptorIdx++;
                }
            }
        }

        // Flatten a single part
        private static FlattenedSelectorPart FlattenPart(StyleSelectorPart part)
        {
            var flattened = new FlattenedSelectorPart
            {
                type = part.type
            };

            // Use cached UniqueStyleString ID (set during CalculateHashes)
            // For ID/Class/Type selectors, cachedUniqueStyleStringId is >= 0
            // For other types (Wildcard, PseudoClass, Predicate), use -1
            if (part.type == StyleSelectorType.Class ||
                part.type == StyleSelectorType.ID ||
                part.type == StyleSelectorType.Type)
            {
                flattened.uniqueStringId = part.cachedUniqueStyleStringId;
            }
            else
            {
                flattened.uniqueStringId = -1;
            }

            return flattened;
        }

        // Fill the key index over the sorted descriptors. Descriptors are sorted by
        // (tableType, tableKey, orderInStyleSheet), so one pass emits one SelectorKeyIndexEntry
        // per distinct key and each table becomes a key-sorted block of the index; key lookup
        // is a binary search at match time (TryGetDescriptorRange / FindKeyRange). The None
        // block keeps direct descriptor ranges and splits into :root (tableKey 0) then
        // wildcard (tableKey 1) sub-blocks, per the keys assigned in FlattenStyleSheet.
        private static unsafe void BuildRangeTables(ref SelectorAccelerationCacheEntry entry, Span<SelectorRangeDescriptor> allDescriptors)
        {
            entry.nameTableRegion = default;
            entry.typeTableRegion = default;
            entry.classTableRegion = default;
            entry.rootSelectorRange = default;
            entry.wildCardSelectorRange = default;
            entry.nonEmptyTablesMask = 0;
            entry.m_KeyIndexCount = 0;

            int descriptorCount = allDescriptors.Length;
            int idx = 0;
            while (idx < descriptorCount)
            {
                var tableType = allDescriptors[idx].tableType;
                int runStart = idx;

                if (tableType == SelectorAccelerationTableType.None)
                {
                    while (idx < descriptorCount && allDescriptors[idx].tableType == tableType)
                        idx++;

                    int rootEnd = runStart;
                    while (rootEnd < idx && allDescriptors[rootEnd].tableKey == 0)
                        rootEnd++;
                    entry.rootSelectorRange = new DescriptorRange { start = runStart, count = rootEnd - runStart };
                    entry.wildCardSelectorRange = new DescriptorRange { start = rootEnd, count = idx - rootEnd };
                    continue;
                }

                // Emit one key-index entry per distinct key of this table's descriptor run.
                int regionStart = entry.m_KeyIndexCount;
                while (idx < descriptorCount && allDescriptors[idx].tableType == tableType)
                {
                    int key = allDescriptors[idx].tableKey;
                    int keyStart = idx;
                    while (idx < descriptorCount && allDescriptors[idx].tableType == tableType && allDescriptors[idx].tableKey == key)
                        idx++;

                    entry.m_KeyIndexPtr[entry.m_KeyIndexCount++] = new SelectorKeyIndexEntry
                    {
                        key = key,
                        range = new DescriptorRange { start = keyStart, count = idx - keyStart },
                    };
                }

                var region = new DescriptorRange { start = regionStart, count = entry.m_KeyIndexCount - regionStart };
                switch (tableType)
                {
                    case SelectorAccelerationTableType.Name:
                        entry.nameTableRegion = region;
                        entry.nonEmptyTablesMask |= 1 << (int)SelectorAccelerationTableType.Name;
                        break;
                    case SelectorAccelerationTableType.Type:
                        entry.typeTableRegion = region;
                        entry.nonEmptyTablesMask |= 1 << (int)SelectorAccelerationTableType.Type;
                        break;
                    case SelectorAccelerationTableType.Class:
                        entry.classTableRegion = region;
                        entry.nonEmptyTablesMask |= 1 << (int)SelectorAccelerationTableType.Class;
                        break;
                }
            }
        }

        // Sort by: tableType, then tableKey, then orderInStyleSheet. Subtraction is safe here
        // because all three fields fit in int and never approach overflow ranges.
        private static int CompareDescriptors(ref SelectorRangeDescriptor a, ref SelectorRangeDescriptor b)
        {
            int result = (int)a.tableType - (int)b.tableType;
            if (result != 0) return result;

            result = a.tableKey - b.tableKey;
            if (result != 0) return result;

            return a.orderInStyleSheet - b.orderInStyleSheet;
        }
    }
}
