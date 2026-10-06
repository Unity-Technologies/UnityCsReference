// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Pool;
using Unity.Profiling;
using UnityEngine.Bindings;
using Unity.IL2CPP.CompilerServices;

namespace UnityEngine.UIElements.StyleSheets
{
    // Hot-path representation of a matched selector. Lives only inside the style traversal
    // (sorting + ProcessMatchedRules); SelectorMatchRecord is the form used everywhere else.
    internal readonly struct StyleSelectorMatch
    {
        // Tier rank sits above Specificity's three byte scores, so tier and specificity compare
        // as one int. Packed values stay below 2^26, keeping the raw subtractions overflow-free.
        internal const int tierRankShift = 24;

        public readonly StyleSheet sheet;
        public readonly int styleSheetIndexInStack;
        public readonly int importedStyleSheetIndex;
        public readonly StyleComplexSelector complexSelector;

        // (tier rank << tierRankShift) | specificity — resolved per use in the constructor
        public readonly int sortKey;

        // Effective tier of this match, recovered from the packed sort key.
        public StyleSheetPriority tier => GetTierPriority(sortKey >> tierRankShift);

        public StyleSelectorMatch(StyleSheet sheet, int styleSheetIndexInStack, int importedStyleSheetIndex, StyleComplexSelector complexSelector, int entryTierRank)
        {
            this.sheet = sheet;
            this.styleSheetIndexInStack = styleSheetIndexInStack;
            this.importedStyleSheetIndex = importedStyleSheetIndex;
            this.complexSelector = complexSelector;

            // The tier is a property of the use, not of the asset: the same sheet can rank
            // differently in different stacks, so the rank comes from the stack entry that pulled
            // the rule in (entryTierRank) rather than from global sheet state. The one sticky tier
            // is Builtin — it holds the builtin theme copy embedded inside a user theme at Builtin
            // rank while the surrounding theme's own rules rank UserTheme. Everything else,
            // including a theme imported by a regular sheet (an unsupported shape the importer
            // warns about), ranks as ordinary imported content of its entry.
            int tierRank = sheet.priority == StyleSheetPriority.Builtin
                ? GetTierRank(StyleSheetPriority.Builtin)
                : entryTierRank;
            sortKey = (tierRank << tierRankShift) | (int)complexSelector.specificity;
        }

        internal static int GetTierRank(StyleSheetPriority priority)
        {
            // Default (unset) ranks highest: regular sheets override themes, themes override builtin.
            // No discard arm: adding a StyleSheetPriority value must fail compilation here (CS8509)
            // so the new tier gets an explicit rank. CS8524 (a cast like (StyleSheetPriority)3) is
            // irrelevant for this internal enum and would force the discard arm back.
#pragma warning disable CS8524
            return priority switch
            {
                StyleSheetPriority.Builtin => 0,
                StyleSheetPriority.UserTheme => 1,
                StyleSheetPriority.Default => 2,
            };
#pragma warning restore CS8524
        }

        internal static StyleSheetPriority GetTierPriority(int tierRank)
        {
            return tierRank switch
            {
                0 => StyleSheetPriority.Builtin,
                1 => StyleSheetPriority.UserTheme,
                2 => StyleSheetPriority.Default,
                _ => throw new ArgumentOutOfRangeException(nameof(tierRank)),
            };
        }

        // Cached comparison delegate. This is the only allocation-free way to sort a
        // List<StyleSelectorMatch> on Unity's Mono runtime: Sort() via Comparer<T>.Default
        // and Sort(inline lambda) both allocate per call.
        [NoAutoStaticsCleanup] // cached delegate; safe to persist
        public static readonly Comparison<StyleSelectorMatch> Comparison = (a, b) => Compare(in a, in b);

        // Same comparison, but ref-based — for SpanSort, which avoids the per-comparison
        // struct copy that List<T>.Sort(Comparison<T>) does on this ~24B readonly struct.
        [NoAutoStaticsCleanup] // cached delegate; safe to persist
        public static readonly RefComparison<StyleSelectorMatch> RefComparison = (ref StyleSelectorMatch a, ref StyleSelectorMatch b) => Compare(in a, in b);

        [Il2CppSetOption(Option.NullChecks, false)]
        static int Compare(in StyleSelectorMatch a, in StyleSelectorMatch b)
        {
            // Use direct int subtraction rather than int.CompareTo: all operands are non-negative
            // and below 2^26 (packed sort key) or plain array indices — no diff can overflow.

            // Tier first, then selector specificity — one comparison, thanks to the packed key
            int res = a.sortKey - b.sortKey;
            if (res != 0) return res;

            // If they are same, use the order into which stylesheets were added to the element or its parents (later wins)
            res = a.styleSheetIndexInStack - b.styleSheetIndexInStack;
            if (res != 0) return res;

            // If they are the same, break the tie by source within the owner style sheet: the owner's own rules
            // (index -1) always rank above any sheet it imports, and among imports the later one wins. Map -1 to
            // int.MaxValue so it sorts highest; both operands are then in [0, int.MaxValue], so the subtraction
            // can't overflow.
            int aImportIndex = a.importedStyleSheetIndex < 0 ? int.MaxValue : a.importedStyleSheetIndex;
            int bImportIndex = b.importedStyleSheetIndex < 0 ? int.MaxValue : b.importedStyleSheetIndex;
            res = aImportIndex - bImportIndex;
            if (res != 0) return res;

            // All else being equal, use the order in the style sheet itself
            return a.complexSelector.orderInStyleSheet - b.complexSelector.orderInStyleSheet;
        }
    }

    // Accelerated flattened selector matching for the style system.
    // Uses the pre-built acceleration cache to efficiently match selectors against elements.
    // For legacy selector matching (UQuery with Predicate support), see LegacySelectorHelper.
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    partial class StyleSelectorHelper<TProfilerType> where TProfilerType : struct, IStyleProfiler
    {
        // This internal flag can be enabled to validate that the Bloom filter never rejects cases where
        // the exhaustive search returns a valid match. This is disabled by default, and is enabled from
        // styling unit tests. The check itself runs inside the native matcher, so the flag forwards to
        // it and the native path stays in use while verifying.
        internal static bool s_VerifyBloomIntegrity
        {
            get => NativeSelectorMatcher.verifyBloomIntegrity;
            set => NativeSelectorMatcher.verifyBloomIntegrity = value;
        }

        // Match one style sheet against the current element — one binding crossing per
        // element x sheet. The native matcher derives the work items from the element's
        // VisualElementSelectorData (type, name, class ids), binary-searches the descriptor
        // table blocks, runs the Bloom prefilter and right-to-left matcher, and covers the
        // :root/wildcard ranges. Only the matched descriptor indices come back; this shim
        // materializes the StyleSelectorMatch records. While profiling (editor-only), the
        // profiler supplies a per-descriptor stats buffer the native loop fills in the same
        // pass, and the Begin/EndSelectorMatching bracket keeps matching time (stats-collection
        // overhead included) out of the profiler's sheet self time — the bracket closes right
        // after the call so match materialization below is attributed to the sheet.
        static unsafe void MatchSheetNative(
            in SelectorAccelerationCacheEntry cacheEntry,
            List<StyleSelectorMatch> matchedSelectors,
            StyleMatchingContext context,
            int currentStyleSheetIndexInStack,
            bool testRootRange,
            int entryTierRank)
        {
            var core = cacheEntry.m_Core;
            if (core == null || core->allDescriptorsCount == 0)
                return;

            int descriptorCount = core->allDescriptorsCount;

            // NoOp returns null with no side effects, so the JIT folds the profiler branches
            // below out of the unprofiled matcher.
            ref TProfilerType profiler = ref StyleProfilerStorage<TProfilerType>.InstanceByRef;
            SelectorMatchStatsInfo[] statsBuffer = profiler.BeginSelectorMatching(in cacheEntry);

            // A descriptor belongs to exactly one (tableType, tableKey) bucket and one call
            // touches disjoint buckets, so it can match at most once: the sheet's descriptor
            // count bounds the output and the context's grow-only scratch holds it.
            var matchedIndices = context.GetMatchedIndicesScratch(descriptorCount);
            int matchedCount;
            fixed (CountingBloomFilter* pAncestorFilter = &context.ancestorFilter.filter)
            fixed (int* pMatchedIndices = matchedIndices)
            fixed (SelectorMatchStatsInfo* pStats = statsBuffer) // null buffer pins to null
            {
                matchedCount = NativeSelectorMatcher.MatchSheetFlat(
                    context.currentElement.selectorDataPtr,
                    core,
                    pAncestorFilter,
                    context.applyPseudoMasks,
                    testRootRange,
                    pMatchedIndices,
                    pStats);
            }

            if (statsBuffer != null)
                profiler.EndSelectorMatching();

            var allDescriptors = cacheEntry.allDescriptors;
            for (int i = 0; i < matchedCount; i++)
            {
                ref readonly var descriptor = ref allDescriptors[matchedIndices[i]];
                var complexSelector = cacheEntry.GetComplexSelector(in descriptor);

                if (descriptor.importedStyleSheetIndex > -1)
                {
                    var sheet = context.GetStyleSheetAt(currentStyleSheetIndexInStack);
                    Debug.Assert(sheet.flattenedRecursiveImports[descriptor.importedStyleSheetIndex] == complexSelector.rule.styleSheet,
                        "StyleRangeDescriptor is not consistent");
                }
                matchedSelectors.Add(new StyleSelectorMatch(
                    complexSelector.rule.styleSheet,
                    currentStyleSheetIndexInStack,
                    descriptor.importedStyleSheetIndex,
                    complexSelector,
                    entryTierRank
                ));
            }
        }

        public static void FindMatches(StyleMatchingContext context, List<StyleSelectorMatch> matchedSelectors)
        {
            // To support having the root pseudo states set for style sheets added onto an element
            // we need to find which sheets belongs to the element itself.
            VisualElement element = context.currentElement;
            int parentSheetIndex =  context.styleSheetCount - 1;
            var elementSheets = element.styleSheetList;
            if (elementSheets != null)
            {
                // The number of style sheet for an element is the count of the styleSheetList + all imported style sheet
                int elementSheetCount = elementSheets.Count;
                for (var i = 0; i < elementSheets.Count; i++)
                {
                    var elementSheet = elementSheets[i];
                    if (elementSheet.flattenedRecursiveImports != null)
                        elementSheetCount += elementSheet.flattenedRecursiveImports.Count;
                }

                parentSheetIndex -= elementSheetCount;
            }

            FindMatches(context, matchedSelectors, parentSheetIndex);
        }

        public static void FindMatches(StyleMatchingContext context, List<StyleSelectorMatch> matchedSelectors, int parentSheetIndex)
        {
            Debug.Assert(matchedSelectors.Count == 0);
            Debug.Assert(context.currentElement != null, "context.currentElement != null");


            ref TProfilerType profiler = ref StyleProfilerStorage<TProfilerType>.InstanceByRef;
            profiler.BeginMatchingElement(context.currentElement);
            var toggleRoot = false;
            var processedStyleSheets = HashSetPool<StyleSheet>.Get();

            try
            {
                var element = context.currentElement;

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

                    MatchSheetNative(in accelerationCacheEntry, matchedSelectors, context, i, toggleRoot,
                        StyleSelectorMatch.GetTierRank(styleSheet.priority));

                    profiler.EndMatchingStyleSheet(styleSheet);
                }

                if (toggleRoot)
                    element.pseudoStates &= ~PseudoStates.Root;
            }
            finally
            {
                HashSetPool<StyleSheet>.Release(processedStyleSheets);
            }
        }

    }
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    class StyleSelectorHelper : StyleSelectorHelper<NoOpStyleProfiler> { }
}
