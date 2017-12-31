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
        StandaloneOSXUniversal = 2,

        [System.Obsolete("StandaloneOSXIntel has been removed in 2017.3")]
        StandaloneOSXIntel = 4,

        // Build a Windows standalone.
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
        StandaloneLinux = 17,

        // Build a Windows x86_64 standalone.
        StandaloneWindows64 = 19,

        // *undocumented*
        WebGL = 20,

        // *undocumented*
        WSAPlayer = 21,

        // Build a Linux standalone (i386 only).
        StandaloneLinux64 = 24,

        // Build a Linux standalone (i386/x86_64 universal).
        StandaloneLinuxUniversal = 25,

        [System.Obsolete("Use WSAPlayer with Windows Phone 8.1 selected")]
        WP8Player = 26,

        [System.Obsolete("StandaloneOSXIntel64 has been removed in 2017.3")]
        StandaloneOSXIntel64 = 27,

        [System.Obsolete("BlackBerry has been removed in 5.4")]
        BlackBerry = 28,

        // *undocumented*
        Tizen = 29,

        /// Build a Vita Standalone
        /// SA: BuildPipeline.BuildPlayer.
        PSP2 = 30,

        /// Build a PS4 Standalone
        /// SA: BuildPipeline.BuildPlayer.
        PS4 = 31,

        /// Build a Unity PlayStation Mobile (PSM) application
        /// SA: BuildPipeline.BuildPlayer.
        [System.Obsolete("warning PSM has been removed in >= 5.3")]
        PSM = 32,

        /// Build an Xbox One Standalone
        /// SA: BuildPipeline.BuildPlayer.
        XboxOne = 33,

        [System.Obsolete("SamsungTV has been removed in 2017.3")]
        SamsungTV = 34,

        /// Build a Nintendo 3DS application
        /// SA: BuildPipeline.BuildPlayer.
        N3DS = 35,

        /// Build a Wii U player
        [System.Obsolete("Wii U support was removed in 2018.1")]
        WiiU = 36,

        tvOS = 37,

        Switch = 38,

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
