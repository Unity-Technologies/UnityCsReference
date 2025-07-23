// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal class BuilderStyleUtilities
    {
        // Private Utilities
        static void GetInlineStyleSheetAndRule(VisualTreeAsset vta, VisualElementAsset vea, out StyleSheet styleSheet, out StyleRule styleRule)
        {
            styleSheet = vta.GetOrCreateInlineStyleSheet();
            styleRule = vta.GetOrCreateInlineStyleRule(vea);
        }

        static StyleProperty GetOrCreateStylePropertyByStyleName(StyleSheet styleSheet, StyleRule styleRule, string styleName)
        {
            var styleProperty = styleRule.FindLastProperty(styleName);
            if (styleProperty == null)
                styleProperty = styleSheet.AddProperty(styleRule, styleName);

            return styleProperty;
        }

        static void SetInlineValue<T>(
            VisualTreeAsset vta,
            VisualElementAsset vea,
            VisualElement element,
            string styleName,
            T value,
            Action<StyleProperty, StyleSheet, T> setter)
        {
            GetInlineStyleSheetAndRule(vta, vea, out var styleSheet, out var styleRule);
            Undo.RegisterCompleteObjectUndo(styleSheet, BuilderConstants.ChangeUIStyleValueUndoMessage);
            var styleProperty = GetOrCreateStylePropertyByStyleName(styleSheet, styleRule, styleName);
            setter(styleProperty, styleSheet, value);
            element?.UpdateInlineRule(styleSheet, styleRule);
        }

        // Inline StyleSheet Value Setters

        public static void SetInlineDimensionValue(VisualTreeAsset vta, VisualElement element, string styleName, Dimension value)
        {
            SetInlineValue(vta, element.GetVisualElementAsset(), element, styleName, value, (p, s, v) => p.SetDimension(s, v));
        }

        public static void SetInlineFloatValue(VisualTreeAsset vta, VisualElement element, string styleName, float value)
        {
            SetInlineValue(vta, element.GetVisualElementAsset(), element, styleName, value, (p, s, v) => p.SetFloat(s, v));
        }

        public static void SetInlineEnumValue(VisualTreeAsset vta, VisualElement element, string styleName, Enum value)
        {
            SetInlineValue(vta, element.GetVisualElementAsset(), element, styleName, value, (p, s, v) => p.SetEnum(s, v));
        }

        public static void SetInlineColorValue(VisualTreeAsset vta, VisualElementAsset vea, VisualElement element, string styleName, Color value)
        {
            SetInlineValue(vta, vea, element, styleName, value, (p, s, v) => p.SetColor(s, v));
        }

        public static string GenerateElementTargetedSelector(VisualElement documentElement)
        {
            string elementTargetedSelector;
            var classList = documentElement?.classList;

            // if element has name, use that to target it
            if (!string.IsNullOrEmpty(documentElement?.name))
            {
                elementTargetedSelector = $"#{documentElement.name}";
            }
            // if element has no name, use its class to target it
            else if (classList != null && classList.Count > 0)
            {
                elementTargetedSelector = $".{classList[^1]}";
            }
            // if element has no class, use its type to target it
            else
            {
                elementTargetedSelector = documentElement?.typeName;
            }

            // add its parents name or class or type to the selector
            if (documentElement?.parent != null && !BuilderSharedStyles.IsDocumentElement(documentElement.parent))
            {
                elementTargetedSelector = GenerateElementTargetedSelector(documentElement.parent) + " > " + elementTargetedSelector;
            }

            return elementTargetedSelector;
        }
    }
}
