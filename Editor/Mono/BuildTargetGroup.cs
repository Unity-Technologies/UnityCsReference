// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using System;

namespace UnityEditor
{
    // Build target group.
    // ADD_NEW_PLATFORM_HERE
    [NativeType(Header = "Editor/Src/BuildPipeline/BuildTargetPlatformSpecific.h")]
    public enum BuildTargetGroup
    {
        // Unknown target.
        Unknown = 0,

        // Mac/PC standalone target.
        Standalone = 1,

        //*undocumented*
        [Obsolete("WebPlayer was removed in 5.4, consider using WebGL", true)]
        WebPlayer = 2,

        // Apple iOS target.
        [Obsolete("Use iOS instead (UnityUpgradable) -> iOS", true)]
        iPhone = 4,

        iOS = 4,

        // *undocumented*
        [Obsolete("PS3 has been removed in >=5.5")]
        PS3 = 5,

        // *undocumented*
        [Obsolete("XBOX360 has been removed in 5.5")]
        XBOX360 = 6,

        // Android target.
        Android = 7,

        // was Broadcom = 8,
        // was GLESEmu = 9,
        // was GLES20Emu = 10,
        // was NaCl = 11,

        WebGL = 13,

        WSA = 14,

        //*undocumented*
        [Obsolete("Use WSA instead")]
        Metro = 14,

        [Obsolete("Use WSA instead")]
        WP8 = 15,

        //*undocumented*
        [Obsolete("BlackBerry has been removed as of 5.4")]
        BlackBerry = 16,

        Tizen = 17,

        /// Sony Playstation Vita target.
        PSP2 = 18,

        /// Sony Playstation 4 target.
        PS4 = 19,

        /// Unity Playstation Mobile target.
        [Obsolete("warning PSM has been removed in >= 5.3")]
        PSM = 20,

        /// Xbox One target.
        XboxOne = 21,

        [Obsolete("SamsungTV has been removed as of 2017.3")]
        SamsungTV = 22,

        /// Nintendo 3DS target.
        N3DS = 23,

        [Obsolete("Wii U support was removed in 2018.1")]
        WiiU = 24,

        tvOS = 25,

        Facebook = 26,

        Switch = 27,
    }
}
