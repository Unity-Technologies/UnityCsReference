// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static class WindowLayout
    {
        public static ContainerWindowProxy ShowWindowWithDynamicLayout(string windowId, string layoutFile)
        {
            var containerWindow = UnityEditor.WindowLayout.ShowWindowWithDynamicLayout(windowId, layoutFile);
            return containerWindow != null ? new ContainerWindowProxy(containerWindow) : null;
        }

        public static void SaveWindowLayout(string path)
        {
            var containerWindows = Resources.FindObjectsOfTypeAll<ContainerWindow>();
            if (containerWindows == null || containerWindows.Length <= 0)
            {
                return; // No valid Windows to save.
            }

            var window = containerWindows[0];
            if (window.rootView == null)
            {
                return; // No valid Root Views to save.
            }

            UnityEditor.WindowLayout.SaveWindowLayout(path, false);
        }

        public static ContainerWindowProxy LoadWindowLayout(string windowId, string path)
        {
            // This ensures that layout created with DynamicLayout and without official Main window can be saved and restored.
            const UnityEditor.WindowLayout.LoadWindowLayoutFlags flags = UnityEditor.WindowLayout.LoadWindowLayoutFlags.LogsErrorToConsole | UnityEditor.WindowLayout.LoadWindowLayoutFlags.NoMainWindowSupport;
            var hasLoaded = UnityEditor.WindowLayout.TryLoadWindowLayout(path, flags);
            if (hasLoaded)
            {
                var containerWindows = Resources.FindObjectsOfTypeAll<ContainerWindow>();
                if (containerWindows.Length > 0)
                {
                    var window = containerWindows[0];
                    window.windowID = windowId;
                    return new ContainerWindowProxy(window);
                }
            }
            return null;
        }

        public static bool TryOpenProjectSettingsWindow(string settingPath)
        {
            return SettingsWindow.Show(SettingsScope.Project, settingPath) != null;
        }

        public static bool IsTooltipViewVisible()
        {
            return TooltipView.S_guiView != null && TooltipView.S_guiView.window != null;
        }
    }
}
