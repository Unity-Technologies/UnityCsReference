// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Graphics/ShadingRateInfo.h")]
    public static partial class ShadingRateInfo
    {
        /// <summary>
        /// Returns true if image-based fragment shading rates are supported by the current graphics card.
        /// </summary>
        public static bool supportsPerImageTile => SupportsPerImageTile();

        /// <summary>
        /// Returns true if variable fragment shading rates per draw call are supported by the current graphics card
        /// </summary>
        public static bool supportsPerDrawCall => SupportsPerDrawCall();

        /// <summary>
        /// Get the shading rate image tile size used by the current platform.
        /// </summary>
        public static Vector2Int imageTileSize => GetImageTileSize();

        /// <summary>
        /// Get the supported shading rate fragment sizes by the current platform.
        /// </summary>
        public static ShadingRateFragmentSize[] availableFragmentSizes => GetAvailableFragmentSizes();

        /// <summary>
        /// Get the shading rate graphics format for the current platform.
        /// </summary>
        public static GraphicsFormat graphicsFormat => GetGraphicsFormat();

        /// <summary>
        /// Get the native graphics API value for ShadingRateFragmentSize.
        /// </summary>
        /// <param name="fragmentSize">Shading rate fragment size to get the native value of</param>
        /// <returns>Native graphics API value</returns>
        [FreeFunction("ShadingRateInfo::QueryNativeValue")]
        public static extern byte QueryNativeValue(ShadingRateFragmentSize fragmentSize);

        [FreeFunction("ShadingRateInfo::SupportsPerImageTile")]
        static extern bool SupportsPerImageTile();

        [FreeFunction("ShadingRateInfo::SupportsPerDrawCall")]
        static extern bool SupportsPerDrawCall();

        [FreeFunction("ShadingRateInfo::GetImageTileSize")]
        static extern Vector2Int GetImageTileSize();

        [FreeFunction("ShadingRateInfo::GetAvailableFragmentSizes")]
        static extern ShadingRateFragmentSize[] GetAvailableFragmentSizes();

        [FreeFunction("ShadingRateInfo::GetGraphicsFormat")]
        static extern GraphicsFormat GetGraphicsFormat();
    }
}
