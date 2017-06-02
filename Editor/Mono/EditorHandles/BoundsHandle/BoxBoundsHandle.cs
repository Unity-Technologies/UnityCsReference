// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    public class BoxBoundsHandle : PrimitiveBoundsHandle
    {
        [Obsolete("Use parameterless constructor instead.")]
        public BoxBoundsHandle(int controlIDHint) : base(controlIDHint) {}

        public BoxBoundsHandle() : base() {}

        public UnityEngine.Vector3 size { get { return GetSize(); } set { SetSize(value); } }

        protected override void DrawWireframe()
        {
            Handles.DrawWireCube(center, size);
        }
    }
}
