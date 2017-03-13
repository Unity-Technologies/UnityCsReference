// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.Rendering
{
    [Flags]
    public enum VisibleLightFlags
    {
        None = 0,
        IntersectsNearPlane = 1,
        IntersectsFarPlane = 2,
    }
}
