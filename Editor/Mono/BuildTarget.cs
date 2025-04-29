// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor
{
    // Target build platform.
    // When adding new platform, read this first - https://confluence.hq.unity3d.com/display/DEV/Adding+new+platform
    // When removing platform, read this first - https://confluence.hq.unity3d.com/display/DEV/Removing+platform
    [NativeType("Runtime/Serialize/SerializationMetaFlags.h")]
    public enum BuildTarget
    {
        // Build an OS X standalone (universal build, with x86_64 currently supported).
        StandaloneOSX = 2,

        [System.Obsolete("Use StandaloneOSX instead (UnityUpgradable) -> StandaloneOSX", true)]
        StandaloneOSXUniversal = 3,

        [System.Obsolete("StandaloneOSXIntel has been removed in 2017.3")]
        StandaloneOSXIntel = 4,

        // Build a 32 bit Windows standalone.
        StandaloneWindows = 5,

        // *undocumented*
        [System.Obsolete("WebPlayer has been removed in 5.4", true)]
        WebPlayer = 6,

        // *undocumented*
        [System.Obsolete("WebPlayerStreamed has been removed in 5.4", true)]
        WebPlayerStreamed = 7,

        // Build an iOS player
        iOS = 9,

        // *undocumented*
        [System.Obsolete("PS3 has been removed in >=5.5")]
        PS3 = 10,

        // *undocumented*
        [System.Obsolete("XBOX360 has been removed in 5.5")]
        XBOX360 = 11,

        // was StandaloneBroadcom = 12,

        // Build an Android .apk standalone app
        Android = 13,

        // was StandaloneGLESEmu = 14,
        // was StandaloneGLES20Emu = 15,
        // was NaCl = 16,

        // Build a Linux standalone (i386 only).
        [System.Obsolete("StandaloneLinux has been removed in 2019.2")]
        StandaloneLinux = 17,

        // Build a Windows standalone.
        StandaloneWindows64 = 19,

        // *undocumented*
        WebGL = 20,

        // *undocumented*
        WSAPlayer = 21,

        // Build a Linux standalone (x86_64 only).
        StandaloneLinux64 = 24,

        // Build a Linux standalone (i386/x86_64 universal).
        [System.Obsolete("StandaloneLinuxUniversal has been removed in 2019.2")]
        StandaloneLinuxUniversal = 25,

        [System.Obsolete("Use WSAPlayer with Windows Phone 8.1 selected")]
        WP8Player = 26,

        [System.Obsolete("StandaloneOSXIntel64 has been removed in 2017.3")]
        StandaloneOSXIntel64 = 27,

        [System.Obsolete("BlackBerry has been removed in 5.4")]
        BlackBerry = 28,

        [System.Obsolete("Tizen has been removed in 2017.3")]
        Tizen = 29,

        /// Build a Vita Standalone
        /// SA: BuildPipeline.BuildPlayer.
        [System.Obsolete("PSP2 is no longer supported as of Unity 2018.3")]
        PSP2 = 30,

        /// Build a PS4 Standalone
        /// SA: BuildPipeline.BuildPlayer.
        PS4 = 31,

        /// Build a Unity PlayStation Mobile (PSM) application
        /// SA: BuildPipeline.BuildPlayer.
        [System.Obsolete("PSM has been removed in >= 5.3")]
        PSM = 32,

        /// Build an Xbox One Standalone
        /// SA: BuildPipeline.BuildPlayer.
        XboxOne = 33,

        [System.Obsolete("SamsungTV has been removed in 2017.3")]
        SamsungTV = 34,

        /// Build a Nintendo 3DS application
        /// SA: BuildPipeline.BuildPlayer.
        [System.Obsolete("Nintendo 3DS support is unavailable since 2018.1")]
        N3DS = 35,

        /// Build a Wii U player
        [System.Obsolete("Wii U support was removed in 2018.1")]
        WiiU = 36,

        tvOS = 37,

        Switch = 38,

        [System.Obsolete("Lumin has been removed in 2022.2")]
        Lumin = 39,

        [System.Obsolete("Stadia has been removed in 2023.1")]
        Stadia = 40,

        [System.Obsolete("CloudRendering is deprecated, please use LinuxHeadlessSimulation (UnityUpgradable) -> LinuxHeadlessSimulation", false)]
        CloudRendering = 41,

        LinuxHeadlessSimulation = 41, // LinuxHeadlessSimulation intenionally set to the same as CloudRendering

        [System.Obsolete("GameCoreScarlett is deprecated, please use GameCoreXboxSeries (UnityUpgradable) -> GameCoreXboxSeries", false)]
        GameCoreScarlett = 42,
        GameCoreXboxSeries = 42, // GameCoreXboxSeries intentionally set to the same as GameCoreScarlett
        GameCoreXboxOne = 43,

        PS5 = 44,

        EmbeddedLinux = 45,

        QNX = 46,

        VisionOS = 47,
        ReservedCFE = 48,

        Kepler = 49,

        // obsolete identifiers. We're using different values so that ToString() works.
        [System.Obsolete("Use iOS instead (UnityUpgradable) -> iOS", true)]
        iPhone = -1,
        [System.Obsolete("BlackBerry has been removed in 5.4")]
        BB10 = -1,
        [System.Obsolete("Use WSAPlayer instead (UnityUpgradable) -> WSAPlayer", true)]
        MetroPlayer = -1,

        // *undocumented*
        NoTarget = -2,
    }
}
