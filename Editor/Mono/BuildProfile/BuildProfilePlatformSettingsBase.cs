// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Base class for platform module specific build settings.
    /// Implementation fetched from BuildProfileExtension, <see cref="ModuleManager.GetBuildProfileExtension"/>.
    /// </summary>
    [Serializable]
    internal abstract class BuildProfilePlatformSettingsBase
    {
        /// <summary>
        /// Set platform setting based on strings for name and value. Native
        /// calls this to keep build profiles and EditorUserBuildSettings 
        /// PlatformSettings dictionary in sync for backward compatibility.
        /// </summary>
        public virtual void SetRawPlatformSetting(string name, string value)
        {
        }

        /// <summary>
        /// Get platform setting value based on its name. Native calls this to
        /// keep build profiles and EditorUserBuildSettings PlatformSettings
        /// dictionary settings in sync for backward compatibility.
        /// </summary>
        public virtual string GetRawPlatformSetting(string name)
        {
            return string.Empty;
        }
    }
}
