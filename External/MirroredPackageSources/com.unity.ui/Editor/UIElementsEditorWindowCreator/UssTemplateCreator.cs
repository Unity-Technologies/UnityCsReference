using UnityEngine;
using UnityEngine.UIElements;

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
    }
}
