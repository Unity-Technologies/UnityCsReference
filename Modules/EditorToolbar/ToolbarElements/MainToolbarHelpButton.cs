// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using static UnityEditor.EditorGUI;

namespace UnityEditor.Toolbars
{
    static class MainToolbarHelpButton
    {
        [UnityOnlyMainToolbarPreset]
        [MainToolbarElement("Services/Toolbar Help", true, defaultDockIndex = 0, defaultDockPosition = MainToolbarDockPosition.Right)]
        static MainToolbarElement QueryElementInfo()
        {
            return new MainToolbarButton(new MainToolbarContent(GUIContents.helpIcon.image as Texture2D), OpenMainToolbarAPIDocumentation);
        }

        static void OpenMainToolbarAPIDocumentation()
        {
            Application.OpenURL(Toolbar.k_MainToolbarAPIDocumentationLink);
        }
    }
}
