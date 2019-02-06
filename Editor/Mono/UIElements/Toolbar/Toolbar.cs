// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.StyleSheets;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class Toolbar : VisualElement
    {
        private static readonly string s_ToolbarCommonStyleSheetPath = "StyleSheets/ToolbarCommon.uss";
        private static readonly string s_ToolbarDarkStyleSheetPath = "StyleSheets/ToolbarDark.uss";
        private static readonly string s_ToolbarLightStyleSheetPath = "StyleSheets/ToolbarLight.uss";

        private static readonly StyleSheet s_ToolbarDarkStyleSheet;
        private static readonly StyleSheet s_ToolbarLightStyleSheet;

        public new class UxmlFactory : UxmlFactory<Toolbar> {}

        static Toolbar()
        {
            s_ToolbarDarkStyleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            s_ToolbarDarkStyleSheet.hideFlags = HideFlags.HideAndDontSave;
            s_ToolbarDarkStyleSheet.isUnityStyleSheet = true;

            s_ToolbarLightStyleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            s_ToolbarLightStyleSheet.hideFlags = HideFlags.HideAndDontSave;
            s_ToolbarLightStyleSheet.isUnityStyleSheet = true;

            ReloadStyleSheets();
        }

        internal static void ReloadStyleSheets()
        {
            var defaultCommonSheet = EditorGUIUtility.Load(s_ToolbarCommonStyleSheetPath) as StyleSheet;
            var defaultDarkSheet = EditorGUIUtility.Load(s_ToolbarDarkStyleSheetPath) as StyleSheet;
            var defaultLightSheet = EditorGUIUtility.Load(s_ToolbarLightStyleSheetPath) as StyleSheet;

            UIElementsEditorUtility.ResolveStyleSheets(s_ToolbarDarkStyleSheet, defaultCommonSheet, defaultDarkSheet);
            UIElementsEditorUtility.ResolveStyleSheets(s_ToolbarLightStyleSheet, defaultCommonSheet, defaultLightSheet);
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
