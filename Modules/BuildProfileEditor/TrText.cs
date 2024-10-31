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
        public static readonly GUIContent resetToGlobals = EditorGUIUtility.TrTextContent("Reset to Globals");

        // Build Profile Player Settings
        public static readonly string playerSettingsLabelText = L10n.Tr("Player Settings Overrides");
        public static readonly string playerSettingsInfo =
            L10n.Tr("Build Profiles can have custom player settings");
        public static readonly string playerSettingsClassicInfo =
            L10n.Tr("Platforms use the global player settings. To customize player settings, create a Build Profile for this platform.");
        public static readonly string customizePlayerSettingsButton = "Customize player settings";
        public static readonly string removePlayerSettingsDialogTitle = L10n.Tr("Remove Player Settings Overrides");
        public static readonly string removePlayerSettingsDialogMessage = L10n.Tr("This will remove all Player Settings overrides");
        public static readonly string resetPlayerSettingsDialogTitle = L10n.Tr("Reset Player Settings to Globals");
        public static readonly string resetPlayerSettingsDialogMessage = L10n.Tr("This will reset all Player Settings overrides and restore all global Player Settings.");
        public static readonly GUIContent playerSettingsReset = EditorGUIUtility.TrTextContent("Reset to Globals");
        public static readonly GUIContent playerSettingsRemove = EditorGUIUtility.TrTextContent("Remove Overrides");

        // Build Profile Graphics Settings
        public static readonly string graphicsSettings = L10n.Tr("Graphics Settings");
        public static readonly string overrideGraphicsSettingsToggleLabel = L10n.Tr("Override Global Graphics Settings");
        public static readonly string overrideFoldoutLabel = L10n.Tr("Override Options");
        public static readonly string removeGraphicsSettingsDialogTitle = L10n.Tr("Remove Graphics Settings Overrides");
        public static readonly string removeGraphicsSettingsDialogMessage = L10n.Tr("This will remove all Graphics Settings overrides");
        public static readonly string resetGraphicsSettingsDialogTitle = L10n.Tr("Reset Graphics Settings to Globals");
        public static readonly string resetGraphicsSettingsDialogMessage = L10n.Tr("This will reset all Graphics Settings overrides to the original globals.");

        // Build Profile Quality Settings
        public static readonly string overrideQualitySettingsToggleLabel = L10n.Tr("Override Global Quality Settings");
        public static readonly string overrideQualitySettingsFoldoutLabel = L10n.Tr("Included Quality Levels");
        public static readonly string removeQualitySettingsDialogTitle = L10n.Tr("Remove Quality Settings Overrides");
        public static readonly string removeQualitySettingsDialogMessage = L10n.Tr("This will remove all Quality Settings overrides");
        public static readonly string resetQualitySettingsDialogTitle = L10n.Tr("Reset Quality Settings to Globals");
        public static readonly string resetQualitySettingsDialogMessage = L10n.Tr("This will reset all Quality Settings overrides to the original globals.");

        // Platform Discovery Window
        public static readonly string platformDiscoveryTitle = L10n.Tr("Platform Browser");
        public static readonly string noModuleFoundWarning = L10n.Tr("No module found for the selected profile.");
        public static readonly string notSupportedWarning = L10n.Tr("Target platform does not currently support build profiles.");
        public static readonly string active = L10n.Tr("Active");
        public static readonly string description = L10n.Tr("Description");
        public static readonly string packageInstalled = L10n.Tr("Package already installed.");
        public static readonly string required = L10n.Tr("Required");
        public static readonly string selectAll = L10n.Tr("Select All");
        public static readonly string deselectAll = L10n.Tr("Deselect All");
        public static readonly string packagesHeader = L10n.Tr("Packages");
        public static readonly string descriptionHeader = L10n.Tr("Description");

        // Asset Import Overrides Window
        public static readonly string assetImportOverrideTitle = L10n.Tr("Asset Import Overrides");
        public static readonly string assetImportOverrideTooltip = L10n.Tr("Asset Import Overrides are enabled");
        public static readonly string maxTextureSizeLabel = L10n.Tr("Max Texture Size");
        public static readonly string textureCompressionLabel = L10n.Tr("Texture Compression");
        public static readonly string assetImportOverrideDescription =
            L10n.Tr("These settings allow you to override the compression\nand max resolution for textures in your project. This is\nuseful for local development, to speed up texture\nimporting or build target switching.");

        public static string GetSettingsSectionName(string platform) => L10n.Tr($"{platform} Settings");
    }
}
