// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    // This class is a raw dynamic atlas implementation with very few checks and validation. Its user must be careful
    // and ensure proper usage.
    internal class DynamicAtlasCore : IDisposable
    {
        private int m_InitialSize;
        private UIRAtlasAllocator m_Allocator;
        private Dictionary<Texture2D, RectInt> m_UVs;
        private bool m_ForceReblitAll;
        private FilterMode m_FilterMode;
        private ColorSpace m_ColorSpace;
        private TextureBlitter m_Blitter;
        private int m_2SidePadding, m_1SidePadding;
        int m_MaxAtlasSize;

        static ProfilerMarker s_MarkerReset = new ProfilerMarker("UIR.AtlasManager.Reset");

        public int maxImageSize { get; }
        public RenderTextureFormat format { get; }

        static int s_TextureCounter;

        /// <summary>
        /// Current atlas texture in use. The texture could change after <c>UIRAtlasManager.Commit</c> is called.
        /// </summary>
        public RenderTexture atlas { get; private set; }

        public DynamicAtlasCore(RenderTextureFormat format = RenderTextureFormat.ARGB32, FilterMode filterMode = FilterMode.Bilinear, int maxImageSize = 64, int initialSize = 64, int maxAtlasSize = 4096)
        {
            Debug.Assert(filterMode == FilterMode.Bilinear || filterMode == FilterMode.Point);
            Debug.Assert(maxAtlasSize <= SystemInfo.maxRenderTextureSize);
            Debug.Assert(initialSize <= maxAtlasSize);
            Debug.Assert(Mathf.IsPowerOfTwo(maxImageSize));
            Debug.Assert(Mathf.IsPowerOfTwo(initialSize));
            Debug.Assert(Mathf.IsPowerOfTwo(maxAtlasSize));

            m_MaxAtlasSize = maxAtlasSize;
            this.format = format;
            this.maxImageSize = maxImageSize;
            m_FilterMode = filterMode;
            m_UVs = new Dictionary<Texture2D, RectInt>(64);
            m_Blitter = new TextureBlitter(64);
            m_InitialSize = initialSize;
            m_2SidePadding = filterMode == FilterMode.Point ? 0 : 2;
            m_1SidePadding = filterMode == FilterMode.Point ? 0 : 1;
            m_Allocator = new UIRAtlasAllocator(m_InitialSize, m_MaxAtlasSize, m_1SidePadding);
            m_ColorSpace = QualitySettings.activeColorSpace;
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
                UIRUtility.Destroy(atlas);
                atlas = null;

                if (m_Allocator != null)
                {
                    m_Allocator.Dispose();
                    m_Allocator = null;
                }

                if (m_Blitter != null)
                {
                    m_Blitter.Dispose();
                    m_Blitter = null;
                }
            }
            else
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        private static void LogDisposeError()
        {
            Debug.LogError("An attempt to use a disposed atlas manager has been detected.");
        }

        #endregion // Dispose Pattern

        public bool IsReleased()
        {
            // Returns true when the atlas hardware resources are released.
            // This can occur when RenderTexture::ReleaseAll() is called.
            return atlas != null && !atlas.IsCreated();
        }

        /// <summary>
        /// If the provided texture is already in the atlas, the uvs are returned immediately. Otherwise, if the
        /// texture passes the requirements (format, etc), it will be virtually added to the atlas and added to the
        /// list of textures to be committed.
        /// </summary>
        public bool TryGetRect(Texture2D image, out RectInt uvs, Func<Texture2D, bool> filter = null)
        {
            uvs = new RectInt();

            if (disposed)
            {
                LogDisposeError();
                return false;
            }

            if (image == null)
                return false;

            // Is the image already in the atlas?
            if (m_UVs.TryGetValue(image, out uvs))
                return true;

            if (filter != null && !filter(image))
                return false;

            // Attempt to allocate.
            if (!AllocateRect(image.width, image.height, out uvs))
                return false;
            m_UVs[image] = uvs;

            // Add a blit instruction.
            m_Blitter.QueueBlit(image, new RectInt(0, 0, image.width, image.height), new Vector2Int(uvs.x, uvs.y), true, Color.white);

            return true;
        }

        public void UpdateTexture(Texture2D image)
        {
            if (disposed)
            {
                LogDisposeError();
                return;
            }

            RectInt uvs;
            if (!m_UVs.TryGetValue(image, out uvs))
                return;

            m_Blitter.QueueBlit(image, new RectInt(0, 0, image.width, image.height), new Vector2Int(uvs.x, uvs.y), true, Color.white);
        }

        public bool AllocateRect(int width, int height, out RectInt uvs)
        {
            // Attempt to allocate.
            if (!m_Allocator.TryAllocate(width + m_2SidePadding, height + m_2SidePadding, out uvs))
                return false;
            uvs = new RectInt(uvs.x + m_1SidePadding, uvs.y + m_1SidePadding, width, height);
            return true;
        }

        // This function, which allows to blit anything anywhere, should probably go in a separate class. Unlike the original
        // "atlas manager", who was able to rebuild the atlas texture from the dictionary, these are not internally tracked.
        // Having this hybrid atlas for the sake of reusing similar code makes the code actually more complicated in terms of
        // how we handle reset sequences, since they may differ based on the actual usage.
        public void EnqueueBlit(Texture image, RectInt srcRect, int x, int y, bool addBorder, Color tint)
        {
            m_Blitter.QueueBlit(image, srcRect, new Vector2Int(x, y), addBorder, tint);
        }

        public void Commit()
        {
            if (disposed)
            {
                LogDisposeError();
                return;
            }

            UpdateAtlasTexture();

            if (m_ForceReblitAll)
            {
                m_ForceReblitAll = false;
                m_Blitter.Reset();
                foreach (KeyValuePair<Texture2D, RectInt> kvp in m_UVs)
                    m_Blitter.QueueBlit(kvp.Key, new RectInt(0, 0, kvp.Key.width, kvp.Key.height), new Vector2Int(kvp.Value.x, kvp.Value.y), true, Color.white);
            }

            m_Blitter.Commit(atlas);
        }

        private void UpdateAtlasTexture()
        {
            if (atlas == null)
            {
                if (m_UVs.Count > m_Blitter.queueLength)
                {
                    // This can happen when the graphic device reloads.
                    m_ForceReblitAll = true;
                }

                atlas = CreateAtlasTexture();
                return;
            }

            if (atlas.width != m_Allocator.physicalWidth || atlas.height != m_Allocator.physicalHeight)
            {
                RenderTexture newAtlas = CreateAtlasTexture();
                if (newAtlas == null)
                    Debug.LogErrorFormat("Failed to allocate a render texture for the dynamic atlas. Current Size = {0}x{1}. Requested Size = {2}x{3}.", atlas.width, atlas.height, m_Allocator.physicalWidth, m_Allocator.physicalHeight);
                else
                    m_Blitter.BlitOneNow(newAtlas, atlas, new RectInt(0, 0, atlas.width, atlas.height), new Vector2Int(0, 0), false, Color.white);
                UIRUtility.Destroy(atlas);
                atlas = newAtlas;
            }
        }

        private RenderTexture CreateAtlasTexture()
        {
            if (m_Allocator.physicalWidth == 0 || m_Allocator.physicalHeight == 0)
                return null;

            // The RenderTextureReadWrite setting is purposely omitted in order to get the "Default" behavior.
            return new RenderTexture(m_Allocator.physicalWidth, m_Allocator.physicalHeight, 0, format)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = "UIR Dynamic Atlas " + s_TextureCounter++,
                filterMode = m_FilterMode
            };
        }
    }
}
