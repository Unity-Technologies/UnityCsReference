// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Rendering;
using System;

namespace UnityEngine.Experimental.Rendering
{
    public class RenderPass : System.IDisposable
    {
        public class SubPass : System.IDisposable
        {
            public SubPass(RenderPass renderPass, RenderPassAttachment[] colors, RenderPassAttachment[] inputs, bool readOnlyDepth = false)
            {
                ScriptableRenderContext.BeginSubPassInternal(renderPass.context.Internal_GetPtr(),
                    colors != null ? colors : new RenderPassAttachment[] {},
                    inputs != null ? inputs : new RenderPassAttachment[] {},
                    readOnlyDepth);
            }

            public void Dispose()
            {
                // Nothing to do here
            }
        }

        public RenderPassAttachment[] colorAttachments { get; private set; }
        public RenderPassAttachment depthAttachment { get; private set; }

        // Render image width in pixels
        public int width { get; private set; }
        // Render image height in pixels
        public int height { get; private set; }

        // Number of MSAA samples, or 1 if no AA
        public int sampleCount { get; private set; }

        public UnityEngine.Experimental.Rendering.ScriptableRenderContext context { get; private set; }

        public void Dispose()
        {
            ScriptableRenderContext.EndRenderPassInternal(context.Internal_GetPtr());
        }

        public RenderPass(ScriptableRenderContext ctx, int w, int h, int samples, RenderPassAttachment[] colors, RenderPassAttachment depth = null)
        {
            width = w;
            height = h;
            sampleCount = samples;
            colorAttachments = colors;
            depthAttachment = depth;
            context = ctx;

            ScriptableRenderContext.BeginRenderPassInternal(ctx.Internal_GetPtr(), w, h, samples, colors, depth);
        }
    }
}
