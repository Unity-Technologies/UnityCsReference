// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;

namespace UnityEngine.Rendering
{
    public partial class RenderPipelineManager
    {
        [Obsolete("beginFrameRendering is deprecated. Use beginContextRendering instead. #from 2023.3", false)]
        public static event Action<ScriptableRenderContext, Camera[]> beginFrameRendering;

        [Obsolete("endFrameRendering is deprecated. Use endContextRendering instead. #from 2023.3", false)]
        public static event Action<ScriptableRenderContext, Camera[]> endFrameRendering;
    }
}
