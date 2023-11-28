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
        public static readonly string forceSkipDataBuild = L10n.Tr("Force skip data build");
        public static readonly string cleanBuild = L10n.Tr("Clean Build...");
        public static readonly string learnMoreUnityDevOps = L10n.Tr("Learn about Unity Dev Ops");
        public static readonly string buildProfileWelcome = L10n.Tr("Welcome to Build Profiles."
            + "\n\nAdd a Build Profile to configure as many builds as you need for any supported platform. "
            + "Build profiles are stored as assets you can share with your team");
        public static readonly string all = L10n.Tr("All");
        public static readonly string addBuildProfile = L10n.Tr("Add Build Profile");
        public static readonly string buildProfilesName = L10n.Tr("Build Profiles");
        public static readonly string platforms = L10n.Tr("Platforms");
        public static readonly string build = L10n.Tr("Build");
        public static readonly string buildAndRun = L10n.Tr("Build And Run");
        public static readonly string buildSettings = L10n.Tr("Build Settings");
        public static readonly string buildData = L10n.Tr("Build Data");
        public static readonly string sharedSettingsInfo =
            L10n.Tr("Platform builds use the shared scene list. To change the scene list or other settings independently, create a Build Profile for this platform.");
        public static readonly string activate = L10n.Tr("Switch Profile");
        public static readonly string sceneList = L10n.Tr("Scene List");

        // Platform Discovery Window
        public static readonly string platformDiscoveryTitle = L10n.Tr("Platform Browser");
        public static readonly string noModuleFoundWarning = L10n.Tr("No module found for the selected profile.");
        public static readonly string notSupportedWarning = L10n.Tr("Target platform does not currently support build profiles.");
        public static readonly string active = L10n.Tr("Active");
    }
}
