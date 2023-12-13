// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;

namespace UnityEngine.Rendering
{
    public abstract partial class RenderPipelineAsset
    {
        [Obsolete($"This property is obsolete. Use {nameof(pipelineType)} instead. #from(23.2)", false)]
        protected internal virtual Type renderPipelineType
        {
            get
            {
                Debug.LogWarning($"You must either inherit from {nameof(RenderPipelineAsset)}<TRenderPipeline> or override {nameof(renderPipelineType)} property");
                return null;
            }
        }

        [Obsolete($"This property is obsolete. Use {nameof(RenderingLayerMask)} API and Tags & Layers project settings instead. #from(23.3)", false)]
        public virtual string[] renderingLayerMaskNames => null;

        [Obsolete($"This property is obsolete. Use {nameof(RenderingLayerMask)} API and Tags & Layers project settings instead. #from(23.3)", false)]
        public virtual string[] prefixedRenderingLayerMaskNames => null;
    }

    public abstract partial class RenderPipelineAsset<TRenderPipeline>
        where TRenderPipeline : RenderPipeline
    {
        [Obsolete($"This property is obsolete. Use {nameof(pipelineType)} instead. #from(23.2)", false)]
        protected internal sealed override Type renderPipelineType => typeof(TRenderPipeline);
    }
}
