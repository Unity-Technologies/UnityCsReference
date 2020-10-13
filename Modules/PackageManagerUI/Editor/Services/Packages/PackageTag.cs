// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    [Flags]
    internal enum PackageTag : uint
    {
        None            = 0,

        // package type
        InDevelopment   = Custom, // Used by UPM develop package
        Custom          = 1 << 0,
        Local           = 1 << 1,
        Git             = 1 << 2,
        Bundled         = 1 << 3,
        BuiltIn         = 1 << 4,

        // attributes
        VersionLocked   = 1 << 8,
        Installable     = 1 << 9,
        Removable       = 1 << 10,
        Downloadable    = 1 << 11,
        Importable      = 1 << 12,
        Embeddable      = 1 << 13,

        // status
        Published       = 1 << 16,
        Deprecated      = 1 << 17,
        Verified        = 1 << 18,   // the recommended version if major version > 0
        Preview         = 1 << 19,   // with `preview`, `preview.x` tag or with `0` as major version
        Release         = 1 << 20    // no pre-release tag & major version > 0
    }
}
