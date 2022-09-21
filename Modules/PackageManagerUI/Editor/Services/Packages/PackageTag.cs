// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Flags]
    internal enum PackageTag : uint
    {
        None                = 0,

        InDevelopment       = Custom, // Used by UPM develop package
        Custom              = 1 << 0,
        Local               = 1 << 1,
        Git                 = 1 << 2,
        Bundled             = 1 << 3,
        BuiltIn             = 1 << 4,
        Feature             = 1 << 5,
        Placeholder         = 1 << 6,
        SpecialInstall      = 1 << 7,
        VersionLocked       = 1 << 8,

        LegacyFormat        = 1 << 10,   // legacy .unitypackage format
        UpmFormat           = 1 << 11,

        Unity               = 1 << 15,
        ScopedRegistry      = 1 << 16,
        MainNotUnity        = 1 << 17,

        Disabled            = 1 << 20,
        Published           = 1 << 21,
        Deprecated          = 1 << 22,
        Release             = 1 << 23,
        Experimental        = 1 << 24,
        PreRelease          = 1 << 25,
        ReleaseCandidate    = 1 << 26
    }
}
