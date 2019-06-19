// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    /// <summary>
    /// Interface for Package Manager UI Menu Extension
    /// </summary>
    internal interface IPackageManagerMenuExtensions
    {
        /// <summary>
        /// Called by the Package Manager UI when the advanced menu is being created. You can add your own items to the menu.
        /// </summary>
        /// <param name="menu">The menu item being created</param>
        void OnAdvancedMenuCreate(DropdownMenu menu);

        /// <summary>
        /// Called by the Package Manager UI when the add menu is being created. You can add your own items to the menu.
        /// </summary>
        /// <param name="menu">The menu item being created</param>
        void OnAddMenuCreate(DropdownMenu menu);

        /// <summary>
        /// Called by the Package Manager UI when the filter menu is being created. You can add your own items to the menu.
        /// </summary>
        /// <param name="menu">The menu item being created</param>
        void OnFilterMenuCreate(DropdownMenu menu);
    }
}
