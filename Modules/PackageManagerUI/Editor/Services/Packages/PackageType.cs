// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Flags]
    internal enum PackageType
    {
        None            = 0,

        Installable     = 1 << 0,
        BuiltIn         = 1 << 1,
        AssetStore      = 1 << 2,
        Unity           = 1 << 3,
        ScopedRegistry  = 1 << 4,
        MainNotUnity    = 1 << 5,
        Feature         = 1 << 6
    }
}
