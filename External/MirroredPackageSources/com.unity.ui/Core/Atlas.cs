using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    abstract class AtlasBase
    {
        public virtual bool TryGetAtlas(VisualElement ctx, Texture2D src, out TextureId atlas, out RectInt atlasRect)
        {
            atlas = TextureId.invalid;
            atlasRect = new RectInt();
            return false;
        }

        public virtual void ReturnAtlas(VisualElement ctx, Texture2D src, TextureId atlas) {}

        public virtual void Reset() {}

        protected virtual void OnAssignedToPanel(IPanel panel) {}
        protected virtual void OnRemovedFromPanel(IPanel panel) {}
        protected virtual void OnUpdateDynamicTextures(IPanel panel) {} // Called just before rendering occurs.

        internal void InvokeAssignedToPanel(IPanel panel) { OnAssignedToPanel(panel); }
        internal void InvokeRemovedFromPanel(IPanel panel) { OnRemovedFromPanel(panel); }
        internal void InvokeUpdateDynamicTextures(IPanel panel) { OnUpdateDynamicTextures(panel); }

        protected static void RepaintTexturedElements(IPanel panel)
        {
            var p = panel as Panel;
            var updater = p?.GetUpdater(VisualTreeUpdatePhase.Repaint) as UIRRepaintUpdater;
            updater?.renderChain?.RepaintTexturedElements();
        }

        protected TextureId AllocateDynamicTexture()
        {
            return textureRegistry.AllocAndAcquireDynamic();
        }

        protected void FreeDynamicTexture(TextureId id)
        {
            textureRegistry.Release(id);
        }

        protected void SetDynamicTexture(TextureId id, Texture texture)
        {
            textureRegistry.UpdateDynamic(id, texture);
        }

        // Overridable for tests
        internal TextureRegistry textureRegistry = TextureRegistry.instance;
    }

    /// <summary>
    /// Options to enable or disable filters for the dynamic atlas.
    /// </summary>
    /// <remarks>
    /// Filters exclude individual textures from the texture atlas based on specific criteria.
    /// </remarks>
    [Flags]
    public enum DynamicAtlasFilters
    {
        /// <summary>
        /// No filtering is performed.
        /// </summary>
        None = 0,

        /// <summary>
        /// Excludes readable textures.<br/><br/>
        ///
        /// Readable textures are textures that are readable from scripts, which means they are also writable or editable.
        /// Another way to think of this filter is as a way to exclude textures that are not read-only.
        /// </summary>
        Readability = 1 << 0,

        /// <summary>
        /// Excludes textures whose size exceeds the maximum sub-texture size specified in the dynamic atlas settings.
        /// </summary>
        Size = 1 << 1,

        /// <summary>
        /// Excludes textures that, because of their format, would lose precision, or be truncated when the system adds them to the atlas. <br/><br/>
        ///
        /// The dynamic atlas system accepts non-HDR texture formats that have 8 bits or less per component, before compression<br/><br/>
        ///
        /// You can add a compressed texture to a dynamic atlas. However, doing so might cause additional image loss because the system must first decompress
        /// the image in order to store it in the atlas. Decompression can yield values that are impossible to represent precisely in 8-bits per
        /// component. For example, a value of 1/256 in the compressed image might decompress to 3/512. The system cannot store 3/512
        /// in the atlas, so it stores the value as either 1/256 or 2/256.<br/><br/>
        ///
        /// This creates potential differences between the source texture and the version stored in the atlas. These differences are
        /// noticeable in the following scenarios:<br/><br/>
        ///   1. Blending Operations: 3/512, 1/256, and 2/256 each produce a different result when you use them in a blending operation.
        ///   2. Rendering to high precision render targets (for example, 16 bits per component).
        ///
        /// In most cases, the performance benefits of allowing compressed textures into the atlas outweigh the inconvenience of
        /// introducing small errors.
        /// </summary>
        Format = 1 << 2,

        /// <summary>
        /// Excludes textures whose color space does not match the color space of the atlas.
        /// </summary>
        ColorSpace = 1 << 3,

        /// <summary>
        /// Excludes textures that use a filter mode that the atlas does not support.<br/><br/>
        ///
        /// This filter is disabled by default. You can enable it to prevent artifacts that might occur when
        /// the atlas does not support the texture's filter mode, and cannot sample the texture correctly. However,
        /// because excluding textures from the atlas can reduce performance, the default behavior is preferable in most cases.<br/><br/>
        ///
        /// On GLES3 (and later) devices, the atlas supports more than one filter mode, so you should not need
        /// to enable this filter.
        /// </summary>
        FilterMode = 1 << 4,
    }

    /// <summary>
    /// Delegate that can be used as a custom filter for the dynamic atlas.
    /// </summary>
    /// <param name="texture">The texture to filter.</param>
    /// <param name="filtersToApply">The filters the dynamic atlas applies when the delegate returns <c>true</c>.
    /// by default, this value is equal to <see cref="DynamicAtlasSettings.activeFilters"/>.</param>
    /// <returns>
    /// When <c>false</c>, the texture cannot be added to the atlas. When <c>true</c> the texture is added to the atlas,
    /// as long as it is not excluded by filtersToApply.
    /// </returns>
    public delegate bool DynamicAtlasCustomFilter(Texture2D texture, ref DynamicAtlasFilters filtersToApply);

    class DynamicAtlas : AtlasBase
    {
        DynamicAtlasCore m_PointCore;
        TextureId m_PointDynamicTexture;

        DynamicAtlasCore m_BilinearCore;
        TextureId m_BilinearDynamicTexture;

        readonly Func<Texture2D, bool> m_IsValidPointDelegate;
        readonly Func<Texture2D, bool> m_IsValidBilinearDelegate;

        ColorSpace m_ColorSpace;
        List<IPanel> m_Panels = new List<IPanel>(1);

        internal bool isInitialized => m_PointCore != null || m_BilinearCore != null;

        public DynamicAtlas()
        {
            m_IsValidPointDelegate = t => IsTextureValid(t, FilterMode.Point);
            m_IsValidBilinearDelegate = t => IsTextureValid(t, FilterMode.Bilinear);
        }

        protected override void OnAssignedToPanel(IPanel panel)
        {
            base.OnAssignedToPanel(panel);
            m_Panels.Add(panel);
            if (m_Panels.Count == 1)
                m_ColorSpace = QualitySettings.activeColorSpace;
        }

        protected override void OnRemovedFromPanel(IPanel panel)
        {
            m_Panels.Remove(panel);
            if (m_Panels.Count == 0 && isInitialized)
                DestroyCores();
            base.OnRemovedFromPanel(panel);
        }

        public override void Reset()
        {
            if (isInitialized)
            {
                DestroyCores();

                for (int i = 0, count = m_Panels.Count; i < count; ++i)
                    RepaintTexturedElements(m_Panels[i]);
            }
        }

        void InitCores()
        {
            // Sanitize the parameters
            int cleanMaxSubTextureSize = Mathf.Max(m_MaxSubTextureSize, 1);
            cleanMaxSubTextureSize = Mathf.NextPowerOfTwo(cleanMaxSubTextureSize);

            int cleanMaxAtlasSize = Mathf.Max(m_MaxAtlasSize, 1);
            cleanMaxAtlasSize = Mathf.NextPowerOfTwo(cleanMaxAtlasSize);
            cleanMaxAtlasSize = Mathf.Min(cleanMaxAtlasSize, SystemInfo.maxRenderTextureSize);

            int cleanMinAtlasSize = Mathf.Max(m_MinAtlasSize, 1);
            cleanMinAtlasSize = Mathf.NextPowerOfTwo(cleanMinAtlasSize);
            cleanMinAtlasSize = Mathf.Min(cleanMinAtlasSize, cleanMaxAtlasSize);

            m_PointCore = new DynamicAtlasCore(RenderTextureFormat.ARGB32, FilterMode.Point, cleanMaxSubTextureSize, cleanMinAtlasSize, cleanMaxAtlasSize);
            m_PointDynamicTexture = AllocateDynamicTexture();

            m_BilinearCore = new DynamicAtlasCore(RenderTextureFormat.ARGB32, FilterMode.Bilinear, cleanMaxSubTextureSize, cleanMinAtlasSize, cleanMaxAtlasSize);
            m_BilinearDynamicTexture = AllocateDynamicTexture();
        }

        void DestroyCores()
        {
            FreeDynamicTexture(m_PointDynamicTexture);
            m_PointCore.Dispose();
            m_PointCore = null;

            FreeDynamicTexture(m_BilinearDynamicTexture);
            m_BilinearCore.Dispose();
            m_BilinearCore = null;
        }

        public override bool TryGetAtlas(VisualElement ve, Texture2D src, out TextureId atlas, out RectInt atlasRect)
        {
            atlas = TextureId.invalid;
            atlasRect = new RectInt();

            if (m_Panels.Count == 0)
                return false;

            if (!isInitialized)
                InitCores();

            // Purposely attempting the bilinear atlas first so that if the user ignores filtering, at least trilinear
            // will possibly end up in the bilinear atlas which is the lesser of two evils.
            if (m_BilinearCore.TryGetRect(src, out atlasRect, m_IsValidBilinearDelegate))
            {
                atlas = m_BilinearDynamicTexture;
                return true;
            }

            if (m_PointCore.TryGetRect(src, out atlasRect, m_IsValidPointDelegate))
            {
                atlas = m_PointDynamicTexture;
                return true;
            }

            return false;
        }

        public override void ReturnAtlas(VisualElement ve, Texture2D src, TextureId atlas)
        {
            // Once we implement removals from the atlas, we should decrement some counter.
        }

        protected override void OnUpdateDynamicTextures(IPanel panel)
        {
            if (m_PointCore != null)
            {
                m_PointCore.Commit();
                SetDynamicTexture(m_PointDynamicTexture, m_PointCore.atlas);
            }

            if (m_BilinearCore != null)
            {
                m_BilinearCore.Commit();
                SetDynamicTexture(m_BilinearDynamicTexture, m_BilinearCore.atlas);
            }
        }

        /// <summary>
        /// The dynamic atlas system accepts non-HDR texture formats that have 8 bits or less per component, before compression.
        /// </summary>
        /// <remarks>
        /// If you add a compressed texture to a dynamic atlas, you might see additional image loss. The system must first decompress
        /// the image in order to store it in the atlas, which can yield values that are impossible to represent precisely in 8-bits per
        /// channel. For example, a value of 1/256 in the compressed image might decompress to 3/512. The system cannot store 3/512
        /// in the atlas, so it stores the value as either 1/256 or 2/256.
        ///
        /// This creates potential differences between the source texture and the version stored in the atlas. These differences are
        /// noticeable in the following scenarios:
        ///   1) Blending Operations: you get different results if you blend 3/512 than you get if you blend with 1/256 or 2/256.
        ///   2) Rendering to high precision render targets (for example, 16 bits per component).
        ///
        /// In most cases, the performance benefits of allowing compressed textures into the atlas outweigh the inconvenience of
        /// introducing a small error.
        /// </remarks>
        internal static bool IsTextureFormatSupported(TextureFormat format)
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
                case TextureFormat.ASTC_RGBA_4x4:
                case TextureFormat.ASTC_RGBA_5x5:
                case TextureFormat.ASTC_RGBA_6x6:
                case TextureFormat.ASTC_RGBA_8x8:
                case TextureFormat.ASTC_RGBA_10x10:
                case TextureFormat.ASTC_RGBA_12x12:
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

        public virtual bool IsTextureValid(Texture2D texture, FilterMode atlasFilterMode)
        {
            var filters = m_ActiveFilters;
            if (m_CustomFilter != null && !m_CustomFilter(texture, ref filters))
                return false;

            bool filterReadability = (filters & DynamicAtlasFilters.Readability) != 0;
            bool filterSize = (filters & DynamicAtlasFilters.Size) != 0;
            bool filterFormat = (filters & DynamicAtlasFilters.Format) != 0;
            bool filterColorSpace = (filters & DynamicAtlasFilters.ColorSpace) != 0;
            bool filterFilterMode = (filters & DynamicAtlasFilters.FilterMode) != 0;

            if (filterReadability && texture.isReadable)
                return false;

            if (filterSize && (texture.width > maxSubTextureSize || texture.height > maxSubTextureSize))
                return false;

            if (filterFormat && !IsTextureFormatSupported(texture.format))
                return false;

            // When in linear color space, the atlas will have sRGB read/write enabled. This means we can't store
            // linear data without potentially causing banding.
            if (filterColorSpace && m_ColorSpace == ColorSpace.Linear && texture.activeTextureColorSpace != ColorSpace.Gamma)
                return false;

            if (filterFilterMode && texture.filterMode != atlasFilterMode)
                return false;

            return true;
        }

        public void SetDirty(Texture2D tex) // This API will be used later.
        {
            m_PointCore?.UpdateTexture(tex);
            m_BilinearCore?.UpdateTexture(tex);
        }

        #region Atlas Settings
        int m_MinAtlasSize = 64;
        int m_MaxAtlasSize = 4096;

        public int minAtlasSize
        {
            get { return m_MinAtlasSize; }
            set
            {
                if (m_MinAtlasSize == value)
                    return;

                m_MinAtlasSize = value;
                Reset();
            }
        }

        public int maxAtlasSize
        {
            get { return m_MaxAtlasSize; }
            set
            {
                if (m_MaxAtlasSize == value)
                    return;

                m_MaxAtlasSize = value;
                Reset();
            }
        }
        #endregion // Atlas Settings

        #region Filter Settings

        int m_MaxSubTextureSize = 64;
        DynamicAtlasFilters m_ActiveFilters = defaultFilters;
        DynamicAtlasCustomFilter m_CustomFilter;

        public static DynamicAtlasFilters defaultFilters =>
            DynamicAtlasFilters.Readability |
            DynamicAtlasFilters.Size |
            DynamicAtlasFilters.Format |
            DynamicAtlasFilters.ColorSpace |
            DynamicAtlasFilters.FilterMode;

        public DynamicAtlasFilters activeFilters
        {
            get { return m_ActiveFilters; }
            set
            {
                if (m_ActiveFilters == value)
                    return;

                m_ActiveFilters = value;
                Reset();
            }
        }

        public int maxSubTextureSize
        {
            get { return m_MaxSubTextureSize; }
            set
            {
                if (m_MaxSubTextureSize == value)
                    return;

                m_MaxSubTextureSize = value;
                Reset();
            }
        }

        public DynamicAtlasCustomFilter customFilter
        {
            get { return m_CustomFilter; }
            set
            {
                if (m_CustomFilter == value)
                    return;

                m_CustomFilter = value;
                Reset();
            }
        }

        #endregion // Filter Settings
    }
}
