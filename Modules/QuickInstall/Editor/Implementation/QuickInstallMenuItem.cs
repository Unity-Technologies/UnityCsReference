// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.QuickInstall
{
    internal class QuickInstallMenuItem
    {
        readonly MenuConfig m_Config;
        readonly string m_PackageName;
        static bool s_RefreshScheduled = false;

        internal QuickInstallMenuItem(string packageName, MenuConfig config)
        {
            m_PackageName = packageName;
            m_Config = config;
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
            if (Menu.MenuItemExists(m_Config.MenuPath))
                return;
            
            Action onClick = () => QuickInstaller.InstallPackage(m_PackageName, InstallMethod.MenuItem);
            Menu.AddMenuItem(m_Config.MenuPath, "", false, m_Config.Priority, onClick, null);
            if (Application.platform == RuntimePlatform.LinuxEditor && !s_RefreshScheduled)
            {
                s_RefreshScheduled = true;
                EditorApplication.delayCall += RefreshMenu;
            }
        }

        internal void RemoveMenuItem()
        {
            if (!Menu.MenuItemExists(m_Config.MenuPath))
                return;
            
            Menu.RemoveMenuItem(m_Config.MenuPath);

            // Traverse up the menu path and remove any empty submenus. Exit early if we encounter a non-empty submenu.
            var subpath = m_Config.MenuPath;
            while (subpath.Contains("/"))
            {
                subpath = subpath.Substring(0, subpath.LastIndexOf("/"));
                if (Menu.GetMenuItems(subpath, false, false).Length == 0)
                    Menu.RemoveMenuItem(subpath);
                else
                    break;
            }
            if (Application.platform == RuntimePlatform.LinuxEditor && !s_RefreshScheduled)
            {
                s_RefreshScheduled = true;
                EditorApplication.delayCall += RefreshMenu;
            }
        }
    }
}
