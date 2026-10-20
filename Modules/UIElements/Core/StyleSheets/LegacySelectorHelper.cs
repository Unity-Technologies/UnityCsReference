// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements.StyleSheets
{
    // Legacy selector matching implementation used by UQuery.
    // The main style system uses the accelerated flattened selector matching in StyleSelectorHelper.
    // This legacy path is kept for UQuery which requires support for Predicate selectors
    // that are not supported in stylesheets.
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal static class LegacySelectorHelper
    {
        // Caches an id from UniqueStyleString's table, which persists across code reload.
        [NoAutoStaticsCleanup]
        static string s_LastClass;
        [NoAutoStaticsCleanup]
        static UniqueStyleString s_LastClassResolved;

        static bool ClassListContains(VisualElement element, string cls)
        {
            if (!ReferenceEquals(s_LastClass, cls))
            {
                // Misses are not cached: a predicate may add this class to the string table later in the same query.
                if (!UniqueStyleString.TryGet(cls, out var resolved))
                    return false;

                s_LastClass = cls;
                s_LastClassResolved = resolved;
            }

            return element.ClassListContains(s_LastClassResolved);
        }

        internal static void ResetClassCache()
        {
            s_LastClass = null;
            s_LastClassResolved = default;
        }

        public static bool MatchesSelector(VisualElement element, StyleSelector selector)
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
                        match = ClassListContains(element, parts[i].value);
                        break;
                    case StyleSelectorType.ID:
                        match = string.Equals(element.name, parts[i].value, StringComparison.Ordinal);
                        break;
                    case StyleSelectorType.Type:
                        //TODO: This tests fails to capture instances of sub-classes
                        match = string.Equals(element.typeName, parts[i].value, StringComparison.Ordinal);
                        break;
                    case StyleSelectorType.Predicate:
                        match = parts[i].tempData is UQuery.IVisualPredicateWrapper w && w.Predicate(element);
                        break;
                    case StyleSelectorType.PseudoClass:
                        // Selectors with invalid pseudo states should be rejected
                        if (selector.pseudoStateMask == StyleSelector.InvalidPseudoStateMask
                            || selector.negatedPseudoStateMask == StyleSelector.InvalidPseudoStateMask)
                        {
                            match = false;
                        }
                        break;
                    default: // ignore, all errors should have been warned before hand
                        match = false;
                        break;
                }
            }

            bool saveMatch = match;

            if (saveMatch && selector.pseudoStateMask != 0)
            {
                match = (selector.pseudoStateMask & (int)element.pseudoStates) == selector.pseudoStateMask;
            }

            if (saveMatch && selector.negatedPseudoStateMask != 0)
            {
                match &= (selector.negatedPseudoStateMask & ~(int)element.pseudoStates) == selector.negatedPseudoStateMask;
            }

            return match;
        }

        public static bool MatchRightToLeft(VisualElement element, StyleComplexSelector complexSelector)
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


                if (!MatchesSelector(current, complexSelector.selectors[nextIndex]))
                {
                    // if we have a descendant relationship, keep trying on the parent
                    // i.e., "div span", div failed on this element, try on the parent
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
    }
}
