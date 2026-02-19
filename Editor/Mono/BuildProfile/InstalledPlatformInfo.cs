// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Build.Profile
{

    /// <summary>
    /// Represents information about an installed platform module.
    /// Platforms are uniquely identified by their GUID.
    /// </summary>
    public struct InstalledPlatformInfo
    {
        /// <summary>
        /// Unique identifier for the build platform (for example, Android, iOS, Windows Standalone, Meta Quest, Windows Server).
        /// </summary>
        public UnityEngine.GUID platformGuid;

        /// <summary>
        /// Human-readable name of the platform displayed in the build window.
        /// </summary>
        public string displayName;
    }
}
