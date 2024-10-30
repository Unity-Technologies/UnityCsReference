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
        /// Get the shading rate feature tier supoprted by the current platform.
        /// </summary>
        public static ShadingRateFeatureTier featureTier => GetFeatureTier();

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

        [FreeFunction("ShadingRateInfo::GetFeatureTier")]
        static extern ShadingRateFeatureTier GetFeatureTier();

        [FreeFunction("ShadingRateInfo::GetImageTileSize")]
        static extern Vector2Int GetImageTileSize();

        [FreeFunction("ShadingRateInfo::GetAvailableFragmentSizes")]
        static extern ShadingRateFragmentSize[] GetAvailableFragmentSizes();

        [FreeFunction("ShadingRateInfo::GetGraphicsFormat")]
        static extern GraphicsFormat GetGraphicsFormat();
    }
}
