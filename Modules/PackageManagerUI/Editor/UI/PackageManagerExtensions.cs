// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    /// <summary>
    /// Package Manager UI Extensions
    /// </summary>
    public static class PackageManagerExtensions
    {
        internal static List<IPackageManagerExtension> Extensions { get { return extensions ?? (extensions = new List<IPackageManagerExtension>()); } }
        static List<IPackageManagerExtension> extensions;

        internal static List<IPackageManagerToolbarExtension> ToolbarExtensions { get { return toolbarExtensions ?? (toolbarExtensions = new List<IPackageManagerToolbarExtension>()); } }
        static List<IPackageManagerToolbarExtension> toolbarExtensions;

        internal static List<IPackageManagerMenuExtensions> MenuExtensions { get { return menuExtensions ?? (menuExtensions = new List<IPackageManagerMenuExtensions>()); } }
        static List<IPackageManagerMenuExtensions> menuExtensions;

        /// <summary>
        /// Registers a new Package Manager UI extension
        /// </summary>
        /// <param name="extension">A Package Manager UI extension</param>
        public static void RegisterExtension(IPackageManagerExtension extension)
        {
            if (extension == null)
                return;

            Extensions.Add(extension);
        }

        /// <summary>
        /// Registers a new Package Manager UI toolbar extension
        /// </summary>
        /// <param name="extension">A Package Manager UI toolbar extension</param>
        internal static void RegisterExtension(IPackageManagerToolbarExtension extension)
        {
            if (extension == null)
                return;

            ToolbarExtensions.Add(extension);
        }

        /// <summary>
        /// Registers a new Package Manager UI toolbar extension
        /// </summary>
        /// <param name="extension">A Package Manager UI toolbar extension</param>
        internal static void RegisterExtension(IPackageManagerMenuExtensions extension)
        {
            if (extension == null)
                return;

            MenuExtensions.Add(extension);
        }

        /// <summary>
        /// Protected call to package manager extension.
        /// </summary>
        internal static void ExtensionCallback(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogError(string.Format(L10n.Tr("[Package Manager Window] Package manager extension failed with error: {0}"), exception.Message));
            }
        }
    }
}
