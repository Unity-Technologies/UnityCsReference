// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Experimental;
using UnityEditor.StyleSheets;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class Toolbar : VisualElement
    {
        private static readonly string s_ToolbarDarkStyleSheetPath = "StyleSheets/Generated/ToolbarDark.uss.asset";
        private static readonly string s_ToolbarLightStyleSheetPath = "StyleSheets/Generated/ToolbarLight.uss.asset";

        private static readonly StyleSheet s_ToolbarDarkStyleSheet;
        private static readonly StyleSheet s_ToolbarLightStyleSheet;

        public new class UxmlFactory : UxmlFactory<Toolbar> {}

        static Toolbar()
        {
            s_ToolbarDarkStyleSheet = EditorGUIUtility.Load(UIElementsEditorUtility.GetStyleSheetPathForCurrentFont(s_ToolbarDarkStyleSheetPath)) as StyleSheet;
            s_ToolbarDarkStyleSheet.isUnityStyleSheet = true;

            s_ToolbarLightStyleSheet = EditorGUIUtility.Load(UIElementsEditorUtility.GetStyleSheetPathForCurrentFont(s_ToolbarLightStyleSheetPath)) as StyleSheet;
            s_ToolbarLightStyleSheet.isUnityStyleSheet = true;
        }

        internal static void SetToolbarStyleSheet(VisualElement ve)
        {
            if (EditorGUIUtility.isProSkin)
            {
                ve.styleSheets.Add(s_ToolbarDarkStyleSheet);
            }
            else
            {
                ve.styleSheets.Add(s_ToolbarLightStyleSheet);
            }
        }

        public static readonly string ussClassName = "unity-toolbar";

        public Toolbar()
        {
            AddToClassList(ussClassName);
            SetToolbarStyleSheet(this);
        }
    }
}
