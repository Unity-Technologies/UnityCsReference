using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    internal static class StyleCache
    {
        // hash of a set of rules to a specified style data
        // the same set of rules will give the same specified styles, caching the hash of the matching rules before
        // resolving styles allows to skip the resolve part when an existing style data already exists
        private static Dictionary<Int64, ComputedStyle> s_ComputedStyleCache = new Dictionary<Int64, ComputedStyle>();
        private static Dictionary<int, StyleVariableContext> s_StyleVariableContextCache = new Dictionary<int, StyleVariableContext>();

        public static bool TryGetValue(Int64 hash, out ComputedStyle data)
        {
            return s_ComputedStyleCache.TryGetValue(hash, out data);
        }

        public static void SetValue(Int64 hash, ComputedStyle data)
        {
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

        public static void ClearStyleCache()
        {
            s_ComputedStyleCache.Clear();
            s_StyleVariableContextCache.Clear();
        }
    }

    internal class VisualTreeStyleUpdater : BaseVisualTreeUpdater
    {
        private HashSet<VisualElement> m_ApplyStyleUpdateList = new HashSet<VisualElement>();
        private bool m_IsApplyingStyles = false;
        private uint m_Version = 0;
        private uint m_LastVersion = 0;

        private VisualTreeStyleUpdaterTraversal m_StyleContextHierarchyTraversal = new VisualTreeStyleUpdaterTraversal();

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
            if ((versionChangeType & VersionChangeType.StyleSheet) != VersionChangeType.StyleSheet)
                return;

            ++m_Version;

            // Applying styles can trigger new changes, store changes in a separate list
            if (m_IsApplyingStyles)
            {
                m_ApplyStyleUpdateList.Add(ve);
            }
            else
            {
                m_StyleContextHierarchyTraversal.AddChangedElement(ve);
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
                m_StyleContextHierarchyTraversal.AddChangedElement(ve);
            }
            m_ApplyStyleUpdateList.Clear();
        }

        private void ApplyStyles()
        {
            Debug.Assert(visualTree.panel != null);
            m_IsApplyingStyles = true;
            m_StyleContextHierarchyTraversal.PrepareTraversal(panel.scaledPixelsPerPoint);
            m_StyleContextHierarchyTraversal.Traverse(visualTree);
            m_IsApplyingStyles = false;
        }
    }

    class StyleMatchingContext
    {
        public List<StyleSheet> styleSheetStack;
        public StyleVariableContext variableContext;
        public VisualElement currentElement;
        public Action<VisualElement, MatchResultInfo> processResult;

        public StyleMatchingContext(Action<VisualElement, MatchResultInfo> processResult)
        {
            styleSheetStack = new List<StyleSheet>();
            variableContext = StyleVariableContext.none;
            currentElement = null;
            this.processResult = processResult;
        }
    }

    internal class VisualTreeStyleUpdaterTraversal : HierarchyTraversal
    {
        private StyleVariableContext m_ProcessVarContext = new StyleVariableContext();
        private HashSet<VisualElement> m_UpdateList = new HashSet<VisualElement>();
        private HashSet<VisualElement> m_ParentList = new HashSet<VisualElement>();

        private List<SelectorMatchRecord> m_TempMatchResults = new List<SelectorMatchRecord>();

        private float currentPixelsPerPoint { get; set; } = 1.0f;

        StyleMatchingContext m_StyleMatchingContext = new StyleMatchingContext(OnProcessMatchResult);
        StylePropertyReader m_StylePropertyReader = new StylePropertyReader();

        public void PrepareTraversal(float pixelsPerPoint)
        {
            currentPixelsPerPoint = pixelsPerPoint;
        }

        public void AddChangedElement(VisualElement ve)
        {
            m_UpdateList.Add(ve);
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

            int originalStyleSheetCount = m_StyleMatchingContext.styleSheetStack.Count;

            if (element.styleSheetList != null)
            {
                for (var index = 0; index < element.styleSheetList.Count; index++)
                {
                    var styleSheetData = element.styleSheetList[index];
                    m_StyleMatchingContext.styleSheetStack.Add(styleSheetData);
                }
            }

            // Store the number of custom style before processing rules in case an element stop
            // to have matching custom styles the event still need to be sent and only looking
            // at the matched custom styles won't suffice.
            int originalCustomStyleCount = element.computedStyle.customPropertiesCount;
            if (updateElement)
            {
                m_StyleMatchingContext.currentElement = element;

                StyleSelectorHelper.FindMatches(m_StyleMatchingContext, m_TempMatchResults);

                ProcessMatchedRules(element, m_TempMatchResults);

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
            if (updateElement && (originalCustomStyleCount > 0 || element.computedStyle.customPropertiesCount > 0))
            {
                using (var evt = CustomStyleResolvedEvent.GetPooled())
                {
                    evt.target = element;
                    element.SendEvent(evt);
                }
            }

            Recurse(element, depth);


            if (m_StyleMatchingContext.styleSheetStack.Count > originalStyleSheetCount)
            {
                m_StyleMatchingContext.styleSheetStack.RemoveRange(originalStyleSheetCount, m_StyleMatchingContext.styleSheetStack.Count - originalStyleSheetCount);
            }
        }

        bool ShouldSkipElement(VisualElement element)
        {
            return !m_ParentList.Contains(element) && !m_UpdateList.Contains(element);
        }

        void ProcessMatchedRules(VisualElement element, List<SelectorMatchRecord> matchingSelectors)
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
                StyleRule rule = record.complexSelector.rule;
                int specificity = record.complexSelector.specificity;
                matchingRulesHash = (matchingRulesHash * 397) ^ rule.GetHashCode();
                matchingRulesHash = (matchingRulesHash * 397) ^ specificity;

                if (rule.customPropertiesCount > 0)
                {
                    ProcessMatchedVariables(record.sheet, rule);
                }
            }

            var parent = element.hierarchy.parent;
            int inheritedStyleHash = parent != null ? parent.inheritedStylesHash : 0;
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

            ComputedStyle resolvedStyles;
            if (StyleCache.TryGetValue(matchingRulesHash, out resolvedStyles))
            {
                element.SetSharedStyles(resolvedStyles);
            }
            else
            {
                var parentStyle = parent?.computedStyle;
                resolvedStyles = ComputedStyle.Create(parentStyle, true);

                float dpiScaling = element.scaledPixelsPerPoint;
                foreach (var record in matchingSelectors)
                {
                    m_StylePropertyReader.SetContext(record.sheet, record.complexSelector, m_StyleMatchingContext.variableContext, dpiScaling);
                    resolvedStyles.ApplyProperties(m_StylePropertyReader, parentStyle);
                }

                resolvedStyles.FinalizeApply(parentStyle);

                StyleCache.SetValue(matchingRulesHash, resolvedStyles);

                element.SetSharedStyles(resolvedStyles);
            }
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
