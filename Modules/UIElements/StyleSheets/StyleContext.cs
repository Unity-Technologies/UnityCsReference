// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.StyleSheets;

namespace UnityEngine.Experimental.UIElements.StyleSheets
{
    internal struct RuleMatcher
    {
        public StyleSheet sheet;
        public StyleComplexSelector complexSelector;

        public override string ToString()
        {
            return complexSelector.ToString();
        }
    }

    internal class StyleContext
    {
        public float currentPixelsPerPoint { get; set; } = 1.0f;

        private VisualElement m_VisualTree;

        internal struct RuleRef
        {
            public StyleComplexSelector selector;
            public StyleSheet sheet;
        }

        // hash of a set of rules to a resolved style
        // the same set of rules will give the same resolved style, caching the hash of the matching rules before
        // resolving styles allows to skip the resolve part when an existing resolved style already exists
        private static Dictionary<Int64, VisualElementStylesData> s_StyleCache = new Dictionary<Int64, VisualElementStylesData>();

        internal static StyleContextHierarchyTraversal styleContextHierarchyTraversal = new StyleContextHierarchyTraversal();

        public StyleContext(VisualElement tree)
        {
            m_VisualTree = tree;
        }

        public void DirtyStyleSheets()
        {
            PropagateDirtyStyleSheets(m_VisualTree);
        }

        public void ApplyStyles()
        {
            Debug.Assert(m_VisualTree.panel != null);
            styleContextHierarchyTraversal.currentPixelsPerPoint = currentPixelsPerPoint;
            styleContextHierarchyTraversal.Traverse(m_VisualTree);
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

        public static void ClearStyleCache()
        {
            s_StyleCache.Clear();
        }

        internal class StyleContextHierarchyTraversal : HierarchyTraversal
        {
            private List<RuleRef> m_MatchedRules = new List<RuleRef>(capacity: 0);
            private long m_MatchingRulesHash;
            public float currentPixelsPerPoint { get; set; }

            public override bool ShouldSkipElement(VisualElement element)
            {
                return !element.IsDirty(ChangeType.Styles)
                    && !element.IsDirty(ChangeType.StylesPath);
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
                if (element.IsDirty(ChangeType.Styles))
                {
                    element.triggerPseudoMask = 0;
                    element.dependencyPseudoMask = 0;
                }

                if (element != null && element.styleSheets != null)
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
                if (s_StyleCache.TryGetValue(m_MatchingRulesHash, out resolvedStyles))
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

                    s_StyleCache[m_MatchingRulesHash] = resolvedStyles;

                    element.SetSharedStyles(resolvedStyles);
                }
            }
        }
    }
}
