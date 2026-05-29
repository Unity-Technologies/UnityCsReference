// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Profiling;
using UnityEngine.Bindings;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static class StyleCache
    {
        // hash of a set of rules to a specified style data
        // the same set of rules will give the same specified styles, caching the hash of the matching rules before
        // resolving styles allows to skip the resolve part when an existing style data already exists
        private static Dictionary<Int64, ComputedStyle> s_ComputedStyleCache = new Dictionary<Int64, ComputedStyle>();
        private static Dictionary<int, StyleVariableContext> s_StyleVariableContextCache = new Dictionary<int, StyleVariableContext>();

        // Cached values for TransitionData converted into a ComputedTransitionProperty array for easier access
        private static Dictionary<int, ComputedTransitionProperty[]> s_ComputedTransitionsCache = new Dictionary<int, ComputedTransitionProperty[]>();

        static StyleCache()
        {
            UnloadingUtility.SubscribeToUnloading(UnloadingSubscriber.StyleCache, ClearStyleCache);
        }

        public static bool TryGetValue(Int64 hash, out ComputedStyle data)
        {
            return s_ComputedStyleCache.TryGetValue(hash, out data);
        }

        public static void SetValue(Int64 hash, ref ComputedStyle data)
        {
            // No need to acquire ComputedStyle here because it's freshly created and already has the correct ref count
            s_ComputedStyleCache[hash] = data;
        }

        public static bool TryGetValue(int hash, out StyleVariableContext data)
        {
            return s_StyleVariableContextCache.TryGetValue(hash, out data);
        }

        public static void SetValue(int hash, StyleVariableContext data)
        {
            s_StyleVariableContextCache[hash] = data;
        }

        public static bool TryGetValue(int hash, out ComputedTransitionProperty[] data)
        {
            return s_ComputedTransitionsCache.TryGetValue(hash, out data);
        }

        public static void SetValue(int hash, ComputedTransitionProperty[] data)
        {
            s_ComputedTransitionsCache[hash] = data;
        }

        public static void ClearStyleCache()
        {
            foreach (var kvp in s_ComputedStyleCache)
                kvp.Value.Release();

            s_ComputedStyleCache.Clear();
            s_StyleVariableContextCache.Clear();
            s_ComputedTransitionsCache.Clear();
        }
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class VisualTreeStyleUpdater : VisualTreeStyleUpdater<VisualTreeStyleUpdaterTraversal, NoOpStyleProfiler>
    {
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class VisualTreeStyleUpdater<TTraversal, TProfiler> : BaseVisualTreeUpdater
        where TTraversal : VisualTreeStyleUpdaterTraversal<TProfiler>, new()
        where TProfiler : struct, IStyleProfiler
    {
        private HashSet<VisualElement> m_TransitionPropertyUpdateList = new HashSet<VisualElement>();
        private uint m_Version = 0;
        private uint m_LastVersion = 0;

        private TTraversal m_StyleContextHierarchyTraversal = new ();

        public TTraversal traversal
        {
            get => m_StyleContextHierarchyTraversal;
            set
            {
                m_StyleContextHierarchyTraversal = value;
                panel?.visualTree.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Layout | VersionChangeType.Transform);
            }
        }

        private static readonly string s_Description = "UIElements.UpdateStyle";
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(ProfilerCategory.UIToolkit, s_Description);
        public override ProfilerMarker profilerMarker => s_ProfilerMarker;

        public void DirtyStyleSheets()
        {
            visualTree.IncrementVersion(VersionChangeType.StyleSheet); // dirty all styles
        }


        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if ((versionChangeType & (VersionChangeType.StyleSheet | VersionChangeType.TransitionProperty)) == 0)
                return;

            ++m_Version;

            if ((versionChangeType & VersionChangeType.StyleSheet) != 0)
            {
                m_StyleContextHierarchyTraversal.AddChangedElement(ve);
            }

            if ((versionChangeType & VersionChangeType.TransitionProperty) != 0)
            {
                m_TransitionPropertyUpdateList.Add(ve);
            }
        }

        public override void Update()
        {
            if (m_Version == m_LastVersion)
                return;

            m_LastVersion = m_Version;
            ApplyStyles();

            // Style resolution replaces computed styles wholesale via SetComputedStyle, which
            // overwrites values that UIAnimationBinder applied via ApplyPropertyAnimation.
            // CSS transitions are handled by ForceUpdateTransitions inside TraverseRecursive,
            // but animation binders have no equivalent. Re-apply the last sampled binder values
            // so they survive style re-resolution.
            var animSystem = panel?.GetUpdater(VisualTreeUpdatePhase.Animation) as VisualElementAnimationSystem;
            if (animSystem != null && animSystem.hasActiveAnimationBinders)
            {
                animSystem.ReapplyAnimationBinderValues();
            }

            m_StyleContextHierarchyTraversal.Clear();

            foreach (var ve in m_TransitionPropertyUpdateList)
            {
                // Allow for transitions to be cancelled if matching transition property was removed.
                if (ve.hasRunningAnimations || ve.hasCompletedAnimations)
                {
                    ComputedTransitionUtils.UpdateComputedTransitions(ref ve.computedStyle, out var computedTransitions);
                    m_StyleContextHierarchyTraversal.CancelAnimationsWithNoTransitionProperty(computedTransitions, ve, ref ve.computedStyle);
                }
            }
            m_TransitionPropertyUpdateList.Clear();
        }

        protected bool disposed { get; private set; }
        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                m_StyleContextHierarchyTraversal.Clear();

            disposed = true;
        }

        protected void ApplyStyles()
        {
            Debug.Assert(visualTree.panel != null);
            m_StyleContextHierarchyTraversal.PrepareTraversal(panel, panel.scaledPixelsPerPoint);
            m_StyleContextHierarchyTraversal.Traverse(visualTree);
        }
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    class StyleMatchingContext
    {
        private List<StyleSheet> m_StyleSheetStack;
        private List<SelectorAccelerationCacheEntry> m_CacheEntryStack;

        public int styleSheetCount => m_StyleSheetStack.Count;

        public StyleVariableContext variableContext;
        public VisualElement currentElement;
        // When true, MatchRightToLeft writes triggerPseudoMask/dependencyPseudoMask back onto
        // the visited elements. The cold-path callers (inspector, debugger) pass false to leave
        // element state untouched.
        public readonly bool applyPseudoMasks;
        public AncestorFilter ancestorFilter = new AncestorFilter();

        public StyleMatchingContext(bool applyPseudoMasks)
        {
            m_StyleSheetStack = new List<StyleSheet>();
            m_CacheEntryStack = new List<SelectorAccelerationCacheEntry>();
            variableContext = StyleVariableContext.none;
            currentElement = null;
            this.applyPseudoMasks = applyPseudoMasks;
        }

        public void AddStyleSheet(StyleSheet sheet)
        {
            if (sheet == null)
                return;

            m_StyleSheetStack.Add(sheet);
            m_CacheEntryStack.Add(SelectorAccelerationCache.shared.GetOrCreate(sheet));
        }

        public void RemoveStyleSheetRange(int index, int count)
        {
            m_StyleSheetStack.RemoveRange(index, count);
            m_CacheEntryStack.RemoveRange(index, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StyleSheet GetStyleSheetAt(int index)
        {
            return m_StyleSheetStack[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SelectorAccelerationCacheEntry GetCacheEntryAt(int index)
        {
            return m_CacheEntryStack[index];
        }
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class VisualTreeStyleUpdaterTraversal : VisualTreeStyleUpdaterTraversal<NoOpStyleProfiler> { }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class VisualTreeStyleUpdaterTraversal<TStyleProfiler> : HierarchyTraversal where TStyleProfiler : struct, IStyleProfiler
    {
        private StyleVariableContext m_ProcessVarContext = new StyleVariableContext();

        private List<StyleSelectorMatch> m_TempMatchResults = new List<StyleSelectorMatch>();

        private List<VisualElement> m_CustomStyleResolvedElements;

        private float currentPixelsPerPoint { get; set; } = 1.0f;
        // Re-entrancy protection: set to true during style traversal, false before event dispatch
        private bool m_IsApplyingStyles;
        private List<VisualElement> m_ApplyStyleUpdateList = new List<VisualElement>();

        StyleMatchingContext m_StyleMatchingContext = new StyleMatchingContext(applyPseudoMasks: true);
        StylePropertyReader m_StylePropertyReader = new StylePropertyReader();

        private BaseVisualElementPanel currentPanel { get; set; }

        public StyleMatchingContext styleMatchingContext => m_StyleMatchingContext;

        public void PrepareTraversal(BaseVisualElementPanel panel, float pixelsPerPoint)
        {
            currentPanel = panel;
            currentPixelsPerPoint = pixelsPerPoint;
        }

        public override void Traverse(VisualElement element)
        {
            m_CustomStyleResolvedElements = VisualElementListPool.Get();

            // Set flag to true during traversal - any AddChangedElement() calls will be queued
            m_IsApplyingStyles = true;

            base.Traverse(element);

            // Set flag to false before dispatching events - user callbacks can directly modify elements
            m_IsApplyingStyles = false;


            // Process elements that were queued during traversal (before m_IsApplyingStyles was set to false)
            // Mark them dirty for next frame
            ProcessQueuedElements();

            try
            {
                // Dispatch accumulated CustomStyleResolvedEvents
                foreach (var elt in m_CustomStyleResolvedElements)
                {
                    using (var evt = CustomStyleResolvedEvent.GetPooled())
                    {
                        EventDispatchUtilities.SendEventDirectlyToTarget(evt, currentPanel, elt);
                    }
                }
            }
            finally
            {
                VisualElementListPool.Release(m_CustomStyleResolvedElements);
                m_CustomStyleResolvedElements = null;
            }
        }

        public void AddChangedElement(VisualElement ve)
        {
            // If we're inside style traversal, queue the element to avoid re-entrancy
            if (m_IsApplyingStyles)
            {
                m_ApplyStyleUpdateList.Add(ve);
            }
            else
            {
                ve.stylesDirty = true;
                PropagateToParents(ve);
            }
        }

        public void ProcessQueuedElements()
        {
            // Move queued elements to changed list for next frame
            foreach (var ve in m_ApplyStyleUpdateList)
            {
                // Since list may contain duplicate, skip if already dirty
                // We assume ProcessQueuedElements() is called on a fully clean tree right after style updated
                // So in theory no desync between stylesDirty and stylesAncestorOfDirty is possible
                if (ve.stylesDirty)
                    continue;
                ve.stylesDirty = true;
                PropagateToParents(ve);
            }
            m_ApplyStyleUpdateList.Clear();
        }

        public void Clear()
        {
            // Note: we don't need to clear flags here because they're cleared during traversal
            m_TempMatchResults.Clear();
        }

        private void PropagateToParents(VisualElement ve)
        {
            var parent = ve.hierarchy.parent;
            while (parent != null)
            {
                if (parent.stylesAncestorOfDirty)
                {
                    // Already marked, no need to continue up the chain
                    break;
                }

                parent.stylesAncestorOfDirty = true;
                parent = parent.hierarchy.parent;
            }
        }

        public override void TraverseRecursive(VisualElement element, int depth)
        {
            // Skip if element is neither dirty nor ancestor of dirty
            // Note: We check parent.stylesDirty because children inherit from parent and need updating
            var parent = element.hierarchy.parent;
            bool isDirty = element.stylesDirty || (parent != null && parent.stylesDirty);

            if (!element.stylesAncestorOfDirty && !isDirty)
            {
                return;
            }

            // If the element is dirty (either directly or inherited from parent), we need to erase pseudo masks
            // and ensure the flag is set so children can inherit it during recursion
            if (isDirty)
            {
                element.stylesDirty = true; // Propagate inherited dirty to this element's flag for children
                element.triggerPseudoMask = 0;
                element.dependencyPseudoMask = 0;
            }

            int originalStyleSheetCount = m_StyleMatchingContext.styleSheetCount;
            if (element.styleSheetList != null)
            {
                for (var i = 0; i < element.styleSheetList.Count; i++)
                {
                    m_StyleMatchingContext.AddStyleSheet(element.styleSheetList[i]);
                }
            }

            // Store the number of custom style before processing rules in case an element stop
            // to have matching custom styles the event still need to be sent and only looking
            // at the matched custom styles won't suffice.
            var originalVariableContext = m_StyleMatchingContext.variableContext;
            int originalCustomStyleCount = element.computedStyle.customPropertiesCount;
            if (isDirty)
            {
                m_StyleMatchingContext.currentElement = element;
                StyleSelectorHelper<TStyleProfiler>.FindMatches(m_StyleMatchingContext, m_TempMatchResults, originalStyleSheetCount - 1);
                var newStyle = ProcessMatchedRules(element, m_TempMatchResults);
                newStyle.Acquire();

                if (element.hasInlineStyle)
                {
                    var variableContext = m_StyleMatchingContext.variableContext;
                    var rule = element.inlineStyleAccess.inlineRule.rule;
                    if (rule != null && rule.customPropertiesCount > 0)
                    {
                        variableContext = new StyleVariableContext(m_StyleMatchingContext.variableContext);
                        foreach (var property in rule.properties)
                        {
                            if (!property.isCustomProperty)
                                continue;
                            var sv = new StyleVariable(property.name, rule.styleSheet, property.values);
                            variableContext.Add(sv);
                        }
                    }
                    element.inlineStyleAccess.ApplyInlineStyles(ref newStyle, variableContext);
                }

                ComputedTransitionUtils.UpdateComputedTransitions(ref newStyle, out var computedTransitions);

                if (element.hasRunningAnimations &&
                    !ComputedTransitionUtils.SameTransitionProperty(ref element.computedStyle, ref newStyle))
                {
                    CancelAnimationsWithNoTransitionProperty(computedTransitions, element, ref newStyle);
                }

                if (computedTransitions.Length > 0 && element.styleInitialized)
                {
                    // Start the transitions. Note that the element still has its old style at this point.
                    ProcessTransitions(computedTransitions, element, ref element.computedStyle, ref newStyle);

                    // Set entire new computed style but immediately force the animated values to be updated again.
                    element.SetComputedStyle(ref newStyle);
                    ForceUpdateTransitions(element);
                }
                else
                {
                    element.SetComputedStyle(ref newStyle);
                }

                newStyle.Release();
                element.styleInitialized = true;
                element.inheritedStylesHash = element.computedStyle.inheritedData.GetHashCode();
                m_StyleMatchingContext.currentElement = null;
                m_TempMatchResults.Clear();
            }
            else
            {
                m_StyleMatchingContext.variableContext = element.variableContext;
            }

            // Need to send the custom styles event after the inheritance is resolved because an element
            // may want to read standard styles too (TextInputFieldBase callback depends on it).
            // Accumulate elements that need the event; dispatch at the end to support future native code path.
            if (isDirty && (originalCustomStyleCount > 0 || element.computedStyle.customPropertiesCount > 0) &&
                element.HasSelfEventInterests(CustomStyleResolvedEvent.EventCategory))
            {
                m_CustomStyleResolvedElements.Add(element);
            }

            m_StyleMatchingContext.ancestorFilter.PushElement(element);

            Recurse(element, depth);

            m_StyleMatchingContext.ancestorFilter.PopElement();

            // Clear flags after traversing children (so children can see parent's dirty state during traversal)
            element.stylesDirty = false;
            element.stylesAncestorOfDirty = false;

            m_StyleMatchingContext.variableContext = originalVariableContext;
            if (m_StyleMatchingContext.styleSheetCount > originalStyleSheetCount)
            {
                m_StyleMatchingContext.RemoveStyleSheetRange(originalStyleSheetCount, m_StyleMatchingContext.styleSheetCount - originalStyleSheetCount);
            }
        }

        private void ProcessTransitions(ComputedTransitionProperty[] computedTransitions, VisualElement element, ref ComputedStyle oldStyle, ref ComputedStyle newStyle)
        {
            for (var i = computedTransitions.Length - 1; i >= 0; i--)
            {
                var t = computedTransitions[i];

                // Need to skip inline styles because they take precedence over USS styles
                if (element.hasInlineStyle && element.inlineStyleAccess.IsValueSet(t.id))
                    continue;

                ComputedStyle.StartAnimation(element, t.id, ref oldStyle, ref newStyle, t.durationMs, t.delayMs,
                    t.easingCurve);
            }
        }

        private void ForceUpdateTransitions(VisualElement element)
        {
            element.styleAnimation.GetAllAnimations(m_AnimatedProperties);
            if (m_AnimatedProperties.Count > 0)
            {
                foreach (var id in m_AnimatedProperties)
                {
                    element.styleAnimation.UpdateAnimation(id);
                }
                m_AnimatedProperties.Clear();
            }
        }

        private readonly List<StylePropertyId> m_AnimatedProperties = new List<StylePropertyId>();
        internal void CancelAnimationsWithNoTransitionProperty(ComputedTransitionProperty[] computedTransitions, VisualElement element, ref ComputedStyle newStyle)
        {
            element.styleAnimation.GetAllAnimations(m_AnimatedProperties);
            foreach (var id in m_AnimatedProperties)
            {
                if (!computedTransitions.HasTransitionProperty(id))
                    element.styleAnimation.CancelAnimation(id);
            }
            m_AnimatedProperties.Clear();
        }

        ComputedStyle ProcessMatchedRules(VisualElement element, List<StyleSelectorMatch> matchingSelectors)
        {
            matchingSelectors.Sort(StyleSelectorMatch.Comparison);

            Int64 matchingRulesHash = element.fullTypeName.GetHashCode();
            // Let current DPI contribute to the hash so cache is invalidated when this changes
            matchingRulesHash = (matchingRulesHash * 397) ^ currentPixelsPerPoint.GetHashCode();

            int oldVariablesHash = m_StyleMatchingContext.variableContext.GetVariableHash();
            int customPropertiesCount = 0;

            foreach (var match in matchingSelectors)
            {
                customPropertiesCount += match.complexSelector.rule.customPropertiesCount;
            }

            if (customPropertiesCount > 0)
            {
                // Element defines new variables, add the parents variables at the beginning of the processing context
                m_ProcessVarContext.AddInitialRange(m_StyleMatchingContext.variableContext);
            }

            foreach (var match in matchingSelectors)
            {
                // UUM-87950 Don't use rule.GetHashCode() as it isn't defined on the class and
                // falls back on the base Object.GetHashCode() which have been seen to return the
                // same value for different rules across scene changes. The rule index is a good
                // alternate unique identifier for the rule.

                var sheet = match.sheet;
                var ruleIndex = match.complexSelector.ruleIndex;
                var specificity = match.complexSelector.specificity;
                matchingRulesHash = (matchingRulesHash * 397) ^ sheet.contentHash;
                matchingRulesHash = (matchingRulesHash * 397) ^ ruleIndex;
                matchingRulesHash = (matchingRulesHash * 397) ^ specificity;

                var rule = match.complexSelector.rule;
                if (rule.customPropertiesCount > 0)
                {
                    ProcessMatchedVariables(match.sheet, rule);
                }
            }

            var parent = element.hierarchy.parent;
            int inheritedStyleHash = parent?.inheritedStylesHash ?? 0;
            matchingRulesHash = (matchingRulesHash * 397) ^ inheritedStyleHash;

            int variablesHash = oldVariablesHash;
            if (customPropertiesCount > 0)
            {
                variablesHash = m_ProcessVarContext.GetVariableHash();
            }
            matchingRulesHash = (matchingRulesHash * 397) ^ variablesHash;

            if (oldVariablesHash != variablesHash)
            {
                StyleVariableContext ctx;
                if (!StyleCache.TryGetValue(variablesHash, out ctx))
                {
                    ctx = new StyleVariableContext(m_ProcessVarContext);
                    StyleCache.SetValue(variablesHash, ctx);
                }

                m_StyleMatchingContext.variableContext = ctx;
            }
            element.variableContext = m_StyleMatchingContext.variableContext;
            m_ProcessVarContext.Clear();

            if (!StyleCache.TryGetValue(matchingRulesHash, out var resolvedStyles))
            {
                ref var parentStyle = ref parent?.computedStyle != null ? ref parent.computedStyle : ref InitialStyle.Get();
                resolvedStyles = ComputedStyle.Create(ref parentStyle);
                resolvedStyles.matchingRulesHash = matchingRulesHash;

                float dpiScaling = element.scaledPixelsPerPoint;
                foreach (var match in matchingSelectors)
                {
                    m_StylePropertyReader.SetContext(match.sheet, match.complexSelector, m_StyleMatchingContext.variableContext, dpiScaling);
                    resolvedStyles.ApplyProperties(m_StylePropertyReader, ref parentStyle);
                }

                resolvedStyles.FinalizeApply(ref parentStyle);

                StyleCache.SetValue(matchingRulesHash, ref resolvedStyles);
            }

            return resolvedStyles;
        }

        private void ProcessMatchedVariables(StyleSheet sheet, StyleRule rule)
        {
            foreach (var property in rule.properties)
            {
                if (property.isCustomProperty)
                {
                    var sv = new StyleVariable(
                        property.name,
                        sheet,
                        property.values
                    );
                    m_ProcessVarContext.Add(sv);
                }
            }
        }
    }
}
