// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/GI/DynamicGI.h")]
    public sealed partial class DynamicGI
    {
        [Obsolete("Enlighten realtime Global Illumination is deprecated and will be removed in a future release. #from(6000.7)", false)]
        public static extern float indirectScale { get; set; }
        [Obsolete("Enlighten realtime Global Illumination is deprecated and will be removed in a future release. #from(6000.7)", false)]
        public static extern float updateThreshold { get; set; }
        [Obsolete("Enlighten realtime Global Illumination is deprecated and will be removed in a future release. #from(6000.7)", false)]
        public static extern int   materialUpdateTimeSlice { get; set; }
        [Obsolete("Enlighten realtime Global Illumination is deprecated and will be removed in a future release. #from(6000.7)", false)]
        public static extern void  SetEmissive(Renderer renderer, Color color);
        [Obsolete("Enlighten realtime Global Illumination is deprecated and will be removed in a future release. #from(6000.7)", false)]
        [NativeMethod(ThrowsException = true)] public static extern void  SetEnvironmentData([NotNull] float[] input);
        [Obsolete("Enlighten realtime Global Illumination is deprecated and will be removed in a future release. #from(6000.7)", false)]
        public static extern bool  synchronousMode { get; set; }
        [Obsolete("Enlighten realtime Global Illumination is deprecated and will be removed in a future release. #from(6000.7)", false)]
        public static extern bool  isConverged { get; }

        internal static extern int scheduledMaterialUpdatesCount { get; }
        internal static extern bool asyncMaterialUpdates { get; set; }
        public static extern void UpdateEnvironment();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("DynamicGI.UpdateMaterials(Renderer) is deprecated; instead, use extension method from RendererExtensions: 'renderer.UpdateGIMaterials()' (UnityUpgradable).", true)]
        public static void UpdateMaterials(Renderer renderer) {}
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("DynamicGI.UpdateMaterials(Terrain) is deprecated; instead, use extension method from TerrainExtensions: 'terrain.UpdateGIMaterials()' (UnityUpgradable).", true)]
        public static void UpdateMaterials(Object renderer) {}
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("DynamicGI.UpdateMaterials(Terrain, int, int, int, int) is deprecated; instead, use extension method from TerrainExtensions: 'terrain.UpdateGIMaterials(x, y, width, height)' (UnityUpgradable).", true)]
        public static void UpdateMaterials(Object renderer, int x, int y, int width, int height) {}
    }
}
