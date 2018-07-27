// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Profiling;
using UnityEngine.StyleSheets;

namespace UnityEngine.Experimental.UIElements
{
    internal static class StyleCache
    {
        // hash of a set of rules to a resolved style
        // the same set of rules will give the same resolved style, caching the hash of the matching rules before
        // resolving styles allows to skip the resolve part when an existing resolved style already exists
        private static Dictionary<Int64, VisualElementStylesData> s_StyleCache = new Dictionary<Int64, VisualElementStylesData>();

        public static bool TryGetValue(Int64 hash, out VisualElementStylesData data)
        {
            return s_StyleCache.TryGetValue(hash, out data);
        }

        public static void SetValue(Int64 hash, VisualElementStylesData data)
        {
            s_StyleCache[hash] = data;
        }

        public static void ClearStyleCache()
        {
            s_StyleCache.Clear();
        }
    }

    internal class VisualTreeStyleUpdater : BaseVisualTreeUpdater
    {
        private HashSet<VisualElement> m_ApplyStyleUpdateList = new HashSet<VisualElement>();
        private bool m_IsApplyingStyles = false;
        private uint m_Version = 0;
        private uint m_LastVersion = 0;

        private VisualTreeStyleUpdaterTraversal m_StyleContextHierarchyTraversal = new VisualTreeStyleUpdaterTraversal();

        public override string description
        {
            get { return "Update Style"; }
        }

        public void DirtyStyleSheets()
        {
            PropagateDirtyStyleSheets(visualTree);
            visualTree.IncrementVersion(VersionChangeType.StyleSheet); // dirty all styles
        }

        private static void PropagateDirtyStyleSheets(VisualElement element)
        {
            if (element != null)
            {
                if (element.styleSheets != null)
                    element.LoadStyleSheetsFromPaths();

                foreach (var child in element.shadow.Children())
                {
                    PropagateDirtyStyleSheets(child);
                }
            }
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
            m_StyleContextHierarchyTraversal.currentPixelsPerPoint = panel.currentPixelsPerPoint;
            m_StyleContextHierarchyTraversal.Traverse(visualTree);
            m_IsApplyingStyles = false;
        }
    }

    class StyleMatchingContext
    {
        public List<StyleSheet> styleSheetStack;
        public VisualElement currentElement;
        public Action<VisualElement, MatchResultInfo> processResult;

        public StyleMatchingContext(Action<VisualElement, MatchResultInfo> processResult)
        {
            styleSheetStack = new List<StyleSheet>();
            currentElement = null;
            this.processResult = processResult;
        }
    }


    internal class VisualTreeStyleUpdaterTraversal : HierarchyTraversal
    {
        private HashSet<VisualElement> m_UpdateList = new HashSet<VisualElement>();
        private HashSet<VisualElement> m_ParentList = new HashSet<VisualElement>();

        private List<SelectorMatchRecord> m_TempMatchResults = new List<SelectorMatchRecord>();

        public float currentPixelsPerPoint { get; set; } = 1.0f;

        StyleMatchingContext m_StyleMatchingContext = new StyleMatchingContext(OnProcessMatchResult);

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
            int count = ve.shadow.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = ve.shadow[i];
                bool result = m_UpdateList.Add(child);
                if (result)
                    PropagateToChildren(child);
            }
        }

        private void PropagateToParents(VisualElement ve)
        {
            var parent = ve.shadow.parent;
            while (parent != null)
            {
                if (!m_ParentList.Add(parent))
                {
                    break;
                }

                parent = parent.shadow.parent;
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
            if (m_UpdateList.Contains(element))
            {
                element.triggerPseudoMask = 0;
                element.dependencyPseudoMask = 0;
            }

            int originalStyleSheetCount = m_StyleMatchingContext.styleSheetStack.Count;

            if (element.styleSheets != null)
            {
                for (var index = 0; index < element.styleSheets.Count; index++)
                {
                    var styleSheetData = element.styleSheets[index];
                    m_StyleMatchingContext.styleSheetStack.Add(styleSheetData);
                }
            }

            if (m_UpdateList.Contains(element))
            {
                m_StyleMatchingContext.currentElement = element;

                StyleSelectorHelper.FindMatches(m_StyleMatchingContext, m_TempMatchResults);


                ProcessMatchedRules(element, m_TempMatchResults);

                m_StyleMatchingContext.currentElement = null;
                m_TempMatchResults.Clear();
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
            matchingSelectors.Sort(SelectorMatchRecord.Compare);

            Int64 matchingRulesHash = element.fullTypeName.GetHashCode();
            // Let current DPI contribute to the hash so cache is invalidated when this changes
            matchingRulesHash = (matchingRulesHash * 397) ^ currentPixelsPerPoint.GetHashCode();

            foreach (var record in matchingSelectors)
            {
                StyleRule rule = record.complexSelector.rule;
                int specificity = record.complexSelector.specificity;
                matchingRulesHash = (matchingRulesHash * 397) ^ rule.GetHashCode();
                matchingRulesHash = (matchingRulesHash * 397) ^ specificity;
            }

            VisualElementStylesData resolvedStyles;
            if (StyleCache.TryGetValue(matchingRulesHash, out resolvedStyles))
            {
                element.SetSharedStyles(resolvedStyles);
            }
            else
            {
                resolvedStyles = new VisualElementStylesData(isShared: true);

                foreach (var record in matchingSelectors)
                {
                    StylePropertyID[] propertyIDs = StyleSheetCache.GetPropertyIDs(record.sheet, record.complexSelector.ruleIndex);
                    resolvedStyles.ApplyRule(record.sheet, record.complexSelector.specificity, record.complexSelector.rule, propertyIDs);
                }

                resolvedStyles.ApplyLayoutValues();


                StyleCache.SetValue(matchingRulesHash, resolvedStyles);

                element.SetSharedStyles(resolvedStyles);
            }
        }
    }
}
