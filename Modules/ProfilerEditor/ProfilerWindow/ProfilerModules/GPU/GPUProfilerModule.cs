// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class GPUProfilerModule : CPUorGPUProfilerModule
    {
        public override void OnEnable(IProfilerWindowController profilerWindow)
        {
            base.OnEnable(profilerWindow);
            m_FrameDataHierarchyView.gpuView = true;
            m_ViewType = ProfilerViewType.Hierarchy;
        }
    }
}
