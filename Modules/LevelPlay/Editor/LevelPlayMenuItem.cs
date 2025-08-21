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

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // Add a callback to check the condition periodically
            EditorApplication.update += UpdateMenuItem;
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
        }

        private static void RemoveMenuItem()
        {
            Menu.RemoveMenuItem(s_MenuPath);
        }
    }
}
