// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Helper class for build profile window localization.
    /// </summary>
    internal class TrText
    {
        public static readonly string forceSkipDataBuild = L10n.Tr("Force skip data build");
        public static readonly string cleanBuild = L10n.Tr("Clean Build...");
        public static readonly string assetImportOverrides = L10n.Tr("Asset Import Overrides");
        public static readonly string playerSettings = L10n.Tr("Player Settings");
        public static readonly string noBuildProfilesFound = L10n.Tr("No build profiles in your project.");
        public static readonly string welcomeToBuildProfiles = L10n.Tr("Welcome to Build Profiles.");
        public static readonly string welcomeToBuildProfilesMessage = L10n.Tr("Add a Build Profile to configure as many builds as you need for any supported platform."
            + " Build profiles are stored as assets you can share with your team.");
        public static readonly string buildProfileWelcome = L10n.Tr("Welcome to Build Profiles."
            + "\n\nAdd a Build Profile to configure as many builds as you need for any supported platform. "
            + "Build profiles are stored as assets you can share with your team");
        public static readonly string all = L10n.Tr("All");
        public static readonly string addBuildProfile = L10n.Tr("Add Build Profile");
        public static readonly string addBuildProfiles = L10n.Tr("Add {0} Build Profiles");
        public static readonly string buildProfilesName = L10n.Tr("Build Profiles");
        public static readonly string platforms = L10n.Tr("Platforms");
        public static readonly string build = L10n.Tr("Build");
        public static readonly string cloudBuild = L10n.Tr("Cloud Build");
        public static readonly string buildAndRun = L10n.Tr("Build And Run");
        public static readonly string sharedSettingsInfo =
            L10n.Tr("Platform builds use the shared scene list. To change the scene list or other settings independently, create a Build Profile for this platform.");
        public static readonly string sharedSettingsSectionInfo =
            L10n.Tr("Platforms use this shared scene list. To change the scene list or other settings independently, create a Build Profile.");
        public static readonly string activate = L10n.Tr("Switch Profile");
        public static readonly string activatePlatform = L10n.Tr("Switch Platform");
        public static readonly string sceneList = L10n.Tr("Scene List");
        public static readonly string addOpenScenes = L10n.Tr("Add Open Scenes");
        public static readonly string sceneListOverride = L10n.Tr("Override Global Scene List");
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
        public static readonly string cancelButtonText = L10n.Tr("Cancel");
        public static readonly string continueButtonText = L10n.Tr("Continue");
        public static readonly string addSettings = L10n.Tr("Add Settings");
        public static readonly string reset = L10n.Tr("Reset");
        public static readonly string remove = L10n.Tr("Remove");
        public static readonly string resetSettings = L10n.Tr("Reset Settings");
        public static readonly string removeSettings = L10n.Tr("Remove Settings");
        public static readonly string resetMessage = L10n.Tr("Are you sure you want to reset these settings? This operation cannot be undone.");
        public static readonly string removeMessage = L10n.Tr("Are you sure you want to remove these settings? This operation cannot be undone.");
        public static readonly string diagnostics = L10n.Tr("Diagnostics");

        // Build Profile Player Settings
        public static readonly string playerSettingsInfo =
            L10n.Tr("Build Profiles can have custom player settings");

        // Build Profile Graphics Settings
        public static readonly string graphicsSettings = L10n.Tr("Graphics Settings");

        // Build Profile Quality Settings
        public static readonly string qualitySettings = L10n.Tr("Quality Settings");

        // Build Profile Bootstrap View
        public static readonly string buildProfileConfiguration = L10n.Tr("Configuring Build Profile...");
        public static readonly string buildProfilePreparation = L10n.Tr("Preparing Build Profile...");
        public static readonly string packageAddDownloading = L10n.Tr("Downloading package...");
        public static readonly string packageAddInstalling = L10n.Tr("Installing package...");
        public static readonly string packageAddError = L10n.Tr("Error adding package {0}!");
        public static readonly string packagesAddError = L10n.Tr("Error adding packages {0}!");
        public static readonly string packagesAddDownloading = L10n.Tr("Downloading packages...");
        public static readonly string packagesAddInstalling = L10n.Tr("Installing packages...");

        // Package Installation Query
        public static readonly string packageInstallationQueryTitle = L10n.Tr("{0} Package Requirement");
        public static readonly string packageInstallationQueryMessage = L10n.Tr("The {0} platform requires the following package(s):\n{1}\n\nWould you like to install them into your project?");
        public static readonly string packageInstallationQueryYes = L10n.Tr("Install");
        public static readonly string packageInstallationQueryNo = L10n.Tr("Don't Install");

        // Platform Discovery Window
        public static readonly string platformDiscoveryTitle = L10n.Tr("Platform Browser");
        public static readonly string noModuleFoundWarning = L10n.Tr("No module found for the selected profile.");
        public static readonly string notSupportedWarning = L10n.Tr("Target platform does not currently support build profiles.");
        public static readonly string active = L10n.Tr("Active");
        public static readonly string description = L10n.Tr("Description");
        public static readonly string packageInstalled = L10n.Tr("Package already installed.");
        public static readonly string packageContainerTitle = L10n.Tr("Packages");
        public static readonly string partnerPackageListTitle = L10n.Tr("Partner Packages");
        public static readonly string required = L10n.Tr("Required");
        public static readonly string publisherLabel = L10n.Tr("Publisher: {0}");
        public static readonly string selectAll = L10n.Tr("Select All");
        public static readonly string deselectAll = L10n.Tr("Deselect All");
        public static readonly string buildProfileNameLabel = L10n.Tr("Name");
        public static readonly string buildProfileConfigurationLabel = L10n.Tr("Build Profile Configurations");

        // Asset Import Overrides Window
        public static readonly string assetImportOverrideTitle = L10n.Tr("Asset Import Overrides");
        public static readonly string assetImportOverrideTooltip = L10n.Tr("Asset Import Overrides are enabled");
        public static readonly string maxTextureSizeLabel = L10n.Tr("Max Texture Size");
        public static readonly string textureCompressionLabel = L10n.Tr("Texture Compression");
        public static readonly string assetImportOverrideDescription =
            L10n.Tr("These settings allow you to override the compression\nand max resolution for textures in your project. This is\nuseful for local development, to speed up texture\nimporting or build target switching.");

        // Platform Configuration Descriptions
        public static readonly string webMobileDevelopmentDescription = L10n.Tr("Optimized for mobile builds with settings that enable fast build times for rapid development iteration.");
        public static readonly string webMobileReleaseDescription = L10n.Tr("Optimized for mobile builds with settings that compress and optimize the build for fast load times, ideal for final release builds.");
        public static readonly string webDesktopDevelopmentDescription = L10n.Tr("Optimized for desktop builds with settings that enable fast build times for rapid development iteration.");
        public static readonly string webDesktopReleaseDescription = L10n.Tr("Optimized for desktop builds with settings that compress and optimize the build for fast load times, ideal for final release builds.");

        public static string GetSettingsSectionName(string platform) => L10n.Tr($"Platform Settings ({platform})");
    }
}
