// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;

namespace UnityEngine.Rendering
{
    public abstract partial class RenderPipelineAsset
    {
        [Obsolete($"{nameof(renderPipelineType)} is obsolete. Use {nameof(pipelineType)} instead. #from(23.2)", false)]
        protected internal virtual Type renderPipelineType
        {
            get
            {
                Debug.LogWarning($"You must either inherit from {nameof(RenderPipelineAsset)}<TRenderPipeline> or override {nameof(renderPipelineType)} property");
                return null;
            }
        }
    }

    public abstract partial class RenderPipelineAsset<TRenderPipeline>
        where TRenderPipeline : RenderPipeline
    {
        [Obsolete($"{nameof(renderPipelineType)} is obsolete. Use {nameof(pipelineType)} instead. #from(23.2)", false)]
        protected internal sealed override Type renderPipelineType => typeof(TRenderPipeline);
    }
}
