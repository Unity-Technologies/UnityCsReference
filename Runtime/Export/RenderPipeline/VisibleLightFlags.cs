// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Rendering
{
    [Flags]
    enum VisibleLightFlags
    {
        IntersectsNearPlane = 1 << 0,
        IntersectsFarPlane = 1 << 1,
        ForcedVisible = 1 << 2,
    }
}
