// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.PackageManager
{
    [Obsolete("PackageStatus is deprecated and will be removed in a later version.", false)]
    public enum PackageStatus
    {
        Unknown,
        Unavailable,
        InProgress,
        Error,
        Available
    }
}
