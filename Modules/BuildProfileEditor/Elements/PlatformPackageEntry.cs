// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// <see cref="PlatformPackageItem"/> entry state.
    /// </summary>
    internal class PlatformPackageEntry
    {
        /// <summary>
        /// Package identifier.
        /// </summary>
        public string packageName { get; private set; }

        /// <summary>
        /// Required package for a build profile targeting the current platform.
        /// Will automatically be installed when a build profile is created.
        /// </summary>
        public bool required { get; private set; }

        /// <summary>
        /// Set when <see cref="packageName"/> is installed.
        /// </summary>
        public bool isInstalled { get; set; }

        /// <summary>
        /// Set when a recommended package should be installed.
        /// </summary>
        public bool shouldInstalled { get; set; }

        public PlatformPackageEntry()
        {
            packageName = string.Empty;
            shouldInstalled = false;
            required = false;
            isInstalled = false;
        }

        /// <param name="name"><see cref="packageName"/></param>
        /// <param name="selected">Set when package is toggled on for installation.</param>
        /// <param name="required"><see cref="required"/></param>
        /// <param name="isInstalled"><see cref="isInstalled"/></param>
        public PlatformPackageEntry(string name, bool selected, bool required, bool isInstalled)
        {
            packageName = name;
            shouldInstalled = required || selected;
            this.required = required;
            this.isInstalled = isInstalled;
        }
    }
}
