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
        public static float indirectScale { get { return 0.0f; } set {} }
        public static float updateThreshold { get { return 0.0f; } set {} }
        public static int   materialUpdateTimeSlice { get { return 0; } set {} }
        public static void  SetEmissive(Renderer renderer, Color color) {}
        public static void  SetEnvironmentData(float[] input) {}
        public static bool  synchronousMode { get { return false; } set {} }
        public static bool  isConverged { get { return false; } }

        internal static int scheduledMaterialUpdatesCount { get { return 0; } }
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
