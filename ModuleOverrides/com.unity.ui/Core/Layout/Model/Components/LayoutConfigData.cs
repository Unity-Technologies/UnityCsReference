// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System.Runtime.InteropServices;

namespace UnityEngine.UIElements.Layout;

[StructLayout(LayoutKind.Sequential)]
struct LayoutConfigData
{
    public static LayoutConfigData Default => new()
    {
        PointScaleFactor = 1f
    };

    public float PointScaleFactor;
    [MarshalAs(UnmanagedType.U1)] public bool UseWebDefaults;
    [MarshalAs(UnmanagedType.U1)] public bool UseLegacyStretchBehaviour;
}
