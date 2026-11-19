// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements.UIR
{
    // The renderer's fields of one filter quad (UIEFilter in UnityUIEFilter.cginc). All rects are xMin, yMin, xMax, yMax. The
    // destination is normalized to the target, y down. A pass without a required input takes a zero scale-offset and
    // FilterInstanceBatch.k_EmptyRect.
    struct FilterQuad
    {
        public Vector4 destRect;
        public Vector4 sourceUVRect;
        public Vector4 sourceReadRect;
        public Vector4 inputScaleOffset;
        public Vector4 inputUVRect;
        public bool outputLinear;
    }

    // The quads of one instanced filter pass, drawn together. Each quad has the renderer's fields (UIEFilter in
    // UnityUIEFilter.cginc) and the pass parameters of its element. Nothing here depends on the size of the textures: the
    // rects are normalized, so a pass draws the same way into a whole texture or a region of a page.
    class FilterInstanceBatch
    {
        static readonly int k_DestRectId = Shader.PropertyToID("_UIE_DestRect");
        static readonly int k_SourceUVRectId = Shader.PropertyToID("_UIE_SourceUVRect");
        static readonly int k_SourceReadRectId = Shader.PropertyToID("_UIE_SourceReadRect");
        static readonly int k_InputScaleOffsetId = Shader.PropertyToID("_UIE_InputScaleOffset");
        static readonly int k_InputUVRectId = Shader.PropertyToID("_UIE_InputUVRect");
        static readonly int k_OutputLinearId = Shader.PropertyToID("_UIE_OutputLinear");
        static readonly int k_MainTexId = Shader.PropertyToID("_MainTex");

        public static readonly Vector4 k_FullRect = new Vector4(0, 0, 1, 1);
        public static readonly Vector4 k_EmptyRect = new Vector4(1, 1, -1, -1); // No sample is inside

        public static bool IsReserved(int nameId)
        {
            return nameId == k_DestRectId || nameId == k_SourceUVRectId || nameId == k_SourceReadRectId ||
                nameId == k_InputScaleOffsetId || nameId == k_InputUVRectId || nameId == k_OutputLinearId;
        }

        class ParameterArray
        {
            public int nameId;
            public FilterParameterKind kind;
            public readonly List<float> floats = new();
            public readonly List<Vector4> vectors = new();
            public readonly List<Matrix4x4> matrices = new();

            public void Clear()
            {
                floats.Clear();
                vectors.Clear();
                matrices.Clear();
            }

            public void Add(in FilterParameterValues.Entry entry)
            {
                switch (kind)
                {
                    case FilterParameterKind.Float: floats.Add(entry.floatValue); break;
                    case FilterParameterKind.Vector: vectors.Add(entry.vector); break;
                    default: matrices.Add(entry.matrix); break;
                }
            }

            public void SetArray(MaterialPropertyBlock block)
            {
                switch (kind)
                {
                    case FilterParameterKind.Float: block.SetFloatArray(nameId, floats); break;
                    case FilterParameterKind.Vector: block.SetVectorArray(nameId, vectors); break;
                    default: block.SetMatrixArray(nameId, matrices); break;
                }
            }
        }

        readonly List<Vector4> m_DestRects = new();
        readonly List<Vector4> m_SourceUVRects = new();
        readonly List<Vector4> m_SourceReadRects = new();
        readonly List<Vector4> m_InputScaleOffsets = new();
        readonly List<Vector4> m_InputUVRects = new();
        readonly List<float> m_OutputLinear = new();

        readonly List<ParameterArray> m_Parameters = new(); // Only the first m_ParameterCount are in use
        int m_ParameterCount;

        // Set when the quads don't all write the same properties with the same types. Draw then returns false, so each quad
        // reads what it reads when drawn alone. Batched, a quad that doesn't write a property would read 0 instead of the
        // material's value, and the engine reads an instanced property only from an array of the type that the shader
        // declares, so a quad that writes another type could hide the values of the other quads.
        bool m_WritesMixedParameters;

        readonly MaterialPropertyBlock m_Block = new();

        public int count => m_DestRects.Count;

        public void Clear()
        {
            m_DestRects.Clear();
            m_SourceUVRects.Clear();
            m_SourceReadRects.Clear();
            m_InputScaleOffsets.Clear();
            m_InputUVRects.Clear();
            m_OutputLinear.Clear();
            for (int i = 0; i < m_ParameterCount; ++i)
                m_Parameters[i].Clear();
            m_ParameterCount = 0;
            m_WritesMixedParameters = false;
        }

        public void Add(in FilterQuad filterQuad, FilterParameterValues values, int passSlot)
        {
            bool isFirst = count == 0;
            m_DestRects.Add(filterQuad.destRect);
            m_SourceUVRects.Add(filterQuad.sourceUVRect);
            m_SourceReadRects.Add(filterQuad.sourceReadRect);
            m_InputScaleOffsets.Add(filterQuad.inputScaleOffset);
            m_InputUVRects.Add(filterQuad.inputUVRect);
            m_OutputLinear.Add(filterQuad.outputLinear ? 1 : 0);

            if (m_WritesMixedParameters)
                return;

            // A pass writes each property once, so the same count and every property found means the same properties
            var entries = values != null ? values.GetPass(passSlot) : default;
            if (!isFirst && entries.Length != m_ParameterCount)
            {
                m_WritesMixedParameters = true;
                return;
            }

            for (int i = 0; i < entries.Length; ++i)
            {
                ref readonly var entry = ref entries[i];
                var array = isFirst ? AddArray(entry.nameId, entry.kind) : FindArray(entry.nameId);
                if (array == null || array.kind != entry.kind)
                {
                    m_WritesMixedParameters = true;
                    return;
                }
                array.Add(entry);
            }
        }

        ParameterArray FindArray(int nameId)
        {
            for (int i = 0; i < m_ParameterCount; ++i)
            {
                if (m_Parameters[i].nameId == nameId)
                    return m_Parameters[i];
            }
            return null;
        }

        ParameterArray AddArray(int nameId, FilterParameterKind kind)
        {
            if (m_ParameterCount == m_Parameters.Count)
                m_Parameters.Add(new ParameterArray());
            var array = m_Parameters[m_ParameterCount++];
            array.nameId = nameId;
            array.kind = kind;
            return array;
        }

        // Draws every quad into the active target. Returns false, without drawing, when the pass can't draw instanced (e.g. the
        // material doesn't enable instancing, the variant was stripped, or the quads write different parameters); the caller
        // then draws one element at a time.
        public bool Draw(in PostProcessingPass pass, RenderTexture target, Texture mainTexture, int inputTextureId, Texture inputTexture)
        {
            if (count == 0)
                return true;

            var material = pass.material;
            if (m_WritesMixedParameters || !material.enableInstancing || !Utility.SupportsInstancedQuads())
                return false;

            GL.PushMatrix();
            GL.Viewport(new Rect(0, 0, target.width, target.height));
            GL.LoadProjectionMatrix(ProjectionUtils.Ortho(0, 1, 1, 0, 0, 1));
            GL.modelview = Matrix4x4.identity;
            try
            {
                var block = m_Block;
                block.Clear();
                SetTextures(block, mainTexture, inputTextureId, inputTexture);
                block.SetVectorArray(k_DestRectId, m_DestRects);
                block.SetVectorArray(k_SourceUVRectId, m_SourceUVRects);
                block.SetVectorArray(k_SourceReadRectId, m_SourceReadRects);
                block.SetVectorArray(k_InputScaleOffsetId, m_InputScaleOffsets);
                block.SetVectorArray(k_InputUVRectId, m_InputUVRects);
                block.SetFloatArray(k_OutputLinearId, m_OutputLinear);
                for (int i = 0; i < m_ParameterCount; ++i)
                    m_Parameters[i].SetArray(block);

                return Utility.DrawInstancedQuads(material, pass.passIndex, block, count);
            }
            finally
            {
                GL.PopMatrix();
            }
        }

        // For a pass drawn one element at a time: the shader reads the same properties as uniforms, except the destination and
        // source UV rects, which the drawn quad carries. The pass parameters are already in the block
        // (FilterParameterValues.ApplyTo, then the settings callback).
        public static void SetUniforms(MaterialPropertyBlock block, in FilterQuad filterQuad)
        {
            block.SetVector(k_SourceReadRectId, filterQuad.sourceReadRect);
            block.SetVector(k_InputScaleOffsetId, filterQuad.inputScaleOffset);
            block.SetVector(k_InputUVRectId, filterQuad.inputUVRect);
            block.SetFloat(k_OutputLinearId, filterQuad.outputLinear ? 1 : 0);
        }

        static void SetTextures(MaterialPropertyBlock block, Texture mainTexture, int inputTextureId, Texture inputTexture)
        {
            block.SetTexture(k_MainTexId, mainTexture);
            if (inputTexture != null)
                block.SetTexture(inputTextureId, inputTexture);
        }
    }
}
