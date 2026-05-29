// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
    internal static class SelectorAccelerationCacheBuilder
    {
        // Limits imposed by data structure sizes (ushort.MaxValue -> 65,535)
        private const int MaxTotalParts = ushort.MaxValue;
        private const int MaxTotalSelectors = ushort.MaxValue;

        // Cached delegate so the per-build sort doesn't allocate one.
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

            entry = SelectorAccelerationCacheEntry.Allocate(totalParts, totalSelectors, totalComplexSelectors);
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
                    var lastSelector = complexSelector.selectors[^1];
                    var lastPart = lastSelector.parts[0];

                    switch (lastPart.type)
                    {
                        case StyleSelectorType.Class:
                            pDesc.tableType = SelectorAccelerationTableType.Class;
                            pDesc.tableKey = lastPart.cachedUniqueStyleStringId;
                            break;
                        case StyleSelectorType.ID:
                            pDesc.tableType = SelectorAccelerationTableType.Name;
                            pDesc.tableKey = lastPart.cachedUniqueStyleStringId;
                            break;
                        case StyleSelectorType.Type:
                            pDesc.tableType = SelectorAccelerationTableType.Type;
                            pDesc.tableKey = lastPart.cachedUniqueStyleStringId;
                            break;
                        case StyleSelectorType.Wildcard:
                            pDesc.tableType = SelectorAccelerationTableType.None;
                            pDesc.tableKey = 1; // 1 for wildcard
                            break;
                        case StyleSelectorType.PseudoClass:
                            pDesc.tableType = SelectorAccelerationTableType.None;
                            pDesc.tableKey = ((lastSelector.pseudoStateMask & (int)PseudoStates.Root) != 0) ? 0 : 1;
                            break;
                    }

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

        // Build range tables from sorted descriptors
        private static void BuildRangeTables(ref SelectorAccelerationCacheEntry entry, Span<SelectorRangeDescriptor> allDescriptors)
        {
            // Initialize to null - only allocate if needed
            entry.nameTable = null;
            entry.typeTable = null;
            entry.classTable = null;
            entry.nonEmptyTablesMask = 0;

            int descriptorCount = allDescriptors.Length;

            // Count unique keys per table type from sorted descriptors
            // Since descriptors are sorted by (tableType, tableKey, orderInStyleSheet),
            // we can count unique (tableType, tableKey) pairs in a single pass
            int uniqueNameCount = 0;
            int uniqueTypeCount = 0;
            int uniqueClassCount = 0;

            SelectorAccelerationTableType lastTableType = (SelectorAccelerationTableType)(-2);
            int lastTableKey = -1;

            for (int i = 0; i < descriptorCount; i++)
            {
                // ref readonly avoids copying the 56B descriptor per iteration.
                ref readonly var descriptor = ref allDescriptors[i];

                // Check if this is a new unique (tableType, tableKey) pair
                if (descriptor.tableType != lastTableType || descriptor.tableKey != lastTableKey)
                {
                    switch (descriptor.tableType)
                    {
                        case SelectorAccelerationTableType.Name:
                            uniqueNameCount++;
                            break;
                        case SelectorAccelerationTableType.Type:
                            uniqueTypeCount++;
                            break;
                        case SelectorAccelerationTableType.Class:
                            uniqueClassCount++;
                            break;
                    }

                    lastTableType = descriptor.tableType;
                    lastTableKey = descriptor.tableKey;
                }
            }

            int rootStart = -1;
            int rootCount = 0;
            int wildcardStart = -1;
            int wildcardCount = 0;

            for (int i = 0; i < descriptorCount; i++)
            {
                ref readonly var descriptor = ref allDescriptors[i];

                if (descriptor.tableType == SelectorAccelerationTableType.None)
                {
                    // Check if it's a :root selector or wildcard
                    var lastSelector = entry.allSelectors[descriptor.selectorsStart + descriptor.selectorCount - 1];
                    if ((lastSelector.pseudoStateMask & (int)PseudoStates.Root) != 0)
                    {
                        if (rootStart < 0) rootStart = i;
                        rootCount++;
                    }
                    else
                    {
                        if (wildcardStart < 0) wildcardStart = i;
                        wildcardCount++;
                    }
                }
                else
                {
                    // Lazily allocate table on first use and set mask bit
                    Dictionary<int, DescriptorRange> table;
                    switch (descriptor.tableType)
                    {
                        case SelectorAccelerationTableType.Name:
                            if (entry.nameTable == null)
                            {
                                entry.nameTable = new Dictionary<int, DescriptorRange>(uniqueNameCount);
                                entry.nonEmptyTablesMask |= (1 << (int)SelectorAccelerationTableType.Name);
                            }
                            table = entry.nameTable;
                            break;
                        case SelectorAccelerationTableType.Type:
                            if (entry.typeTable == null)
                            {
                                entry.typeTable = new Dictionary<int, DescriptorRange>(uniqueTypeCount);
                                entry.nonEmptyTablesMask |= (1 << (int)SelectorAccelerationTableType.Type);
                            }
                            table = entry.typeTable;
                            break;
                        case SelectorAccelerationTableType.Class:
                            if (entry.classTable == null)
                            {
                                entry.classTable = new Dictionary<int, DescriptorRange>(uniqueClassCount);
                                entry.nonEmptyTablesMask |= (1 << (int)SelectorAccelerationTableType.Class);
                            }
                            table = entry.classTable;
                            break;
                        default:
                            continue; // Should not happen
                    }

                    if (!table.TryGetValue(descriptor.tableKey, out var range))
                    {
                        // New key, start new range
                        table[descriptor.tableKey] = new DescriptorRange { start = i, count = 1 };
                    }
                    else
                    {
                        // Extend existing range
                        range.count++;
                        table[descriptor.tableKey] = range;
                    }
                }
            }

            entry.rootSelectorRange = new DescriptorRange { start = rootStart < 0 ? 0 : rootStart, count = rootCount };
            entry.wildCardSelectorRange = new DescriptorRange { start = wildcardStart < 0 ? 0 : wildcardStart, count = wildcardCount };
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
