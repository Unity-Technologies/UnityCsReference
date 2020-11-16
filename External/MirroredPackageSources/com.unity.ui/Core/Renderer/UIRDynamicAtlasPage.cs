using System;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    class DynamicAtlasPage : IDisposable
    {
        public TextureId textureId { get; private set; }
        public RenderTexture atlas { get; private set; }
        public RenderTextureFormat format { get; }
        public FilterMode filterMode { get; }
        public Vector2Int minSize { get; }
        public Vector2Int maxSize { get; }
        public Vector2Int currentSize => m_CurrentSize;

        readonly int m_1Padding = 1;
        readonly int m_2Padding = 2;
        Allocator2D m_Allocator;
        TextureBlitter m_Blitter;
        Vector2Int m_CurrentSize;

        static int s_TextureCounter;

        public DynamicAtlasPage(RenderTextureFormat format, FilterMode filterMode, Vector2Int minSize, Vector2Int maxSize)
        {
            textureId = TextureRegistry.instance.AllocAndAcquireDynamic();
            this.format = format;
            this.filterMode = filterMode;
            this.minSize = minSize;
            this.maxSize = maxSize;

            m_Allocator = new Allocator2D(minSize, maxSize, m_2Padding);
            m_Blitter = new TextureBlitter(64);
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
                if (atlas != null)
                {
                    UIRUtility.Destroy(atlas);
                    atlas = null;
                }

                if (m_Allocator != null)
                {
                    // m_Allocator.Dispose(); TODO once we pool content
                    m_Allocator = null;
                }

                if (m_Blitter != null)
                {
                    m_Blitter.Dispose();
                    m_Blitter = null;
                }

                if (textureId != TextureId.invalid)
                {
                    TextureRegistry.instance.Release(textureId);
                    textureId = TextureId.invalid;
                }
            }
            else
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern

        public bool TryAdd(Texture2D image, out Allocator2D.Alloc2D alloc, out RectInt rect)
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                alloc = new Allocator2D.Alloc2D();
                rect = new RectInt();
                return false;
            }

            if (!m_Allocator.TryAllocate(image.width + m_2Padding, image.height + m_2Padding, out alloc))
            {
                rect = new RectInt();
                return false;
            }

            m_CurrentSize.x = Mathf.Max(m_CurrentSize.x, UIRUtility.GetNextPow2(alloc.rect.xMax));
            m_CurrentSize.y = Mathf.Max(m_CurrentSize.y, UIRUtility.GetNextPow2(alloc.rect.yMax));
            rect = new RectInt(alloc.rect.xMin + m_1Padding, alloc.rect.yMin + m_1Padding, image.width, image.height);

            Update(image, rect);

            return true;
        }

        public void Update(Texture2D image, RectInt rect)
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return;
            }

            Debug.Assert(image != null && rect.width > 0 && rect.height > 0);
            m_Blitter.QueueBlit(image, new RectInt(0, 0, image.width, image.height), new Vector2Int(rect.x, rect.y), true, Color.white);
        }

        public void Remove(Allocator2D.Alloc2D alloc)
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return;
            }

            Debug.Assert(alloc.rect.width > 0 && alloc.rect.height > 0);
            m_Allocator.Free(alloc);
        }

        public void Commit()
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return;
            }

            UpdateAtlasTexture();
            m_Blitter.Commit(atlas);
        }

        void UpdateAtlasTexture()
        {
            if (atlas == null)
            {
                atlas = CreateAtlasTexture();
                return;
            }

            if (atlas.width != m_CurrentSize.x || atlas.height != m_CurrentSize.x)
            {
                RenderTexture newAtlas = CreateAtlasTexture();
                if (newAtlas == null)
                    Debug.LogErrorFormat("Failed to allocate a render texture for the dynamic atlas. Current Size = {0}x{1}. Requested Size = {2}x{3}.",
                        atlas.width, atlas.height, m_CurrentSize.x, m_CurrentSize.y);
                else
                    m_Blitter.BlitOneNow(newAtlas, atlas,
                        new RectInt(0, 0, atlas.width, atlas.height),
                        new Vector2Int(0, 0), false, Color.white);
                UIRUtility.Destroy(atlas);
                atlas = newAtlas;
            }
        }

        RenderTexture CreateAtlasTexture()
        {
            if (m_CurrentSize.x == 0 || m_CurrentSize.y == 0)
                return null;

            // The RenderTextureReadWrite setting is purposely omitted in order to get the "Default" behavior.
            return new RenderTexture(m_CurrentSize.x, m_CurrentSize.y, 0, format)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = "UIR Dynamic Atlas Page " + s_TextureCounter++,
                filterMode = filterMode
            };
        }
    }
}
