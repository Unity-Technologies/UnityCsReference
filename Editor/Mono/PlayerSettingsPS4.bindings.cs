// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using System;
using System.Runtime.CompilerServices;

namespace UnityEditor
{
    public sealed partial class PlayerSettings
    {
        [NativeHeader("Runtime/Misc/PlayerSettings.h")]
        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
        public sealed partial class PS4
        {
            public enum PS4AppCategory
            {
                Application = 0,    // Application package.
                Patch = 1,          // Patch
                Remaster = 2,       // Remaster package
            }

            public enum PS4RemotePlayKeyAssignment
            {
                None = -1,
                PatternA = 0,
                PatternB = 1,
                PatternC = 2,
                PatternD = 3,
                PatternE = 4,
                PatternF = 5,
                PatternG = 6,
                PatternH = 7,
            }

            public enum PS4EnterButtonAssignment
            {
                CircleButton = 0,
                CrossButton = 1,
            }

            public enum PlayStationVREyeToEyeDistanceSettings
            {
                PerUser = 0,
                ForceDefault = 1,
                DynamicModeAtRuntime = 2,
            }

            [NativeProperty("ps4NPTrophyPackPath")] extern public static string npTrophyPackPath { get; set; }
            [NativeProperty("ps4NPAgeRating", false, TargetType.Field)] extern public static int npAgeRating { get; set; }
            [NativeProperty("ps4NPTitleSecret", false, TargetType.Function)] extern public static string npTitleSecret { get; set; }
            [NativeProperty("ps4ParentalLevel", false, TargetType.Field)] extern public static int parentalLevel { get; set; }
            [NativeProperty("ps4ApplicationParam1", false, TargetType.Field)] extern public static int applicationParameter1 { get; set; }
            [NativeProperty("ps4ApplicationParam2", false, TargetType.Field)] extern public static int applicationParameter2 { get; set; }
            [NativeProperty("ps4ApplicationParam3", false, TargetType.Field)] extern public static int applicationParameter3 { get; set; }
            [NativeProperty("ps4ApplicationParam4", false, TargetType.Field)] extern public static int applicationParameter4 { get; set; }
            [NativeProperty("ps4Passcode", false, TargetType.Function)] extern public static string passcode { get; set; }
            [NativeProperty("monoEnv", false, TargetType.Function)] extern public static string monoEnv { get; set; }
            [NativeProperty(TargetType = TargetType.Field)] extern public static bool playerPrefsSupport { get; set; }
            [NativeProperty(TargetType = TargetType.Field)] extern public static bool restrictedAudioUsageRights { get; set; }
            [NativeProperty("ps4UseResolutionFallback", false, TargetType.Field)] extern public static bool useResolutionFallback { get; set; }
            [NativeProperty("ps4ContentID", false, TargetType.Function)] extern public static string contentID { get; set; }
            [NativeProperty("ps4Category", false, TargetType.Field)] extern public static PS4AppCategory category { get; set; }
            [NativeProperty("ps4AppType", false, TargetType.Field)] extern public static int appType { get; set; }
            [NativeProperty("ps4MasterVersion", false, TargetType.Function)] extern public static string masterVersion { get; set; }
            [NativeProperty("ps4AppVersion", false, TargetType.Function)] extern public static string appVersion { get; set; }
            [NativeProperty("ps4RemotePlayKeyAssignment", false, TargetType.Field)] extern public static PS4RemotePlayKeyAssignment remotePlayKeyAssignment { get; set; }
            [NativeProperty("ps4RemotePlayKeyMappingDir", false, TargetType.Function)] extern public static string remotePlayKeyMappingDir { get; set; }
            [NativeProperty("ps4PlayTogetherPlayerCount", false, TargetType.Field)] extern public static int playTogetherPlayerCount { get; set; }
            [NativeProperty("ps4EnterButtonAssignment", false, TargetType.Field)] extern public static PS4EnterButtonAssignment enterButtonAssignment { get; set; }
            [NativeProperty("ps4ParamSfxPath", false, TargetType.Function)] extern public static string paramSfxPath { get; set; }
            [NativeProperty("ps4VideoOutPixelFormat", false, TargetType.Field)] extern public static int videoOutPixelFormat { get; set; }
            [NativeProperty("ps4VideoOutInitialWidth", false, TargetType.Field)] extern public static int videoOutInitialWidth { get; set; }

            [Obsolete("videoOutResolution is deprecated. Use PlayerSettings.PS4.videoOutInitialWidth and PlayerSettings.PS4.videoOutReprojectionRate to control initial display resolution and reprojection rate.")]
            public static int videoOutResolution
            {
                get
                {
                    int reprojectionRate = videoOutReprojectionRate;
                    int initialWidth = videoOutInitialWidth;
                    switch (reprojectionRate)
                    { // if we have any projection rates then assume we have 1920x1080 res
                        case 60:
                            return 5; // 1920By1080_VR60
                        case 90:
                            return 6; // 1920By1080_VR90
                        case 120:
                            return 7; // 1920By1080_VR120
                        default:
                            switch (initialWidth)
                            {
                                case 1280:
                                    return 0; // 1280By720
                                case 1440:
                                    return 1; // 1440By810
                                case 1600:
                                    return 2; // 1660By900
                                case 1760:
                                    return 3; // 1760By990
                                default:
                                case 1920:
                                    return 4; // 1920By1080
                            }
                    }
                }
                set
                {
                    int reprojectionRate = 0;
                    int initialWidth = 1920;
                    switch (value)
                    {
                        case 0:
                            initialWidth = 1280;
                            break;
                        case 1:
                            initialWidth = 1440;
                            break;
                        case 2:
                            initialWidth = 1600;
                            break;
                        case 3:
                            initialWidth = 1760;
                            break;
                        case 4:
                            initialWidth = 1920;
                            break;
                        case 5:
                            reprojectionRate = 60;
                            break;
                        case 6:
                            reprojectionRate = 90;
                            break;
                        case 7:
                            reprojectionRate = 120;
                            break;
                    }
                    videoOutInitialWidth = initialWidth;
                    videoOutReprojectionRate = reprojectionRate;
                }
            }

            [NativeProperty("ps4VideoOutBaseModeInitialWidth", false, TargetType.Field)] extern public static int videoOutBaseModeInitialWidth { get; set; }
            [NativeProperty("ps4VideoOutReprojectionRate", false, TargetType.Field)] extern public static int videoOutReprojectionRate { get; set; }
            [NativeProperty("ps4PronunciationXMLPath", false, TargetType.Function)] extern public static string PronunciationXMLPath { get; set; }
            [NativeProperty("ps4PronunciationSIGPath", false, TargetType.Function)] extern public static string PronunciationSIGPath { get; set; }
            [NativeProperty("ps4BackgroundImagePath", false, TargetType.Function)] extern public static string BackgroundImagePath { get; set; }
            [NativeProperty("ps4StartupImagePath", false, TargetType.Function)] extern public static string StartupImagePath { get; set; }
            [NativeProperty("ps4StartupImagesFolder", false, TargetType.Function)] extern public static string startupImagesFolder { get; set; }
            [NativeProperty("ps4IconImagesFolder", false, TargetType.Function)] extern public static string iconImagesFolder { get; set; }
            [NativeProperty("ps4SaveDataImagePath", false, TargetType.Function)] extern public static string SaveDataImagePath { get; set; }
            [NativeProperty("ps4SdkOverride", false, TargetType.Function)] extern public static string SdkOverride { get; set; }
            [NativeProperty("ps4BGMPath", false, TargetType.Function)] extern public static string BGMPath { get; set; }
            [NativeProperty("ps4ShareFilePath", false, TargetType.Function)] extern public static string ShareFilePath { get; set; }
            [NativeProperty("ps4ShareOverlayImagePath", false, TargetType.Function)] extern public static string ShareOverlayImagePath { get; set; }
            [NativeProperty("ps4PrivacyGuardImagePath", false, TargetType.Function)] extern public static string PrivacyGuardImagePath { get; set; }
            [NativeProperty("ps4PatchDayOne", false, TargetType.Field)] extern public static bool patchDayOne { get; set; }
            [NativeProperty("ps4PatchPkgPath", false, TargetType.Function)] extern public static string PatchPkgPath { get; set; }
            [NativeProperty("ps4PatchLatestPkgPath", false, TargetType.Function)] extern public static string PatchLatestPkgPath { get; set; }
            [NativeProperty("ps4PatchChangeinfoPath", false, TargetType.Function)] extern public static string PatchChangeinfoPath { get; set; }
            [NativeProperty("ps4NPtitleDatPath", false, TargetType.Function)] extern public static string NPtitleDatPath { get; set; }
            [NativeProperty("ps4pnSessions", false, TargetType.Field)] extern public static bool pnSessions { get; set; }
            [NativeProperty("ps4pnPresence", false, TargetType.Field)] extern public static bool pnPresence { get; set; }
            [NativeProperty("ps4pnFriends", false, TargetType.Field)] extern public static bool pnFriends { get; set; }
            [NativeProperty("ps4pnGameCustomData", false, TargetType.Field)] extern public static bool pnGameCustomData { get; set; }
            [NativeProperty("ps4DownloadDataSize", false, TargetType.Field)] extern public static int downloadDataSize { get; set; }
            [NativeProperty("ps4GarlicHeapSize", false, TargetType.Field)] extern public static int garlicHeapSize { get; set; }
            [NativeProperty("ps4ProGarlicHeapSize", false, TargetType.Field)] extern public static int proGarlicHeapSize { get; set; }
            [NativeProperty("ps4ReprojectionSupport", false, TargetType.Field)] extern public static bool reprojectionSupport { get; set; }
            [NativeProperty("ps4UseAudio3dBackend", false, TargetType.Field)] extern public static bool useAudio3dBackend { get; set; }
            [NativeProperty("ps4Audio3dVirtualSpeakerCount", false, TargetType.Field)] extern public static int audio3dVirtualSpeakerCount { get; set; }
            [NativeProperty("ps4ScriptOptimizationLevel", false, TargetType.Field)] extern public static int scriptOptimizationLevel { get; set; }
            [NativeProperty("ps4SocialScreenEnabled", false, TargetType.Field)] extern public static int socialScreenEnabled { get; set; }
            [NativeProperty("ps4attribUserManagement", false, TargetType.Field)] extern public static bool attribUserManagement { get; set; }
            [NativeProperty("ps4attribMoveSupport", false, TargetType.Field)] extern public static bool attribMoveSupport { get; set; }
            [NativeProperty("ps4attrib3DSupport", false, TargetType.Field)] extern public static bool attrib3DSupport { get; set; }
            [NativeProperty("ps4attribShareSupport", false, TargetType.Field)] extern public static bool attribShareSupport { get; set; }
            [NativeProperty("ps4attribExclusiveVR", false, TargetType.Field)] extern public static bool attribExclusiveVR { get; set; }
            [NativeProperty("ps4disableAutoHideSplash", false, TargetType.Field)] extern public static bool disableAutoHideSplash { get; set; }
            [NativeProperty("ps4attribCpuUsage", false, TargetType.Field)] extern public static int attribCpuUsage { get; set; }
            [NativeProperty("ps4videoRecordingFeaturesUsed", false, TargetType.Field)] extern public static bool videoRecordingFeaturesUsed { get; set; }
            [NativeProperty("ps4contentSearchFeaturesUsed", false, TargetType.Field)] extern public static bool contentSearchFeaturesUsed { get; set; }
            [NativeProperty("ps4attribEyeToEyeDistanceSettingVR", false, TargetType.Field)] extern public static PlayStationVREyeToEyeDistanceSettings attribEyeToEyeDistanceSettingVR { get; set; }
            [NativeProperty("ps4IncludedModules", false, TargetType.Field)] extern public static string[] includedModules { get; set; }
            [NativeProperty(TargetType = TargetType.Field)] extern public static bool enableApplicationExit { get; set; }
        }
    }
}
