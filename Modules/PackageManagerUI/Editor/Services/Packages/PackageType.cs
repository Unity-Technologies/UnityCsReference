// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager.UI
{
    [Flags]
    internal enum PackageType
    {
        None            = 0,

        Installable     = 1 << 0,
        BuiltIn         = 1 << 1,
        AssetStore      = 1 << 2
    }
}
