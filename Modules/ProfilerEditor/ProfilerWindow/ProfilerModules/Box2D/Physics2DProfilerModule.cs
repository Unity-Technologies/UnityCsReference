// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class Physics2DProfilerModule : ProfilerModuleBase
    {
        public override void DrawToolbar(Rect position)
        {
            DrawOtherToolbar(ProfilerArea.Physics2D);
        }

        public override void DrawView(Rect position)
        {
            DrawOverviewText(ProfilerArea.Physics2D, position);
        }
    }
}
