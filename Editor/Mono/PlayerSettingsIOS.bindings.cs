// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine.Bindings;

using UnityEngine;

namespace UnityEditor
{
    // Supported iOS SDK versions
    public enum iOSSdkVersion
    {
        // Device SDK
        DeviceSDK = 988,
        // Simulator SDK
        SimulatorSDK = 989
    }

    // Target iOS device
    public enum iOSTargetDevice
    {
        // iPhone/iPod Only
        iPhoneOnly = 0,

        // iPad Only
        iPadOnly = 1,

        // Universal : iPhone/iPod + iPad
        iPhoneAndiPad = 2,
    }

    // Activity Indicator on loading
    public enum iOSShowActivityIndicatorOnLoading
    {
        // White Large
        WhiteLarge = 0,

        // White
        White = 1,

        // Gray
        Gray = 2,

        // Don't Show
        DontShow = -1
    }

    // iOS status bar style
    public enum iOSStatusBarStyle
    {
        // A dark status bar, intended for use on light backgrounds
        Default = 0,

        // A light status bar, intended for use on dark backgrounds
        LightContent = 1,

        [Obsolete("BlackTranslucent has no effect, use LightContent instead (UnityUpgradable) -> LightContent", true)]
        BlackTranslucent = -1,

        [Obsolete("BlackOpaque has no effect, use LightContent instead (UnityUpgradable) -> LightContent", true)]
        BlackOpaque = -1,
    }

    public enum iOSAppInBackgroundBehavior
    {
        Custom = -1,
        Suspend = 0,
        Exit = 1,
    }

    [Flags]
    public enum iOSBackgroundMode: uint
    {
        None = 0,
        Audio = 1 << 0,
        Location = 1 << 1,
        VOIP = 1 << 2,
        NewsstandContent = 1 << 3,
        ExternalAccessory = 1 << 4,
        BluetoothCentral = 1 << 5,
        BluetoothPeripheral = 1 << 6,
        Fetch = 1 << 7,
        RemoteNotification = 1 << 8,
    }

    public enum iOSLaunchScreenImageType
    {
        iPhonePortraitImage = 0,
        iPhoneLandscapeImage = 1,
        iPadImage = 2,
    }

    // extern splash screen type (on iOS)
    public enum iOSLaunchScreenType
    {
        // Default
        Default = 0,

        // Image and background (relative size)
        ImageAndBackgroundRelative = 1,

        // extern XIB file
        CustomXib = 2,

        // None
        None = 3,

        // Image and background (constant size)
        ImageAndBackgroundConstant = 4
    }

    internal enum iOSAutomaticallySignValue
    {
        AutomaticallySignValueNotSet = 0,
        AutomaticallySignValueTrue  = 1,
        AutomaticallySignValueFalse = 2
    }

    public class iOSDeviceRequirement
    {
        SortedDictionary<string, string> m_Values = new SortedDictionary<string, string>();
        public IDictionary<string, string> values { get { return m_Values; } }
    }

    [NativeHeader("Runtime/Misc/PlayerSettings.h")]
    [NativeHeader("Editor/Src/PlayerSettingsIOS.bindings.h")]
    internal partial class iOSDeviceRequirementGroup
    {
        private string m_VariantName;

        [FreeFunction("PlayerSettingsIOSBindings::SetOrAddDeviceRequirementForVariantNameImpl")]
        extern private static void SetOrAddDeviceRequirementForVariantNameImpl(string name, int index, string[] keys, string[] values);

        [NativeMethod(Name = "GetIOSDeviceRequirementCountForVariantName")]
        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        extern private static int GetCountForVariantImpl(string name);

        //[NativeMethod("RemoveIOSDeviceRequirementForVariantName")]
        [NativeMethod(Name = "RemoveIOSDeviceRequirementForVariantName")]
        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        extern private static void RemoveAtImpl(string name, int index);

        internal iOSDeviceRequirementGroup(string variantName)
        {
            m_VariantName = variantName;
        }

        public int count { get { return GetCountForVariantImpl(m_VariantName); } }

        public iOSDeviceRequirement this[int index]
        {
            get
            {
                string[] keys;
                string[] values;
                GetDeviceRequirementForVariantNameImpl(m_VariantName, index, out keys, out values);
                var result = new iOSDeviceRequirement();
                for (int i = 0; i < keys.Length; ++i)
                {
                    result.values.Add(keys[i], values[i]);
                }
                return result;
            }
            set
            {
                SetOrAddDeviceRequirementForVariantNameImpl(m_VariantName, index, value.values.Keys.ToArray(),
                    value.values.Values.ToArray());
            }
        }

        public void RemoveAt(int index)
        {
            RemoveAtImpl(m_VariantName, index);
        }

        public void Add(iOSDeviceRequirement requirement)
        {
            SetOrAddDeviceRequirementForVariantNameImpl(m_VariantName, -1, requirement.values.Keys.ToArray(),
                requirement.values.Values.ToArray());
        }
    }


    // Player Settings is where you define various parameters for the final game that you will build in Unity. Some of these values are used in the Resolution Dialog that launches when you open a standalone game.
    public partial class PlayerSettings : UnityEngine.Object
    {
        // iOS specific player settings
        [NativeHeader("Runtime/Misc/PlayerSettings.h")]
        [NativeHeader("Editor/Src/EditorUserBuildSettings.h")]
        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        public partial class iOS
        {
            // iOS application display name
            [NativeProperty("ProductName")]
            public extern static string applicationDisplayName { get; set; }

            // iOS bundle build number
            public static string buildNumber
            {
                get { return PlayerSettings.GetBuildNumber(BuildTargetGroup.iOS); }
                set { PlayerSettings.SetBuildNumber(BuildTargetGroup.iOS, value); }
            }
            public extern static bool disableDepthAndStencilBuffers  { get; set; }

            // Script calling optimization
            private extern static int  scriptCallOptimizationInternal
            {
                [NativeMethod("GetiPhoneScriptCallOptimization")]
                get;
                [NativeMethod("SetiPhoneScriptCallOptimization")]
                set;
            }

            public static ScriptCallOptimizationLevel scriptCallOptimization
            {
                get { return (ScriptCallOptimizationLevel)scriptCallOptimizationInternal; }
                set { scriptCallOptimizationInternal = (int)value; }
            }
            private extern static int sdkVersionInternal
            {
                [NativeMethod("GetiPhoneSdkVersion")]
                get;
                [NativeMethod("SetiPhoneSdkVersion")]
                set;
            }

            // Active iOS SDK version used for build
            public static iOSSdkVersion sdkVersion
            {
                get { return (iOSSdkVersion)sdkVersionInternal; }
                set { sdkVersionInternal = (int)value; }
            }

            [FreeFunction]
            private extern static string iOSTargetOSVersionObsoleteEnumToString(int val);

            [FreeFunction]
            private extern static int iOSTargetOSVersionStringToObsoleteEnum(string val);

            // Deployment minimal version of iOS
            [Obsolete("OBSOLETE warning targetOSVersion is obsolete, use targetOSVersionString")]
            public static iOSTargetOSVersion targetOSVersion
            {
                get
                {
                    return (iOSTargetOSVersion)iOSTargetOSVersionStringToObsoleteEnum(targetOSVersionString);
                }
                set
                {
                    targetOSVersionString = iOSTargetOSVersionObsoleteEnumToString((int)value);
                }
            }

            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            [NativeMethod("GetiOSMinimumVersionString")]
            static extern string GetMinimumVersionString();

            internal static readonly Version minimumOsVersion = new Version(GetMinimumVersionString());

            [NativeProperty("iOSTargetOSVersion")]
            public extern static string targetOSVersionString
            {
                [NativeMethod("GetiOSTargetOSVersion")]
                get;
                [NativeMethod("SetiOSTargetOSVersion")]
                set;
            }

            // Targeted device
            [NativeProperty("TargetDevice")]
            private extern static int  targetDeviceInternal { get; set; }


            // Active iOS SDK version used for build
            public static iOSTargetDevice targetDevice
            {
                get { return (iOSTargetDevice)targetDeviceInternal; }
                set { targetDeviceInternal = (int)value; }
            }

            // Icon is prerendered
            [NativeProperty("UIPrerenderedIcon")]
            public extern static bool prerenderedIcon { get; set; }

            // Application requires persistent WiFi
            [NativeProperty("UIRequiresPersistentWiFi")]
            public extern static bool requiresPersistentWiFi  { get; set; }

            // Require Full Screen on iOS for iOS 9.0 Multitasking support
            [NativeProperty("UIRequiresFullScreen")]
            public extern static bool requiresFullScreen  { get; set; }

            // Status bar style
            [NativeProperty("UIStatusBarStyle")]
            private extern static int  statusBarStyleInternal { get; set; }


            [NativeProperty("UIStatusBarStyle")]
            public static iOSStatusBarStyle statusBarStyle
            {
                get { return (iOSStatusBarStyle)statusBarStyleInternal; }
                set { statusBarStyleInternal = (int)value; }
            }

            // On iPhone 10 the home button is implemented as a system gesture. (swipe up
            // from the lower edge). This might interfere with games that use swipes as
            // an interaction method. iOS provides a way to reduce the chance of unwanted
            // interaction by marking edges as "protected" edges, so the system gesture
            // is not recognized on the first swipe, but on the second if it comes
            // immediately afterwards.
            [NativeProperty("DeferSystemGesturesMode")]
            private extern static int deferSystemGesturesModeInternal { get; set; }

            public static UnityEngine.iOS.SystemGestureDeferMode deferSystemGesturesMode
            {
                get { return (UnityEngine.iOS.SystemGestureDeferMode)deferSystemGesturesModeInternal; }
                set { deferSystemGesturesModeInternal = (int)value; }
            }

            [NativeProperty("HideHomeButton")]
            public static bool hideHomeButton { get; set; }

            [NativeProperty("IOSAppInBackgroundBehavior")]
            private extern static int  appInBackgroundBehaviorInternal { get; set; }

            [NativeProperty("IOSAppInBackgroundBehavior")]
            public static iOSAppInBackgroundBehavior appInBackgroundBehavior
            {
                get { return (iOSAppInBackgroundBehavior)appInBackgroundBehaviorInternal; }
                set { appInBackgroundBehaviorInternal = (int)value; }
            }

            [NativeProperty("IOSBackgroundModes")]
            private extern static int  backgroundModesInternal { get; set; }

            [NativeProperty("IOSAppInBackgroundBehavior")]
            public static iOSBackgroundMode backgroundModes
            {
                get { return (iOSBackgroundMode)backgroundModesInternal; }
                set { backgroundModesInternal = (int)value; }
            }

            [NativeProperty("IOSMetalForceHardShadows")]
            public extern static bool forceHardShadowsOnMetal
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [NativeProperty("IOSAllowHTTPDownload")]
            public extern static bool allowHTTPDownload { get; set; }

            [NativeProperty("AppleDeveloperTeamID")]
            private extern static string appleDeveloperTeamIDInternal
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            public static string appleDeveloperTeamID
            {
                get
                {
                    return appleDeveloperTeamIDInternal.Length < 1 ?
                        EditorPrefs.GetString("DefaultiOSAutomaticSignTeamId") : appleDeveloperTeamIDInternal;
                }
                set { appleDeveloperTeamIDInternal = value; }
            }


            [NativeProperty("iOSManualProvisioningProfileID")]
            private extern static string iOSManualProvisioningProfileIDInternal
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            public static string iOSManualProvisioningProfileID
            {
                get
                {
                    return String.IsNullOrEmpty(iOSManualProvisioningProfileIDInternal) ?
                        EditorPrefs.GetString("DefaultiOSProvisioningProfileUUID") : iOSManualProvisioningProfileIDInternal;
                }
                set { iOSManualProvisioningProfileIDInternal = value; }
            }


            [NativeProperty("tvOSManualProvisioningProfileID")]
            private extern static string tvOSManualProvisioningProfileIDInternal
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            public static string tvOSManualProvisioningProfileID
            {
                get
                {
                    return String.IsNullOrEmpty(tvOSManualProvisioningProfileIDInternal) ?
                        EditorPrefs.GetString("DefaulttvOSProvisioningProfileUUID") : tvOSManualProvisioningProfileIDInternal;
                }
                set { tvOSManualProvisioningProfileIDInternal = value; }
            }


            [NativeProperty("AppleEnableAutomaticSigning")]
            private extern static int appleEnableAutomaticSigningInternal
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            public static bool appleEnableAutomaticSigning
            {
                get
                {
                    return appleEnableAutomaticSigningInternal == (int)iOSAutomaticallySignValue.AutomaticallySignValueNotSet ?
                        EditorPrefs.GetBool("DefaultiOSAutomaticallySignBuild") :
                        (iOSAutomaticallySignValue)appleEnableAutomaticSigningInternal == iOSAutomaticallySignValue.AutomaticallySignValueTrue;
                }
                set
                {
                    appleEnableAutomaticSigningInternal = value ?
                        (int)iOSAutomaticallySignValue.AutomaticallySignValueTrue :
                        (int)iOSAutomaticallySignValue.AutomaticallySignValueFalse;
                }
            }


            [NativeProperty("CameraUsageDescription")]
            public extern static string cameraUsageDescription { get; set; }

            [NativeProperty("LocationUsageDescription")]
            public extern static string locationUsageDescription { get; set; }

            [NativeProperty("MicrophoneUsageDescription")]
            public extern static string microphoneUsageDescription { get; set; }

            [NativeProperty("IOSShowActivityIndicatorOnLoading")]
            private extern static int  showActivityIndicatorOnLoadingInternal { get; set; }

            // Application should show ActivityIndicator when loading
            [NativeProperty("IOSAppInBackgroundBehavior")]
            public static iOSShowActivityIndicatorOnLoading showActivityIndicatorOnLoading
            {
                get { return (iOSShowActivityIndicatorOnLoading)showActivityIndicatorOnLoadingInternal; }
                set { showActivityIndicatorOnLoadingInternal = (int)value; }
            }

            [NativeProperty("UseOnDemandResources")]
            public extern static bool useOnDemandResources  { get; set; }

            // will be public
            [NativeMethod(Name = "GetIOSVariantsWithDeviceRequirements")]
            extern internal static string[] GetAssetBundleVariantsWithDeviceRequirements();

            private static extern int GetIOSDeviceRequirementCountForVariantName(string name);

            private static bool CheckAssetBundleVariantHasDeviceRequirements(string name)
            {
                return GetIOSDeviceRequirementCountForVariantName(name) > 0;
            }

            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
            [NativeMethod(Name = "SetiOSLaunchScreenImage")]
            private extern static void SetLaunchScreenImageInternal(Texture2D image, int type);

            public static void SetLaunchScreenImage(Texture2D image, iOSLaunchScreenImageType type)
            {
                SetLaunchScreenImageInternal(image, (int)type);
            }

            [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
            private extern static void SetiOSLaunchScreenType(int type, int device);

            public static void SetiPhoneLaunchScreenType(iOSLaunchScreenType type)
            {
                SetiOSLaunchScreenType((int)type, 0);
            }

            public static void SetiPadLaunchScreenType(iOSLaunchScreenType type)
            {
                SetiOSLaunchScreenType((int)type, 1);
            }

            // will be public
            internal static iOSDeviceRequirementGroup GetDeviceRequirementsForAssetBundleVariant(string name)
            {
                if (!CheckAssetBundleVariantHasDeviceRequirements(name))
                    return null;
                return new iOSDeviceRequirementGroup(name);
            }

            // will be public
            internal static void RemoveDeviceRequirementsForAssetBundleVariant(string name)
            {
                var group = GetDeviceRequirementsForAssetBundleVariant(name);
                for (int i = 0; i < group.count; ++i)
                    group.RemoveAt(0);
            }

            // will be public
            internal static iOSDeviceRequirementGroup AddDeviceRequirementsForAssetBundleVariant(string name)
            {
                return new iOSDeviceRequirementGroup(name);
            }

            [NativeProperty("iOSURLSchemes", false, TargetType.Field)]
            private extern static string[] iOSURLSchemes
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
            }

            [NativeProperty("iOSRequireARKit")]
            internal extern static bool requiresARKitSupport
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            internal extern static bool appleEnableProMotion
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            internal static bool IsTargetVersionEqualOrHigher(Version requiredVersion)
            {
                Version requestedVersion;
                try
                {
                    requestedVersion = new Version(targetOSVersionString);
                }
                catch (Exception)
                {
                    requestedVersion = minimumOsVersion;
                }
                return requestedVersion >= requiredVersion;
            }

            internal static string[] GetURLSchemes()
            {
                return iOSURLSchemes;
            }
        }
    }
}
