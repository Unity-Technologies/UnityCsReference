// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using System.Collections.Generic;

[assembly: InternalsVisibleTo("Unity.RenderPipelines.GPUDriven.Runtime")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.Core.Editor.Tests")]

namespace UnityEngine.Rendering
{
    using GPUDrivenRendererDataCallback = Action<GPUDrivenRendererData, IList<Mesh>, IList<Material>>;
    using GPUDrivenRendererDataNativeCallback = Action<GPUDrivenRendererDataNative, List<Mesh>, List<Material>, Action<GPUDrivenRendererData, IList<Mesh>, IList<Material>>>;
    using GPUDrivenLODGroupDataCallback = Action<GPUDrivenLODGroupData>;
    using GPUDrivenLODGroupDataNativeCallback = Action<GPUDrivenLODGroupDataNative, Action<GPUDrivenLODGroupData>>;

    [RequiredByNativeCode]
    [NativeHeader("Runtime/Camera/GPUDrivenProcessor.h")]
    internal class GPUDrivenProcessor
    {
        internal IntPtr m_Ptr;

        internal List<Mesh> scratchMeshes { get; private set; }
        internal List<Material> scratchMaterials { get; private set; }

        public GPUDrivenProcessor()
        {
            m_Ptr = Internal_Create();

            scratchMeshes = new List<Mesh>();
            scratchMaterials = new List<Material>();
        }

        ~GPUDrivenProcessor()
        {
            Destroy();
        }

        public void Dispose()
        {
            scratchMeshes = null;
            scratchMaterials = null;

            Destroy();
            GC.SuppressFinalize(this);
        }

        private void Destroy()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        private static extern IntPtr Internal_Create();

        private static extern void Internal_Destroy(IntPtr ptr);

        private static unsafe GPUDrivenRendererDataNativeCallback s_NativeRendererCallback = (GPUDrivenRendererDataNative nativeData, List<Mesh> meshes, List<Material> materials, GPUDrivenRendererDataCallback callback) =>
        {
            var rendererID = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(nativeData.rendererID, nativeData.rendererCount, Allocator.Invalid);
            var localBounds = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Bounds>(nativeData.localBounds, nativeData.rendererCount, Allocator.Invalid);
            var localToWorldMatrix = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Matrix4x4>(nativeData.localToWorldMatrix, nativeData.rendererCount, Allocator.Invalid);
            var prevLocalToWorldMatrix = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Matrix4x4>(nativeData.prevLocalToWorldMatrix, nativeData.rendererCount, Allocator.Invalid);
            var lightmapScaleOffset = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector4>(nativeData.lightmapScaleOffset, nativeData.rendererCount, Allocator.Invalid);
            var meshIndex = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(nativeData.meshIndex, nativeData.rendererCount, Allocator.Invalid);
            var subMeshStartIndex = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<short>(nativeData.subMeshStartIndex, nativeData.rendererCount, Allocator.Invalid);
            var materialsOffset = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(nativeData.materialsOffset, nativeData.rendererCount, Allocator.Invalid);
            var materialsCount = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<short>(nativeData.materialsCount, nativeData.rendererCount, Allocator.Invalid);
            var gameObjectLayer = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(nativeData.gameObjectLayer, nativeData.rendererCount, Allocator.Invalid);
            var renderingLayerMask = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<uint>(nativeData.renderingLayerMask, nativeData.rendererCount, Allocator.Invalid);
            var lodGroupID = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(nativeData.lodGroupID, nativeData.rendererCount, Allocator.Invalid);
            var lightmapIndex = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(nativeData.motionVecGenMode, nativeData.rendererCount, Allocator.Invalid);
            var packedRendererData = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<GPUDrivenPackedRendererData>(nativeData.packedRendererData, nativeData.rendererCount, Allocator.Invalid);
            var rendererPriority = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(nativeData.rendererPriority, nativeData.rendererCount, Allocator.Invalid);

            var meshID = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(nativeData.meshID, nativeData.meshCount, Allocator.Invalid);
            var subMeshCount = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<short>(nativeData.subMeshCount, nativeData.meshCount, Allocator.Invalid);
            var subMeshDescOffset = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(nativeData.subMeshDescOffset, nativeData.meshCount, Allocator.Invalid);

            var subMeshDesc = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<SubMeshDescriptor>(nativeData.subMeshDesc, nativeData.subMeshDescCount, Allocator.Invalid);

            var materialIndex = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(nativeData.materialIndex, nativeData.materialIndexCount, Allocator.Invalid);

            var materialID = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(nativeData.materialID, nativeData.materialCount, Allocator.Invalid);
            var isTransparent = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<bool>(nativeData.isTransparent, nativeData.materialCount, Allocator.Invalid);
            var isMotionVectorsPassEnabled = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<bool>(nativeData.isMotionVectorsPassEnabled, nativeData.materialCount, Allocator.Invalid);
            var materialFilterFlags = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(nativeData.materialFilterFlags, nativeData.materialCount, Allocator.Invalid);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref rendererID, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref localBounds, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref localToWorldMatrix, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref prevLocalToWorldMatrix, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref lightmapScaleOffset, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref meshIndex, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref subMeshStartIndex, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref materialsOffset, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref materialsCount, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref gameObjectLayer, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref renderingLayerMask, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref lodGroupID, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref lightmapIndex, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref packedRendererData, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref rendererPriority, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref meshID, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref subMeshCount, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref subMeshDescOffset, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref subMeshDesc, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref materialIndex, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref materialID, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref isTransparent, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref isMotionVectorsPassEnabled, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref materialFilterFlags, AtomicSafetyHandle.Create());
            GPUDrivenRendererData data = new GPUDrivenRendererData
            {
                rendererID = rendererID,
                localBounds = localBounds,
                localToWorldMatrix = localToWorldMatrix,
                prevLocalToWorldMatrix = prevLocalToWorldMatrix,
                lightmapScaleOffset = lightmapScaleOffset,
                meshIndex = meshIndex,
                subMeshStartIndex = subMeshStartIndex,
                materialsOffset = materialsOffset,
                materialsCount = materialsCount,
                gameObjectLayer = gameObjectLayer,
                renderingLayerMask = renderingLayerMask,
                lodGroupID = lodGroupID,
                lightmapIndex = lightmapIndex,
                packedRendererData = packedRendererData,
                rendererPriority = rendererPriority,
                meshID = meshID,
                subMeshCount = subMeshCount,
                subMeshDescOffset = subMeshDescOffset,
                subMeshDesc = subMeshDesc,
                materialIndex = materialIndex,
                materialID = materialID,
                isTransparent = isTransparent,
                isMotionVectorsPassEnabled = isMotionVectorsPassEnabled,
                materialFilterFlags = materialFilterFlags
            };

            callback(data, meshes, materials);

            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(rendererID));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(localBounds));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(localToWorldMatrix));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(prevLocalToWorldMatrix));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(lightmapScaleOffset));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(meshIndex));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(subMeshStartIndex));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(materialsOffset));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(materialsCount));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(gameObjectLayer));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(renderingLayerMask));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(lodGroupID));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(lightmapIndex));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(packedRendererData));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(rendererPriority));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(meshID));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(subMeshCount));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(subMeshDescOffset));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(subMeshDesc));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(materialIndex));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(materialID));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(isTransparent));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(isMotionVectorsPassEnabled));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(materialFilterFlags));
        };

        private static unsafe GPUDrivenLODGroupDataNativeCallback s_NativeLODGroupCallback = (GPUDrivenLODGroupDataNative nativeData, GPUDrivenLODGroupDataCallback callback) =>
        {
            var lodGroupID = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(nativeData.lodGroupID, nativeData.lodGroupCount, Allocator.Invalid);
            var lodOffset = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(nativeData.lodOffset, nativeData.lodGroupCount, Allocator.Invalid);
            var lodCount = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(nativeData.lodCount, nativeData.lodGroupCount, Allocator.Invalid);
            var fadeMode = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<LODFadeMode>(nativeData.fadeMode, nativeData.lodGroupCount, Allocator.Invalid);
            var worldSpaceReferencePoint = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector3>(nativeData.worldSpaceReferencePoint, nativeData.lodGroupCount, Allocator.Invalid);
            var worldSpaceSize = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<float>(nativeData.worldSpaceSize, nativeData.lodGroupCount, Allocator.Invalid);
            var renderersCount = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<short>(nativeData.renderersCount, nativeData.lodGroupCount, Allocator.Invalid);

            var invalidLODGroupID = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(nativeData.invalidLODGroupID, nativeData.invalidLODGroupCount, Allocator.Invalid);

            var lodRenderersCount = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<short>(nativeData.lodRenderersCount, nativeData.lodDataCount, Allocator.Invalid);
            var lodScreenRelativeTransitionHeight = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<float>(nativeData.lodScreenRelativeTransitionHeight, nativeData.lodDataCount, Allocator.Invalid);
            var lodFadeTransitionWidth = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<float>(nativeData.lodFadeTransitionWidth, nativeData.lodDataCount, Allocator.Invalid);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref lodGroupID, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref lodOffset, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref lodCount, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref fadeMode, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref worldSpaceReferencePoint, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref worldSpaceSize, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref renderersCount, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref invalidLODGroupID, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref lodRenderersCount, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref lodScreenRelativeTransitionHeight, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref lodFadeTransitionWidth, AtomicSafetyHandle.Create());
            GPUDrivenLODGroupData data = new GPUDrivenLODGroupData
            {
                lodGroupID = lodGroupID,
                lodOffset = lodOffset,
                lodCount = lodCount,
                fadeMode = fadeMode,
                worldSpaceReferencePoint = worldSpaceReferencePoint,
                worldSpaceSize = worldSpaceSize,
                renderersCount = renderersCount,
                invalidLODGroupID = invalidLODGroupID,
                lodRenderersCount = lodRenderersCount,
                lodScreenRelativeTransitionHeight = lodScreenRelativeTransitionHeight,
                lodFadeTransitionWidth = lodFadeTransitionWidth
            };

            callback(data);

            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(lodGroupID));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(lodOffset));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(lodCount));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(fadeMode));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(worldSpaceReferencePoint));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(worldSpaceSize));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(renderersCount));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(invalidLODGroupID));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(lodRenderersCount));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(lodScreenRelativeTransitionHeight));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(lodFadeTransitionWidth));
        };

        private extern void EnableGPUDrivenRenderingAndDispatchRendererData(ReadOnlySpan<int> renderersID, GPUDrivenRendererDataNativeCallback callback, List<Mesh> meshes, List<Material> materials, GPUDrivenRendererDataCallback param);
        public void EnableGPUDrivenRenderingAndDispatchRendererData(ReadOnlySpan<int> renderersID, GPUDrivenRendererDataCallback callback)
        {
            EnableGPUDrivenRenderingAndDispatchRendererData(renderersID, s_NativeRendererCallback, scratchMeshes, scratchMaterials, callback);
        }
        public extern void DisableGPUDrivenRendering(ReadOnlySpan<int> renderersID);

        private extern void DispatchLODGroupData(ReadOnlySpan<int> lodGroupID, GPUDrivenLODGroupDataNativeCallback callback, GPUDrivenLODGroupDataCallback param);
        public void DispatchLODGroupData(ReadOnlySpan<int> lodGroupID, GPUDrivenLODGroupDataCallback callback)
        {
            DispatchLODGroupData(lodGroupID, s_NativeLODGroupCallback, callback);
        }

        public extern bool enablePartialRendering { set; get; }
        public extern bool enableMaterialFilters { set; get; }
        public extern void AddMaterialFilters([NotNull] GPUDrivenMaterialFilterEntry[] filters);
        public extern void ClearMaterialFilters();

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(UnityEngine.Rendering.GPUDrivenProcessor obj) => obj.m_Ptr;
        }
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct GPUDrivenRendererDataNative
    {
        public int* rendererID;
        public Bounds* localBounds;
        public Matrix4x4* localToWorldMatrix;
        public Matrix4x4* prevLocalToWorldMatrix;
        public Vector4* lightmapScaleOffset;
        public int* meshIndex;
        public short* subMeshStartIndex;
        public int* materialsOffset;
        public short* materialsCount;
        public int* gameObjectLayer;
        public uint* renderingLayerMask;
        public int* lodGroupID;
        public MotionVectorGenerationMode* motionVecGenMode;
        public GPUDrivenPackedRendererData* packedRendererData;
        public int* rendererPriority;
        public int rendererCount;

        public int* meshID;
        public short* subMeshCount;
        public int* subMeshDescOffset;
        public int meshCount;

        public SubMeshDescriptor* subMeshDesc;
        public int subMeshDescCount;

        public int* materialIndex;
        public int materialIndexCount;

        public int* materialID;
        public bool* isTransparent;
        public bool* isMotionVectorsPassEnabled;
        public int* materialFilterFlags;
        public int materialCount;
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct GPUDrivenLODGroupDataNative
    {
        public int* lodGroupID;
        public int* lodOffset;
        public int* lodCount;
        public LODFadeMode* fadeMode;
        public Vector3* worldSpaceReferencePoint;
        public float* worldSpaceSize;
        public short* renderersCount;
        public int lodGroupCount;

        public int* invalidLODGroupID;
        public int invalidLODGroupCount;

        public short* lodRenderersCount;
        public float* lodScreenRelativeTransitionHeight;
        public float* lodFadeTransitionWidth;
        public int lodDataCount;
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct GPUDrivenPackedRendererData
    {
        uint data;

        public bool receiveShadows => (data & 1) != 0;
        public bool staticShadowCaster => (data & 1 << 1) != 0;
        public byte lodMask => (byte)(data >> 2 & 0xFF);
        public ShadowCastingMode shadowCastingMode => (ShadowCastingMode)(data >> 10 & 0x3);
        public LightProbeUsage lightProbeUsage => (LightProbeUsage)(data >> 12 & 0x7);
        public MotionVectorGenerationMode motionVecGenMode => (MotionVectorGenerationMode)(data >> 15 & 0x3);
        public bool isPartOfStaticBatch => (data & 1 << 17) != 0;
        public bool movedCurrentFrame => (data & 1 << 18) != 0;
    }

    internal enum GPUDrivenBitOpType
    {
        And,
        Or
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct GPUDrivenMaterialFilterEntry
    {
        public GPUDrivenBitOpType op;
        public int minQueueValue;
        public int maxQueueValue;
        public ShaderTagId keyTag;
        public ShaderTagId valueTag;
        public int flags;
        public string keyword;
    }

    internal struct GPUDrivenRendererData
    {
        // MeshRenderer data.
        public NativeArray<int> rendererID;
        public NativeArray<Bounds> localBounds;
        public NativeArray<Matrix4x4> localToWorldMatrix;
        public NativeArray<Matrix4x4> prevLocalToWorldMatrix;
        public NativeArray<Vector4> lightmapScaleOffset;
        public NativeArray<int> meshIndex;
        public NativeArray<short> subMeshStartIndex;
        public NativeArray<int> materialsOffset;
        public NativeArray<short> materialsCount;
        public NativeArray<int> gameObjectLayer;
        public NativeArray<uint> renderingLayerMask;
        public NativeArray<int> lodGroupID;
        public NativeArray<int> lightmapIndex;
        public NativeArray<GPUDrivenPackedRendererData> packedRendererData;
        public NativeArray<int> rendererPriority;

        // Mesh data.
        // Indexed by meshIndex.
        public NativeArray<int> meshID;
        public NativeArray<short> subMeshCount;
        public NativeArray<int> subMeshDescOffset;

        // SubMesh Descriptor data.
        // Indexed by subMeshCount and subMeshDescOffset.
        public NativeArray<SubMeshDescriptor> subMeshDesc;

        // Material data indices.
        // Indexed by materialsOffset and materialsCount.
        public NativeArray<int> materialIndex;

        // Material data.
        // Indexed by materialIndex.
        public NativeArray<int> materialID;
        public NativeArray<bool> isTransparent;
        public NativeArray<bool> isMotionVectorsPassEnabled;
        public NativeArray<int> materialFilterFlags;
    }

    internal struct GPUDrivenLODGroupData
    {
        // LODGroup data.
        public NativeArray<int> lodGroupID;
        public NativeArray<int> lodOffset;
        public NativeArray<int> lodCount;
        public NativeArray<LODFadeMode> fadeMode;
        public NativeArray<Vector3> worldSpaceReferencePoint;
        public NativeArray<float> worldSpaceSize;
        public NativeArray<short> renderersCount;

        // Invalid or disabled LODGroup IDs.
        public NativeArray<int> invalidLODGroupID;

        // LOD data.
        // Indexed by lodOffset and lodCount.
        public NativeArray<short> lodRenderersCount;
        public NativeArray<float> lodScreenRelativeTransitionHeight;
        public NativeArray<float> lodFadeTransitionWidth;
    }
}
