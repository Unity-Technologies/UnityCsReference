// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    // Options for how to send a message.
    public enum SendMessageOptions
    {
        // A receiver is required for SendMessage.
        RequireReceiver = 0,
        // No receiver is required for SendMessage.
        DontRequireReceiver = 1
    }

    // The various primitives that can be created using the GameObject.CreatePrimitive function.
    public enum PrimitiveType
    {
        // A sphere primitive
        Sphere = 0,
        // A capsule primitive
        Capsule = 1,
        // A cylinder primitive
        Cylinder = 2,
        // A cube primitive
        Cube = 3,
        // A plane primitive
        Plane = 4,
        // A quad primitive
        Quad = 5
    }

    // The coordinate space in which to operate.
    public enum Space
    {
        // Applies transformation relative to the world coordinate system
        World = 0,
        // Applies transformation relative to the local coordinate system
        Self = 1
    }

    // The platform application is running. Returned by Application.platform.
    // NOTE: also match with enum in SystemInfo.h
    // ADD_NEW_PLATFORM_HERE
    public enum RuntimePlatform
    {
        // In the Unity editor on Mac OS X.
        OSXEditor = 0,
        // In the player on Mac OS X.
        OSXPlayer = 1,
        // In the player on Windows.
        WindowsPlayer = 2,
        //*undocumented*
        [System.Obsolete("WebPlayer export is no longer supported in Unity 5.4+.", true)]
        OSXWebPlayer = 3,
        // In the Dashboard widget on Mac OS X.
        [System.Obsolete("Dashboard widget on Mac OS X export is no longer supported in Unity 5.4+.", true)]
        OSXDashboardPlayer = 4,
        //*undocumented*
        [System.Obsolete("WebPlayer export is no longer supported in Unity 5.4+.", true)]
        WindowsWebPlayer = 5,
        // In the Unity editor on Windows.
        WindowsEditor = 7,
        // In the player on the iPhone.
        IPhonePlayer = 8,
        //*undocumented*
        [System.Obsolete("Xbox360 export is no longer supported in Unity 5.5+.")]
        XBOX360 = 10,
        //*undocumented*
        [System.Obsolete("PS3 export is no longer supported in Unity >=5.5.")]
        PS3 = 9,
        // In the player on Android devices.
        Android = 11,
        [System.Obsolete("NaCl export is no longer supported in Unity 5.0+.")]
        NaCl = 12,
        [System.Obsolete("FlashPlayer export is no longer supported in Unity 5.0+.")]
        FlashPlayer = 15,
        //*undocumented*
        LinuxPlayer = 13,
        LinuxEditor = 16,
        WebGLPlayer = 17,
        //*undocumented*
        [System.Obsolete("Use WSAPlayerX86 instead")]
        MetroPlayerX86 = 18,
        WSAPlayerX86 = 18,
        //*undocumented*
        [System.Obsolete("Use WSAPlayerX64 instead")]
        MetroPlayerX64 = 19,
        WSAPlayerX64 = 19,
        //*undocumented*
        [System.Obsolete("Use WSAPlayerARM instead")]
        MetroPlayerARM = 20,
        WSAPlayerARM = 20,
        [System.Obsolete("Windows Phone 8 was removed in 5.3")]
        WP8Player = 21,
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Obsolete("BB10Player export is no longer supported in Unity 5.4+.")]
        BB10Player = 22,
        [System.Obsolete("BlackBerryPlayer export is no longer supported in Unity 5.4+.")]
        BlackBerryPlayer = 22,
        //*undocumented*
        TizenPlayer = 23,
        // In the player on PS Vita
        PSP2 = 24,
        // In the player on PS4
        PS4 = 25,
        // In the player on PSM
        PSM = 26,
        // In the player on XboxOne
        XboxOne = 27,
        [System.Obsolete("SamsungTVPlayer export is no longer supported in Unity 2017.3+.")]
        SamsungTVPlayer = 28,
        // Wii U
        WiiU = 30,
        // tvOS
        tvOS = 31,
        // Nintendo Switch
        Switch = 32,
    }

    // The operating system family application is running. Returned by SystemInfo.operatingSystemFamily.
    // NOTE: also match with enum in SystemInfo.h
    // ADD_NEW_OPERATING_SYSTEM_FAMILY_HERE
    public enum  OperatingSystemFamily
    {
        // For operating systems that do not fall into any other category.
        Other = 0,

        // MacOSX operating system family.
        MacOSX = 1,

        // Windows operating system family.
        Windows = 2,

        // Linux operating system family.
        Linux = 3,
    }

    // The language the user's operating system is running in. Returned by Application.systemLanguage.
    public enum SystemLanguage
    {
        //Afrikaans
        Afrikaans = 0,
        //Arabic
        Arabic = 1,
        //Basque
        Basque = 2,
        //Belarusian
        Belarusian = 3,
        //Bulgarian
        Bulgarian = 4,
        //Catalan
        Catalan = 5,
        //Chinese
        Chinese = 6,
        //Czech
        Czech = 7,
        //Danish
        Danish = 8,
        //Dutch
        Dutch = 9,
        //English
        English = 10,
        //Estonian
        Estonian = 11,
        //Faroese
        Faroese = 12,
        //Finnish
        Finnish = 13,
        //French
        French = 14,
        //German
        German = 15,
        //Greek
        Greek = 16,
        //Hebrew
        Hebrew = 17,
        [System.Obsolete("Use SystemLanguage.Hungarian instead (UnityUpgradable) -> Hungarian", true)]

        Hugarian = 18,
        //Icelandic
        Icelandic = 19,
        //Indonesian
        Indonesian = 20,
        //Italian
        Italian = 21,
        //Japanese
        Japanese = 22,
        //Korean
        Korean = 23,
        //Latvian
        Latvian = 24,
        //Lithuanian
        Lithuanian = 25,
        //Norwegian
        Norwegian = 26,
        //Polish
        Polish = 27,
        //Portuguese
        Portuguese = 28,
        //Romanian
        Romanian = 29,
        //Russian
        Russian = 30,
        //Serbo-Croatian
        SerboCroatian = 31,
        //Slovak
        Slovak = 32,
        //Slovenian
        Slovenian = 33,
        //Spanish
        Spanish = 34,
        //Swedish
        Swedish = 35,
        //Thai
        Thai = 36,
        //Turkish
        Turkish = 37,
        //Ukrainian
        Ukrainian = 38,
        //Vietnamese
        Vietnamese = 39,
        //Chinese-Simplified
        ChineseSimplified = 40,
        //Chinese-Traditional
        ChineseTraditional = 41,
        //Unknown
        Unknown = 42,
        //Hungarian
        Hungarian = 18
    }
    // The type of the log message in the delegate registered with Application.RegisterLogCallback.
    public enum LogType
    {
        // LogType used for Errors.
        Error = 0,
        // LogType used for Asserts. (These indicate an error inside Unity itself.)
        Assert = 1,
        // LogType used for Warnings.
        Warning = 2,
        // LogType used for regular log messages.
        Log = 3,
        // LogType used for Exceptions.
        Exception = 4
    }
    // Enumeration for [[SystemInfo.deviceType]], denotes a coarse grouping of kinds of devices.
    public enum DeviceType
    {
        // Device type is unknown. You should never see this in practice.
        Unknown = 0,
        // A handheld device like mobile phone or a tablet.
        Handheld = 1,
        // A stationary gaming console.
        Console = 2,
        // Desktop or laptop computer.
        Desktop = 3,
    }

    // Enumeration for [[SystemInfo.batteryStatus]]
    public enum BatteryStatus
    {
        Unknown = 0,
        Charging = 1,
        Discharging = 2,
        NotCharging = 3,
        Full = 4
    }

    // Priority of a thread.
    public enum ThreadPriority
    {
        // Lowest thread priority
        Low = 0,
        // Below normal thread priority
        BelowNormal = 1,
        // Normal thread priority
        Normal = 2,
        // Highest thread priority
        High = 4
    }
}
