// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    struct Line_Internal
    {
        public Vector2 Start;
        public Vector2 End;
        public Line_Internal(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
        }
    }
}
