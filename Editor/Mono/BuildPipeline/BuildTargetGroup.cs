// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using System;
using System.ComponentModel;
using UnityEngine.Internal;

namespace UnityEditor
{
    // Build target group.
    // ADD_NEW_PLATFORM_HERE
    ///<summary>Build target group.</summary>
    ///<seealso cref="BuildPipeline.BuildPlayer" />
    ///<seealso cref="BuildTarget" />
    ///<seealso cref="PlayerSettings.SetArchitecture" />
    [NativeHeader("Editor/Src/BuildPipeline/BuildTargetPlatformSpecific.h")]
    public enum BuildTargetGroup
    {
        // Unknown target.
        ///<summary>Unknown target.</summary>
        Unknown = 0,

        // PC (Windows, Mac, Linux).
        ///<summary>PC (Windows, Mac, Linux) target.</summary>
        Standalone = 1,

        ///<summary>Mac/PC webplayer target.</summary>
        ///<remarks>Note that WebPlayer is not supported from 5.4 and onwards.</remarks>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("WebPlayer was removed in 5.4, consider using WebGL", true)]
        [ExcludeFromDocs]
        WebPlayer = 2,

        ///<summary>OBSOLETE: Use iOS. Apple iOS target.</summary>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use iOS instead (UnityUpgradable) -> iOS", true)]
        iPhone = 4,

        ///<summary>Apple iOS target.</summary>
        iOS = 4,

        [Obsolete("PS3 has been removed in >=5.5")]
        [ExcludeFromDocs]
        PS3 = 5,

        [Obsolete("XBOX360 has been removed in 5.5")]
        [ExcludeFromDocs]
        XBOX360 = 6,

        // Android target.
        ///<summary>Android target.</summary>
        Android = 7,

        // was Broadcom = 8,
        // was GLESEmu = 9,
        // was GLES20Emu = 10,
        // was NaCl = 11,

        ///<summary>WebGL.</summary>
        WebGL = 13,

        ///<summary>Windows Store Apps target.</summary>
        WSA = 14,

        [Obsolete("Use WSA instead")]
        [ExcludeFromDocs]
        Metro = 14,

        [Obsolete("Use WSA instead")]
        [ExcludeFromDocs]
        WP8 = 15,

        [Obsolete("BlackBerry has been removed as of 5.4")]
        [ExcludeFromDocs]
        BlackBerry = 16,

        [System.Obsolete("Tizen has been removed in 2017.3")]
        [ExcludeFromDocs]
        Tizen = 17,

        /// Sony Playstation Vita target.
        [System.Obsolete("PSP2 is no longer supported as of Unity 2018.3")]
        [ExcludeFromDocs]
        PSP2 = 18,

        ///<summary>Sony Playstation 4 target.</summary>
        PS4 = 19,

        /// Unity Playstation Mobile target.
        [Obsolete("PSM has been removed in >= 5.3")]
        [ExcludeFromDocs]
        PSM = 20,

        ///<summary>Microsoft Xbox One target.</summary>
        XboxOne = 21,

        [Obsolete("SamsungTV has been removed as of 2017.3")]
        [ExcludeFromDocs]
        SamsungTV = 22,

        /// Nintendo 3DS target.
        [System.Obsolete("Nintendo 3DS support is unavailable since 2018.1")]
        [ExcludeFromDocs]
        N3DS = 23,

        [Obsolete("Wii U support was removed in 2018.1")]
        [ExcludeFromDocs]
        WiiU = 24,

        ///<summary>Apple's tvOS target.</summary>
        tvOS = 25,

        [Obsolete("Facebook support was removed in 2019.3")]
        [ExcludeFromDocs]
        Facebook = 26,

        ///<summary>Nintendo Switch target.</summary>
        Switch = 27,

        [Obsolete("Lumin has been removed in 2022.2")]
        [ExcludeFromDocs]
        Lumin = 28,

        [Obsolete("Stadia has been removed in 2023.1")]
        [ExcludeFromDocs]
        Stadia = 29,

        ///<summary>CloudRendering target.</summary>
        [System.Obsolete("CloudRendering is deprecated, please use LinuxHeadlessSimulation (UnityUpgradable) -> LinuxHeadlessSimulation", false)]
        CloudRendering = 30,
        ///<summary>LinuxHeadlessSimulation target.</summary>
        LinuxHeadlessSimulation = 30,

        [System.Obsolete("GameCoreScarlett is deprecated, please use GameCoreXboxSeries (UnityUpgradable) -> GameCoreXboxSeries", false)]
        [ExcludeFromDocs]
        GameCoreScarlett = 31,
        [ExcludeFromDocs]
        GameCoreXboxSeries = 31, // GameCoreXboxSeries intentionally set to the same as GameCoreScarlett
        [ExcludeFromDocs]
        GameCoreXboxOne = 32,

        ///<summary>Sony Playstation 5 target.</summary>
        PS5 = 33,

        [ExcludeFromDocs]
        EmbeddedLinux = 34,

        [ExcludeFromDocs]
        QNX = 35,

        ///<summary>Apple visionOS target.</summary>
        VisionOS = 36,

        [ExcludeFromDocs]
        Switch2 = 37,

        [ExcludeFromDocs]
        Kepler = 38,
    }
}
