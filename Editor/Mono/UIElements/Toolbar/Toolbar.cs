// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class Toolbar : VisualElement
    {
        private static readonly string s_ToolbarCommonStyleSheetPath = "StyleSheets/ToolbarCommon.uss";
        private static readonly string s_ToolbarDarkStyleSheetPath = "StyleSheets/ToolbarDark.uss";
        private static readonly string s_ToolbarLightStyleSheetPath = "StyleSheets/ToolbarLight.uss";

        private static readonly StyleSheet s_ToolbarCommonStyleSheet;
        private static readonly StyleSheet s_ToolbarDarkStyleSheet;
        private static readonly StyleSheet s_ToolbarLightStyleSheet;

        public new class UxmlFactory : UxmlFactory<Toolbar> {}

        static Toolbar()
        {
            s_ToolbarCommonStyleSheet = EditorGUIUtility.Load(s_ToolbarCommonStyleSheetPath) as StyleSheet;
            s_ToolbarCommonStyleSheet.isUnityStyleSheet = true;

            s_ToolbarDarkStyleSheet = EditorGUIUtility.Load(s_ToolbarDarkStyleSheetPath) as StyleSheet;
            s_ToolbarDarkStyleSheet.isUnityStyleSheet = true;

            s_ToolbarLightStyleSheet = EditorGUIUtility.Load(s_ToolbarLightStyleSheetPath) as StyleSheet;
            s_ToolbarLightStyleSheet.isUnityStyleSheet = true;
        }

        internal static void SetToolbarStyleSheet(VisualElement ve)
        {
            ve.styleSheets.Add(s_ToolbarCommonStyleSheet);
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
