// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI
{
    /// <summary>
    /// Interface for handlers for the PackageSelectionChanged event
    /// </summary>
    internal interface IPackageSelectionChangedHandler
    {
        /// <summary>
        /// Called when the package selection is changed in the Package Manager window.
        /// </summary>
        /// <param name="args">The arguments for the selection changed event.</param>
        void OnPackageSelectionChanged(PackageSelectionArgs args);
    }
}
