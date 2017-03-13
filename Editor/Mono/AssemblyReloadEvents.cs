// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    internal class AssemblyReloadEvents
    {
        // Called from C++
        public static void OnBeforeAssemblyReload()
        {
            InternalEditorUtility.AuxWindowManager_OnAssemblyReload();
        }

        // Called from C++
        public static void OnAfterAssemblyReload()
        {
            // Repaint to ensure ProjectBrowser UI is initialized. This fixes the issue where a new script is created from the application menu
            // bar while the project browser ui was not initialized.
            foreach (ProjectBrowser pb in ProjectBrowser.GetAllProjectBrowsers())
                pb.Repaint();
        }
    }
}
