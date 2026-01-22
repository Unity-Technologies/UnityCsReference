// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace UnityEditor
{
    // Target build platform.
    // When adding new platform, read this first - https://confluence.hq.unity3d.com/display/DEV/Adding+new+platform
    // When removing platform, read this first - https://confluence.hq.unity3d.com/display/DEV/Removing+platform
    ///<summary>Specifies the target platform for a Player or AssetBundle build.</summary>
    ///<remarks>Pass a platform property to <c>BuildTarget</c> to specify the target platform to build a Player for. For example, use <see cref="BuildTarget.Android" /> to target the Android platform. At runtime, use <c>BuildTarget</c> to identify the currently selected build target. Only actively supported platforms are documented in this list.
    ///
    ///**Important**: When targeting Windows, it's recommended to use the <c>StandaloneWindow64</c> target unless you specifically need to target devices that use a 32-bit CPU.</remarks>
    ///<example>
    ///  <code source="../../../Modules/ContentBuild/Tests/local.test.build-examples/Editor/BuildPipeline/BuildTarget_BuildTarget.cs"/>
    ///</example>
    ///<seealso cref="BuildPipeline.BuildPlayer" />
    ///<seealso cref="EditorUserBuildSettings.activeBuildTarget" />
    ///<seealso cref="BuildAssetBundlesParameters.targetPlatform" />
    [NativeHeader("Runtime/Serialize/SerializationMetaFlags.h")]
    public enum BuildTarget
    {
        ///<summary>Build a macOS standalone.
        ///
        ///To specify which architecture to use (Intel, ARM or Universal), please use <see cref="PlayerSettings.SetArchitecture" />.</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        StandaloneOSX = 2,

        ///<summary>Build a macOS Universal standalone.
        ///
        ///This build target has been removed, please use <see cref="StandaloneOSX" /> instead and specify the architecture to use using <see cref="PlayerSettings.SetArchitecture" />.</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        [System.Obsolete("Use StandaloneOSX instead (UnityUpgradable) -> StandaloneOSX", true)]
        StandaloneOSXUniversal = 3,

        ///<summary>Build a macOS Intel 32-bit standalone. (This build target is deprecated)</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        [System.Obsolete("StandaloneOSXIntel has been removed in 2017.3")]
        StandaloneOSXIntel = 4,

        ///<summary>Build a Windows 32-bit standalone.</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        StandaloneWindows = 5,

        ///<summary>Build a web player. (This build target is deprecated. Building for web player will no longer be supported in future versions of Unity.)</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        [System.Obsolete("WebPlayer has been removed in 5.4", true)]
        WebPlayer = 6,

        ///<summary>Build a streamed web player.</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        [System.Obsolete("WebPlayerStreamed has been removed in 5.4", true)]
        WebPlayerStreamed = 7,

        ///<summary>Build an iOS player.</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        iOS = 9,

        [System.Obsolete("PS3 has been removed in >=5.5")]
        [ExcludeFromDocs]
        PS3 = 10,

        [System.Obsolete("XBOX360 has been removed in 5.5")]
        [ExcludeFromDocs]
        XBOX360 = 11,

        // was StandaloneBroadcom = 12,

        ///<summary>Build an Android .apk standalone app.</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        Android = 13,

        // was StandaloneGLESEmu = 14,
        // was StandaloneGLES20Emu = 15,
        // was NaCl = 16,

        ///<summary>Build a Linux standalone.</summary>
        [System.Obsolete("StandaloneLinux has been removed in 2019.2")]
        StandaloneLinux = 17,

        ///<summary>Build a Windows 64-bit standalone.</summary>
        ///<remarks>For Arm64 and x64 architectures.</remarks>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        StandaloneWindows64 = 19,

        ///<summary>Build to WebGL platform.</summary>
        WebGL = 20,

        ///<summary>Build an Windows Store Apps player.</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        WSAPlayer = 21,

        ///<summary>Build a Linux 64-bit standalone.</summary>
        StandaloneLinux64 = 24,

        ///<summary>Build a Linux universal standalone.</summary>
        [System.Obsolete("StandaloneLinuxUniversal has been removed in 2019.2")]
        StandaloneLinuxUniversal = 25,

        [System.Obsolete("Use WSAPlayer with Windows Phone 8.1 selected")]
        [ExcludeFromDocs]
        WP8Player = 26,

        ///<summary>Build a macOS Intel 64-bit standalone. (This build target is deprecated).</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        [System.Obsolete("StandaloneOSXIntel64 has been removed in 2017.3")]
        StandaloneOSXIntel64 = 27,

        [System.Obsolete("BlackBerry has been removed in 5.4")]
        [ExcludeFromDocs]
        BlackBerry = 28,

        [System.Obsolete("Tizen has been removed in 2017.3")]
        [ExcludeFromDocs]
        Tizen = 29,

        /// Build a Vita Standalone
        /// SA: BuildPipeline.BuildPlayer.
        [System.Obsolete("PSP2 is no longer supported as of Unity 2018.3")]
        [ExcludeFromDocs]
        PSP2 = 30,

        ///<summary>Build a PS4 Standalone.</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        PS4 = 31,

        /// Build a Unity PlayStation Mobile (PSM) application
        /// SA: BuildPipeline.BuildPlayer.
        [System.Obsolete("PSM has been removed in >= 5.3")]
        [ExcludeFromDocs]
        PSM = 32,

        ///<summary>Build an Xbox One Standalone.</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        XboxOne = 33,

        [System.Obsolete("SamsungTV has been removed in 2017.3")]
        [ExcludeFromDocs]
        SamsungTV = 34,

        /// Build a Nintendo 3DS application
        /// SA: BuildPipeline.BuildPlayer.
        [System.Obsolete("Nintendo 3DS support is unavailable since 2018.1")]
        [ExcludeFromDocs]
        N3DS = 35,

        /// Build a Wii U player
        [System.Obsolete("Wii U support was removed in 2018.1")]
        [ExcludeFromDocs]
        WiiU = 36,

        ///<summary>Build to Apple's tvOS platform.</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        tvOS = 37,

        ///<summary>Build a Nintendo Switch player.</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        Switch = 38,

        [System.Obsolete("Lumin has been removed in 2022.2")]
        [ExcludeFromDocs]
        Lumin = 39,

        [System.Obsolete("Stadia has been removed in 2023.1")]
        [ExcludeFromDocs]
        Stadia = 40,

        ///<summary>Build a CloudRendering standalone.</summary>
        [System.Obsolete("CloudRendering is deprecated, please use LinuxHeadlessSimulation (UnityUpgradable) -> LinuxHeadlessSimulation", false)]
        CloudRendering = 41,

        ///<summary>Build a LinuxHeadlessSimulation standalone.</summary>
        LinuxHeadlessSimulation = 41, // LinuxHeadlessSimulation intenionally set to the same as CloudRendering

        [System.Obsolete("GameCoreScarlett is deprecated, please use GameCoreXboxSeries (UnityUpgradable) -> GameCoreXboxSeries", false)]
        [ExcludeFromDocs]
        GameCoreScarlett = 42,
        ///<summary>Build an Xbox Series player.</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        GameCoreXboxSeries = 42, // GameCoreXboxSeries intentionally set to the same as GameCoreScarlett
        ///<summary>Build an Xbox one player.</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        GameCoreXboxOne = 43,

        ///<summary>Build to PlayStation 5 platform.</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        PS5 = 44,

        [ExcludeFromDocs]
        EmbeddedLinux = 45,

        [ExcludeFromDocs]
        QNX = 46,

        ///<summary>Build a visionOS player.</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        VisionOS = 47,

        [ExcludeFromDocs]
        Switch2 = 48,

        [ExcludeFromDocs]
        Kepler = 49,

        ///<summary>OBSOLETE: Use iOS. Build an iOS player.</summary>
        ///<seealso cref="BuildPipeline.BuildPlayer" />
        [System.Obsolete("Use iOS instead (UnityUpgradable) -> iOS", true)]
        iPhone = -1,
        [System.Obsolete("BlackBerry has been removed in 5.4")]
        [ExcludeFromDocs]
        BB10 = -1,
        [System.Obsolete("Use WSAPlayer instead (UnityUpgradable) -> WSAPlayer", true)]
        [ExcludeFromDocs]
        MetroPlayer = -1,

        [ExcludeFromDocs]
        NoTarget = -2,
    }
}
