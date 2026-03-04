// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.QuickInstall
{
    internal class QuickInstallMenuItem
    {
        readonly string m_MenuPath;
        readonly string m_PackageId;
        static bool s_RefreshScheduled = false;

        internal QuickInstallMenuItem(string packageId, string menuPath)
        {
            m_MenuPath = menuPath;
            m_PackageId = packageId;
        }


        // UUM-113166 - Linux does not update menus immediately after Editor startup. This potentially leaves menu items
        // that have been added via scripts not visible until the user forces a refresh (e.g. by opening a context menu).
        // Menu refreshing is complicated and can lead to crashes if not done carefully, so we only do it once to ensure
        // that we've picked up these menu changes.
        void RefreshMenu()
        {
            if (s_RefreshScheduled)
            {
                EditorUtility.Internal_UpdateAllMenus();
                s_RefreshScheduled = false;
            }
        }

        internal void AddMenuItem()
        {
            if (Menu.MenuItemExists(m_MenuPath))
                return;
            
            Action onClick = () => QuickInstaller.InstallPackage(m_PackageId, InstallMethod.MenuItem);
            Menu.AddMenuItem(m_MenuPath, "", false, 0, onClick, null);
            if (Application.platform == RuntimePlatform.LinuxEditor && !s_RefreshScheduled)
            {
                s_RefreshScheduled = true;
                EditorApplication.delayCall += RefreshMenu;
            }
        }

        internal void RemoveMenuItem()
        {
            if (!Menu.MenuItemExists(m_MenuPath))
                return;
            
            Menu.RemoveMenuItem(m_MenuPath);
            if (Application.platform == RuntimePlatform.LinuxEditor && !s_RefreshScheduled)
            {
                s_RefreshScheduled = true;
                EditorApplication.delayCall += RefreshMenu;
            }
        }
    }
}
