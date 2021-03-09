// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.PackageManager.UI
{
    /// <summary>
    /// A struct that contains the current selected package and version in the Package Manager window
    /// </summary>
    internal struct PackageSelectionArgs
    {
        /// <summary>
        /// The selected package
        /// </summary>
        public IPackage package { get; internal set; }

        /// <summary>
        /// The selected package version
        /// </summary>
        public IPackageVersion packageVersion { get; internal set; }

        /// <summary>
        /// The handle to the Package Manager window
        /// </summary>
        public IWindow window { get; internal set; }
    }
}
