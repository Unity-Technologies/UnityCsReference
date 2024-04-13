// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    public sealed partial class GraphicsStateCollection : Object
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct GraphicsState
        {
            public VertexAttributeDescriptor[] vertexAttributes;
            public AttachmentDescriptor[] attachments;
            public SubPassDescriptor[] subPasses;
            public RenderStateBlock renderState;
            public MeshTopology topology;
            public CullMode forceCullMode;
            public float depthBias;
            public float slopeDepthBias;
            public int depthAttachmentIndex;
            public int subPassIndex;
            public int shadingRateIndex;
            public int multiviewCount;
            public int sampleCount;
            public bool wireframe;
            public bool invertCulling;
            public bool negativeScale;
            public bool invertProjection;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ShaderVariant
        {
            public Shader shader;
            public PassIdentifier passId;
            public LocalKeyword[] keywords;
        }
    }

    public sealed partial class GraphicsStateCollection : Object
    {
        public GraphicsStateCollection() { Internal_Create(this); }
        public GraphicsStateCollection(string filePath) { Internal_Create(this); LoadFromFile(filePath); }

        public void GetGraphicsStatesForVariant(ShaderVariant variant, List<GraphicsState> results)
        {
            GetGraphicsStatesForVariant(variant.shader, variant.passId, variant.keywords, results);
        }
        public int GetGraphicsStateCountForVariant(ShaderVariant variant)
        {
            return GetGraphicsStateCountForVariant(variant.shader, variant.passId, variant.keywords);
        }
    }
}
