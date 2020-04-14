// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.Experimental.GraphView
{
    struct Line2
    {
        public Vector2 start { get; set; }
        public Vector2 end { get; set; }
        public Line2(Vector2 start, Vector2 end)
        {
            this.start = start;
            this.end = end;
        }
    }
}
