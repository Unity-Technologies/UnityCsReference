// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    /// <summary>
    /// Interface for Package Manager UI Extension
    /// </summary>
    internal interface IPackageManagerToolbarExtension
    {
        /// <summary>
        /// Called by the Package Manager UI when the package selection changed.
        /// </summary>
        /// <param name="packageInfo">The newly selected package information (can be null)</param>
        /// <param name="toolbar">The toolbar VisualElement to add new action into.</param>
        void OnPackageSelectionChange(PackageInfo packageInfo, VisualElement toolbar);

        /// <summary>
        /// Called by the Package Manager UI when its window is being closed
        /// </summary>
        void OnWindowDestroy();
    }
}
