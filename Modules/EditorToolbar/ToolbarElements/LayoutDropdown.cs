// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.Toolbars
{
    static class LayoutDropdown
    {
        const string k_Path = "Editor Utility/Layout";

        static LayoutDropdown()
        {
            EditorApplication.delayCall += RebuildContent; //Immediately after a domain reload, calling check availability sometimes returns the wrong value
            ModeService.modeChanged += (args) => RebuildContent();
            WindowLayout.lastLoadedLayoutChanged += RebuildContent;
        }

        static void RebuildContent()
        {
            MainToolbar.Refresh(k_Path);
        }

        [UnityOnlyMainToolbarPreset]
        [MainToolbarElement(k_Path, true, defaultDockIndex = 0, defaultDockPosition = MainToolbarDockPosition.Right)]
        static MainToolbarElement CreateButton()
        {
            return new MainToolbarDropdown(new MainToolbarContent(
                WindowLayout.lastLoadedLayoutName,
                EditorGUIUtility.LoadIcon("StyleSheets/Northstar/Images/layout.png"),
                L10n.Tr("Select editor layout")),
                (buttonRect) => OpenLayoutWindow(buttonRect))
            {
                displayed = ModeService.HasCapability(ModeCapability.LayoutWindowMenu, true)
            };
        }

        static void OpenLayoutWindow(Rect buttonRect)
        {
            Vector2 temp = GUIUtility.GUIToScreenPoint(new Vector2(buttonRect.x, buttonRect.y));
            buttonRect.x = temp.x;
            buttonRect.y = temp.y;
            EditorUtility.Internal_DisplayPopupMenu(buttonRect, "Window/Layouts", null, 0, true);
        }
    }
}
