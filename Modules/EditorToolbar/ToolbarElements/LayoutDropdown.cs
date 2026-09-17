// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Toolbars
{
    static partial class LayoutDropdown
    {
        const string k_Path = "Editor Controls/Layout";

        // All three events are cleared on code reload, so these subscriptions have to be re-established on
        // every load. A static constructor would only run once per domain, and the layout dropdown would
        // stop refreshing its label after the first reload.
        [OnCodeLoaded]
        static void Initialize()
        {
            EditorApplication.delayCall += RebuildContent; //Immediately after a domain reload, calling check availability sometimes returns the wrong value
            ModeService.modeChanged += OnModeChanged;
            WindowLayout.lastLoadedLayoutChanged += RebuildContent;
        }

        [OnCodeUnloading]
        static void Shutdown()
        {
            ModeService.modeChanged -= OnModeChanged;
            WindowLayout.lastLoadedLayoutChanged -= RebuildContent;
        }

        static void OnModeChanged(ModeService.ModeChangedArgs args) => RebuildContent();

        static void RebuildContent()
        {
            MainToolbar.Refresh(k_Path);
        }

        [UnityOnlyMainToolbarPreset]
        [MainToolbarElement(k_Path, defaultDockIndex = 1, defaultDockPosition = MainToolbarDockPosition.Right)]
        static MainToolbarElement CreateButton()
        {
            return new MainToolbarDropdown(new MainToolbarContent(
                WindowLayout.lastLoadedLayoutName,
                EditorGUIUtility.LoadIcon("StyleSheets/Northstar/Images/layout.png"),
                L10n.Tr("Select editor layout", null)),
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
