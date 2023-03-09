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

        Upm             = 1 << 0,
        BuiltIn         = 1 << 1,
        AssetStore      = 1 << 2,
        Feature         = 1 << 3,
        Placeholder     = 1 << 4
    }
}
