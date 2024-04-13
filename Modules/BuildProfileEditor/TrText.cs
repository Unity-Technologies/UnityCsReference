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
        public static readonly string assetImportOverrides = L10n.Tr("Asset Import Overrides");
        public static readonly string playerSettings = L10n.Tr("Player Settings");
        public static readonly string buildProfileWelcome = L10n.Tr("Welcome to Build Profiles."
            + "\n\nAdd a Build Profile to configure as many builds as you need for any supported platform. "
            + "Build profiles are stored as assets you can share with your team");
        public static readonly string all = L10n.Tr("All");
        public static readonly string addBuildProfile = L10n.Tr("Add Build Profile");
        public static readonly string buildProfilesName = L10n.Tr("Build Profiles");
        public static readonly string platforms = L10n.Tr("Platforms");
        public static readonly string build = L10n.Tr("Build");
        public static readonly string buildAndRun = L10n.Tr("Build And Run");
        public static readonly string platformSettings = L10n.Tr("Platform Settings");
        public static readonly string buildData = L10n.Tr("Build Data");
        public static readonly string sharedSettingsInfo =
            L10n.Tr("Platform builds use the shared scene list. To change the scene list or other settings independently, create a Build Profile for this platform.");
        public static readonly string sharedSettingsSectionInfo =
            L10n.Tr("Platforms use this shared scene list. To change the scene list or other settings independently, create a Build Profile.");
        public static readonly string activate = L10n.Tr("Switch Profile");
        public static readonly string sceneList = L10n.Tr("Scene List");
        public static readonly string openSceneList = L10n.Tr("Open Scene List");
        public static readonly string compilingMessage = L10n.Tr("Cannot build player while editor is importing assets or compiling scripts.");
        public static readonly string invalidVirtualTexturingSettingMessage = L10n.Tr("Cannot build player because Virtual Texturing is enabled, but the target platform or graphics API does not support Virtual Texturing. Go to Player Settings to resolve the incompatibility.");
        public static readonly string scriptingDefines = L10n.Tr("Scripting Defines");
        public static readonly string scriptingDefinesTooltip = L10n.Tr("Preprocessor defines passed to the C# script compiler");
        public static readonly string scriptingDefinesModified = L10n.Tr("Build Profile Scripting Defines Have Been Modified");
        public static readonly string scriptingDefinesModifiedBody = L10n.Tr("Do you want to apply changes now?");
        public static readonly string scriptingDefinesWarningHelpbox = L10n.Tr("Additional scripting defines are specified in Player Settings.");
        public static readonly string apply = L10n.Tr("Apply");
        public static readonly string revert = L10n.Tr("Revert");

        // Platform Discovery Window
        public static readonly string platformDiscoveryTitle = L10n.Tr("Platform Browser");
        public static readonly string noModuleFoundWarning = L10n.Tr("No module found for the selected profile.");
        public static readonly string notSupportedWarning = L10n.Tr("Target platform does not currently support build profiles.");
        public static readonly string active = L10n.Tr("Active");

        // Asset Import Overrides Window
        public static readonly string assetImportOverrideTitle = L10n.Tr("Asset Import Overrides");
        public static readonly string assetImportOverrideTooltip = L10n.Tr("Asset Import Overrides are enabled");
        public static readonly string maxTextureSizeLabel = L10n.Tr("Max Texture Size");
        public static readonly string textureCompressionLabel = L10n.Tr("Texture Compression");
        public static readonly string assetImportOverrideDescription =
            L10n.Tr("These settings allow you to override the compression\nand max resolution for textures in your project. This is\nuseful for local development, to speed up texture\nimporting or build target switching.");
    }
}
