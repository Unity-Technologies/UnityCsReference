// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor
{
    static class SceneViewToolbarStyles
    {
        const string k_StyleSheet = "StyleSheets/SceneViewToolbarElements/SceneViewToolbarElements.uss";
        const string k_StyleLight = "StyleSheets/SceneViewToolbarElements/SceneViewToolbarElementsLight.uss";
        const string k_StyleDark = "StyleSheets/SceneViewToolbarElements/SceneViewToolbarElementsDark.uss";

        static StyleSheet s_Style;
        static StyleSheet s_Skin;
        internal static  void AddStyleSheets(VisualElement ve)
        {
            if (s_Skin == null)
            {
                if (EditorGUIUtility.isProSkin)
                    s_Skin = EditorGUIUtility.Load(k_StyleDark) as StyleSheet;
                else
                    s_Skin = EditorGUIUtility.Load(k_StyleLight) as StyleSheet;
            }
            if (s_Style == null)
            {
                s_Style = EditorGUIUtility.Load(k_StyleSheet) as StyleSheet;
            }
            ve.styleSheets.Add(s_Style);
            ve.styleSheets.Add(s_Skin);
        }
    }
}
