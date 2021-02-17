using System;

namespace UnityEngine.UIElements
{
    [Flags]
    enum DynamicAtlasFiltersInternal
    {
        None = 0,
        Readability = 1 << 0,
        Size = 1 << 1,
        Format = 1 << 2,
        ColorSpace = 1 << 3,
        FilterMode = 1 << 4,
    }

    /// <summary>
    /// Contains the settings used by the dynamic atlas system.
    /// </summary>
    [Serializable]
    public class DynamicAtlasSettings
    {
        [HideInInspector]
        [SerializeField]
        int m_MinAtlasSize;

        /// <summary>
        /// Specifies the minimum size (width/height) of the atlas texture, in pixels. This value must be a power of two,
        /// and must be greater than 0 and less than or equal to <see cref="maxAtlasSize"/>.
        /// </summary>
        public int minAtlasSize { get => m_MinAtlasSize; set => m_MinAtlasSize = value; }

        [HideInInspector]
        [SerializeField]
        int m_MaxAtlasSize;

        /// <summary>
        /// Specifies the maximum size (width/height) of the atlas texture, in pixels. This value must be a power of two,
        /// and must be greater than or equal to <see cref="minAtlasSize"/>.
        /// </summary>
        public int maxAtlasSize { get => m_MaxAtlasSize; set => m_MaxAtlasSize = value; }

        [HideInInspector]
        [SerializeField]
        int m_MaxSubTextureSize;

        /// <summary>
        /// Specifies the maximum size (width/height) of a texture that can be added to the atlas. When <see cref="activeFilters"/>
        /// contains <see cref="DynamicAtlasFilters.Size"/>, textures larger than this size are excluded from the atlas. Otherwise, this
        /// value is not used.
        /// </summary>
        public int maxSubTextureSize { get => m_MaxSubTextureSize; set => m_MaxSubTextureSize = value; }

        [HideInInspector]
        [SerializeField]
        DynamicAtlasFiltersInternal m_ActiveFilters;

        /// <summary>
        /// Defines the filters that the dynamic atlas system uses to exclude textures from the texture atlas.
        /// </summary>
        /// <remarks>
        /// When you assign a delegate to the atlas's custom filter, activeFilters is passed to the custom filter.
        /// </remarks>
        public DynamicAtlasFilters activeFilters { get => (DynamicAtlasFilters)m_ActiveFilters; set => m_ActiveFilters = (DynamicAtlasFiltersInternal)value; }

        /// <summary>
        /// Default filters for a dynamic atlas.
        /// </summary>
        public static DynamicAtlasFilters defaultFilters => DynamicAtlas.defaultFilters;

        DynamicAtlasCustomFilter m_CustomFilter;

        /// <summary>
        /// When a delegate is assigned, the dynamic atlas system calls it to determine whether or not a texture can be added to the atlas.
        /// </summary>
        public DynamicAtlasCustomFilter customFilter { get => m_CustomFilter; set => m_CustomFilter = value; }

        /// <summary>
        /// Specifies default values used to initialize the structure.
        /// </summary>
        public static DynamicAtlasSettings defaults =>
            new DynamicAtlasSettings
        {
            minAtlasSize = 64,
            maxAtlasSize = 4096,
            maxSubTextureSize = 64,
            activeFilters = defaultFilters,
            customFilter = null
        };
    }
}
