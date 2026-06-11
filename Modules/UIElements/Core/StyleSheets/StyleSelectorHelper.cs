// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Buffers;
using System.Collections.Generic;
using UnityEngine.Pool;
using Unity.Profiling;
using UnityEngine.Assertions;
using UnityEngine.Bindings;
using Unity.IL2CPP.CompilerServices;

namespace UnityEngine.UIElements.StyleSheets
{
    // Hot-path representation of a matched selector. Lives only inside the style traversal
    // (sorting + ProcessMatchedRules); SelectorMatchRecord is the form used everywhere else.
    internal readonly struct StyleSelectorMatch
    {
        public readonly StyleSheet sheet;
        public readonly int styleSheetIndexInStack;
        public readonly int importedStyleSheetIndex;
        public readonly StyleComplexSelector complexSelector;

        public StyleSelectorMatch(StyleSheet sheet, int styleSheetIndexInStack, int importedStyleSheetIndex, StyleComplexSelector complexSelector)
        {
            this.sheet = sheet;
            this.styleSheetIndexInStack = styleSheetIndexInStack;
            this.importedStyleSheetIndex = importedStyleSheetIndex;
            this.complexSelector = complexSelector;
        }

        // Cached comparison delegate. This is the only allocation-free way to sort a
        // List<StyleSelectorMatch> on Unity's Mono runtime: Sort() via Comparer<T>.Default
        // and Sort(inline lambda) both allocate per call.
        public static readonly Comparison<StyleSelectorMatch> Comparison = (a, b) => Compare(in a, in b);

        // Same comparison, but ref-based — for SpanSort, which avoids the per-comparison
        // struct copy that List<T>.Sort(Comparison<T>) does on this ~24B readonly struct.
        public static readonly RefComparison<StyleSelectorMatch> RefComparison = (ref StyleSelectorMatch a, ref StyleSelectorMatch b) => Compare(in a, in b);

        [Il2CppSetOption(Option.NullChecks, false)]
        static int Compare(in StyleSelectorMatch a, in StyleSelectorMatch b)
        {
            // Cache the chased fields once so we don't re-deref the `in` parameter on every step,
            // and don't call into the StyleSheet property getter twice on the default-sheet branch.
            var sheetA = a.sheet;
            var sheetB = b.sheet;

            // First compare absolute priority (Unity style sheets are always lower priority)
            bool aDefault = sheetA.isDefaultStyleSheet;
            bool bDefault = sheetB.isDefaultStyleSheet;
            if (aDefault != bDefault)
                return aDefault ? -1 : 1;

            var csA = a.complexSelector;
            var csB = b.complexSelector;

            // Use direct int subtraction rather than int.CompareTo / Specificity.CompareTo: Specificity packs
            // three byte-sized scores into a 24-bit non-negative int, and the index fields below are non-negative
            // array indices — no diff can overflow. This skips the per-call IComparable<int>.CompareTo branching.

            // Then use selector specificity according to standards
            int res = (int)csA.specificity - (int)csB.specificity;
            if (res != 0) return res;

            // If they are same, use the order into which stylesheets were added to the element or its parents (later wins)
            res = a.styleSheetIndexInStack - b.styleSheetIndexInStack;
            if (res != 0) return res;

            // If they are the same, use the index in the imported style sheets of the owner style sheets (later wins)
            res = a.importedStyleSheetIndex - b.importedStyleSheetIndex;
            if (res != 0) return res;

            // All else being equal, use the order in the style sheet itself
            return csA.orderInStyleSheet - csB.orderInStyleSheet;
        }
    }

    // Accelerated flattened selector matching for the style system.
    // Uses the pre-built acceleration cache to efficiently match selectors against elements.
    // For legacy selector matching (UQuery with Predicate support), see LegacySelectorHelper.
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    class StyleSelectorHelper<TProfilerType> where TProfilerType : struct, IStyleProfiler
    {
        // This internal flag can be enabled to validate that the Bloom filter never rejects cases where
        // the exhaustive search returns a valid match. This is disabled by default, and is enabled from
        // styling unit tests.
        internal static bool s_VerifyBloomIntegrity = false;

        // Reverse lookup: Get StyleComplexSelector from descriptor
        static StyleComplexSelector GetComplexSelector(in SelectorRangeDescriptor descriptor, in SelectorAccelerationCacheEntry cacheEntry)
        {
            var styleSheet = descriptor.importedStyleSheetIndex == -1
                ? cacheEntry.ownerStyleSheet
                : cacheEntry.ownerStyleSheet.flattenedRecursiveImports[descriptor.importedStyleSheetIndex];

            return styleSheet.rules[descriptor.ruleIndex].complexSelectors[descriptor.selectorIndexInRule];
        }

        // Bloom filter check using descriptor hashes
        static unsafe bool IsDescriptorCandidate(in SelectorRangeDescriptor descriptor, AncestorFilter ancestorFilter)
        {
            fixed (SelectorRangeDescriptor* pDesc = &descriptor)
            {
                return ancestorFilter.IsCandidate(pDesc->ancestorHashes);
            }
        }

        // Match element against flattened selector (StyleSheet selectors only, no predicates)
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        static bool MatchesSelectorFlat(
            VisualElement element,
            in FlattenedSelector flatSelector,
            ReadOnlySpan<FlattenedSelectorPart> selectorParts,
            out PseudoStates triggerPseudoMask,
            out PseudoStates dependencyPseudoMask)
        {
            bool match = true;

            for (int i = 0; i < selectorParts.Length && match; i++)
            {
                var part = selectorParts[i];
                switch (part.type)
                {
                    case StyleSelectorType.Wildcard:
                        break;
                    case StyleSelectorType.Class:
                    {
                        var classList = element.GetClassesForIteration();
                        var classIds = classList.GetClassIds();
                        match = false;
                        for (int j = 0; j < classIds.Length; j++)
                        {
                            if (classIds[j] == part.uniqueStringId)
                            {
                                match = true;
                                break;
                            }
                        }
                        break;
                    }
                    case StyleSelectorType.ID:
                    {
                        match = element.nameId == part.uniqueStringId;
                        break;
                    }
                    case StyleSelectorType.Type:
                    {
                        match = element.typeNameId == part.uniqueStringId;
                        break;
                    }
                    case StyleSelectorType.PseudoClass:
                        // Selectors with invalid pseudo states should be rejected
                        if (flatSelector.pseudoStateMask == StyleSelector.InvalidPseudoStateMask
                            || flatSelector.negatedPseudoStateMask == StyleSelector.InvalidPseudoStateMask)
                        {
                            match = false;
                        }
                        break;
                    case StyleSelectorType.Predicate:
                        // Predicates should NEVER appear in stylesheet selectors
                        Debug.LogError("Predicate found in stylesheet selector - this should never happen!");
                        match = false;
                        break;
                    default:
                        match = false;
                        break;
                }
            }

            int triggerPseudoStateMask = 0;
            int dependencyPseudoStateMask = 0;
            bool saveMatch = match;

            if (saveMatch && flatSelector.pseudoStateMask != 0)
            {
                match = (flatSelector.pseudoStateMask & (int)element.pseudoStates) == flatSelector.pseudoStateMask;

                if (match)
                {
                    dependencyPseudoStateMask = flatSelector.pseudoStateMask;
                }
                else
                {
                    triggerPseudoStateMask = flatSelector.pseudoStateMask;
                }
            }

            if (saveMatch && flatSelector.negatedPseudoStateMask != 0)
            {
                match &= (flatSelector.negatedPseudoStateMask & ~(int)element.pseudoStates) == flatSelector.negatedPseudoStateMask;

                if (match)
                {
                    triggerPseudoStateMask |= flatSelector.negatedPseudoStateMask;
                }
                else
                {
                    dependencyPseudoStateMask |= flatSelector.negatedPseudoStateMask;
                }
            }

            triggerPseudoMask = (PseudoStates)triggerPseudoStateMask;
            dependencyPseudoMask = (PseudoStates)dependencyPseudoStateMask;
            return match;
        }

        // Match right-to-left using flattened data (StyleSheet selectors only)
        static bool MatchRightToLeftFlat(
            VisualElement element,
            in SelectorAccelerationCacheEntry cacheEntry,
            ReadOnlySpan<FlattenedSelector> descriptorSelectors,
            bool applyPseudoMasks)
        {
            var current = element;
            int nextIndex = descriptorSelectors.Length - 1;
            VisualElement saved = null;
            int savedIdx = -1;

            while (nextIndex >= 0)
            {
                if (current == null)
                    break;

                ref readonly var flatSelector = ref descriptorSelectors[nextIndex];

                bool success = MatchesSelectorFlat(current, flatSelector, cacheEntry.PartsFor(flatSelector),
                    out var triggerPseudoMask, out var dependencyPseudoMask);
                if (applyPseudoMasks)
                {
                    current.triggerPseudoMask |= triggerPseudoMask;
                    current.dependencyPseudoMask |= dependencyPseudoMask;
                }

                if (!success)
                {
                    // if we have a descendant relationship, keep trying on the parent
                    // i.e., "div span", div failed on this element, try on the parent
                    // happens earlier than the backtracking saving below
                    if (nextIndex < descriptorSelectors.Length - 1 &&
                        descriptorSelectors[nextIndex + 1].previousRelationship == StyleSelectorRelationship.Descendent)
                    {
                        current = current.parent;
                        continue;
                    }

                    // otherwise, if there's a previous relationship, it's a 'child' one. backtrack from the saved point and try again
                    // ie.  for "#x > .a .b", #x failed, backtrack to .a on the saved element
                    if (saved != null)
                    {
                        current = saved;
                        nextIndex = savedIdx;
                        continue;
                    }

                    break;
                }

                // backtracking save
                // for "a > b c": we're considering the b matcher. c's previous relationship is Descendent
                // save the current element parent to try to match b again
                if (nextIndex < descriptorSelectors.Length - 1
                    && descriptorSelectors[nextIndex + 1].previousRelationship == StyleSelectorRelationship.Descendent)
                {
                    saved = current.parent;
                    savedIdx = nextIndex;
                }

                // from now, the element is a match
                if (--nextIndex < 0)
                {
                    return true;
                }
                current = current.parent;
            }
            return false;
        }

        // Test range of descriptors in sorted allDescriptors array
        static void TestSelectorListFlat(
            ReadOnlySpan<SelectorRangeDescriptor> descriptors,
            in SelectorAccelerationCacheEntry cacheEntry,
            List<StyleSelectorMatch> matchedSelectors,
            StyleMatchingContext context,
            int currentStyleSheetIndexInStack)
        {
            ref TProfilerType profiler = ref StyleProfilerStorage<TProfilerType>.InstanceByRef;

            for (int i = 0; i < descriptors.Length; i++)
            {
                // ref readonly avoids copying the 52B descriptor per iteration on the matcher hot path.
                ref readonly var descriptor = ref descriptors[i];
                StyleComplexSelector currentComplexSelector = default;

                // For profiling, we need the actual StyleComplexSelector
                currentComplexSelector = GetComplexSelector(descriptor, cacheEntry);
                profiler.BeginMatchingSelector(currentComplexSelector);

                bool isCandidate = true;
                bool isMatchRightToLeft = false;

                // Bloom prefilter only matters for complex selectors (with ancestor relationships).
                // Single-selector descriptors are equivalent to the legacy isSimple == true case.
                if (descriptor.selectorCount > 1)
                {
                    isCandidate = IsDescriptorCandidate(descriptor, context.ancestorFilter);
                }

                if (isCandidate || s_VerifyBloomIntegrity)
                {
                    isMatchRightToLeft = MatchRightToLeftFlat(
                        context.currentElement,
                        in cacheEntry,
                        cacheEntry.SelectorsFor(descriptor),
                        context.applyPseudoMasks);
                }

                if (s_VerifyBloomIntegrity)
                {
                    Assert.IsTrue(isCandidate || !isMatchRightToLeft, "The Bloom filter returned a false negative match.");
                }

                if (isMatchRightToLeft)
                {
                    // Only do reverse lookup when we have a match (if not already done for profiling)
                    if (currentComplexSelector == default)
                        currentComplexSelector = GetComplexSelector(descriptor, cacheEntry);

                    if (descriptor.importedStyleSheetIndex > -1)
                    {
                        var sheet = context.GetStyleSheetAt(currentStyleSheetIndexInStack);
                        Debug.Assert(sheet.flattenedRecursiveImports[descriptor.importedStyleSheetIndex] == currentComplexSelector.rule.styleSheet,
                            "StyleRangeDescriptor is not consistent");
                    }
                    matchedSelectors.Add(new StyleSelectorMatch(
                        currentComplexSelector.rule.styleSheet,
                        currentStyleSheetIndexInStack,
                        descriptor.importedStyleSheetIndex,
                        currentComplexSelector
                    ));
                }

                profiler.EndMatchingSelector(currentComplexSelector, isMatchRightToLeft, isCandidate);
            }
        }

        // Fast lookup using int key in range-based table
        static void FastLookupFlat(
            Dictionary<int, DescriptorRange> table,
            in SelectorAccelerationCacheEntry cacheEntry,
            List<StyleSelectorMatch> matchedSelectors,
            StyleMatchingContext context,
            int uniqueStringId,
            int currentStyleSheetIndexInStack)
        {
            if (table != null && table.TryGetValue(uniqueStringId, out var range))
            {
                TestSelectorListFlat(
                    cacheEntry.DescriptorsFor(range),
                    in cacheEntry,
                    matchedSelectors,
                    context,
                    currentStyleSheetIndexInStack);
            }
        }

        // Helper to get the appropriate table by type
        static Dictionary<int, DescriptorRange> GetFlatTableByType(in SelectorAccelerationCacheEntry cacheEntry, SelectorAccelerationTableType type)
        {
            return type switch
            {
                SelectorAccelerationTableType.Name => cacheEntry.nameTable,
                SelectorAccelerationTableType.Type => cacheEntry.typeTable,
                SelectorAccelerationTableType.Class => cacheEntry.classTable,
                _ => null
            };
        }

        public static void FindMatches(StyleMatchingContext context, List<StyleSelectorMatch> matchedSelectors)
        {
            // To support having the root pseudo states set for style sheets added onto an element
            // we need to find which sheets belongs to the element itself.
            VisualElement element = context.currentElement;
            int parentSheetIndex =  context.styleSheetCount - 1;
            if (element.styleSheetList != null)
            {
                // The number of style sheet for an element is the count of the styleSheetList + all imported style sheet
                int elementSheetCount = element.styleSheetList.Count;
                for (var i = 0; i < element.styleSheetList.Count; i++)
                {
                    var elementSheet = element.styleSheetList[i];
                    if (elementSheet.flattenedRecursiveImports != null)
                        elementSheetCount += elementSheet.flattenedRecursiveImports.Count;
                }

                parentSheetIndex -= elementSheetCount;
            }

            FindMatches(context, matchedSelectors, parentSheetIndex);
        }

        struct SelectorWorkItem
        {
            public SelectorAccelerationTableType type;
            public int uniqueStringId;

            public SelectorWorkItem(SelectorAccelerationTableType type, int uniqueStringId)
            {
                this.type = type;
                this.uniqueStringId = uniqueStringId;
            }
        }

        public static void FindMatches(StyleMatchingContext context, List<StyleSelectorMatch> matchedSelectors, int parentSheetIndex)
        {
            Debug.Assert(matchedSelectors.Count == 0);
            Debug.Assert(context.currentElement != null, "context.currentElement != null");

            ref TProfilerType profiler = ref StyleProfilerStorage<TProfilerType>.InstanceByRef;
            profiler.BeginMatchingElement(context.currentElement);
            var toggleRoot = false;
            var processedStyleSheets = HashSetPool<StyleSheet>.Get();
            SelectorWorkItem[] rentedArray = null;

            try
            {
                var element = context.currentElement;
                var classList = element.GetClassesForIteration();
                var classIds = classList.GetClassIds();

                // Build work items with UniqueStyleString IDs
                // Size = classIds.Length + 1 (type) + 1 (name, if valid nameId >= 0)
                int workItemsCount = classIds.Length + 1 + (element.nameId >= 0 ? 1 : 0);

                // Use stackalloc for small counts, ArrayPool for large counts to prevent stack overflow
                const int StackAllocThreshold = 64;
                Span<SelectorWorkItem> workItems = workItemsCount <= StackAllocThreshold
                    ? stackalloc SelectorWorkItem[workItemsCount]
                    : (rentedArray = ArrayPool<SelectorWorkItem>.Shared.Rent(workItemsCount)).AsSpan(0, workItemsCount);

                int idx = 0;
                // Use cached typeNameId from VisualElement
                workItems[idx++] = new SelectorWorkItem(SelectorAccelerationTableType.Type, element.typeNameId);

                // Only add to work items if name exists in UniqueStyleString system (nameId >= 0)
                if (element.nameId >= 0)
                {
                    workItems[idx++] = new SelectorWorkItem(SelectorAccelerationTableType.Name, element.nameId);
                }

                for (int i = 0; i < classIds.Length; i++)
                {
                    workItems[idx++] = new SelectorWorkItem(SelectorAccelerationTableType.Class, classIds[i]);
                }

                for (var i = context.styleSheetCount - 1; i >= 0; --i)
                {
                    var styleSheet = context.GetStyleSheetAt(i);

                    if (!processedStyleSheets.Add(styleSheet))
                        continue;

                    styleSheet.RebuildIfNecessary();

                    SelectorAccelerationCacheEntry accelerationCacheEntry = context.GetCacheEntryAt(i);

                    profiler.BeginMatchingStyleSheet(styleSheet, accelerationCacheEntry);

                    // If the sheet is added on the element consider it as :root
                    if (i > parentSheetIndex)
                    {
                        element.pseudoStates |= PseudoStates.Root;
                        toggleRoot = true;
                    }
                    else
                        element.pseudoStates &= ~PseudoStates.Root;

                    for (int j = 0; j < workItemsCount; j++)
                    {
                        var item = workItems[j];

                        if ((accelerationCacheEntry.nonEmptyTablesMask & (1 << (int)item.type)) == 0)
                            continue;

                        var table = GetFlatTableByType(in accelerationCacheEntry, item.type);
                        FastLookupFlat(table, in accelerationCacheEntry, matchedSelectors, context, item.uniqueStringId, i);
                    }

                    // Handle :root selectors
                    if (toggleRoot && accelerationCacheEntry.rootSelectorRange.count > 0)
                    {
                        TestSelectorListFlat(
                            accelerationCacheEntry.DescriptorsFor(accelerationCacheEntry.rootSelectorRange),
                            in accelerationCacheEntry,
                            matchedSelectors,
                            context,
                            i);
                    }

                    // Handle wildcard selectors
                    if (accelerationCacheEntry.wildCardSelectorRange.count > 0)
                    {
                        TestSelectorListFlat(
                            accelerationCacheEntry.DescriptorsFor(accelerationCacheEntry.wildCardSelectorRange),
                            in accelerationCacheEntry,
                            matchedSelectors,
                            context,
                            i);
                    }

                    profiler.EndMatchingStyleSheet(styleSheet);
                }

                if (toggleRoot)
                    element.pseudoStates &= ~PseudoStates.Root;
            }
            finally
            {
                HashSetPool<StyleSheet>.Release(processedStyleSheets);
                if (rentedArray != null)
                    ArrayPool<SelectorWorkItem>.Shared.Return(rentedArray);
            }
        }
    }
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    class StyleSelectorHelper : StyleSelectorHelper<NoOpStyleProfiler> { }
}
