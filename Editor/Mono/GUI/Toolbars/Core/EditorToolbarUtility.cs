// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    static class EditorToolbarUtility
    {
        const string k_UxmlPath = "UXML/Toolbars/";
        const string k_StyleSheetsPath = "StyleSheets/Toolbars/";
        internal const string stripElementClassName = "unity-editor-toolbar__button-strip-element";
        internal const string leftStripElementClassName = stripElementClassName + "--left";
        internal const string middleStripElementClassName = stripElementClassName + "--middle";
        internal const string rightStripElementClassName = stripElementClassName + "--right";
        internal const string aloneStripElementClassName = stripElementClassName + "--alone";

        public static void SetupChildrenAsButtonStrip(VisualElement root)
        {
            int count = root.hierarchy.childCount;
            for (var i = 0; i < count; ++i)
            {
                var element = root.hierarchy.ElementAt(i);

                //Skip if element not visible
                bool visible = element.style.display != DisplayStyle.None && element.visible;

                element.AddToClassList(stripElementClassName);
                element.EnableInClassList(aloneStripElementClassName, visible && count == 1);
                element.EnableInClassList(leftStripElementClassName, visible && i == 0);
                element.EnableInClassList(rightStripElementClassName, visible && i == count - 1);
                element.EnableInClassList(middleStripElementClassName, visible && i > 0 && i < count - 1);
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

        internal static VisualElement AddIconElement(VisualElement target)
        {
            var icon = new VisualElement();
            icon.AddToClassList(EditorToolbar.elementIconClassName);
            target.Add(icon);
            return icon;
        }

        internal static TextElement MakeDropdown(VisualElement target)
        {
            var text = AddTextElement(target);
            text.style.flexGrow = 1;
            AddArrowElement(target);
            return text;
        }

        internal static TextElement AddTextElement(VisualElement target)
        {
            var label = new TextElement();
            label.AddToClassList(EditorToolbar.elementLabelClassName);
            target.Add(label);
            return label;
        }

        internal static void AddArrowElement(VisualElement target)
        {
            var arrow = new VisualElement();
            arrow.AddToClassList("unity-icon-arrow");
            target.Add(arrow);
        }
    }
}
