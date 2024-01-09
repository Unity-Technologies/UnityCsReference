// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
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
    internal class VisualTreeStyleUpdater : BaseVisualTreeUpdater
    {
        private HashSet<VisualElement> m_ApplyStyleUpdateList = new HashSet<VisualElement>();
        private HashSet<VisualElement> m_TransitionPropertyUpdateList = new HashSet<VisualElement>();
        private bool m_IsApplyingStyles = false;
        private uint m_Version = 0;
        private uint m_LastVersion = 0;

        private VisualTreeStyleUpdaterTraversal m_StyleContextHierarchyTraversal = new VisualTreeStyleUpdaterTraversal();

        public VisualTreeStyleUpdaterTraversal traversal
        {
            get => m_StyleContextHierarchyTraversal;
            set
            {
                m_StyleContextHierarchyTraversal = value;
                panel?.visualTree.IncrementVersion(VersionChangeType.Styles | VersionChangeType.StyleSheet | VersionChangeType.Layout | VersionChangeType.Transform);
            }
        }

        private static readonly string s_Description = "Update Style";
        private static readonly ProfilerMarker s_ProfilerMarker = new ProfilerMarker(s_Description);
        public override ProfilerMarker profilerMarker => s_ProfilerMarker;

        public void DirtyStyleSheets()
        {
            // When a style sheet is re-imported, we must make sure to purge internal caches that are depending on it
            StyleCache.ClearStyleCache();
            visualTree.IncrementVersion(VersionChangeType.StyleSheet); // dirty all styles
        }


        public override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            if ((versionChangeType & (VersionChangeType.StyleSheet | VersionChangeType.TransitionProperty)) == 0)
                return;

            ++m_Version;

            if ((versionChangeType & VersionChangeType.StyleSheet) != 0)
            {
                // Applying styles can trigger new changes, store changes in a separate list
                if (m_IsApplyingStyles)
                {
                    m_ApplyStyleUpdateList.Add(ve);
                }
                else
                {
                    m_StyleContextHierarchyTraversal.AddChangedElement(ve, versionChangeType);
                }
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

            m_StyleContextHierarchyTraversal.Clear();

            // Add elements to process next frame
            foreach (var ve in m_ApplyStyleUpdateList)
            {
                m_StyleContextHierarchyTraversal.AddChangedElement(ve, VersionChangeType.StyleSheet);
            }
            m_ApplyStyleUpdateList.Clear();

            foreach (var ve in m_TransitionPropertyUpdateList)
            {
                // Allow for transitions to be cancelled if matching transition property was removed.
                if (ve.hasRunningAnimations || ve.hasCompletedAnimations)
                {
                    ComputedTransitionUtils.UpdateComputedTransitions(ref ve.computedStyle);
                    m_StyleContextHierarchyTraversal.CancelAnimationsWithNoTransitionProperty(ve, ref ve.computedStyle);
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

        private void ApplyStyles()
        {
            Debug.Assert(visualTree.panel != null);
            m_IsApplyingStyles = true;
            m_StyleContextHierarchyTraversal.PrepareTraversal(panel, panel.scaledPixelsPerPoint);
            m_StyleContextHierarchyTraversal.Traverse(visualTree);
            m_IsApplyingStyles = false;
        }
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    class StyleMatchingContext
    {
        private List<StyleSheet> m_StyleSheetStack;

        public int styleSheetCount => m_StyleSheetStack.Count;

        public StyleVariableContext variableContext;
        public VisualElement currentElement;
        public Action<VisualElement, MatchResultInfo> processResult;
        public AncestorFilter ancestorFilter = new AncestorFilter();

        public StyleMatchingContext(Action<VisualElement, MatchResultInfo> processResult)
        {
            m_StyleSheetStack = new List<StyleSheet>();
            variableContext = StyleVariableContext.none;
            currentElement = null;
            this.processResult = processResult;
        }

        public void AddStyleSheet(StyleSheet sheet)
        {
            if (sheet == null)
                return;

            m_StyleSheetStack.Add(sheet);
        }

        public void RemoveStyleSheetRange(int index, int count)
        {
            m_StyleSheetStack.RemoveRange(index, count);
        }

        public StyleSheet GetStyleSheetAt(int index)
        {
            return m_StyleSheetStack[index];
        }
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class VisualTreeStyleUpdaterTraversal : HierarchyTraversal
    {
        private StyleVariableContext m_ProcessVarContext = new StyleVariableContext();
        private HashSet<VisualElement> m_UpdateList = new HashSet<VisualElement>();
        private HashSet<VisualElement> m_ParentList = new HashSet<VisualElement>();

        private List<SelectorMatchRecord> m_TempMatchResults = new List<SelectorMatchRecord>();

        private float currentPixelsPerPoint { get; set; } = 1.0f;

        StyleMatchingContext m_StyleMatchingContext = new StyleMatchingContext(OnProcessMatchResult);
        StylePropertyReader m_StylePropertyReader = new StylePropertyReader();

        private BaseVisualElementPanel currentPanel { get; set; }

        public StyleMatchingContext styleMatchingContext => m_StyleMatchingContext;

        public void PrepareTraversal(BaseVisualElementPanel panel, float pixelsPerPoint)
        {
            currentPanel = panel;
            currentPixelsPerPoint = pixelsPerPoint;
        }

        public void AddChangedElement(VisualElement ve, VersionChangeType versionChangeType)
        {
            m_UpdateList.Add(ve);

            // If VersionChangeType.StyleSheet is not set no need to propagate to children
            if ((versionChangeType & VersionChangeType.StyleSheet) == VersionChangeType.StyleSheet)
                PropagateToChildren(ve);

            PropagateToParents(ve);
        }

        public void Clear()
        {
            m_UpdateList.Clear();
            m_ParentList.Clear();
            m_TempMatchResults.Clear();
        }

        private void PropagateToChildren(VisualElement ve)
        {
            int count = ve.hierarchy.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = ve.hierarchy[i];
                bool result = m_UpdateList.Add(child);
                if (result)
                    PropagateToChildren(child);
            }
        }

        private void PropagateToParents(VisualElement ve)
        {
            var parent = ve.hierarchy.parent;
            while (parent != null)
            {
                if (!m_ParentList.Add(parent))
                {
                    break;
                }

                parent = parent.hierarchy.parent;
            }
        }

        static void OnProcessMatchResult(VisualElement current, MatchResultInfo info)
        {
            current.triggerPseudoMask |= info.triggerPseudoMask;
            current.dependencyPseudoMask |= info.dependencyPseudoMask;
        }

        public override void TraverseRecursive(VisualElement element, int depth)
        {
            if (ShouldSkipElement(element))
            {
                return;
            }

            // If the element is fully dirty, we need to erase those flags since the full element and its subtree
            // will be re-styled.
            // If the element is not in the update list, it's a parent of something dirty and therefore it won't be restyled.
            bool updateElement = m_UpdateList.Contains(element);
            if (updateElement)
            {
                element.triggerPseudoMask = 0;
                element.dependencyPseudoMask = 0;
            }

            int originalStyleSheetCount = m_StyleMatchingContext.styleSheetCount;
            if (element.styleSheetList != null)
            {
                for (var i = 0; i < element.styleSheetList.Count; i++)
                {
                    var addedStyleSheet = element.styleSheetList[i];
                    if (addedStyleSheet.flattenedRecursiveImports != null)
                    {
                        for (var j = 0; j < addedStyleSheet.flattenedRecursiveImports.Count; j++)
                        {
                            m_StyleMatchingContext.AddStyleSheet(addedStyleSheet.flattenedRecursiveImports[j]);
                        }
                    }

                    m_StyleMatchingContext.AddStyleSheet(addedStyleSheet);
                }
            }

            // Store the number of custom style before processing rules in case an element stop
            // to have matching custom styles the event still need to be sent and only looking
            // at the matched custom styles won't suffice.
            var originalVariableContext = m_StyleMatchingContext.variableContext;
            int originalCustomStyleCount = element.computedStyle.customPropertiesCount;
            if (updateElement)
            {
                m_StyleMatchingContext.currentElement = element;
                StyleSelectorHelper.FindMatches(m_StyleMatchingContext, m_TempMatchResults, originalStyleSheetCount - 1);

                var newStyle = ProcessMatchedRules(element, m_TempMatchResults);
                newStyle.Acquire();

                if (element.hasInlineStyle)
                    element.inlineStyleAccess.ApplyInlineStyles(ref newStyle);

                ComputedTransitionUtils.UpdateComputedTransitions(ref newStyle);

                if (element.hasRunningAnimations &&
                    !ComputedTransitionUtils.SameTransitionProperty(ref element.computedStyle, ref newStyle))
                {
                    CancelAnimationsWithNoTransitionProperty(element, ref newStyle);
                }

                if (newStyle.hasTransition && element.styleInitialized)
                {
                    // Start the transitions. Note that the element still has its old style at this point.
                    ProcessTransitions(element, ref element.computedStyle, ref newStyle);

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
            if (updateElement && (originalCustomStyleCount > 0 || element.computedStyle.customPropertiesCount > 0) &&
                element.HasSelfEventInterests(CustomStyleResolvedEvent.EventCategory))
            {
                using (var evt = CustomStyleResolvedEvent.GetPooled())
                {
                    evt.elementTarget = element;
                    EventDispatchUtilities.HandleEventAtTargetAndDefaultPhase(evt, currentPanel, element);
                }
            }

            m_StyleMatchingContext.ancestorFilter.PushElement(element);

            Recurse(element, depth);

            m_StyleMatchingContext.ancestorFilter.PopElement();

            m_StyleMatchingContext.variableContext = originalVariableContext;
            if (m_StyleMatchingContext.styleSheetCount > originalStyleSheetCount)
            {
                m_StyleMatchingContext.RemoveStyleSheetRange(originalStyleSheetCount, m_StyleMatchingContext.styleSheetCount - originalStyleSheetCount);
            }
        }

        private void ProcessTransitions(VisualElement element, ref ComputedStyle oldStyle, ref ComputedStyle newStyle)
        {
            for (var i = newStyle.computedTransitions.Length - 1; i >= 0; i--)
            {
                var t = newStyle.computedTransitions[i];

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
        internal void CancelAnimationsWithNoTransitionProperty(VisualElement element, ref ComputedStyle newStyle)
        {
            element.styleAnimation.GetAllAnimations(m_AnimatedProperties);
            foreach (var id in m_AnimatedProperties)
            {
                if (!newStyle.HasTransitionProperty(id))
                    element.styleAnimation.CancelAnimation(id);
            }
            m_AnimatedProperties.Clear();
        }

        protected bool ShouldSkipElement(VisualElement element)
        {
            return !m_ParentList.Contains(element) && !m_UpdateList.Contains(element);
        }

        ComputedStyle ProcessMatchedRules(VisualElement element, List<SelectorMatchRecord> matchingSelectors)
        {
            matchingSelectors.Sort((a, b) => SelectorMatchRecord.Compare(a, b));

            Int64 matchingRulesHash = element.fullTypeName.GetHashCode();
            // Let current DPI contribute to the hash so cache is invalidated when this changes
            matchingRulesHash = (matchingRulesHash * 397) ^ currentPixelsPerPoint.GetHashCode();

            int oldVariablesHash = m_StyleMatchingContext.variableContext.GetVariableHash();
            int customPropertiesCount = 0;

            foreach (var record in matchingSelectors)
            {
                customPropertiesCount += record.complexSelector.rule.customPropertiesCount;
            }

            if (customPropertiesCount > 0)
            {
                // Element defines new variables, add the parents variables at the beginning of the processing context
                m_ProcessVarContext.AddInitialRange(m_StyleMatchingContext.variableContext);
            }

            foreach (var record in matchingSelectors)
            {
                var sheet = record.sheet;
                var rule = record.complexSelector.rule;
                var specificity = record.complexSelector.specificity;
                matchingRulesHash = (matchingRulesHash * 397) ^ sheet.contentHash;
                matchingRulesHash = (matchingRulesHash * 397) ^ rule.GetHashCode();
                matchingRulesHash = (matchingRulesHash * 397) ^ specificity;

                if (rule.customPropertiesCount > 0)
                {
                    ProcessMatchedVariables(record.sheet, rule);
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
                foreach (var record in matchingSelectors)
                {
                    m_StylePropertyReader.SetContext(record.sheet, record.complexSelector, m_StyleMatchingContext.variableContext, dpiScaling);
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
