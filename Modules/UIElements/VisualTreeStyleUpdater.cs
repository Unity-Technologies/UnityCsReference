// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Experimental.UIElements.StyleSheets;
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

    internal class VisualTreeStyleUpdaterTraversal : HierarchyTraversal
    {
        private HashSet<VisualElement> m_UpdateList = new HashSet<VisualElement>();
        private HashSet<VisualElement> m_ParentList = new HashSet<VisualElement>();

        internal struct RuleRef
        {
            public StyleComplexSelector selector;
            public StyleSheet sheet;
        }

        protected List<RuleRef> m_MatchedRules = new List<RuleRef>(capacity: 0);
        protected long m_MatchingRulesHash;
        public float currentPixelsPerPoint { get; set; }

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

        public override bool ShouldSkipElement(VisualElement element)
        {
            return !m_ParentList.Contains(element) && !m_UpdateList.Contains(element);
        }

        public override bool OnRuleMatchedElement(RuleMatcher matcher, VisualElement element)
        {
            StyleRule rule = matcher.complexSelector.rule;
            int specificity = matcher.complexSelector.specificity;
            m_MatchingRulesHash = (m_MatchingRulesHash * 397) ^ rule.GetHashCode();
            m_MatchingRulesHash = (m_MatchingRulesHash * 397) ^ specificity;
            m_MatchedRules.Add(new RuleRef { selector = matcher.complexSelector, sheet = matcher.sheet });
            return false;
        }

        public override void OnBeginElementTest(VisualElement element, List<RuleMatcher> ruleMatchers)
        {
            if (element == null)
                return;

            // If the element is fully dirty, we need to purge those caches
            // they will be recomputed as part of the following loop
            if (m_UpdateList.Contains(element))
            {
                element.triggerPseudoMask = 0;
                element.dependencyPseudoMask = 0;
            }

            if (element.styleSheets != null)
            {
                for (var index = 0; index < element.styleSheets.Count; index++)
                {
                    var styleSheetData = element.styleSheets[index];
                    var complexSelectors = styleSheetData.complexSelectors;

                    // To avoid excessive re-allocations, just resize the list right now
                    int futureSize = ruleMatchers.Count + complexSelectors.Length;
                    ruleMatchers.Capacity = Math.Max(ruleMatchers.Capacity, futureSize);

                    for (int i = 0; i < complexSelectors.Length; i++)
                    {
                        StyleComplexSelector complexSelector = complexSelectors[i];

                        // For every complex selector, push a matcher for first sub selector
                        ruleMatchers.Add(new RuleMatcher
                        {
                            sheet = styleSheetData,
                            complexSelector = complexSelector,
                        });
                    }
                }
            }
            m_MatchedRules.Clear();
            string elementTypeName = element.fullTypeName;

            Int64 matchingRulesHash = elementTypeName.GetHashCode();
            // Let current DPI contribute to the hash so cache is invalidated when this changes
            m_MatchingRulesHash = (matchingRulesHash * 397) ^ currentPixelsPerPoint.GetHashCode();
        }

        public override void OnProcessMatchResult(UIElements.VisualElement element, ref StyleSheets.RuleMatcher matcher, ref MatchResultInfo matchInfo)
        {
            element.triggerPseudoMask |= matchInfo.triggerPseudoMask;
            element.dependencyPseudoMask |= matchInfo.dependencyPseudoMask;
        }

        public override void ProcessMatchedRules(VisualElement element)
        {
            VisualElementStylesData resolvedStyles;
            if (StyleCache.TryGetValue(m_MatchingRulesHash, out resolvedStyles))
            {
                // we should not new it in StyleTree
                element.SetSharedStyles(resolvedStyles);
            }
            else
            {
                resolvedStyles = new VisualElementStylesData(isShared: true);

                for (int i = 0, ruleCount = m_MatchedRules.Count; i < ruleCount; i++)
                {
                    RuleRef ruleRef = m_MatchedRules[i];
                    StylePropertyID[] propertyIDs = StyleSheetCache.GetPropertyIDs(ruleRef.sheet, ruleRef.selector.ruleIndex);
                    resolvedStyles.ApplyRule(ruleRef.sheet, ruleRef.selector.specificity, ruleRef.selector.rule, propertyIDs);
                }

                StyleCache.SetValue(m_MatchingRulesHash, resolvedStyles);

                element.SetSharedStyles(resolvedStyles);
            }
        }
    }
}
