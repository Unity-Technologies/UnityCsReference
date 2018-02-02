// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.iOS
{
    [Flags]
    public enum SystemGestureDeferMode: uint
    {
        None = 0,
        TopEdge = 1 << 0,
        LeftEdge = 1 << 1,
        BottomEdge = 1 << 2,
        RightEdge = 1 << 3,
        All = TopEdge | LeftEdge | BottomEdge | RightEdge
    }
}
