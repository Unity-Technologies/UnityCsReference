// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using UnityEngine.Bindings;

namespace UnityEngine.Rendering
{
    [NativeHeader("Runtime/Export/RenderPipeline/ScriptableRenderPipeline.bindings.h")]
    [NativeHeader("Runtime/Graphics/ScriptableRenderLoop/ScriptableCulling.h")]
    [NativeHeader("Runtime/Scripting/ScriptingCommonStructDefinitions.h")]
    public partial struct CullingResults
    {
        [FreeFunction("ScriptableRenderPipeline_Bindings::GetLightIndexCount")]
        static extern int GetLightIndexCount(IntPtr cullingResultsPtr);

        [FreeFunction("ScriptableRenderPipeline_Bindings::GetReflectionProbeIndexCount")]
        static extern int GetReflectionProbeIndexCount(IntPtr cullingResultsPtr);

        [FreeFunction("FillLightAndReflectionProbeIndices")]
        static extern void FillLightAndReflectionProbeIndices(IntPtr cullingResultsPtr, ComputeBuffer computeBuffer);
        [FreeFunction("FillLightAndReflectionProbeIndices")]
        static extern void FillLightAndReflectionProbeIndicesGraphicsBuffer(IntPtr cullingResultsPtr, GraphicsBuffer buffer);

        [FreeFunction("GetLightIndexMapSize")]
        static extern int GetLightIndexMapSize(IntPtr cullingResultsPtr);

        [FreeFunction("GetReflectionProbeIndexMapSize")]
        static extern int GetReflectionProbeIndexMapSize(IntPtr cullingResultsPtr);

        [FreeFunction("FillLightIndexMapScriptable")]
        static extern void FillLightIndexMap(IntPtr cullingResultsPtr, IntPtr indexMapPtr, int indexMapSize);

        [FreeFunction("FillReflectionProbeIndexMapScriptable")]
        static extern void FillReflectionProbeIndexMap(IntPtr cullingResultsPtr, IntPtr indexMapPtr, int indexMapSize);

        [FreeFunction("SetLightIndexMapScriptable")]
        static extern void SetLightIndexMap(IntPtr cullingResultsPtr, IntPtr indexMapPtr, int indexMapSize);

        [FreeFunction("SetReflectionProbeIndexMapScriptable")]
        static extern void SetReflectionProbeIndexMap(IntPtr cullingResultsPtr, IntPtr indexMapPtr, int indexMapSize);

        [FreeFunction("ScriptableRenderPipeline_Bindings::GetShadowCasterBounds")]
        static extern bool GetShadowCasterBounds(IntPtr cullingResultsPtr, int lightIndex, out Bounds bounds);

        [FreeFunction("ScriptableRenderPipeline_Bindings::ComputeSpotShadowMatricesAndCullingPrimitives")]
        static extern bool ComputeSpotShadowMatricesAndCullingPrimitives(IntPtr cullingResultsPtr, int activeLightIndex,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);

        [FreeFunction("ScriptableRenderPipeline_Bindings::ComputePointShadowMatricesAndCullingPrimitives")]
        static extern bool ComputePointShadowMatricesAndCullingPrimitives(IntPtr cullingResultsPtr, int activeLightIndex,
            CubemapFace cubemapFace, float fovBias,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);

        [FreeFunction("ScriptableRenderPipeline_Bindings::ComputeDirectionalShadowMatricesAndCullingPrimitives")]
        static extern bool ComputeDirectionalShadowMatricesAndCullingPrimitives(IntPtr cullingResultsPtr, int activeLightIndex,
            int splitIndex, int splitCount, Vector3 splitRatio, int shadowResolution, float shadowNearPlaneOffset,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);
    }
}
