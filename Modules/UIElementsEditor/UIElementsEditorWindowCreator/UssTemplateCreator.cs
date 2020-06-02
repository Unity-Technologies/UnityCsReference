// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    static partial class UIElementsTemplate
    {
        // Add submenu after GUI Skin
        [MenuItem("Assets/Create/UI Toolkit/Style Sheet", false, 603, false)]
        public static void CreateUSSFile()
        {
            if (CommandService.Exists(nameof(CreateUSSFile)))
                CommandService.Execute(nameof(CreateUSSFile), CommandHint.Menu);
            else
            {
                var folder = GetCurrentFolder();
                var path = AssetDatabase.GenerateUniqueAssetPath(folder + "/NewUSSFile.uss");
                var contents = "VisualElement {}";
                var icon = EditorGUIUtility.IconContent<StyleSheet>().image as Texture2D;
                ProjectWindowUtil.CreateAssetWithContent(path, contents, icon);
            }
        }
    }
}
