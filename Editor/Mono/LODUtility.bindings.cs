// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    internal struct LODVisualizationInformation
    {
        public int triangleCount;
        public int vertexCount;
        public int rendererCount;
        public int submeshCount;

        public int activeLODLevel;
        public float activeLODFade;
        public float activeDistance;
        public float activeRelativeScreenSize;
        public float activePixelSize;
        public float worldSpaceSize;
    }

    [NativeHeader("Editor/Mono/LODUtility.bindings.h")]
    public sealed class LODUtility
    {
        [FreeFunction("LODUtilityBindings::CalculateVisualizationData")]
        extern internal static LODVisualizationInformation CalculateVisualizationData(Camera camera, LODGroup group, int lodLevel);

        [FreeFunction("LODUtilityBindings::IsLODAnimatingOnDisplay")]
        extern internal static bool IsLODAnimatingOnDisplay(int displayId);

        [FreeFunction("LODUtilityBindings::IsLODAnimating")]
        extern internal static bool IsLODAnimating(Camera camera);

        [FreeFunction("LODUtilityBindings::CalculateDistance")]
        extern internal static float CalculateDistance(Camera camera, float relativeScreenHeight, LODGroup group);

        [FreeFunction("LODUtilityBindings::CalculateMeshLOD")]
        extern internal static UInt16 CalculateMeshLOD(Camera camera, Renderer renderer);

        [FreeFunction("LODUtilityBindings::GetMeshRelativeHeight")]
        extern internal static float GetMeshRelativeHeight(Camera camera, Renderer renderer);

        [FreeFunction("LODUtilityBindings::CalculateMeshLODDistance")]
        extern internal static float CalculateMeshLODDistance(Camera camera, float relativeScreenHeight, Renderer renderer);

        [FreeFunction("LODUtilityBindings::CalculateMeshLODBoundsPercentage")]
        extern internal static float CalculateMeshLODBoundsPercentage(Camera camera, UInt16 lodLevel, Renderer renderer);

        internal static Vector3 CalculateWorldReferencePoint(LODGroup group)
        {
            return group.worldReferencePoint;
        }

        [FreeFunction]
        extern internal static bool NeedUpdateLODGroupBoundingBox([NotNull] LODGroup group);

        public static void CalculateLODGroupBoundingBox(LODGroup group)
        {
            if (group == null)
                throw new ArgumentNullException("group");
            group.RecalculateBounds();
        }
    }
}
