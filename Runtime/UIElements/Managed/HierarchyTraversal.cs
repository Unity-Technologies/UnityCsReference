// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.StyleSheets;

namespace UnityEngine.Experimental.UIElements.StyleSheets
{
    internal abstract class HierarchyTraversal
    {
        public abstract bool ShouldSkipElement(VisualElement element);
        public abstract bool OnRuleMatchedElement(RuleMatcher matcher, VisualElement element);

        public virtual void OnBeginElementTest(VisualElement element, List<RuleMatcher> ruleMatchers)
        {
        }

        public void BeginElementTest(VisualElement element, List<RuleMatcher> ruleMatchers)
        {
            OnBeginElementTest(element, ruleMatchers);
        }

        public virtual void ProcessMatchedRules(VisualElement element)
        {
        }

        internal void Traverse(VisualElement element, int depth, List<RuleMatcher> ruleMatchers)
        {
            // if subtree is up to date skip
            if (ShouldSkipElement(element))
            {
                return;
            }

            int originalCount = ruleMatchers.Count;
            BeginElementTest(element, ruleMatchers);

            int count = ruleMatchers.Count; // changes while we iterate so save

            for (int j = 0; j < count; j++)
            {
                RuleMatcher matcher = ruleMatchers[j];

                if (matcher.depth < depth || // ignore matchers that don't apply to this depth
                    !Match(element, ref matcher))
                {
                    continue;
                }
                // from now, the element is a match, at least a partial one

                StyleSelector[] selectors = matcher.complexSelector.selectors;
                int nextIndex = matcher.simpleSelectorIndex + 1;
                int selectorsCount = selectors.Length;
                // if this sub selector in the complex selector is not the last
                // we create a new matcher for the next element
                // will stay in the list of matchers for as long as we visit descendents
                if (nextIndex < selectorsCount)
                {
                    RuleMatcher copy = new RuleMatcher()
                    {
                        complexSelector = matcher.complexSelector,
                        depth = selectors[nextIndex].previousRelationship == StyleSelectorRelationship.Child ? depth + 1 : Int32.MaxValue,
                        simpleSelectorIndex = nextIndex,
                        sheet = matcher.sheet
                    };

                    ruleMatchers.Add(copy);
                }
                // Otherwise we add the rule as matching this element
                else
                {
                    //TODO: abort if return false
                    if (OnRuleMatchedElement(matcher, element))
                        return;
                }
            }

            ProcessMatchedRules(element);

            Recurse(element, depth, ruleMatchers);

            // Remove all matchers that we could possibly have added at this level of recursion
            if (ruleMatchers.Count > originalCount)
            {
                ruleMatchers.RemoveRange(originalCount, ruleMatchers.Count - originalCount);
            }
        }

        protected virtual void Recurse(VisualElement element, int depth, List<RuleMatcher> ruleMatchers)
        {
            for (int i = 0; i < element.shadow.childCount; i++)
            {
                var child = element.shadow[i];
                Traverse(child, depth + 1, ruleMatchers);
            }
        }

        protected virtual bool MatchSelectorPart(VisualElement element, StyleSelector selector, StyleSelectorPart part)
        {
            bool match = true;

            switch (part.type)
            {
                case StyleSelectorType.Wildcard:
                    break;
                case StyleSelectorType.Class:
                    match = element.ClassListContains(part.value);
                    break;
                case StyleSelectorType.ID:
                    match = (element.name == part.value);
                    break;
                case StyleSelectorType.Type:
                    //TODO: This tests fails to capture instances of sub-classes
                    match = element.typeName == part.value;
                    break;
                case StyleSelectorType.PseudoClass:
                    int pseudoStates = (int)element.pseudoStates;
                    match = (selector.pseudoStateMask & pseudoStates) == selector.pseudoStateMask;
                    match &= (selector.negatedPseudoStateMask & ~pseudoStates) == selector.negatedPseudoStateMask;
                    break;
                default: // ignore, all errors should have been warned before hand
                    match = false;
                    break;
            }
            return match;
        }

        public virtual bool Match(VisualElement element, ref RuleMatcher matcher)
        {
            bool match = true;
            StyleSelector selector = matcher.complexSelector.selectors[matcher.simpleSelectorIndex];
            int count = selector.parts.Length;
            for (int i = 0; i < count && match; i++)
            {
                match = MatchSelectorPart(element, selector, selector.parts[i]);
            }
            return match;
        }
    }
}
