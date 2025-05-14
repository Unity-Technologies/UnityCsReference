// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    public static class EditorToolbarUtility
    {
        const string k_UxmlPath = "UXML/Toolbars/";
        const string k_StyleSheetsPath = "StyleSheets/Toolbars/";
        internal const string buttonStripClassName = "unity-editor-toolbar__button-strip";
        internal const string stripElementClassName = buttonStripClassName + "-element";
        internal const string leftStripElementClassName = stripElementClassName + "--left";
        internal const string middleStripElementClassName = stripElementClassName + "--middle";
        internal const string rightStripElementClassName = stripElementClassName + "--right";
        internal const string aloneStripElementClassName = stripElementClassName + "--alone";
        static bool IsRendered(VisualElement element) => element.style.display != DisplayStyle.None && element.visible;

        public static void SetupChildrenAsButtonStrip(VisualElement root)
        {
            root.AddToClassList(buttonStripClassName);
            foreach(var child in root.Children())
                child.AddToClassList(stripElementClassName);
            ApplyButtonStripStylesToChildren(root);
        }
        internal static void ApplyButtonStripStylesToChildren(VisualElement root)
        {
            int count = root.hierarchy.childCount;

            int begin = -1, end = -1;

            for (int i = count - 1; end < 0 && i > -1; i--)
            {
                if (IsRendered(root.ElementAt(i)))
                    end = i;
            }

            for (var i = 0; i < count; ++i)
            {
                var element = root.hierarchy.ElementAt(i);

                if (!IsRendered(element))
                    continue;

                if (begin < 0)
                    begin = i;

                element.EnableInClassList(aloneStripElementClassName, i == begin && i == end);
                element.EnableInClassList(leftStripElementClassName, i == begin && i != end);
                element.EnableInClassList(rightStripElementClassName, i == end && i != begin);
                element.EnableInClassList(middleStripElementClassName, i != begin && i != end);
            }
        }
        internal static void LoadStyleSheets(string name, VisualElement target)
        {
            var path = k_StyleSheetsPath + name;

            var common = EditorGUIUtility.Load($"{path}Common.uss") as StyleSheet;
            if (common != null)
                target.styleSheets.Add(common);

            var themeSpecificName = EditorGUIUtility.isProSkin ? "Dark" : "Light";
            var themeSpecific = EditorGUIUtility.Load($"{path}{themeSpecificName}.uss") as StyleSheet;
            if (themeSpecific != null)
                target.styleSheets.Add(themeSpecific);
        }

        internal static VisualTreeAsset LoadUxml(string name)
        {
            return EditorGUIUtility.Load($"{k_UxmlPath}{name}.uxml") as VisualTreeAsset;
        }

        internal static void UpdateIconContent(
        string text,
        string textIcon,
        Texture2D icon,
        TextElement textElement,
        TextElement textIconElement,
        Image iconElement)
        {
            if (text == string.Empty)
            {
                textElement.style.display = DisplayStyle.None;
                textIconElement.style.display = DisplayStyle.None;
            }
            else
                textElement.style.display = StyleKeyword.Null;

            // First priority: image icon, if available
            if (icon != null)
            {
                if (iconElement != null)
                {
                    iconElement.style.display = DisplayStyle.Flex;
                    iconElement.image = icon;
                }
                if (textIconElement != null)
                    textIconElement.style.display = DisplayStyle.None;
            }
            else if (iconElement != null && iconElement.resolvedStyle.backgroundImage != null)
            {
                if (textIconElement != null)
                    textIconElement.style.display = DisplayStyle.None;
                iconElement.style.display = DisplayStyle.Flex;
            }
            // Second priority: text icon, if available
            else if (!string.IsNullOrEmpty(textIcon))
            {
                if (textIconElement != null)
                {
                    textIconElement.style.display = DisplayStyle.Flex;
                    textIconElement.text = OverlayUtilities.GetSignificantLettersForIcon(textIcon);
                }
                if (iconElement != null)
                    iconElement.style.display = DisplayStyle.None;
            }
            // Fall back: abbreviation of text.
            else if (!string.IsNullOrEmpty(text))
            {
                if (textIconElement != null)
                {
                    textIconElement.style.display = StyleKeyword.Null;
                    textIconElement.text = OverlayUtilities.GetSignificantLettersForIcon(text);
                }
                if (iconElement != null)
                    iconElement.style.display = DisplayStyle.None;
            }
            else
            {
                if (iconElement != null)
                    iconElement.style.display = DisplayStyle.Flex;
            }
        }
    }
}
