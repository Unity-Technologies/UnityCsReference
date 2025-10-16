// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.LevelPlay
{
    internal static class LevelPlayInstallMenuItem
    {
        private static string s_MenuPath = "Services/Ads Mediation (LevelPlay)/Install";
        private static string s_MenuInstallationMethod = "menuItem";
        private static bool s_menuItemChanged = false;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // Add a callback to check the condition periodically
            EditorApplication.update += UpdateMenuItem;

            // UUM-113166 - Linux does not update menus immediately after Editor startup. This potentially leaves menu items
            // that have been added via scripts not visible until the user forces a refresh (e.g. by opening a context menu).
            // Menu refreshing is complicated and can lead to crashes if not done carefully, so we only do it once to ensure
            // that we've picked up these menu changes.
            EditorApplication.delayCall += RefreshMenu;
        }

        private static void RefreshMenu()
        {
            if (s_menuItemChanged)
            {
                EditorUtility.Internal_UpdateAllMenus();
                s_menuItemChanged = false;
            }
        }

        private static void UpdateMenuItem()
        {
            // Check the condition
            if (!LevelPlayInstaller.PackageIsInstalled() && !Menu.MenuItemExists(s_MenuPath))
                AddMenuItem();
            else if (LevelPlayInstaller.PackageIsInstalled() && Menu.MenuItemExists(s_MenuPath))
                RemoveMenuItem();
        }

        private static void AddMenuItem()
        {
            Menu.AddMenuItem(s_MenuPath, "", false, 0, () =>
            {
                PackageManager.Client.Add(LevelPlayInstaller.s_ManagementPackageId);
                LevelPlayQuickInstallAnalytic.SendEvent(s_MenuInstallationMethod);
            }, null);
            s_menuItemChanged = true;
        }

        private static void RemoveMenuItem()
        {
            Menu.RemoveMenuItem(s_MenuPath);
            s_menuItemChanged = true;
        }
    }
}
