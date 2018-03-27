// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngineInternal;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/LightProbeVisualization.h")]
    internal static partial class LightProbeVisualization
    {
        internal enum LightProbeVisualizationMode
        {
            OnlyProbesUsedBySelection = 0,
            AllProbesNoCells = 1,
            AllProbesWithCells = 2,
            None = 3,
        }

        [StaticAccessor("GetLightProbeVisualizationSettings()")]
        public extern static LightProbeVisualizationMode lightProbeVisualizationMode { get; set; }

        [StaticAccessor("GetLightProbeVisualizationSettings()")]
        public extern static bool showInterpolationWeights { get; set; }

        [StaticAccessor("GetLightProbeVisualizationSettings()")]
        public extern static bool showOcclusions { get; set; }

        [StaticAccessor("GetLightProbeVisualizationSettings()")]
        public extern static bool dynamicUpdateLightProbes { get; set; }

        [StaticAccessor("GetLightProbeVisualizationSettings()")]
        internal static extern void DrawPointCloud([NotNull] Vector3[] unselectedPositions, [NotNull] Vector3[] selectedPositions, Color baseColor, Color selectedColor, float scale, Transform cloudTransform);

        [StaticAccessor("GetLightProbeVisualizationSettings()")]
        internal static extern void DrawTetrahedra(bool shouldRecalculateTetrahedra, Vector3 cameraPosition);
    }
}
