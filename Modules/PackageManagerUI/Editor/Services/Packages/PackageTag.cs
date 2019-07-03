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

        // package source/origin
        InDevelopment   = 1 << 0,
        Local           = 1 << 1,
        Git             = 1 << 2,
        BuiltIn         = 1 << 3,
        Core            = 1 << 4,
        AssetStore      = 1 << 5,
        Published       = 1 << 6,
        Deprecated      = 1 << 7,

        // preview status
        Verified        = 1 << 10,   // the recommended version if major version > 0
        Preview         = 1 << 11,   // with `preview`, `preview.x` tag or with `0` as major version
        Release         = 1 << 12    // no pre-release tag & major version > 0
    }
}
