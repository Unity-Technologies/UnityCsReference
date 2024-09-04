// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using System;
using System.Text.RegularExpressions;

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
        [Obsolete("X86 is no longer supported.")]
        X86 = 1 << 2,

        // x86_64
        X86_64 = 1 << 3,

        // All architectures
        All = 0xffffffff,
    }

    // Target devices.
    [Obsolete("AndroidTargetDevices is deprecated since ChromeOS is no longer supported.")]
    public enum AndroidTargetDevices
    {
        // All devices. The Android aplication is allowed to run on all devices.
        AllDevices = 0,
        // Only run the Android aplication on mobile phones, tablets, and TV devices, not on any Chrome OS devices.
        PhonesTabletsAndTVDevicesOnly = 1,
        // Only run the Android application on Chrome OS devices, not on any Android mobile phones, tablets, or TV devices.
        ChromeOSDevicesOnly = 2,
    }

    // Supported Android SDK versions
    public enum AndroidSdkVersions
    {
        // Set target API level to latest installed
        AndroidApiLevelAuto = 0,

        // Android 4.1, "Jelly Bean", API level 16
        [Obsolete(PlayerSettings.Android.MinSupportedAPILevelWarning, true)]
        AndroidApiLevel16 = 16,

        // Android 4.2, "Jelly Bean", API level 17
        [Obsolete(PlayerSettings.Android.MinSupportedAPILevelWarning, true)]
        AndroidApiLevel17 = 17,

        // Android 4.3, "Jelly Bean", API level 18
        [Obsolete(PlayerSettings.Android.MinSupportedAPILevelWarning, true)]
        AndroidApiLevel18 = 18,

        // Android 4.4, "KitKat", API level 19
        [Obsolete(PlayerSettings.Android.MinSupportedAPILevelWarning, true)]
        AndroidApiLevel19 = 19,

        // Android 5.0, "Lollipop", API level 21
        [Obsolete(PlayerSettings.Android.MinSupportedAPILevelWarning, true)]
        AndroidApiLevel21 = 21,

        // Android 5.1, "Lollipop", API level 22
        [Obsolete(PlayerSettings.Android.MinSupportedAPILevelWarning, true)]
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

        // Android 11.0, API level 30
        AndroidApiLevel30 = 30,

        // Android 12.0, API level 31
        AndroidApiLevel31 = 31,

        // Android 12L, API level 32
        AndroidApiLevel32 = 32,

        // Android 13.0, API level 33
        AndroidApiLevel33 = 33,

        // Android 14.0, API level 34
        AndroidApiLevel34 = 34,

        // Android 15.0, API level 35
        AndroidApiLevel35 = 35,
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

    public enum AndroidAutoRotationBehavior
    {
        User = 1,
        Sensor = 2,
    }

    [Flags]
    public enum AndroidApplicationEntry : uint
    {
        /// <summary>
        /// Include entry which derives from Activity
        /// - Activity https://developer.android.com/reference/android/app/Activity
        /// </summary>
        Activity = 1 << 0,
        /// <summary>
        /// Include entry which derives from Game Activity https://developer.android.com/games/agdk/game-activity
        /// </summary>
        GameActivity = 1 << 1
    }

    public struct AndroidDeviceFilterData
    {
        public string vendorName;
        public string deviceName;
        public string brandName;
        public string productName;
        public string androidOsVersionString;
        public string vulkanApiVersionString;
        public string driverVersionString;
    }

    // Player Settings is where you define various parameters for the final game that you will build in Unity. Some of these values are used in the Resolution Dialog that launches when you open a standalone game.
    public partial class PlayerSettings : UnityEngine.Object
    {
        // Android specific player settings
        [NativeHeader("Runtime/Misc/PlayerSettings.h")]
        [StaticAccessor("GetPlayerSettings()", StaticAccessorType.Dot)]
        public partial class Android
        {
            internal const string MinSupportedAPILevelWarning = "Minimum supported Android API level is 23 (Android 6.0 Marshmallow). Please use AndroidApiLevel23 or higher";

            // Disable Depth and Stencil Buffers
            public static extern bool disableDepthAndStencilBuffers { get; set; }

            // 24-bit Depth Buffer is used
            [Obsolete("use24BitDepthBuffer is deprecated, use disableDepthAndStencilBuffers instead.")]
            public static bool use24BitDepthBuffer
            {
                get { return !disableDepthAndStencilBuffers; }
                set {}
            }

            // Default horizontal dimension of Android player window.
            public static extern int defaultWindowWidth
            {
                [NativeMethod("GetAndroidDefaultWindowWidth")]
                get;
                [NativeMethod("SetAndroidDefaultWindowWidth")]
                set;
            }

            // Default vertical dimension of Android player window.
            public static extern int defaultWindowHeight
            {
                [NativeMethod("GetAndroidDefaultWindowHeight")]
                get;
                [NativeMethod("SetAndroidDefaultWindowHeight")]
                set;
            }

            // Minimum horizontal dimension of Android player window.
            public static extern int minimumWindowWidth
            {
                [NativeMethod("GetAndroidMinimumWindowWidth")]
                get;
                [NativeMethod("SetAndroidMinimumWindowWidth")]
                set;
            }

            // Minimum vertical dimension of Android player window.
            public static extern int minimumWindowHeight
            {
                [NativeMethod("GetAndroidMinimumWindowHeight")]
                get;
                [NativeMethod("SetAndroidMinimumWindowHeight")]
                set;
            }

            [Obsolete("resizableWindow has been deprecated and renamed to match Android documentation. Please use resizeableActivity instead. (UnityUpgradable) -> resizeableActivity", false)]
            // Should application resizing be allowed (deprecated old naming).
            public static bool resizableWindow
            {
                set => resizeableActivity = value;
                get => resizeableActivity;
            }

            // Should application resizing be allowed (new naming).
            public static extern bool resizeableActivity
            {
                [NativeMethod("GetAndroidResizeableActivity")]
                get;
                [NativeMethod("SetAndroidResizeableActivity")]
                set;
            }

            public static bool runWithoutFocus
            {
                set => runInBackground = value;
                get => runInBackground;
            }

            // Full screen mode. Full screen window or windowed.
            public static extern FullScreenMode fullscreenMode
            {
                [NativeMethod("GetAndroidFullscreenMode")]
                get;
                [NativeMethod("SetAndroidFullscreenMode")]
                set;
            }

            public static extern AndroidAutoRotationBehavior autoRotationBehavior
            {
                [NativeMethod("GetAndroidAutoRotationBehavior")]
                get;
                [NativeMethod("SetAndroidAutoRotationBehavior")]
                set;
            }

            // Android bundle version code
            public static extern int bundleVersionCode
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

            [Obsolete("ChromeOS is no longer supported.")]
            public static bool chromeosInputEmulation { get; set; }

            // Returns Android banner list
            internal static extern AndroidBanner[] GetAndroidBanners();

            internal static extern Texture2D GetAndroidBannerForHeight(int height);
            internal static extern Texture2D GetAndroidBannerForHeightCustomList(int height, AndroidBanner[] allBanners);

            internal static extern void SetAndroidBanners(Texture2D[] banners);
            internal static extern AndroidBanner[] SetAndroidBannersCustomList(Texture2D[] banners, AndroidBanner[] allBanners);

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

            // Enable Armv9 Security Features - Pointer Authentication (PAuth, PAC) and Branch Target Identification (BTI) for Arm64 builds
            public static extern bool enableArmv9SecurityFeatures
            {
                [NativeMethod("GetEnableArmv9SecurityFeatures")]
                get;
                [NativeMethod("SetEnableArmv9SecurityFeatures")]
                set;
            }

            // Enable Armv8.5a MTE - Memory Tagging for Arm64 builds
            internal static extern bool enableArm64MTE
            {
                [NativeMethod("GetEnableArm64MTE")]
                get;
                [NativeMethod("SetEnableArm64MTE")]
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

            [Obsolete("androidTargetDevices is deprecated since ChromeOS is no longer supported.")]
            public static AndroidTargetDevices androidTargetDevices { get; set; }

            [StaticAccessor("GetPlayerSettings().GetEditorOnly()", StaticAccessorType.Dot)]
            [NativeProperty("androidSplashScreen", TargetType.Field)]
            internal static extern Texture2D splashScreen { get; }

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
            public static extern bool splitApplicationBinary
            {
                [NativeMethod("GetAndroidSplitApplicationBinary")]
                get;
                [NativeMethod("SetAndroidSplitApplicationBinary")]
                set;
            }

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

            public static float minAspectRatio
            {
                get { return GetAndroidMinAspectRatio(); }
                set { SetAndroidMinAspectRatioInternal(value); }
            }

            internal static extern float GetAndroidMaxAspectRatio();
            internal static extern float GetAndroidMinAspectRatio();

            private static void SetAndroidMaxAspectRatioInternal(float value)
            {
                if (value < 1.86f)
                {
                    Debug.LogWarning($"Maximum Aspect Ratio must be greater or equal to 1.86. {value} is too small, setting to 1.86 instead.");
                    SetAndroidMaxAspectRatio(1.86f);
                }
                else
                {
                    SetAndroidMaxAspectRatio(value);
                }

                SetAndroidSupportedAspectRatio(2); // set supported aspect ratio mode to "Custom"
            }

            private static void SetAndroidMinAspectRatioInternal(float value)
            {
                if (value < 1.0f)
                {
                    Debug.LogWarning($"Minimum Aspect Ratio must be greater or equal to 1.0. {value} is too small, setting to 1.0 instead.");
                    SetAndroidMinAspectRatio(1.0f);
                }
                else
                {
                    SetAndroidMinAspectRatio(value);
                }

                SetAndroidSupportedAspectRatio(2); // set supported aspect ratio mode to "Custom"
            }

            internal static extern void SetAndroidSupportedAspectRatio(int value);

            internal static extern void SetAndroidMaxAspectRatio(float value);

            internal static extern void SetAndroidMinAspectRatio(float value);

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

            // Allow the application to render outside the safe area.
            public static extern bool renderOutsideSafeArea
            {
                [NativeMethod("GetAndroidRenderOutsideSafeArea")]
                get;
                [NativeMethod("SetAndroidRenderOutsideSafeArea")]
                set;
            }

            // Minify java code in release build
            [NativeProperty("AndroidMinifyRelease", TargetType.Function)]
            public static extern bool minifyRelease { get; set; }
            // Minify java code in development build
            [NativeProperty("AndroidMinifyDebug", TargetType.Function)]
            public static extern bool minifyDebug { get; set; }

            // Validate release App Bundle size after build
            internal static extern bool validateAppBundleSize { get; set; }

            // App Bundle size which should cause warning message appear
            internal static extern int appBundleSizeToValidate { get; set; }

            public static extern bool optimizedFramePacing
            {
                [NativeMethod("GetAndroidUseSwappy")]
                get;
                [NativeMethod("SetAndroidUseSwappy")]
                set;
            }

            public static TextureCompressionFormat[] textureCompressionFormats
            {
                get
                {
                    return GetTextureCompressionFormatsImpl(BuildTarget.Android);
                }
                set
                {
                    if (value == null || value.Length == 0)
                    {
                        throw new ArgumentException($"Android textureCompressionFormats can't be null or empty");
                    }
                    foreach (var format in value)
                    {
                        if (format == TextureCompressionFormat.Unknown || format == TextureCompressionFormat.BPTC)
                        {
                            throw new ArgumentException($"{format} can't be used as a target texture compression for Android");
                        }
                    }
                    SetTextureCompressionFormatsImpl(BuildTarget.Android, value);
                }
            }

            // Google Play App Dependencies info.
            [NativeProperty("AndroidReportGooglePlayAppDependencies", TargetType.Function)]
            public static extern bool reportGooglePlayAppDependencies { get; set; }

            public static extern AndroidApplicationEntry applicationEntry
            {
                [NativeMethod("GetAndroidApplicationEntry")]
                get;
                [NativeMethod("SetAndroidApplicationEntry")]
                set;
            }

            // Add enableOnBackInvokedCallback flag to AndroidManifest
            [NativeProperty("AndroidPredictiveBackSupport", TargetType.Function)]
            public static extern bool predictiveBackSupport { get; set; }

            internal static extern AndroidDeviceFilterData[] GetAndroidVulkanDenyFilterListImpl();
            internal static extern void SetAndroidVulkanDenyFilterListImpl(AndroidDeviceFilterData[] filterData);
            internal static extern AndroidDeviceFilterData[] GetAndroidVulkanAllowFilterListImpl();
            internal static extern void SetAndroidVulkanAllowFilterListImpl(AndroidDeviceFilterData[] filterData);

            private static readonly string vendorNameString = "vendorName";
            private static readonly string deviceNameString = "deviceName";
            private static readonly string brandNameString = "brandName";
            private static readonly string productNameString = "productName";
            private static readonly string vulkanApiVersionStringValue = "vulkanApiVersionString";
            private static readonly string driverVersionStringValue = "driverVersionString";

            // Keep in sync with same error message in AndroidDeviceFile.cs
            private static readonly string versionErrorMessage = "Version information should be formatted as:" +
                "\n1. 'MajorVersion.MinorVersion.PatchVersion' where MinorVersion and PatchVersion are optional and must only " +
                "contain numbers, or \n2. Hex number beginning with '0x' (max 4-bytes)";

            // Keep in sync with m_ValidVersionString in AndroidDeviceFile.cs
            private static readonly Regex validVersionString = new Regex(@"(^[0-9]+(\.[0-9]+){0,2}$)|(^0(x|X)([A-Fa-f0-9]{1,8})$)", RegexOptions.Compiled);

            internal static void CheckVersion(string value, string filterName, string fieldName)
            {
                if (!validVersionString.IsMatch(value))
                    throw new ArgumentException($"Invalid version string in {filterName} for {fieldName}=\"{value}\": {versionErrorMessage}");
            }

            internal static void CheckRegex(string value, string filterName, string fieldName)
            {
                try
                {
                    // Try to create a regex from the input string to determine if it is a valid regex
                    Regex regex = new Regex(value);
                }
                catch (ArgumentException e)
                {
                    throw new ArgumentException($"Invalid Regular Expression in {filterName} for {fieldName}=\"{value}\": {e.Message}");
                }
            }

            private static void CheckAllFilterData(AndroidDeviceFilterData[] filterDataList, string filterName)
            {
                // The check will throw an exception if there's an issue with the data.
                // We need to check the data here, as an invalid regex on the native side, can crash the game.
                foreach (var filterData in filterDataList)
                {
                    if (!String.IsNullOrEmpty(filterData.vendorName))
                        CheckRegex(filterData.vendorName, filterName, vendorNameString);
                    if (!String.IsNullOrEmpty(filterData.deviceName))
                        CheckRegex(filterData.deviceName, filterName, deviceNameString);
                    if (!String.IsNullOrEmpty(filterData.brandName))
                        CheckRegex(filterData.brandName, filterName, brandNameString);
                    if (!String.IsNullOrEmpty(filterData.productName))
                        CheckRegex(filterData.productName, filterName, productNameString);

                    if (!String.IsNullOrEmpty(filterData.vulkanApiVersionString))
                        CheckVersion(filterData.vulkanApiVersionString, filterName, vulkanApiVersionStringValue);
                    if (!String.IsNullOrEmpty(filterData.driverVersionString))
                        CheckVersion(filterData.driverVersionString, filterName, driverVersionStringValue);
                }
            }

            public static AndroidDeviceFilterData[] androidVulkanDenyFilterList
            {
                get
                {
                    return GetAndroidVulkanDenyFilterListImpl();
                }
                set
                {
                    if (value == null || value.Length == 0)
                        return;

                    CheckAllFilterData(value, "Vulkan Deny filter list");
                    SetAndroidVulkanDenyFilterListImpl(value);
                }
            }

            public static AndroidDeviceFilterData[] androidVulkanAllowFilterList
            {
                get
                {
                    return GetAndroidVulkanAllowFilterListImpl();
                }
                set
                {
                    if (value == null || value.Length == 0)
                        return;

                    CheckAllFilterData(value, "Vulkan Allow filter list");
                    SetAndroidVulkanAllowFilterListImpl(value);
                }
            }
        }
    }
}
