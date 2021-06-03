// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
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

        public static void SetupChildrenAsButtonStrip(VisualElement root)
        {
            root.AddToClassList(buttonStripClassName);

            int count = root.hierarchy.childCount;

            if (count == 1)
            {
                var element = root.hierarchy.ElementAt(0);
                bool visible = element.style.display != DisplayStyle.None && element.visible;
                element.EnableInClassList(aloneStripElementClassName, visible);
            }
            else
            {
                for (var i = 0; i < count; ++i)
                {
                    var element = root.hierarchy.ElementAt(i);

                    //Skip if element not visible
                    bool visible = element.style.display != DisplayStyle.None && element.visible;

                    element.AddToClassList(stripElementClassName);
                    element.EnableInClassList(leftStripElementClassName, visible && i == 0);
                    element.EnableInClassList(rightStripElementClassName, visible && i == count - 1);
                    element.EnableInClassList(middleStripElementClassName, visible && i > 0 && i < count - 1);
                }
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
