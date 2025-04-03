// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditor.StyleSheets;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal static class StyleComplexSelectorExtensions
    {
        private const string k_DescendantSymbol = ">";

        internal static bool InitializeSelector(StyleComplexSelector complexSelector, string complexSelectorStr, out string error)
        {
            complexSelectorStr = complexSelectorStr.RemoveExtraWhitespace();
            var selectorSplit = complexSelectorStr.Split(' ');

            var fullSpecificity = CSSSpec.GetSelectorSpecificity(complexSelectorStr);
            if (fullSpecificity == 0)
            {
                error = $"Selector '{complexSelectorStr}' is invalid: failed to calculate selector specificity.";
                return false;
            }

            var simpleSelectors = new List<StyleSelector>();
            var previousRelationship = StyleSelectorRelationship.None;
            foreach (var simpleSelectorStr in selectorSplit)
            {
                if (simpleSelectorStr == k_DescendantSymbol)
                {
                    previousRelationship = StyleSelectorRelationship.Child;
                    continue;
                }

                if (!CSSSpec.ParseSelector(simpleSelectorStr, out var parts))
                {
                    error = $"Selector '{complexSelectorStr}' is invalid: the selector could not be parsed.";
                    return false;
                }
                if (parts.Any(p => p.type == StyleSelectorType.Unknown))
                {
                    error = $"Selector '{complexSelectorStr}' is invalid: the selector contains unknown parts.";
                    return false;
                }
                if (parts.Any(p => p.type == StyleSelectorType.RecursivePseudoClass))
                {
                    error = $"Selector '{complexSelectorStr}' is invalid: the selector contains recursive parts.";
                    return false;
                }

                var simpleSelector = new StyleSelector
                {
                    parts = parts,
                    previousRelationship = previousRelationship
                };
                simpleSelectors.Add(simpleSelector);

                // This is the default (if no > came before).
                previousRelationship = StyleSelectorRelationship.Descendent;
            }

            complexSelector.selectors = simpleSelectors.ToArray();
            complexSelector.specificity = fullSpecificity;
            error = null;
            return true;
        }

        public static bool SetSelectorString(this StyleSheet styleSheet, StyleComplexSelector complexSelector, string newComplexSelectorStr, out string error)
        {
            if (InitializeSelector(complexSelector, newComplexSelectorStr, out error))
            {
                if (styleSheet)
                    styleSheet.SetTemporaryContentHash();
                return true;
            }

            return false;
        }

        public static StyleProperty FindProperty(this StyleSheet styleSheet, StyleComplexSelector selector, string propertyName)
        {
            return styleSheet.FindLastProperty(selector.rule, propertyName);
        }

        public static StyleProperty AddProperty(
            this StyleSheet styleSheet, StyleComplexSelector selector, string name,
            string undoMessage = null)
        {
            return styleSheet.AddProperty(selector.rule, name, undoMessage);
        }

        internal static void RemoveProperty(
            this StyleSheet styleSheet, StyleComplexSelector selector,
            StyleProperty property, string undoMessage = null)
        {
            styleSheet.RemoveProperty(selector.rule, property, undoMessage);
        }

        internal static void RemoveProperty(
            this StyleSheet styleSheet, StyleComplexSelector selector, string name,
            string undoMessage = null)
        {
            styleSheet.RemoveProperty(selector.rule, name, undoMessage);
        }

        public static bool IsSelected(this StyleComplexSelector scs)
        {
            var selectionProperty = scs.FindProperty(BuilderConstants.SelectedStyleRulePropertyName);
            return selectionProperty != null;
        }
    }
}
