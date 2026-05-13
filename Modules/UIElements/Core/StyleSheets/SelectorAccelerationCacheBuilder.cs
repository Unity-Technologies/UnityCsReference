// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

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

        // Static comparer instance to avoid allocations
        private static readonly DescriptorComparer s_DescriptorComparer = new DescriptorComparer();
        // Public API
        public static void BuildFlattenedCache(ref SelectorAccelerationCacheEntry entry, StyleSheet styleSheet)
        {
            // Count total parts, selectors, and complex selectors (including imported sheets)
            int totalParts = 0;
            int totalSelectors = 0;
            int totalComplexSelectors = 0;

            if (!CountSelectorsInStyleSheet(styleSheet, ref totalParts, ref totalSelectors, ref totalComplexSelectors))
            {
                // Limits exceeded, allocate empty arrays
                entry.allParts = Array.Empty<FlattenedSelectorPart>();
                entry.allSelectors = Array.Empty<FlattenedSelector>();
                entry.allDescriptors = Array.Empty<SelectorRangeDescriptor>();
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
                        // Limits exceeded, allocate empty arrays
                        entry.allParts = Array.Empty<FlattenedSelectorPart>();
                        entry.allSelectors = Array.Empty<FlattenedSelector>();
                        entry.allDescriptors = Array.Empty<SelectorRangeDescriptor>();
                        entry.ownerStyleSheet = styleSheet;
                        return;
                    }
                }
            }

            // Allocate arrays
            entry.allParts = new FlattenedSelectorPart[totalParts];
            entry.allSelectors = new FlattenedSelector[totalSelectors];
            entry.allDescriptors = new SelectorRangeDescriptor[totalComplexSelectors];
            entry.ownerStyleSheet = styleSheet;

            // Flatten data
            int partIdx = 0;
            int selectorIdx = 0;
            int descriptorIdx = 0;

            FlattenStyleSheet(ref entry, styleSheet, -1, ref partIdx, ref selectorIdx, ref descriptorIdx);

            if (styleSheet.flattenedRecursiveImports != null)
            {
                for (int i = 0; i < styleSheet.flattenedRecursiveImports.Count; i++)
                {
                    var sheet = styleSheet.flattenedRecursiveImports[i];
                    if (sheet == null) continue;
                    FlattenStyleSheet(ref entry, sheet, i, ref partIdx, ref selectorIdx, ref descriptorIdx);
                }
            }

            // Sort descriptors by (tableType, tableKey, orderInStyleSheet)
            Array.Sort(entry.allDescriptors, s_DescriptorComparer);

            // Build range tables from sorted descriptors
            BuildRangeTables(ref entry);
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

        // Flatten a stylesheet into the arrays
        private static unsafe void FlattenStyleSheet(
            ref SelectorAccelerationCacheEntry entry,
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
                            entry.allParts[partIdx] = FlattenPart(part);
                            partIdx++;
                        }

                        int partCount = partIdx - startPartIndex;

                        // Create flattened selector
                        entry.allSelectors[selectorIdx] = new FlattenedSelector
                        {
                            pseudoStateMask = selector.pseudoStateMask,
                            negatedPseudoStateMask = selector.negatedPseudoStateMask,
                            previousRelationship = selector.previousRelationship,
                            startPartIndex = (ushort)startPartIndex,
                            partCount = (ushort)partCount
                        };
                        selectorIdx++;
                    }

                    int selectorCount = selectorIdx - startSelectorIndex;

                    // Skip this complex selector if it has no valid selectors
                    if (selectorCount == 0)
                        continue;

                    // Create range descriptor
                    ref var descriptor = ref entry.allDescriptors[descriptorIdx];
                    descriptor.startSelectorIndex = (ushort)startSelectorIndex;
                    descriptor.selectorCount = (ushort)selectorCount;
                    descriptor.ruleIndex = ruleIdx;
                    descriptor.selectorIndexInRule = selIdx;
                    descriptor.orderInStyleSheet = complexSelector.orderInStyleSheet;
                    descriptor.importedStyleSheetIndex = importedStyleSheetIndex;
                    descriptor.specificity = complexSelector.specificity;
                    descriptor.isSimple = complexSelector.isSimple;

                    // Copy ancestor hashes
                    fixed (SelectorRangeDescriptor* pDesc = &descriptor)
                    {
                        for (int i = 0; i < 4; i++)
                            pDesc->ancestorHashes[i] = complexSelector.ancestorHashes.hashes[i];
                    }

                    // Set tableType and tableKey based on last part
                    var lastSelector = complexSelector.selectors[^1];
                    var lastPart = lastSelector.parts[0];

                    switch (lastPart.type)
                    {
                        case StyleSelectorType.Class:
                            descriptor.tableType = SelectorAccelerationTableType.Class;
                            descriptor.tableKey = lastPart.cachedUniqueStyleStringId;
                            break;
                        case StyleSelectorType.ID:
                            descriptor.tableType = SelectorAccelerationTableType.Name;
                            descriptor.tableKey = lastPart.cachedUniqueStyleStringId;
                            break;
                        case StyleSelectorType.Type:
                            descriptor.tableType = SelectorAccelerationTableType.Type;
                            descriptor.tableKey = lastPart.cachedUniqueStyleStringId;
                            break;
                        case StyleSelectorType.Wildcard:
                            descriptor.tableType = SelectorAccelerationTableType.None;
                            descriptor.tableKey = 1; // 1 for wildcard
                            break;
                        case StyleSelectorType.PseudoClass:
                            descriptor.tableType = SelectorAccelerationTableType.None;
                            descriptor.tableKey = ((lastSelector.pseudoStateMask & (int)PseudoStates.Root) != 0) ? 0 : 1;
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
        private static void BuildRangeTables(ref SelectorAccelerationCacheEntry entry)
        {
            // Initialize to null - only allocate if needed
            entry.nameTable = null;
            entry.typeTable = null;
            entry.classTable = null;
            entry.nonEmptyTablesMask = 0;

            // Count unique keys per table type from sorted descriptors
            // Since descriptors are sorted by (tableType, tableKey, orderInStyleSheet),
            // we can count unique (tableType, tableKey) pairs in a single pass
            int uniqueNameCount = 0;
            int uniqueTypeCount = 0;
            int uniqueClassCount = 0;

            SelectorAccelerationTableType lastTableType = (SelectorAccelerationTableType)(-2);
            int lastTableKey = -1;

            for (int i = 0; i < entry.allDescriptors.Length; i++)
            {
                var descriptor = entry.allDescriptors[i];

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

            int rootStart = -1, rootCount = 0;
            int wildcardStart = -1, wildcardCount = 0;

            for (int i = 0; i < entry.allDescriptors.Length; i++)
            {
                var descriptor = entry.allDescriptors[i];

                if (descriptor.tableType == SelectorAccelerationTableType.None)
                {
                    // Check if it's a :root selector or wildcard
                    var lastSelector = entry.allSelectors[descriptor.startSelectorIndex + descriptor.selectorCount - 1];
                    if ((lastSelector.pseudoStateMask & (int)PseudoStates.Root) != 0)
                    {
                        if (rootStart == -1) rootStart = i;
                        rootCount++;
                    }
                    else
                    {
                        if (wildcardStart == -1) wildcardStart = i;
                        wildcardCount++;
                    }
                }
                else
                {
                    // Lazily allocate table on first use and set mask bit
                    Dictionary<int, (int startIndex, int count)> table;
                    switch (descriptor.tableType)
                    {
                        case SelectorAccelerationTableType.Name:
                            if (entry.nameTable == null)
                            {
                                entry.nameTable = new Dictionary<int, (int startIndex, int count)>(uniqueNameCount);
                                entry.nonEmptyTablesMask |= (1 << (int)SelectorAccelerationTableType.Name);
                            }
                            table = entry.nameTable;
                            break;
                        case SelectorAccelerationTableType.Type:
                            if (entry.typeTable == null)
                            {
                                entry.typeTable = new Dictionary<int, (int startIndex, int count)>(uniqueTypeCount);
                                entry.nonEmptyTablesMask |= (1 << (int)SelectorAccelerationTableType.Type);
                            }
                            table = entry.typeTable;
                            break;
                        case SelectorAccelerationTableType.Class:
                            if (entry.classTable == null)
                            {
                                entry.classTable = new Dictionary<int, (int startIndex, int count)>(uniqueClassCount);
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
                        table[descriptor.tableKey] = (i, 1);
                    }
                    else
                    {
                        // Extend existing range
                        table[descriptor.tableKey] = (range.startIndex, range.count + 1);
                    }
                }
            }

            entry.rootSelectorRange = rootStart >= 0 ? (rootStart, rootCount) : (0, 0);
            entry.wildCardSelectorRange = wildcardStart >= 0 ? (wildcardStart, wildcardCount) : (0, 0);
        }

        // Comparer for sorting descriptors
        private class DescriptorComparer : IComparer<SelectorRangeDescriptor>
        {
            public int Compare(SelectorRangeDescriptor a, SelectorRangeDescriptor b)
            {
                // Sort by: tableType, then tableKey, then orderInStyleSheet
                // Use basic int comparison instead of CompareTo to avoid allocations
                int result = (int)a.tableType - (int)b.tableType;
                if (result != 0) return result;

                result = a.tableKey - b.tableKey;
                if (result != 0) return result;

                return a.orderInStyleSheet - b.orderInStyleSheet;
            }
        }
    }
}
