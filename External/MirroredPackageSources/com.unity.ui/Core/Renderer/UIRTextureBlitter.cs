using System;
using System.Collections.Generic;
using Unity.Profiling;

namespace UnityEngine.UIElements.UIR
{
    /// <summary>
    /// This class allows to queue blit commands and apply them up to 8 at a time later on.
    /// </summary>
    class TextureBlitter : IDisposable
    {
        const int k_TextureSlotCount = 8;
        static readonly int[] k_TextureIds;

        static ProfilerMarker s_CommitSampler = new ProfilerMarker("UIR.TextureBlitter.Commit");

        BlitInfo[] m_SingleBlit = new BlitInfo[1];
        Material m_BlitMaterial;
        RectInt m_Viewport;
        RenderTexture m_PrevRT;
        List<BlitInfo> m_PendingBlits;

        struct BlitInfo
        {
            public Texture src;
            // We assume an origin at the bottom-left corner of the textures.
            public RectInt srcRect;
            public Vector2Int dstPos;
            public int border; // Typically 0 or 1.
            public Color tint;
        }

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                UIRUtility.Destroy(m_BlitMaterial);
                m_BlitMaterial = null;
            }
            else
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern

        static TextureBlitter()
        {
            k_TextureIds = new int[k_TextureSlotCount];
            for (int i = 0; i < k_TextureSlotCount; ++i)
                k_TextureIds[i] = Shader.PropertyToID("_MainTex" + i);
        }

        public TextureBlitter(int capacity = 512)
        {
            m_PendingBlits = new List<BlitInfo>(capacity);
        }

        public void QueueBlit(Texture src, RectInt srcRect, Vector2Int dstPos, bool addBorder, Color tint)
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return;
            }

            m_PendingBlits.Add(new BlitInfo { src = src, srcRect = srcRect, dstPos = dstPos, border = addBorder ? 1 : 0, tint = tint });
        }

        public void BlitOneNow(RenderTexture dst, Texture src, RectInt srcRect, Vector2Int dstPos, bool addBorder, Color tint)
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return;
            }

            m_SingleBlit[0] = new BlitInfo { src = src, srcRect = srcRect, dstPos = dstPos, border = addBorder ? 1 : 0, tint = tint };
            BeginBlit(dst);
            DoBlit(m_SingleBlit, 0);
            EndBlit();
        }

        public int queueLength => m_PendingBlits.Count;

        public void Commit(RenderTexture dst)
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return;
            }

            if (m_PendingBlits.Count == 0)
                return;

            s_CommitSampler.Begin();
            BeginBlit(dst);
            for (int i = 0; i <  m_PendingBlits.Count; i += k_TextureSlotCount)
                DoBlit(m_PendingBlits, i);
            EndBlit();
            s_CommitSampler.End();

            m_PendingBlits.Clear();
        }

        public void Reset()
        {
            m_PendingBlits.Clear();
        }

        void BeginBlit(RenderTexture dst)
        {
            if (m_BlitMaterial == null)
            {
                var blitShader = Shader.Find(Shaders.k_AtlasBlit);
                m_BlitMaterial = new Material(blitShader);
                m_BlitMaterial.hideFlags |= HideFlags.DontSaveInEditor;
            }

            // store viewport as we'll have to restore it once the AtlasManager is done rendering
            m_Viewport = Utility.GetActiveViewport();
            m_PrevRT = RenderTexture.active;
            GL.LoadPixelMatrix(0, dst.width, 0, dst.height);
            Graphics.SetRenderTarget(dst);
        }

        void DoBlit(IList<BlitInfo> blitInfos, int startIndex)
        {
            int stopIndex = Mathf.Min(startIndex + k_TextureSlotCount, blitInfos.Count);

            // Bind and update the material.
            for (int blitIndex = startIndex, slotIndex = 0; blitIndex < stopIndex; ++blitIndex, ++slotIndex)
            {
                var texture = blitInfos[blitIndex].src;
                if (texture != null)
                    m_BlitMaterial.SetTexture(k_TextureIds[slotIndex], texture);
            }

            // Draw.
            m_BlitMaterial.SetPass(0);
            GL.Begin(GL.QUADS);
            for (int blitIndex = startIndex, slotIndex = 0; blitIndex < stopIndex; ++blitIndex, ++slotIndex)
            {
                BlitInfo current = blitInfos[blitIndex];

                float srcTexelWidth = 1f / current.src.width;
                float srcTexelHeight = 1f / current.src.height;

                // Destination coordinates (in integer pixels).
                float dstLeft = current.dstPos.x - current.border;
                float dstBottom = current.dstPos.y - current.border;
                float dstRight = current.dstPos.x + current.srcRect.width + current.border;
                float dstTop = current.dstPos.y + current.srcRect.height + current.border;

                // Source coordinates (normalized 0..1).
                float srcLeft = (current.srcRect.x - current.border) * srcTexelWidth;
                float srcBottom = (current.srcRect.y - current.border) * srcTexelHeight;
                float srcRight = (current.srcRect.xMax + current.border) * srcTexelWidth;
                float srcTop = (current.srcRect.yMax + current.border) * srcTexelHeight;

                // Bottom left
                GL.Color(current.tint);
                GL.TexCoord3(srcLeft, srcBottom, slotIndex);
                GL.Vertex3(dstLeft, dstBottom, 0.0f);

                // Top left
                GL.Color(current.tint);
                GL.TexCoord3(srcLeft, srcTop, slotIndex);
                GL.Vertex3(dstLeft, dstTop, 0.0f);

                // Top right
                GL.Color(current.tint);
                GL.TexCoord3(srcRight, srcTop, slotIndex);
                GL.Vertex3(dstRight, dstTop, 0.0f);

                // Bottom right
                GL.Color(current.tint);
                GL.TexCoord3(srcRight, srcBottom, slotIndex);
                GL.Vertex3(dstRight, dstBottom, 0.0f);
            }

            GL.End();
        }

        void EndBlit()
        {
            Graphics.SetRenderTarget(m_PrevRT);

            // restore viewport (which has been implicitly modified as we used a rendertarget)
            GL.Viewport(new Rect(m_Viewport.x, m_Viewport.y, m_Viewport.width, m_Viewport.height));
        }
    }
}
