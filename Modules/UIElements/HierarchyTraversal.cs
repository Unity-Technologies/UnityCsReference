// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.StyleSheets;

namespace UnityEngine.Experimental.UIElements.StyleSheets
{
    internal interface IHierarchyTraversal
    {
        void Traverse(VisualElement element);
    }

    internal abstract class HierarchyTraversal : IHierarchyTraversal
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

        List<RuleMatcher> m_ruleMatchers = new List<RuleMatcher>();

        public virtual void OnProcessMatchResult(VisualElement element, ref RuleMatcher matcher, ref MatchResultInfo matchInfo)
        {
        }

        public virtual void Traverse(VisualElement element)
        {
            TraverseRecursive(element, 0, m_ruleMatchers);
            m_ruleMatchers.Clear();
        }

        public virtual void TraverseRecursive(VisualElement element, int depth, List<RuleMatcher> ruleMatchers)
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

                if (MatchRightToLeft(element, ref matcher))
                    return;
            }

            ProcessMatchedRules(element);

            Recurse(element, depth, ruleMatchers);

            // Remove all matchers that we could possibly have added at this level of recursion
            if (ruleMatchers.Count > originalCount)
            {
                ruleMatchers.RemoveRange(originalCount, ruleMatchers.Count - originalCount);
            }
        }

        private bool MatchRightToLeft(VisualElement element, ref RuleMatcher matcher)
        {
            // see https://speakerdeck.com/constellation/css-jit-just-in-time-compiled-css-selectors-in-webkit for
            // a detailed explaination of the algorithm

            var current = element;
            int nextIndex = matcher.complexSelector.selectors.Length - 1;
            VisualElement saved = null;
            int savedIdx = -1;

            // go backward
            while (nextIndex >= 0)
            {
                if (current == null)
                    break;

                MatchResultInfo matchInfo = Match(current, ref matcher, nextIndex);
                OnProcessMatchResult(current, ref matcher, ref matchInfo);

                if (!matchInfo.success)
                {
                    // if we have a descendent relationship, keep trying on the parent
                    // ie. "div span", div failed on this element, try on the parent
                    // happens earlier than the backtracking saving below
                    if (nextIndex < matcher.complexSelector.selectors.Length - 1 &&
                        matcher.complexSelector.selectors[nextIndex + 1].previousRelationship == StyleSelectorRelationship.Descendent)
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
                if (nextIndex < matcher.complexSelector.selectors.Length - 1
                    && matcher.complexSelector.selectors[nextIndex + 1].previousRelationship == StyleSelectorRelationship.Descendent)
                {
                    saved = current.parent;
                    savedIdx = nextIndex;
                }

                // from now, the element is a match
                if (--nextIndex < 0)
                {
                    //TODO: abort if return false
                    if (OnRuleMatchedElement(matcher, element))
                        return true;
                }
                current = current.parent;
            }
            return false;
        }

        protected virtual void Recurse(VisualElement element, int depth, List<RuleMatcher> ruleMatchers)
        {
            int i = 0;

            while (i < element.shadow.childCount)
            {
                var child = element.shadow[i];
                TraverseRecursive(child, depth + 1, ruleMatchers);

                // if the child has been moved to another parent, which happens when its parent has changed, then do not increment the iterator
                if (child.shadow.parent != element)
                    continue;
                i++;
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

        public struct MatchResultInfo
        {
            public bool success;
            public PseudoStates triggerPseudoMask;
            public PseudoStates dependencyPseudoMask;
        }

        public virtual MatchResultInfo Match(VisualElement element, ref RuleMatcher matcher, int selectorIndex)
        {
            if (element == null)
                return default(MatchResultInfo);
            bool match = true;
            StyleSelector selector = matcher.complexSelector.selectors[selectorIndex];
            int count = selector.parts.Length;

            int triggerPseudoStateMask = 0;
            int dependencyPseudoMask = 0;
            bool failedOnlyOnPseudoStates = true;

            for (int i = 0; i < count; i++)
            {
                bool isPartMatch = MatchSelectorPart(element, selector, selector.parts[i]);

                if (!isPartMatch)
                {
                    if (selector.parts[i].type == StyleSelectorType.PseudoClass)
                    {
                        // if the element had those flags defined, it would match this selector
                        triggerPseudoStateMask |= selector.pseudoStateMask;
                        // if the element didnt' have those flags, it would match this selector
                        dependencyPseudoMask |= selector.negatedPseudoStateMask;
                    }
                    else
                    {
                        failedOnlyOnPseudoStates = false;
                    }
                }
                else
                {
                    if (selector.parts[i].type == StyleSelectorType.PseudoClass)
                    {
                        // the element matches this selector because it has those flags
                        dependencyPseudoMask |= selector.pseudoStateMask;
                        // the element matches this selector because it does not have those flags
                        triggerPseudoStateMask |= selector.negatedPseudoStateMask;
                    }
                }

                match &= isPartMatch;
            }

            var result = new MatchResultInfo() { success = match };
            if (match || failedOnlyOnPseudoStates)
            {
                result.triggerPseudoMask = (PseudoStates)triggerPseudoStateMask;
                result.dependencyPseudoMask = (PseudoStates)dependencyPseudoMask;
            }

            return result;
        }
    }
}
