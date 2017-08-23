// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Experimental.UIElements.GraphView
{
    [Flags]
    internal
    enum Capabilities
    {
        Normal = 1 << 0,
        Selectable = 1 << 1,
        DoesNotCollapse = 1 << 2,
        Floating = 1 << 3,
        Resizable = 1 << 4,
        Movable = 1 << 5,
        Deletable = 1 << 6,
        Droppable = 1 << 7
    }
}
