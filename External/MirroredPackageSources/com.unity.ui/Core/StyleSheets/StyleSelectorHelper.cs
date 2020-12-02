using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements.StyleSheets
{
    // Result of a single match between a selector and visual element.
    internal struct MatchResultInfo
    {
        public readonly bool success;
        public readonly PseudoStates triggerPseudoMask; // what pseudo states contributes to matching this selector
        public readonly PseudoStates dependencyPseudoMask; // what pseudo states if set, would have given a different result

        public MatchResultInfo(bool success, PseudoStates triggerPseudoMask, PseudoStates dependencyPseudoMask)
        {
            this.success = success;
            this.triggerPseudoMask = triggerPseudoMask;
            this.dependencyPseudoMask = dependencyPseudoMask;
        }
    }

    // Each struct represents on match for a visual element against a complex
    internal struct SelectorMatchRecord
    {
        public StyleSheet sheet;
        public int styleSheetIndexInStack;
        public StyleComplexSelector complexSelector;

        public SelectorMatchRecord(StyleSheet sheet, int styleSheetIndexInStack) : this()
        {
            this.sheet = sheet;
            this.styleSheetIndexInStack = styleSheetIndexInStack;
        }

        public static int Compare(SelectorMatchRecord a, SelectorMatchRecord b)
        {
            if (a.sheet.isUnityStyleSheet != b.sheet.isUnityStyleSheet)
                return a.sheet.isUnityStyleSheet ? -1 : 1;

            int res = a.complexSelector.specificity.CompareTo(b.complexSelector.specificity);

            if (res == 0)
            {
                res = a.styleSheetIndexInStack.CompareTo(b.styleSheetIndexInStack);
            }

            if (res == 0)
            {
                res = a.complexSelector.orderInStyleSheet.CompareTo(b.complexSelector.orderInStyleSheet);
            }

            return res;
        }
    }

    // Pure functions for the central logic of selector application
    static class StyleSelectorHelper
    {
        public static MatchResultInfo MatchesSelector(VisualElement element, StyleSelector selector)
        {
            bool match = true;

            StyleSelectorPart[] parts = selector.parts;
            int count = parts.Length;

            for (int i = 0; i < count && match; i++)
            {
                switch (parts[i].type)
                {
                    case StyleSelectorType.Wildcard:
                        break;
                    case StyleSelectorType.Class:
                        match = element.ClassListContains(parts[i].value);
                        break;
                    case StyleSelectorType.ID:
                        match = (element.name == parts[i].value);
                        break;
                    case StyleSelectorType.Type:
                        //TODO: This tests fails to capture instances of sub-classes
                        match = element.typeName == parts[i].value;
                        break;
                    case StyleSelectorType.Predicate:
                        UQuery.IVisualPredicateWrapper w = parts[i].tempData as UQuery.IVisualPredicateWrapper;
                        match = w != null && w.Predicate(element);
                        break;
                    case StyleSelectorType.PseudoClass:
                        break;
                    default: // ignore, all errors should have been warned before hand
                        match = false;
                        break;
                }
            }

            int triggerPseudoStateMask = 0;
            int dependencyPseudoMask = 0;

            bool saveMatch = match;

            if (saveMatch  && selector.pseudoStateMask != 0)
            {
                match = (selector.pseudoStateMask & (int)element.pseudoStates) == selector.pseudoStateMask;

                if (match)
                {
                    // the element matches this selector because it has those flags
                    dependencyPseudoMask = selector.pseudoStateMask;
                }
                else
                {
                    // if the element had those flags defined, it would match this selector
                    triggerPseudoStateMask = selector.pseudoStateMask;
                }
            }

            if (saveMatch && selector.negatedPseudoStateMask != 0)
            {
                match &= (selector.negatedPseudoStateMask & ~(int)element.pseudoStates) == selector.negatedPseudoStateMask;

                if (match)
                {
                    // the element matches this selector because it does not have those flags
                    triggerPseudoStateMask |= selector.negatedPseudoStateMask;
                }
                else
                {
                    // if the element didn't have those flags, it would match this selector
                    dependencyPseudoMask |= selector.negatedPseudoStateMask;
                }
            }

            return new MatchResultInfo(match, (PseudoStates)triggerPseudoStateMask, (PseudoStates)dependencyPseudoMask);
        }

        public static bool MatchRightToLeft(VisualElement element, StyleComplexSelector complexSelector, Action<VisualElement, MatchResultInfo> processResult)
        {
            // see https://speakerdeck.com/constellation/css-jit-just-in-time-compiled-css-selectors-in-webkit for
            // a detailed explaination of the algorithm

            var current = element;
            int nextIndex = complexSelector.selectors.Length - 1;
            VisualElement saved = null;
            int savedIdx = -1;

            // go backward
            while (nextIndex >= 0)
            {
                if (current == null)
                    break;

                MatchResultInfo matchInfo = MatchesSelector(current, complexSelector.selectors[nextIndex]);
                processResult(current, matchInfo);

                if (!matchInfo.success)
                {
                    // if we have a descendent relationship, keep trying on the parent
                    // ie. "div span", div failed on this element, try on the parent
                    // happens earlier than the backtracking saving below
                    if (nextIndex < complexSelector.selectors.Length - 1 &&
                        complexSelector.selectors[nextIndex + 1].previousRelationship == StyleSelectorRelationship.Descendent)
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
                if (nextIndex < complexSelector.selectors.Length - 1
                    && complexSelector.selectors[nextIndex + 1].previousRelationship == StyleSelectorRelationship.Descendent)
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

        static void FastLookup(IDictionary<string, StyleComplexSelector> table, List<SelectorMatchRecord> matchedSelectors, StyleMatchingContext context, string input, ref SelectorMatchRecord record)
        {
            StyleComplexSelector currentComplexSelector;
            if (table.TryGetValue(input, out currentComplexSelector))
            {
                while (currentComplexSelector != null)
                {
                    if (MatchRightToLeft(context.currentElement, currentComplexSelector, context.processResult))
                    {
                        record.complexSelector = currentComplexSelector;
                        matchedSelectors.Add(record);
                    }
                    currentComplexSelector = currentComplexSelector.nextInTable;
                }
            }
        }

        public static void FindMatches(StyleMatchingContext context, List<SelectorMatchRecord> matchedSelectors)
        {
            // To support having the root pseudo states set for style sheets added onto an element
            // we need to find which sheets belongs to the element itself.
            VisualElement element = context.currentElement;
            int parentSheetIndex =  context.styleSheetStack.Count - 1;
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

        public static void FindMatches(StyleMatchingContext context, List<SelectorMatchRecord> matchedSelectors, int parentSheetIndex)
        {
            Debug.Assert(matchedSelectors.Count == 0);

            Debug.Assert(context.currentElement != null, "context.currentElement != null");

            VisualElement element = context.currentElement;
            bool toggledRoot = false;
            for (int i = 0; i < context.styleSheetStack.Count; i++)
            {
                // If the sheet is added on the element consider it as :root
                if (!toggledRoot && i > parentSheetIndex)
                {
                    element.pseudoStates |= PseudoStates.Root;
                    toggledRoot = true;
                }

                StyleSheet styleSheet = context.styleSheetStack[i];
                SelectorMatchRecord record = new SelectorMatchRecord(styleSheet, i);

                FastLookup(styleSheet.orderedTypeSelectors, matchedSelectors, context, element.typeName, ref record);
                FastLookup(styleSheet.orderedTypeSelectors, matchedSelectors, context, "*", ref record);

                if (!string.IsNullOrEmpty(element.name))
                {
                    FastLookup(styleSheet.orderedNameSelectors, matchedSelectors, context, element.name, ref record);
                }

                foreach (string @class in element.GetClassesForIteration())
                {
                    FastLookup(styleSheet.orderedClassSelectors, matchedSelectors, context, @class, ref record);
                }
            }

            if (toggledRoot)
                element.pseudoStates &= ~PseudoStates.Root;
        }
    }
}
