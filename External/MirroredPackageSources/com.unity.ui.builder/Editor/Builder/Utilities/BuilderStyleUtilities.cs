using UnityEngine.UIElements;
using System;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal class BuilderStyleUtilities
    {
        // Private Utilities

        static void GetInlineStyleSheetAndRule(VisualTreeAsset vta, VisualElement element, out StyleSheet styleSheet, out StyleRule styleRule)
        {
            var vea = element.GetVisualElementAsset();
            styleSheet = vta.GetOrCreateInlineStyleSheet();
            styleRule = vta.GetOrCreateInlineStyleRule(vea);
        }

        static StyleProperty GetOrCreateStylePropertyByStyleName(StyleSheet styleSheet, StyleRule styleRule, string styleName)
        {
            var styleProperty = styleSheet.FindProperty(styleRule, styleName);
            if (styleProperty == null)
                styleProperty = styleSheet.AddProperty(styleRule, styleName);

            return styleProperty;
        }

        // Inline StyleSheet Value Setters

        static public void SetInlineStyleValue(VisualTreeAsset vta, VisualElement element, string styleName, float value)
        {
            GetInlineStyleSheetAndRule(vta, element, out StyleSheet styleSheet, out StyleRule styleRule);
            SetStyleSheetRuleValue(styleSheet, styleRule, styleName, value);
        }

        static public void SetInlineStyleValue(VisualTreeAsset vta, VisualElement element, string styleName, Enum value)
        {
            GetInlineStyleSheetAndRule(vta, element, out StyleSheet styleSheet, out StyleRule styleRule);
            SetStyleSheetRuleValue(styleSheet, styleRule, styleName, value);
        }

        // StyleSheet Value Setters

        static public void SetStyleSheetRuleValue(StyleSheet styleSheet, StyleRule styleRule, string styleName, float value)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleSheet, styleRule, styleName);
            var isNewValue = styleProperty.values.Length == 0;

            // If the current style property is saved as a float instead of a dimension,
            // it means it's a user file where they left out the unit. We need to resave
            // it here as a dimension to create final proper uss.
            if (!isNewValue && styleProperty.values[0].valueType != StyleValueType.Dimension)
            {
                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            var dimension = new Dimension();
            dimension.unit = Dimension.Unit.Pixel;
            dimension.value = value;

            if (isNewValue)
                styleSheet.AddValue(styleProperty, dimension);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], dimension);
        }

        static public void SetStyleSheetRuleValue(StyleSheet styleSheet, StyleRule styleRule, string styleName, Enum value)
        {
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleSheet, styleRule, styleName);
            var isNewValue = styleProperty.values.Length == 0;

            if (!isNewValue && styleProperty.IsVariable())
            {
                styleProperty.values = new StyleValueHandle[0];
                isNewValue = true;
            }

            if (isNewValue)
                styleSheet.AddValue(styleProperty, value);
            else // TODO: Assume only one value.
                styleSheet.SetValue(styleProperty.values[0], value);
        }
    }
}
