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
        public sealed partial class PSVita
        {
            public enum PSVitaPowerMode
            {
                ModeA = 0,
                ModeB = 1,
                ModeC = 2,
            }

            public enum PSVitaTvBootMode
            {
                Default = 0,
                PSVitaBootablePSVitaTvBootable = 1,
                PSVitaBootablePSVitaTvNotBootable = 2,
            }

            public enum PSVitaEnterButtonAssignment
            {
                Default = 0,
                CircleButton = 1,
                CrossButton = 2,
            }

            public enum PSVitaAppCategory
            {
                Application = 0,    // Application package.
                ApplicationPatch = 1, // Application patch package.
            }

            public enum PSVitaMemoryExpansionMode
            {
                None = 0,
                ExpandBy29MB = 1,
                ExpandBy77MB = 2,
                ExpandBy109MB = 3,
            }

            public enum PSVitaDRMType
            {
                PaidFor = 0, // Paid for content.
                Free = 1, // Free content.
            }

            [NativeProperty("psp2NPTrophyPackPath")] extern public static string npTrophyPackPath { get; set; }
            [NativeProperty("psp2PowerMode", false, TargetType.Field)] extern public static PSVitaPowerMode powerMode { get; set; }
            [NativeProperty("psp2AcquireBGM", false, TargetType.Field)] extern public static bool acquireBGM { get; set; }
            [NativeProperty("psp2NPSupportGBMorGJP", false, TargetType.Field)] extern public static bool npSupportGBMorGJP { get; set; }
            [NativeProperty("psp2TVBootMode", false, TargetType.Field)] extern public static PSVitaTvBootMode tvBootMode { get; set; }
            [NativeProperty("psp2TVDisableEmu", false, TargetType.Field)] extern public static bool tvDisableEmu { get; set; }
            [NativeProperty("psp2Upgradable", false, TargetType.Field)] extern public static bool upgradable { get; set; }
            [NativeProperty("psp2HealthWarning", false, TargetType.Field)] extern public static bool healthWarning { get; set; }
            [NativeProperty("psp2UseLibLocation", false, TargetType.Field)] extern public static bool useLibLocation { get; set; }
            [NativeProperty("psp2InfoBarOnStartup", false, TargetType.Field)] extern public static bool infoBarOnStartup { get; set; }
            [NativeProperty("psp2InfoBarColor", false, TargetType.Field)] extern public static bool infoBarColor { get; set; }
            [NativeProperty("psp2ScriptOptimizationLevel", false, TargetType.Field)] extern public static int scriptOptimizationLevel { get; set; }
            [NativeProperty("psp2EnterButtonAssignment", false, TargetType.Field)] extern public static PSVitaEnterButtonAssignment enterButtonAssignment { get; set; }
            [NativeProperty("psp2SaveDataQuota", false, TargetType.Field)] extern public static int saveDataQuota { get; set; }
            [NativeProperty("psp2ParentalLevel", false, TargetType.Field)] extern public static int parentalLevel { get; set; }
            [NativeProperty("psp2ShortTitle")] extern public static string shortTitle { get; set; }
            [NativeProperty("psp2ContentID")] extern public static string contentID { get; set; }
            [NativeProperty("psp2Category", false, TargetType.Field)] extern public static PSVitaAppCategory category { get; set; }
            [NativeProperty("psp2MasterVersion")] extern public static string masterVersion { get; set; }
            [NativeProperty("psp2AppVersion")] extern public static string appVersion { get; set; }
            [Obsolete("AllowTwitterDialog has no effect as of SDK 3.570")]
            [NativeProperty("psp2AllowTwitterDialog", false, TargetType.Field)] extern public static bool AllowTwitterDialog { get; set; }
            [NativeProperty("psp2NPAgeRating", false, TargetType.Field)] extern public static int npAgeRating { get; set; }
            [NativeProperty("psp2NPTitleDatPath")] extern public static string npTitleDatPath { get; set; }
            [NativeProperty("psp2NPCommunicationsID")] extern public static string npCommunicationsID { get; set; }
            [NativeProperty("psp2NPCommsPassphrase")] extern public static string npCommsPassphrase { get; set; }
            [NativeProperty("psp2NPCommsSig")] extern public static string npCommsSig { get; set; }
            [NativeProperty("psp2ParamSfxPath")] extern public static string paramSfxPath { get; set; }
            [NativeProperty("psp2ManualPath")] extern public static string manualPath { get; set; }
            [NativeProperty("psp2LiveAreaGatePath")] extern public static string liveAreaGatePath { get; set; }
            [NativeProperty("psp2LiveAreaBackroundPath")] extern public static string liveAreaBackroundPath { get; set; }
            [NativeProperty("psp2LiveAreaPath")] extern public static string liveAreaPath { get; set; }
            [NativeProperty("psp2LiveAreaTrialPath")] extern public static string liveAreaTrialPath { get; set; }
            [NativeProperty("psp2PatchChangeInfoPath")] extern public static string patchChangeInfoPath { get; set; }
            [NativeProperty("psp2PatchOriginalPackage")] extern public static string patchOriginalPackage { get; set; }
            [NativeProperty("psp2PackagePassword")] extern public static string packagePassword { get; set; }
            [NativeProperty("psp2KeystoneFile")] extern public static string keystoneFile { get; set; }
            [NativeProperty("psp2MemoryExpansionMode", false, TargetType.Field)] extern public static PSVitaMemoryExpansionMode memoryExpansionMode { get; set; }
            [NativeProperty("psp2DRMType", false, TargetType.Field)] extern public static PSVitaDRMType drmType { get; set; }
            [NativeProperty("psp2StorageType", false, TargetType.Field)] extern public static int storageType { get; set; }
            [NativeProperty("psp2MediaCapacity", false, TargetType.Field)] extern public static int mediaCapacity { get; set; }
        }
    }
}
