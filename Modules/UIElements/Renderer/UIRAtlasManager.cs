// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace UnityEngine.UIElements
{
    internal interface IAtlasMonitor
    {
        bool RequiresReset();
    }

    internal class UIRAtlasManager : IDisposable
    {
        public static event Action<UIRAtlasManager> atlasManagerCreated;
        public static event Action<UIRAtlasManager> atlasManagerDisposed;

        // This utility can be moved outside this class once we got more use cases
        public struct ReadOnlyList<T> : IEnumerable<T>
        {
            List<T> m_List;

            public ReadOnlyList(List<T> list)
            {
                m_List = list;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return m_List.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return m_List.GetEnumerator();
            }

            public int Count => m_List.Count;

            public T this[int i] => m_List[i];
        }

        // A component interested in registering callbacks to our static creation event,
        // may be initialized after some UIRAtlasManager instances have already been created,
        // We therefore need to provide access to all current instances of UIRAtlasManager
        private static List<UIRAtlasManager> s_Instances = new List<UIRAtlasManager>();
        private static ReadOnlyList<UIRAtlasManager> s_InstancesreadOnly = new ReadOnlyList<UIRAtlasManager>(s_Instances);

        public static ReadOnlyList<UIRAtlasManager> Instances()
        {
            return s_InstancesreadOnly;
        }

        private struct BlitInfo
        {
            public Texture src;
            // Coordinates of the bottom-left corner of the texture relative to the bottom-left corner of the atlas.
            public int x;
            public int y;
            public bool addBorder;
        }

        private const int k_TextureSlotCount = 8;
        private static readonly int[] k_TextureIds;

        private UIRAtlasAllocator m_Allocator;
        private Dictionary<Texture2D, RectInt> m_UVs;
        private Material m_BlitMaterial;
        private List<BlitInfo> m_PendingBlits;
        private BlitInfo[] m_SingleBlit = new BlitInfo[1];
        private bool m_ForceReblitAll;
        private ColorSpace m_ColorSpace;
        private bool m_RequiresReset;

        public int maxImageSize { get; }

        /// <summary>
        /// Current atlas texture in use. The texture could change after <c>UIRAtlasManager.Commit</c> is called.
        /// </summary>
        public RenderTexture atlas { get; private set; }

        static UIRAtlasManager()
        {
            k_TextureIds = new int[k_TextureSlotCount];
            for (int i = 0; i < k_TextureSlotCount; ++i)
                k_TextureIds[i] = Shader.PropertyToID("_MainTex" + i);
        }

        public UIRAtlasManager(int maxImageSize = 64)
        {
            this.maxImageSize = maxImageSize;
            m_UVs = new Dictionary<Texture2D, RectInt>(64);
            m_PendingBlits = new List<BlitInfo>(64);
            Reset();

            s_Instances.Add(this);
            if (atlasManagerCreated != null)
                atlasManagerCreated(this);
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
            s_Instances.Remove(this);

            if (disposed)
                return;

            if (disposing)
            {
                UIRUtility.Destroy(atlas);
                atlas = null;

                UIRUtility.Destroy(m_BlitMaterial);
                m_BlitMaterial = null;

                if (m_Allocator != null)
                {
                    m_Allocator.Dispose();
                    m_Allocator = null;
                }

                if (atlasManagerDisposed != null)
                    atlasManagerDisposed(this);
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

        private HashSet<IAtlasMonitor> m_Monitors = new HashSet<IAtlasMonitor>();

        public void AddMonitor(IAtlasMonitor monitor)
        {
            m_Monitors.Add(monitor);
        }

        public void RemoveMonitor(IAtlasMonitor monitor)
        {
            m_Monitors.Remove(monitor);
        }

        public bool RequiresReset()
        {
            if (disposed)
            {
                LogDisposeError();
                return false;
            }

            if (!m_RequiresReset)
            {
                // Perform ALL the calls to merge all reset requests.
                foreach (IAtlasMonitor monitor in m_Monitors)
                {
                    bool requiresReset = monitor.RequiresReset();
                    m_RequiresReset = m_RequiresReset || requiresReset;
                }
            }

            return m_RequiresReset;
        }

        /// <remarks>
        /// When textures that have been previously allowed into the atlas manager change, or if the project color
        /// space changes, this method MUST be called. Textures that had been previously accepted into the atlas may
        /// now be refused, and previously refused textures may now be accepted.
        /// </remarks>
        public void Reset()
        {
            if (disposed)
            {
                LogDisposeError();
                return;
            }

            m_PendingBlits.Clear();
            m_UVs.Clear();
            m_Allocator = new UIRAtlasAllocator(64, 4096);
            m_ForceReblitAll = false;
            m_ColorSpace = QualitySettings.activeColorSpace;
            UIRUtility.Destroy(atlas);

            m_RequiresReset = false;

            if (ResetPerformed != null)
                ResetPerformed(this, EventArgs.Empty);
        }

        public event EventHandler ResetPerformed;

        /// <summary>
        /// If the provided texture is already in the atlas, the uvs are returned immediately. Otherwise, if the
        /// texture passes the requirements (format, etc), it will be virtually added to the atlas and added to the
        /// list of textures to be committed.
        /// </summary>
        public bool TryGetLocation(Texture2D image, out RectInt uvs)
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

            if (!IsTextureValid(image))
                return false;

            // Attempt to allocate.
            RectInt alloc;
            if (!m_Allocator.TryAllocate(image.width + 2, image.height + 2, out alloc))
                return false;
            uvs = new RectInt(alloc.x + 1, alloc.y + 1, image.width, image.height);
            m_UVs[image] = uvs;

            // Add a blit instruction.
            m_PendingBlits.Add(new BlitInfo { src = image, x = uvs.x, y = uvs.y, addBorder = true });

            return true;
        }

        /// <summary>
        /// We accept non-HDR formats that have 8 bits or less per component (before compression).
        /// </summary>
        /// <remarks>
        /// When the texture format is a compressed one, it is possible that we get additional loss due to the use of
        /// the atlas. For example, 1/256 could end up being uncompressed to 3/512. As long as we are within the shader
        /// with a float, there is no loss. However, since we're storing the value in a 8 bits per component atlas,
        /// 3/512 could end up being stored as 1/256 or 2/256. This creates potential differences in the results that
        /// may be observable in the following scenarios:
        ///   1) Blending Operations (e.g. blending with 3/512 vs 1/256 or 2/256).
        ///   2) Rendering to high precision (e.g. 16 bits per component) render targets.
        ///
        /// That being said, the benefit that we get from allowing these textures into the atlas seems much larger than
        /// the inconvenience caused by the slight error that we potentially introduce.
        /// </remarks>
        public static bool IsTextureFormatSupported(TextureFormat format)
        {
            switch (format)
            {
                case TextureFormat.Alpha8:
                case TextureFormat.ARGB4444:
                case TextureFormat.RGB24:
                case TextureFormat.RGBA32:
                case TextureFormat.ARGB32:
                case TextureFormat.RGB565:
                case TextureFormat.R16:
                case TextureFormat.DXT1:                // (BC1) Source is 5/6/5 bits per component
                case TextureFormat.DXT5:                // (BC3) Source is 5/6/5/8 bits per component
                case TextureFormat.RGBA4444:
                case TextureFormat.BGRA32:
                case TextureFormat.BC7:                 // Source is typically 8 bits per component (BUT COULD BE MORE)
                case TextureFormat.BC4:                 // Source is 1 components per color, 8 bits per component
                case TextureFormat.BC5:                 // Source is 2 components per color, 8 bits per component
                case TextureFormat.DXT1Crunched:        // See DXT1
                case TextureFormat.DXT5Crunched:        // See DXT5
                case TextureFormat.PVRTC_RGB2:          // Source is 8 bits per component or less
                case TextureFormat.PVRTC_RGBA2:         // Source is 8 bits per component or less
                case TextureFormat.PVRTC_RGB4:          // Source is 8 bits per component or less
                case TextureFormat.PVRTC_RGBA4:         // Source is 8 bits per component or less
                case TextureFormat.ETC_RGB4:            // Source is 8 bits per component
                case TextureFormat.EAC_R:               // Source is 8 bits per component
                case TextureFormat.EAC_R_SIGNED:        // Source is 8 bits per component
                case TextureFormat.EAC_RG:              // Source is 8 bits per component
                case TextureFormat.EAC_RG_SIGNED:       // Source is 8 bits per component
                case TextureFormat.ETC2_RGB:            // Source is 8 bits per component
                case TextureFormat.ETC2_RGBA1:          // Source is 8 bits per component
                case TextureFormat.ETC2_RGBA8:          // Source is 8 bits per component
                case TextureFormat.ASTC_RGB_4x4:        // Source is 8 bits per component
                case TextureFormat.ASTC_RGB_5x5:        // Source is 8 bits per component
                case TextureFormat.ASTC_RGB_6x6:        // Source is 8 bits per component
                case TextureFormat.ASTC_RGB_8x8:        // Source is 8 bits per component
                case TextureFormat.ASTC_RGB_10x10:      // Source is 8 bits per component
                case TextureFormat.ASTC_RGB_12x12:      // Source is 8 bits per component
                case TextureFormat.ASTC_RGBA_4x4:       // Source is 8 bits per component
                case TextureFormat.ASTC_RGBA_5x5:       // Source is 8 bits per component
                case TextureFormat.ASTC_RGBA_6x6:       // Source is 8 bits per component
                case TextureFormat.ASTC_RGBA_8x8:       // Source is 8 bits per component
                case TextureFormat.ASTC_RGBA_10x10:     // Source is 8 bits per component
                case TextureFormat.ASTC_RGBA_12x12:     // Source is 8 bits per component
                case TextureFormat.RG16:                // Source is 8 bits per component
                case TextureFormat.R8:                  // Source is 8 bits per component
                case TextureFormat.ETC_RGB4Crunched:    // See ETC
                case TextureFormat.ETC2_RGBA8Crunched:  // See ETC2
#pragma warning disable 618
                case TextureFormat.ETC_RGB4_3DS:        // Source is 4 bits per component
                case TextureFormat.ETC_RGBA8_3DS:       // Source is 8 bits per component
#pragma warning restore 618
                    return true;
                case TextureFormat.RHalf:               // HDR
                case TextureFormat.RGHalf:              // HDR
                case TextureFormat.RGBAHalf:            // HDR
                case TextureFormat.RFloat:              // HDR
                case TextureFormat.RGFloat:             // HDR
                case TextureFormat.RGBAFloat:           // HDR
                case TextureFormat.YUY2:                // Video Content
                case TextureFormat.RGB9e5Float:         // HDR
                case TextureFormat.BC6H:                // HDR
                case TextureFormat.ASTC_HDR_4x4:        // HDR
                case TextureFormat.ASTC_HDR_5x5:        // HDR
                case TextureFormat.ASTC_HDR_6x6:        // HDR
                case TextureFormat.ASTC_HDR_8x8:        // HDR
                case TextureFormat.ASTC_HDR_10x10:      // HDR
                case TextureFormat.ASTC_HDR_12x12:      // HDR
                    return false;
                default:
                    // This exception is required if we want to be able to detect new enum values in test
                    // UIRAtlasManagerTests.AllTextureFormatsAreHandled.
                    throw new NotImplementedException($"The support of texture format '{format}' is undefined.");
            }
        }

        private bool IsTextureValid(Texture2D image)
        {
            if (image.isReadable)
                return false;

            if (image.width > maxImageSize || image.height > maxImageSize)
                return false;

            if (!IsTextureFormatSupported(image.format))
                return false;

            // When in linear color space, the atlas will have sRGB read/write enabled. This means we can't store
            // linear data without potentially causing some potentially bad banding.
            if (m_ColorSpace == ColorSpace.Linear && image.activeTextureColorSpace != ColorSpace.Gamma)
                return false;

            // TODO: Add support for bilinear filtering. On modern APIs, we should switch between two samplers. On
            // older APIs, we could set the default atlas manager sampler to bilinear, and round to the center of the
            // pixels to when sampling with Point.
            if (image.filterMode != FilterMode.Point)
                return false;

            if (image.wrapMode != TextureWrapMode.Clamp)
                return false;

            return true;
        }

        public void Update()
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
                m_PendingBlits.Clear();
                foreach (KeyValuePair<Texture2D, RectInt> kvp in m_UVs)
                    m_PendingBlits.Add(new BlitInfo { src = kvp.Key, x = kvp.Value.x, y = kvp.Value.y, addBorder = true });
            }

            if (m_PendingBlits.Count > 0)
                Commit();
        }

        private void Commit()
        {
            Profiler.BeginSample("UIRAtlasManager.Commit");
            BeginBlit(atlas);
            for (int i = 0; i < m_PendingBlits.Count; i += k_TextureSlotCount)
                DoBlit(m_PendingBlits, i);
            EndBlit();
            Profiler.EndSample();

            m_PendingBlits.Clear();
        }

        private void UpdateAtlasTexture()
        {
            if (atlas == null)
            {
                if (m_UVs.Count > m_PendingBlits.Count)
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
                BlitSingle(atlas, newAtlas, 0, 0, false);
                UIRUtility.Destroy(atlas);
                atlas = newAtlas;
            }
        }

        private RenderTexture CreateAtlasTexture()
        {
            if (m_Allocator.physicalWidth == 0 || m_Allocator.physicalHeight == 0)
                return null;

            // The RenderTextureReadWrite setting is purposely omitted in order to get the "Default" behavior.
            return new RenderTexture(m_Allocator.physicalWidth, m_Allocator.physicalHeight, 0, RenderTextureFormat.ARGB32)
            {
                name = "UIR Atlas " + Random.Range(int.MinValue, int.MaxValue),
                filterMode = FilterMode.Point
            };
        }

        /// <param name="x">Distance between the left of the atlas and the left boundary of the texture.</param>
        /// <param name="y">Distance between the bottom of the atlas and the bottom boundary of the texture.</param>
        /// <param name="addBorder">Blit a border using the texture wrap mode.</param>
        private void BlitSingle(Texture src, RenderTexture dst, int x, int y, bool addBorder)
        {
            m_SingleBlit[0] = new BlitInfo { src = src, x = x, y = y, addBorder = addBorder };
            BeginBlit(dst);
            DoBlit(m_SingleBlit, 0);
            EndBlit();
        }

        private void BeginBlit(RenderTexture dst)
        {
            if (m_BlitMaterial == null)
            {
                var blitShader = Shader.Find("Hidden/Internal-UIRAtlasBlitCopy");
                m_BlitMaterial = new Material(blitShader);
            }

            GL.LoadPixelMatrix(0, dst.width, 0, dst.height);
            Graphics.SetRenderTarget(dst);
        }

        private void DoBlit(IList<BlitInfo> blitInfos, int startIndex)
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
                // Pixel offset for the border in the destination texture:
                float borderDstOffset = current.addBorder ? 1 : 0;

                // UV offset used to create the border when reading the texture.
                float borderSrcWidth = borderDstOffset / current.src.width;
                float borderSrcHeight = borderDstOffset / current.src.height;

                // Destination coordinates.
                float dstLeft = current.x - borderDstOffset;
                float dstBottom = current.y - borderDstOffset;
                float dstRight = current.x + current.src.width + borderDstOffset;
                float dstTop = current.y + current.src.height + borderDstOffset;

                // Source coordinates.
                float srcLeft = 0 - borderSrcWidth;
                float srcBottom = 0 - borderSrcHeight;
                float srcRight = 1 + borderSrcWidth;
                float srcTop = 1 + borderSrcHeight;

                // Bottom left
                GL.TexCoord3(srcLeft, srcBottom, slotIndex);
                GL.Vertex3(dstLeft, dstBottom, 0.0f);

                // Top left
                GL.TexCoord3(srcLeft, srcTop, slotIndex);
                GL.Vertex3(dstLeft, dstTop, 0.0f);

                // Top right
                GL.TexCoord3(srcRight, srcTop, slotIndex);
                GL.Vertex3(dstRight, dstTop, 0.0f);

                // Bottom right
                GL.TexCoord3(srcRight, srcBottom, slotIndex);
                GL.Vertex3(dstRight, dstBottom, 0.0f);
            }
            GL.End();
        }

        private void EndBlit()
        {
            Graphics.SetRenderTarget(null);
        }
    }
}
