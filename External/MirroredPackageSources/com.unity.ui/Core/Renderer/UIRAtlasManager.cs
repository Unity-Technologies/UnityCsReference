using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
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

        private int m_InitialSize;
        private UIRAtlasAllocator m_Allocator;
        private Dictionary<Texture2D, RectInt> m_UVs;
        private bool m_ForceReblitAll;
        private bool m_FloatFormat;
        private FilterMode m_FilterMode;
        private ColorSpace m_ColorSpace;
        private TextureBlitter m_Blitter;
        private int m_2SidePadding, m_1SidePadding;

        static ProfilerMarker s_MarkerReset = new ProfilerMarker("UIR.AtlasManager.Reset");

        public int maxImageSize { get; }
        public RenderTextureFormat format { get; }

        /// <summary>
        /// Current atlas texture in use. The texture could change after <c>UIRAtlasManager.Commit</c> is called.
        /// </summary>
        public RenderTexture atlas { get; private set; }

        public UIRAtlasManager(RenderTextureFormat format = RenderTextureFormat.ARGB32, FilterMode filterMode = FilterMode.Bilinear, int maxImageSize = 64, int initialSize = 64)
        {
            if (filterMode != FilterMode.Bilinear && filterMode != FilterMode.Point)
                throw new NotSupportedException("The only supported atlas filter modes are point or bilinear");

            this.format = format;
            this.maxImageSize = maxImageSize;
            m_FloatFormat = (format == RenderTextureFormat.ARGBFloat); // Identify any other formats to be used as float here
            m_FilterMode = filterMode;
            m_UVs = new Dictionary<Texture2D, RectInt>(64);
            m_Blitter = new TextureBlitter(64);
            m_InitialSize = initialSize;
            m_2SidePadding = filterMode == FilterMode.Point ? 0 : 2;
            m_1SidePadding = filterMode == FilterMode.Point ? 0 : 1;
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

        static int s_GlobalResetVersion;
        int m_ResetVersion = s_GlobalResetVersion;

        public static void MarkAllForReset()
        {
            ++s_GlobalResetVersion;
        }

        public void MarkForReset()
        {
            m_ResetVersion = s_GlobalResetVersion - 1;
        }

        public bool RequiresReset()
        {
            return m_ResetVersion != s_GlobalResetVersion;
        }

        public bool IsReleased()
        {
            // Returns true when the atlas hardware resources are released.
            // This can occur when RenderTexture::ReleaseAll() is called.
            return atlas != null && !atlas.IsCreated();
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

            s_MarkerReset.Begin();

            m_Blitter.Reset();
            m_UVs.Clear();
            m_Allocator = new UIRAtlasAllocator(m_InitialSize, 4096, m_1SidePadding);
            m_ForceReblitAll = false;
            m_ColorSpace = QualitySettings.activeColorSpace;
            UIRUtility.Destroy(atlas);

            s_MarkerReset.End();

            m_ResetVersion = s_GlobalResetVersion;
        }

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
            if (!AllocateRect(image.width, image.height, out uvs))
                return false;
            m_UVs[image] = uvs;

            // Add a blit instruction.
            m_Blitter.QueueBlit(image, new RectInt(0, 0, image.width, image.height), new Vector2Int(uvs.x, uvs.y), true, Color.white);

            return true;
        }

        public bool AllocateRect(int width, int height, out RectInt uvs)
        {
            // Attempt to allocate.
            if (!m_Allocator.TryAllocate(width + m_2SidePadding, height + m_2SidePadding, out uvs))
                return false;
            uvs = new RectInt(uvs.x + m_1SidePadding, uvs.y + m_1SidePadding, width, height);
            return true;
        }

        public void EnqueueBlit(Texture image, int x, int y, bool addBorder, Color tint)
        {
            m_Blitter.QueueBlit(image, new RectInt(0, 0, image.width, image.height), new Vector2Int(x, y), addBorder, tint);
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
                case TextureFormat.ASTC_4x4:            // Source is 8 bits per component
                case TextureFormat.ASTC_5x5:            // Source is 8 bits per component
                case TextureFormat.ASTC_6x6:            // Source is 8 bits per component
                case TextureFormat.ASTC_8x8:            // Source is 8 bits per component
                case TextureFormat.ASTC_10x10:          // Source is 8 bits per component
                case TextureFormat.ASTC_12x12:          // Source is 8 bits per component
#pragma warning disable 618
                // obsolete enums that are still warning (and not error)
                // please note that we reuse ASTC_RGB_NxN values for new enums so these are handled "automatically"
                case TextureFormat.ASTC_RGBA_4x4: case TextureFormat.ASTC_RGBA_5x5: case TextureFormat.ASTC_RGBA_6x6:
                case TextureFormat.ASTC_RGBA_8x8: case TextureFormat.ASTC_RGBA_10x10: case TextureFormat.ASTC_RGBA_12x12:
#pragma warning restore 618
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
                case TextureFormat.RG32:                // HDR
                case TextureFormat.RGB48:               // HDR
                case TextureFormat.RGBA64:              // HDR
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
            // linear data without potentially causing banding.
            if (!m_FloatFormat && m_ColorSpace == ColorSpace.Linear && image.activeTextureColorSpace != ColorSpace.Gamma)
                return false;

            if (SystemInfo.graphicsShaderLevel >= 35)
            {
                if (image.filterMode != FilterMode.Bilinear && image.filterMode != FilterMode.Point)
                    return false;
            }
            else
            {
                if (m_FilterMode != image.filterMode)
                    return false;
            }

            if (image.wrapMode != TextureWrapMode.Clamp)
                return false;

            return true;
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
                name = "UIR Atlas " + Random.Range(int.MinValue, int.MaxValue),
                filterMode = m_FilterMode
            };
        }
    }
}
