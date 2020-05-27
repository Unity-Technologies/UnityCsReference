// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;

namespace UnityEditor
{
    public enum XboxOneEncryptionLevel
    {
        None = 0,
        DevkitCompatible = 1,
        FullEncryption = 2
    }

    public enum XboxOnePackageUpdateGranularity
    {
        Chunk = 1,
        File = 2
    }

    public enum XboxOneLoggingLevel
    {
        AllLogging = 4,
        WarningsAndErrors = 2,
        ErrorsOnly = 1
    }

    [Obsolete("Mono script compiler is no longer supported.")]
    public enum ScriptCompiler
    {
        Mono = 0,
        Roslyn = 1
    }

    public sealed partial class PlayerSettings
    {
        [NativeHeader("Runtime/Misc/PlayerSettings.h")]
        [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
        public sealed partial class XboxOne
        {
            [NativeProperty("XboxOneXTitleMemory", TargetType.Field)]
            extern public static int XTitleMemory
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("XboxOneLoggingLevel")]
            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            extern public static XboxOneLoggingLevel defaultLoggingLevel { get; set; }

            // Xbox One product id
            [NativeProperty("XboxOneProductId", false, TargetType.Function)]
            extern public static string ProductId { get; set; }

            // Xbox One update key required to ship updates
            [NativeProperty("XboxOneUpdateKey", false, TargetType.Function)]
            extern public static string UpdateKey { get; set; }

            // Xbox One App Sandbox Id
            [Obsolete("SandboxId is obsolete please remove")]
            [NativeProperty("XboxOneSandboxId", false, TargetType.Function)]
            extern public static string SandboxId { get; set; }

            // Xbox One App Content Id
            [NativeProperty("XboxOneContentId", false, TargetType.Function)]
            extern public static string ContentId { get; set; }

            // Xbox One App Title Id
            [NativeProperty("XboxOneTitleId", false, TargetType.Function)]
            extern public static string TitleId { get; set; }

            // Xbox One App Title SCID
            [NativeProperty("XboxOneSCId", false, TargetType.Function)]
            extern public static string SCID { get; set; }

            [NativeProperty("XboxOneEnableGPUVariability", TargetType.Field)]
            extern public static bool EnableVariableGPU
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("XboxOnePresentImmediateThreshold")]
            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            extern public static UInt32 PresentImmediateThreshold { get; set; }

            [NativeProperty("XboxOneEnable7thCore")]
            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            extern public static bool Enable7thCore { get; set; }

            [NativeProperty("XboxOneDisableKinectGpuReservation")]
            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            extern public static bool DisableKinectGpuReservation { get; set; }

            [NativeProperty("XboxEnablePIXSampling")]
            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            extern public static bool EnablePIXSampling { get; set; }

            // Path of the os image shipped with this app.
            [NativeProperty("XboxOneGameOsOverridePath", false, TargetType.Function)]
            extern public static string GameOsOverridePath { get; set; }

            // Packaging manifest used to build this app
            [NativeProperty("XboxOnePackagingOverridePath", false, TargetType.Function)]
            extern public static string PackagingOverridePath { get; set; }

            // Encryption option used when making this package
            [NativeProperty("XboxOnePackageEncryption", TargetType.Field)]
            extern public static XboxOneEncryptionLevel PackagingEncryption
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            // The update granularity used when making a package.
            [NativeProperty("XboxOnePackageUpdateGranularity", TargetType.Field)]
            extern public static XboxOnePackageUpdateGranularity PackageUpdateGranularity
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            // Xbox One override auto generated identity name in app manifest (does not work with app manifest override).
            [NativeProperty("XboxOneOverrideIdentityName", false, TargetType.Function)]
            extern public static string OverrideIdentityName { get; set; }

            // Xbox One override auto generated identity publisher in app manifest (does not work with app manifest override).
            [NativeProperty("XboxOneOverrideIdentityPublisher", false, TargetType.Function)]
            extern public static string OverrideIdentityPublisher { get; set; }

            // Optional override path for app manifest.
            [NativeProperty("XboxOneAppManifestOverridePath", false, TargetType.Function)]
            extern public static string AppManifestOverridePath { get; set; }

            // Returns true if this project represents DLC / content.
            [NativeProperty("XboxOneIsContentPackage", TargetType.Field)]
            extern public static bool IsContentPackage
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            // Version used in AppManifest
            [NativeProperty("XboxOneVersion", false, TargetType.Function)]
            extern public static string Version { get; set; }

            // Description used in AppManifest
            [NativeProperty("XboxOneDescription", false, TargetType.Function)]
            extern public static string Description { get; set; }

            // *undocumented*
            [NativeMethod("SetXboxOneCapability")]
            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
            extern public static void SetCapability(string capability, bool value);

            // *undocumented*
            [NativeMethod("GetXboxOneCapability")]
            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            extern public static bool GetCapability(string capability);


            [NativeMethod("SetXboxOneLanguage")]
            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
            extern public static void SetSupportedLanguage(string language, bool enabled);

            [NativeMethod("GetXboxOneLanguage")]
            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            extern public static bool GetSupportedLanguage(string language);


            [NativeMethod("RemoveXboxOneSocketDefinition")]
            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
            extern public static void RemoveSocketDefinition(string name);

            [NativeMethod("SetXboxOneSocketDefinition")]
            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
            extern public static void SetSocketDefinition(string name, string port, int protocol, int[] usages, string templateName, int sessionRequirment, int[] deviceUsages);

            [NativeMethod("GetXboxOneSocketDefinition")]
            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
            extern private static void GetSocketDefinitionInternal(string name, out string port, out int protocol, [Out] int[] usages, out string templateName, out int sessionRequirment, [Out] int[] deviceUsages);

            public static void GetSocketDefinition(string name, out string port, out int protocol, out int[] usages, out string templateName, out int sessionRequirment, out int[] deviceUsages)
            {
                int numUsages = GetXboxOneSocketDefinitionNumUsages(name);
                int numDeviceUsages = GetXboxOneSocketDefinitionNumDeviceUsages(name);
                if (numUsages < 0 || numDeviceUsages < 0)
                    throw new ArgumentException("Could not find socket definition " + name + ".");

                usages = new int[numUsages];
                deviceUsages = new int[numDeviceUsages];

                GetSocketDefinitionInternal(name, out port, out protocol, usages, out templateName, out sessionRequirment, deviceUsages);
            }

            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
            extern public static string[] SocketNames
            {
                [NativeMethod("GetXboxOneSocketNames")]
                get;
            }

            [NativeMethod("GetXboxOneSocketDefinitionNumUsages")]
            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
            extern private static int GetXboxOneSocketDefinitionNumUsages(string name);

            [NativeMethod("GetXboxOneSocketDefinitionNumDeviceUsages")]
            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
            extern private static int GetXboxOneSocketDefinitionNumDeviceUsages(string name);

            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
            extern public static string[] AllowedProductIds
            {
                [NativeMethod("GetXboxOneAllowedProductIds")]
                get;
            }

            [NativeMethod("RemoveXboxOneAllowedProductId")]
            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
            extern public static void RemoveAllowedProductId(string id);

            [NativeMethod("AddXboxOneAllowedProductId")]
            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
            extern public static bool AddAllowedProductId(string id);

            [NativeMethod("UpdateXboxOneAllowedProductId")]
            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
            extern public static void UpdateAllowedProductId(int idx, string id);

            // *undocumented*
            [Obsolete("Starting May 11th 2020 any new base game submission releasing digital only, " +
                "digital and disc, or disc only, should not include a ratings element in the " +
                "AppxManifest. This ratings policy update applies to all Xbox supported ratings. " +
                "New base submissions that come in on or after this date will be " +
                "rejected by your Microsoft Representative if a ratings element is present.", false)]
            [NativeMethod("SetXboxOneGameRating")]
            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
            extern public static void SetGameRating(string name, int value);

            // *undocumented*
            [Obsolete("Starting May 11th 2020 any new base game submission releasing digital only, " +
                "digital and disc, or disc only, should not include a ratings element in the " +
                "AppxManifest. This ratings policy update applies to all Xbox supported ratings. " +
                "New base submissions that come in on or after this date will be " +
                "rejected by your Microsoft Representative if a ratings element is present.", false)]
            [NativeMethod("GetXboxOneGameRating")]
            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            extern public static int GetGameRating(string name);

            // The presence of any other value than 0 for this property will result in a PLS reservation in your app manifest.
            public static uint PersistentLocalStorageSize
            {
                get { return persistentLocalStorageSizeInternal; }
                set
                {
                    if (value < 256 || value >= 4096)
                        throw new ArgumentException(string.Format("PersistentLocalStorageSize must be between 256 and 4096, but was {0}", value));

                    persistentLocalStorageSizeInternal = value;
                }
            }

            [NativeProperty("XboxOnePersistentLocalStorageSize", TargetType.Field)]
            extern private static uint persistentLocalStorageSizeInternal
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            // Enable/Disable Type Optimization in C++ Compiler 'Master' build, applies to LTCG.
            [NativeProperty("XboxOneEnableTypeOptimization")]
            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            extern public static bool EnableTypeOptimization { get; set; }

            // Whether we have enabled mono trace logs on xboxOne for debugging purposes.
            [NativeProperty("XboxOneMonoLoggingLevel")]
            [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
            extern public static int monoLoggingLevel { get; set; }

            // Compiler to use for user script
            [Obsolete("Mono script compiler is no longer supported.")]
            public static ScriptCompiler scriptCompiler
            {
                get
                {
                    return ScriptCompiler.Roslyn;
                }
                set
                {
                }
            }
        }
    }
}
