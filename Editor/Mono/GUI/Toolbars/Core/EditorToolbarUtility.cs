// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
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
    }
}
