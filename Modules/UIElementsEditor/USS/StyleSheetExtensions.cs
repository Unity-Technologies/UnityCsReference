// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal static class StyleSheetExtensions
    {
        static StyleSheetExporter s_Exporter = new();

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
    }
}
