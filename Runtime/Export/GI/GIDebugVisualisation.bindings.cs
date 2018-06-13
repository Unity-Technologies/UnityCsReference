// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngineInternal
{
    // Texture type that can be shown when GIDebugVisualisation is enabled.
    public enum GITextureType
    {
        Charting,
        Albedo,
        Emissive,
        Irradiance,
        Directionality,
        Baked,
        BakedDirectional,
        InputWorkspace,
        BakedShadowMask,
        BakedAlbedo,
        BakedEmissive,
        BakedCharting,
        BakedTexelValidity,
        BakedUVOverlap,
        BakedLightmapCulling
    }

    [NativeHeader("Runtime/Export/GI/GIDebugVisualisation.bindings.h")]
    public static partial class GIDebugVisualisation
    {
        [FreeFunction]
        public extern static void ResetRuntimeInputTextures();

        [FreeFunction]
        public extern static void PlayCycleMode();

        [FreeFunction]
        public extern static void PauseCycleMode();

        [FreeFunction]
        public extern static void StopCycleMode();

        // Skip forwards or backwards in the systems list.
        [FreeFunction]
        public extern static void CycleSkipSystems(int skip);

        // Skip forwards or backwards in the instance list.
        [FreeFunction]
        public extern static void CycleSkipInstances(int skip);

        public static extern bool cycleMode {[FreeFunction] get; }

        public static extern bool pauseCycleMode {[FreeFunction] get; }

        public static extern GITextureType texType {[FreeFunction] get; [FreeFunction] set; }
    }
}
