// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using System;

namespace UnityEditor
{
    // Android CPU architecture.
    // Matches enum in EditorOnlyPlayerSettings.h.
    [Flags]
    public enum AndroidArchitecture : uint
    {
        // Invalid architecture
        None = 0,

        // armeabi-v7a
        ARMv7 = 1 << 0,

        // arm64-v8a
        ARM64 = 1 << 1,

        // x86
        // X86 = 1 << 2,

        // All architectures
        All = 0xffffffff,
    }

    // Supported Android SDK versions
    public enum AndroidSdkVersions
    {
        // Set target API level to latest installed
        AndroidApiLevelAuto = 0,

        // Android 4.1, "Jelly Bean", API level 16
        [Obsolete("Minimum supported Android API level is 19 (Android 4.4 KitKat). Please use AndroidApiLevel19 or higher", true)]
        AndroidApiLevel16 = 16,

        // Android 4.2, "Jelly Bean", API level 17
        [Obsolete("Minimum supported Android API level is 19 (Android 4.4 KitKat). Please use AndroidApiLevel19 or higher", true)]
        AndroidApiLevel17 = 17,

        // Android 4.3, "Jelly Bean", API level 18
        [Obsolete("Minimum supported Android API level is 19 (Android 4.4 KitKat). Please use AndroidApiLevel19 or higher", true)]
        AndroidApiLevel18 = 18,

        // Android 4.4, "KitKat", API level 19
        AndroidApiLevel19 = 19,

        // Android 5.0, "Lollipop", API level 21
        AndroidApiLevel21 = 21,

        // Android 5.1, "Lollipop", API level 22
        AndroidApiLevel22 = 22,

        // Android 6.0, "Marshmallow", API level 23
        AndroidApiLevel23 = 23,

        // Android 7.0, "Nougat", API level 24
        AndroidApiLevel24 = 24,

        // Android 7.1, "Nougat", API level 25
        AndroidApiLevel25 = 25,

        // Android 8.0, "Oreo", API level 26
        AndroidApiLevel26 = 26,

        // Android 8.1, "Oreo", API level 27
        AndroidApiLevel27 = 27,

        // Android 9.0, "Pie", API level 28
        AndroidApiLevel28 = 28,

        // Android 10.0, API level 29
        AndroidApiLevel29 = 29,
    }

    // Preferred application install location
    public enum AndroidPreferredInstallLocation
    {
        // Let the OS decide, app doesn't have any preferences
        Auto = 0,

        // Prefer external, if possible. Install to internal otherwise
        PreferExternal = 1,

        // Force installation into internal memory. Needed for things like Live Wallpapers
        ForceInternal = 2,
    }

    public enum AndroidShowActivityIndicatorOnLoading
    {
        // Large == progressBarStyleLarge
        Large = 0,

        // Inversed Large == progressBarStyleLargeInverse
        InversedLarge = 1,

        // Small == progressBarStyleSmall
        Small = 2,

        // Inversed Small == progressBarStyleSmallInverse
        InversedSmall = 3,

        // Don't Show
        DontShow = -1,
    }

    // Gamepad support level for Android TV
    public enum AndroidGamepadSupportLevel
    {
        // Game is fully operational with a D-pad, no gamepad needed
        SupportsDPad = 0,

        // Works with a gamepad, but does not require it
        SupportsGamepad = 1,

        // Requires a gamepad for gameplay
        RequiresGamepad = 2,
    }

    // Android splash screen scale modes
    public enum AndroidSplashScreenScale
    {
        // Center
        Center = 0,

        // Scale to fit
        ScaleToFit = 1,

        // Scale to fill
        ScaleToFill = 2,
    }

    // Android screen blit types
    public enum AndroidBlitType
    {
        // Always blit
        Always = 0,

        // Never blit
        Never = 1,

        // Automatic
        Auto = 2,
    }

    //internal struct AndroidBanner
    internal struct AndroidBanner
    {
        public int width;
        public int height;
        public Texture2D banner;
    }

    // Player Settings is where you define various parameters for the final game that you will build in Unity. Some of these values are used in the Resolution Dialog that launches when you open a standalone game.
    public partial class PlayerSettings : UnityEngine.Object
    {
        // Android specific player settings
        [NativeHeader("Runtime/Misc/PlayerSettings.h")]
        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        public partial class Android
        {
            // Disable Depth and Stencil Buffers
            public static extern bool disableDepthAndStencilBuffers { get; set; }

            // 24-bit Depth Buffer is used
            [Obsolete("use24BitDepthBuffer is deprecated, use disableDepthAndStencilBuffers instead.")]
            public static bool use24BitDepthBuffer
            {
                get { return !disableDepthAndStencilBuffers; }
                set {}
            }

            // Android bundle version code
            public static extern int  bundleVersionCode
            {
                [NativeMethod("GetAndroidBundleVersionCode")]
                get;
                [NativeMethod("SetAndroidBundleVersionCode")]
                set;
            }

            // Minimal Android SDK version
            public static extern AndroidSdkVersions minSdkVersion
            {
                [NativeMethod("GetAndroidMinSdkVersion")]
                get;
                [NativeMethod("SetAndroidMinSdkVersion")]
                set;
            }

            // Target Android SDK version
            public static extern AndroidSdkVersions targetSdkVersion
            {
                [NativeMethod("GetAndroidTargetSdkVersion")]
                get;
                [NativeMethod("SetAndroidTargetSdkVersion")]
                set;
            }

            // Preferred application install location
            public static extern AndroidPreferredInstallLocation preferredInstallLocation
            {
                [NativeMethod("GetAndroidPreferredInstallLocation")]
                get;
                [NativeMethod("SetAndroidPreferredInstallLocation")]
                set;
            }

            // Force internet permission flag
            public static extern bool forceInternetPermission
            {
                [NativeMethod("GetForceAndroidInternetPermission")]
                get;
                [NativeMethod("SetForceAndroidInternetPermission")]
                set;
            }

            // Force SD card permission
            public static extern bool forceSDCardPermission
            {
                [NativeMethod("GetForceAndroidSDCardPermission")]
                get;
                [NativeMethod("SetForceAndroidSDCardPermission")]
                set;
            }

            // Android TV compatible build
            public static extern bool androidTVCompatibility { get; set; }

            // Android TV - is it a game or a regular app
            public static extern bool androidIsGame { get; set; }

            // Google Tango mixed reality support
            public static extern bool ARCoreEnabled { get; set; }

            // Whether Android banner is added to the APK
            internal static extern bool androidBannerEnabled { get; set; }

            // Gamepad support level for Android TV
            internal static extern AndroidGamepadSupportLevel androidGamepadSupportLevel { get; set; }

            // Returns Android banner list
            internal static extern AndroidBanner[] GetAndroidBanners();

            internal static extern Texture2D GetAndroidBannerForHeight(int height);

            internal static extern void SetAndroidBanners(Texture2D[] banners);

            //*undocumented*
            // only available in developer builds for now.
            internal static extern bool createWallpaper
            {
                [NativeMethod("GetCreateAndroidWallpaper")]
                get;
                [NativeMethod("SetCreateAndroidWallpaper")]
                set;
            }

            // Targeted CPU architectures.
            public static extern AndroidArchitecture targetArchitectures
            {
                [NativeMethod("GetAndroidTargetArchitectures")]
                get;
                [NativeMethod("SetAndroidTargetArchitectures")]
                set;
            }

            // Support different CPU architectures with each APK (a.k.a. Multiple APK support).
            public static extern bool buildApkPerCpuArchitecture
            {
                [NativeMethod("GetBuildApkPerCpuArchitecture")]
                get;
                [NativeMethod("SetBuildApkPerCpuArchitecture")]
                set;
            }

            // Android splash screen scale mode
            public static extern AndroidSplashScreenScale splashScreenScale
            {
                [NativeMethod("GetAndroidSplashScreenScale")]
                get;
                [NativeMethod("SetAndroidSplashScreenScale")]
                set;
            }

            [NativeProperty("androidUseCustomKeystore", TargetType.Function)]
            public static extern bool useCustomKeystore
            {
                [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
                get;
                [StaticAccessor("GetPlayerSettings().GetEditorOnlyForUpdate()", StaticAccessorType.Dot)]
                set;
            }

            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            internal static extern string ConvertAndroidKeystorePath(string keystoreName);

            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            internal static extern string ConvertAndroidKeystoreNameToFullPath(string keystoreName);

            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            internal static extern string ConvertAndroidKeystoreNameToPath(string keystoreName);

            // Android keystore name
            public static extern string keystoreName
            {
                [NativeMethod("GetAndroidKeystorePath")]
                get;
                [NativeMethod("SetAndroidKeystorePath")]
                set;
            }

            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            internal static extern string GetAndroidKeystoreFullPath();

            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            internal static extern string GetAndroidKeystoresDedicatedLocationKey();

            // Android keystore password
            public static extern string keystorePass
            {
                [NativeMethod("GetAndroidKeystorePass")]
                get;
                [NativeMethod("SetAndroidKeystorePass")]
                set;
            }

            // Android key alias name
            public static extern string keyaliasName
            {
                [NativeMethod("GetAndroidKeyaliasName")]
                get;
                [NativeMethod("SetAndroidKeyaliasName")]
                set;
            }

            // Android key alias password
            public static extern string keyaliasPass
            {
                [NativeMethod("GetAndroidKeyaliasPass")]
                get;
                [NativeMethod("SetAndroidKeyaliasPass")]
                set;
            }

            // License verification flag
            public static extern bool licenseVerification
            {
                [NativeMethod("GetAndroidLicenseVerification")]
                get;
            }

            // Use APK Expansion Files
            public static extern bool useAPKExpansionFiles { get; set; }

            // Application should show ActivityIndicator when loading
            public static extern AndroidShowActivityIndicatorOnLoading showActivityIndicatorOnLoading
            {
                [NativeMethod("GetAndroidShowActivityIndicatorOnLoading")]
                get;
                [NativeMethod("SetAndroidShowActivityIndicatorOnLoading")]
                set;
            }

            // Android screen blit type
            public static extern AndroidBlitType blitType
            {
                [NativeMethod("GetAndroidBlitType")]
                get;
                [NativeMethod("SetAndroidBlitType")]
                set;
            }

            // enum { Legacy Wide Screen (1.86) = 0, Super Wide Screen (2.1) = 1, Custom = 2 }
            internal static extern int supportedAspectRatioMode
            {
                [NativeMethod("GetAndroidSupportedAspectRatio")]
                get;
                [NativeMethod("SetAndroidSupportedAspectRatio")]
                set;
            }

            // Maximum aspect ratio that is supported by the app. Black bars are added by Android OS if supportedAspectRatio is smaller than the screen aspect ratio.
            public static float maxAspectRatio
            {
                get { return GetAndroidMaxAspectRatio(); }
                set { SetAndroidMaxAspectRatioInternal(value); }
            }

            internal static extern float GetAndroidMaxAspectRatio();

            private static void SetAndroidMaxAspectRatioInternal(float value)
            {
                if ((Mathf.Abs(value - 1.86f) <= 0.001f) || (value >= 1.86f))
                {
                    SetAndroidSupportedAspectRatio(2);  // set supported aspect ratio mode to "Custom"
                    SetAndroidMaxAspectRatio(value);
                }
            }

            internal static extern void SetAndroidSupportedAspectRatio(int value);

            internal static extern void SetAndroidMaxAspectRatio(float value);

            internal static extern bool useLowAccuracyLocation
            {
                [NativeMethod("GetAndroidUseLowAccuracyLocation")]
                get;
            }

            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            [NativeMethod("GetAndroidMinSupportedAPILevel")]
            internal static extern int GetMinSupportedAPILevel();

            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            [NativeMethod("GetAndroidMinTargetAPILevel")]
            internal static extern int GetMinTargetAPILevel();

            // Start application in fullscreen mode
            public static extern bool startInFullscreen
            {
                [NativeMethod("GetAndroidStartInFullscreen")]
                get;
                [NativeMethod("SetAndroidStartInFullscreen")]
                set;
            }
            // Start application in fullscreen mode
            public static extern bool renderOutsideSafeArea
            {
                [NativeMethod("GetAndroidRenderOutsideSafeArea")]
                get;
                [NativeMethod("SetAndroidRenderOutsideSafeArea")]
                set;
            }

            // Validate release App Bundle size after build
            internal static extern bool validateAppBundleSize { get; set; }

            // App Bundle size which should cause warning message appear
            internal static extern int appBundleSizeToValidate { get; set; }
        }
    }
}
