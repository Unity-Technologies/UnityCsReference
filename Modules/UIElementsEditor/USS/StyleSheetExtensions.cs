// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal static class StyleSheetExtensions
    {
        static StyleSheetExporter s_Exporter = new();
        static Regex styleSelectorRegex { get; } = new(@"^[a-zA-Z0-9\-_:#\*>. ]+$");

        /// <summary>
        /// Copies selectors and properties from one StyleRule to another, preserving multi-selector rules.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal static void SwallowStyleRule(StyleSheet toStyleSheet, StyleRule toRule, StyleSheet fromStyleSheet, StyleRule fromRule)
        {
            // Copy each selector individually to preserve multi-selector rules
            foreach (var complexSelector in fromRule.complexSelectors)
            {
                var selectorString = s_Exporter.ToUssString(fromStyleSheet, complexSelector);
                toRule.AddSelector(selectorString);
            }

            // Copy all properties
            foreach (var fromProperty in fromRule.properties)
            {
                var toProperty = toRule.AddProperty(fromProperty.name);
                StyleSheetUtility.TransferStylePropertyHandles(fromStyleSheet, fromProperty, toStyleSheet, toProperty);
            }
        }

        public static void Swallow(this StyleSheet toStyleSheet, StyleSheet fromStyleSheet)
        {
            foreach (var fromRule in fromStyleSheet.rules)
            {
                var toRule = toStyleSheet.AddRule();
                SwallowStyleRule(toStyleSheet, toRule, fromStyleSheet, fromRule);
            }

            toStyleSheet.contentHash = fromStyleSheet.contentHash;
        }

        /// <summary>
        /// Validates a selector string without creating it.
        /// </summary>
        public static bool ValidateSelector(string selectorString, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrEmpty(selectorString))
            {
                errorMessage = "Selector string is empty.";
                return false;
            }

            if (!styleSelectorRegex.IsMatch(selectorString))
            {
                errorMessage = "Style Selector can only contain *_-.#>, letters, and numbers.";
                return false;
            }

            if (!CSSSpec.ValidateSelector(selectorString) &&
                !SelectorUtility.ExtractSelectorsAndSpecificityFromString(selectorString, out var selectors, out var specificity, out errorMessage))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Splits a comma-separated selector list, trimming entries and dropping empty or whitespace-only groups.
        /// </summary>
        public static string[] SplitSelectors(string selectorList)
        {
            if (string.IsNullOrEmpty(selectorList))
                return [];

            using var _ = ListPool<string>.Get(out var selectors);
            foreach (var selector in selectorList.Split(','))
            {
                if (!string.IsNullOrWhiteSpace(selector))
                    selectors.Add(selector.Trim());
            }
            return selectors.ToArray();
        }

        public static bool ValidateStyleRule(string selectorString, out string errorMessage)
        {
            errorMessage = null;

            var selectors = SplitSelectors(selectorString);
            if (selectors.Length == 0)
            {
                errorMessage = "Selector string is empty.";
                return false;
            }

            foreach (var selector in selectors)
            {
                if (!ValidateSingleSelector(selector, out errorMessage))
                    return false;
            }

            return true;
        }

        static bool ValidateSingleSelector(string selectorString, out string errorMessage)
        {
            errorMessage = null;

            if (!styleSelectorRegex.IsMatch(selectorString))
            {
                errorMessage = "Style Selector can only contain *_-.#>, letters, and numbers.";
                return false;
            }

            return SelectorUtility.ExtractSelectorsAndSpecificityFromString(selectorString, out _, out _, out errorMessage);
        }
    }
}
