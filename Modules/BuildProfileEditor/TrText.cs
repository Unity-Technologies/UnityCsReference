// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Helper class for build profile window localization.
    /// </summary>
    internal class TrText
    {
        public static readonly string buildProfilesName = L10n.Tr("Build Profiles");
        public static readonly string classicPlatforms = L10n.Tr("Classic Platforms");
        public static readonly string build = L10n.Tr("Build");
        public static readonly string buildSettings = L10n.Tr("Build Settings");
        public static readonly string buildData = L10n.Tr("Build Data");
        public static readonly string sharedSettingsInfo =
            L10n.Tr("Platform builds use the shared scene list. To change the scene list or other settings independently, create a Build Profile for this platform.");
    }
}
