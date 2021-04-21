// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements.StyleSheets;

namespace UnityEditor.UIElements
{
    static partial class UIElementsTemplate
    {
        // Add submenu after GUI Skin
        [MenuItem("Assets/Create/UI Toolkit/Style Sheet", false, 603, false)]
        private static void CreateUSSAsset()
        {
            var folder = GetCurrentFolder();
            var path = AssetDatabase.GenerateUniqueAssetPath(folder + "/NewUSSFile.uss");
            var contents = "VisualElement {}";
            var icon = EditorGUIUtility.IconContent<StyleSheet>().image as Texture2D;
            ProjectWindowUtil.CreateAssetWithContent(path, contents, icon);
        }

        [MenuItem("Assets/Create/UI Toolkit/TSS Theme File", false, 604, false)]
        public static void CreateTSSFile()
        {
            if (CommandService.Exists(nameof(CreateTSSFile)))
                CommandService.Execute(nameof(CreateTSSFile), CommandHint.Menu);
            else
            {
                var folder = GetCurrentFolder();
                var path = AssetDatabase.GenerateUniqueAssetPath(folder + "/NewTSSFile.tss");
                var contents = "VisualElement {}";
                var icon = EditorGUIUtility.IconContent<ThemeStyleSheet>().image as Texture2D;
                ProjectWindowUtil.CreateAssetWithContent(path, contents, icon);
            }
        }

        [MenuItem("Assets/Create/UI Toolkit/Default Runtime Theme File", false, 605, false)]
        public static void CreateDefaultRuntimeTSSFile()
        {
            if (CommandService.Exists(nameof(CreateDefaultRuntimeTSSFile)))
                CommandService.Execute(nameof(CreateDefaultRuntimeTSSFile), CommandHint.Menu);
            else
            {
                var folder = GetCurrentFolder();
                var path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + ThemeRegistry.kUnityRuntimeThemeFileName);
                var contents = "@import url(\"" + ThemeRegistry.kThemeScheme + "://default\");\nVisualElement {}";
                var icon = EditorGUIUtility.IconContent<ThemeStyleSheet>().image as Texture2D;
                ProjectWindowUtil.CreateAssetWithContent(path, contents, icon);
            }
        }
    }
}
