// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements;

[Flags]
internal enum PanelTransformFlags
{
    IsFlat = 1 << 0,
    DuringLayoutPhase = 1 << 1,

    Init = IsFlat
}

internal struct PanelTransformData
{
    public static readonly PanelTransformData Default = new()
    {
        Flags = PanelTransformFlags.Init
    };

    public PanelTransformFlags Flags;
}
