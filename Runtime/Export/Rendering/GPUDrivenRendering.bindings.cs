// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine.Assertions;

[assembly: InternalsVisibleTo("Unity.RenderPipelines.GPUDriven.Runtime")]
[assembly: InternalsVisibleTo("Unity.RenderPipelines.Core.Editor.Tests")]

namespace UnityEngine.Rendering
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct InternalMeshRendererSettings
    {
        const int kShadowCastingBitSize = 2;
        const int kLightProbeBitSize = 3;
        const int kMotionVecGenModeBitSize = 2;

        public static readonly InternalMeshRendererSettings Default = new InternalMeshRendererSettings(
            renderingLayerMask: 0b1,
            objectLayer: 0,
            receiveShadows: true,
            staticShadowCaster: false,
            ShadowCastingMode.On,
            LightProbeUsage.Off,
            MotionVectorGenerationMode.Object,
            smallMeshCulling: true,
            isPartOfStaticBatch: false);

        uint m_RenderingLayerMask;
        ushort m_Data;
        byte m_ObjectLayer;

        public InternalMeshRendererSettings(uint renderingLayerMask,
            byte objectLayer,
            bool receiveShadows,
            bool staticShadowCaster,
            ShadowCastingMode shadowCastingMode,
            LightProbeUsage lightProbeUsage,
            MotionVectorGenerationMode motionMode,
            bool smallMeshCulling,
            bool isPartOfStaticBatch)
        {
            Assert.IsTrue(((int)shadowCastingMode) < 1 << kShadowCastingBitSize);
            Assert.IsTrue(((int)lightProbeUsage) < 1 << kLightProbeBitSize);
            Assert.IsTrue(((int)motionMode) < 1 << kMotionVecGenModeBitSize);

            int data = 0;
            data |= receiveShadows ? 1 : 0;
            data |= staticShadowCaster ? 1 << 1 : 0;
            data |= (int)shadowCastingMode << 2;
            data |= (int)lightProbeUsage << 4;
            data |= (int)motionMode << 7;
            data |= smallMeshCulling ? 1 << 9 : 0;
            data |= isPartOfStaticBatch ? 1 << 10 : 0;
            // Only set from C++. Keep this bit reserved
            //data |= hasTree ? 1 << 11 : 0;

            m_Data = (ushort)data;
            m_RenderingLayerMask = renderingLayerMask;
            m_ObjectLayer = objectLayer;
        }

        public uint RenderingLayerMask
        {
            get => m_RenderingLayerMask;
            set => m_RenderingLayerMask = value;
        }

        public byte ObjectLayer
        {
            get => m_ObjectLayer;
            set => m_ObjectLayer = value;
        }

        public bool ReceiveShadows
        {
            get => (m_Data & 1) != 0;
            set => m_Data = (ushort)((m_Data & ~(1 << 0)) | (value ? 1 << 0 : 0));
        }

        public bool StaticShadowCaster
        {
            get => (m_Data & 1 << 1) != 0;
            set => m_Data = (ushort)((m_Data & ~(1 << 1)) | (value ? 1 << 1 : 0));
        }

        public ShadowCastingMode ShadowCastingMode
        {
            get => (ShadowCastingMode)(m_Data >> 2 & 0x3);
            set
            {
                Assert.IsTrue(((int)value) < 1 << kShadowCastingBitSize);
                m_Data = (ushort)((m_Data & ~(0x3 << 2)) | ((int)value << 2));
            }
        }

        public LightProbeUsage LightProbeUsage
        {
            get => (LightProbeUsage)(m_Data >> 4 & 0x7);
            set
            {
                Assert.IsTrue(((int)value) < 1 << kLightProbeBitSize);
                m_Data = (ushort)((m_Data & ~(0x7 << 4)) | ((int)value << 4));
            }
        }

        public MotionVectorGenerationMode MotionVectorGenerationMode
        {
            get => (MotionVectorGenerationMode)(m_Data >> 7 & 0x3);
            set
            {
                Assert.IsTrue(((int)value) < 1 << kMotionVecGenModeBitSize);
                m_Data = (ushort)((m_Data & ~(0x3 << 7)) | ((int)value << 7));
            }
        }

        public bool SmallMeshCulling
        {
            get => (m_Data & 1 << 9) != 0;
            set => m_Data = (ushort)((m_Data & ~(1 << 9)) | (value ? 1 << 9 : 0));
        }

        public bool IsPartOfStaticBatch
        {
            get => (m_Data & 1 << 10) != 0;
            set => m_Data = (ushort)((m_Data & ~(1 << 10)) | (value ? 1 << 10 : 0));
        }

        // Currently only set from C++ and used by GPUResidentDrawer internally.
        internal bool HasTree
        {
            get => (m_Data & 1 << 11) != 0;
            set => m_Data = (ushort)((m_Data & ~(1 << 11)) | (value ? 1 << 11 : 0));
        }
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct InternalMeshLodRendererSettings
    {
        public static readonly InternalMeshLodRendererSettings Default = new InternalMeshLodRendererSettings
        {
            forceLod = -1,
            lodSelectionBias = 0f,
        };

        public int forceLod;
        public float lodSelectionBias;
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct InternalLODGroupSettings
    {
        const int kLODGroupFadeModeBitSize = 2;

        // UInt32 fadeMode : 2;
        // UInt32 lastLODIsBillboard : 1;
        byte m_Data;

        public InternalLODGroupSettings(LODFadeMode fadeMode, bool lastLODIsBillboard)
        {
            Assert.IsTrue(((int)fadeMode) < 1 << kLODGroupFadeModeBitSize);

            int data = 0;
            data |= (int)fadeMode;
            data |= lastLODIsBillboard ? 0b100 : 0;

            m_Data = (byte)data;
        }

        public LODFadeMode fadeMode
        {
            get => (LODFadeMode)(m_Data & 0b11);
            set
            {
                Assert.IsTrue(((int)value) < 1 << kLODGroupFadeModeBitSize);
                m_Data = (byte)((m_Data & ~0b11) | ((int)value & 0b11));
            }
        }

        public bool lastLODIsBillboard 
        {
            get => (m_Data & 0b100) != 0;
            set => m_Data = (byte)((m_Data & ~0b100) | (value ? 0b100 : 0));
        } 
    }

    // Packed inline LOD buffer able to contain up to 8 LODs.
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct EmbeddedLODBuffer
    {
        public const int kMaxLength = 8;

        // ScreenRelativeTransitionHeights and FadeTransitionWidths are normally floats between 0 and 1.
        // We can use this information to optimize the storage a bit here. Basically using fixed-point format.
        // Instead of storing full floats, we store uint16 that we map to [0,1] floats on demand (set/get).
        // This allows for a precision of 1 / 2^16 which is about 1e-5 and should be enough for our needs.
        fixed ushort m_ScreenRelativeTransitionHeights[kMaxLength];
        fixed ushort m_FadeTransitionWidths[kMaxLength];
        fixed byte m_RendererCounts[kMaxLength];
        byte m_Length;

        public int Length => m_Length;

        public EmbeddedLODBuffer(int length)
        {
            Assert.IsTrue(length <= kMaxLength, "EmbeddedLODBuffer does not support more than 8 LODs");

            this = default;
            m_Length = (byte)length;
        }

        public float GetScreenRelativeTransitionHeight(int index)
        {
            Assert.IsTrue(index < Length);
            ushort rawValue = m_ScreenRelativeTransitionHeights[index];
            return MapUShortToFloat01(rawValue);
        }

        public void SetScreenRelativeTransitionHeight(int index, float value)
        {
            Assert.IsTrue(index < Length);
            m_ScreenRelativeTransitionHeights[index] = MapFloat01ToUshort(value);
        }

        public float GetFadeTransitionWidth(int index)
        {
            Assert.IsTrue(index < Length);
            ushort rawValue = m_FadeTransitionWidths[index];
            return MapUShortToFloat01(rawValue);
        }

        public void SetFadeTransitionWidth(int index, float value)
        {
            Assert.IsTrue(index < Length);
            m_FadeTransitionWidths[index] = MapFloat01ToUshort(value);
        }

        public int GetRendererCount(int index)
        {
            Assert.IsTrue(index < Length);
            return m_RendererCounts[index];
        }

        public void SetRendererCount(int index, int value)
        {
            Assert.IsTrue(value <= byte.MaxValue);
            m_RendererCounts[index] = (byte)value;
        }

        static ushort MapFloat01ToUshort(float x)
        {
            int xI32 = (int)(x * ushort.MaxValue);
            if (xI32 < 0) xI32 = 0;
            else if (xI32 > ushort.MaxValue) xI32 = ushort.MaxValue;

            return (ushort)xI32;
        }

        static float MapUShortToFloat01(ushort x)
        {
            const float RcpMaxValue = 1f / ushort.MaxValue;

            float valueFloat = x * RcpMaxValue;
            if (valueFloat < 0f) valueFloat = 0f;
            else if (valueFloat > 1f) valueFloat = 1f;

            return valueFloat;
        }
    }

    internal delegate void GPUDrivenLODGroupDataCallback(in GPUDrivenLODGroupData lodGroupData);
    internal delegate void GPUDrivenRendererDataCallback(in GPUDrivenMeshRendererData rendererData);
    internal delegate void GPUDrivenFetchMeshesDataCallback(NativeArray<EntityId> meshIDs, NativeArray<GPUDrivenMeshData> meshDatas, NativeArray<int> subMeshOffsets, NativeArray<GPUDrivenSubMesh> subMeshDatas);

    [RequiredByNativeCode]
    internal static class GPUDrivenCallbacks
    {
        [RequiredByNativeCode(GenerateProxy = true)]
        public unsafe static void InvokeGPUDrivenLODGroupDataNativeCallback(IntPtr nativeDataPtr, GPUDrivenLODGroupDataCallback callback)
        {
            GPUDrivenLODGroupDataNative* nativeData = (GPUDrivenLODGroupDataNative*)nativeDataPtr;
            var lodGroup = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(nativeData->lodGroup, nativeData->lodGroupCount, Allocator.Invalid);
            var worldSpaceReferencePoint = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector3>(nativeData->worldSpaceReferencePoint, nativeData->lodGroupCount, Allocator.Invalid);
            var worldSpaceSize = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<float>(nativeData->worldSpaceSize, nativeData->lodGroupCount, Allocator.Invalid);
            var forceLODMask = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(nativeData->forceLODMask, nativeData->lodGroupCount, Allocator.Invalid);
            var invalidLODGroup = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(nativeData->invalidLODGroups, nativeData->invalidLODGroupCount, Allocator.Invalid);

            NativeArray<InternalLODGroupSettings> groupSettings = default;
            NativeArray<EmbeddedLODBuffer> lodBuffer = default;
            if (!nativeData->transformOnly)
            {
                groupSettings = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<InternalLODGroupSettings>(nativeData->groupSettings, nativeData->lodGroupCount, Allocator.Invalid);
                lodBuffer = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EmbeddedLODBuffer>(nativeData->lodBuffer, nativeData->lodGroupCount, Allocator.Invalid);
            }

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref lodGroup, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref worldSpaceReferencePoint, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref worldSpaceSize, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref groupSettings, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref forceLODMask, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref lodBuffer, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref invalidLODGroup, AtomicSafetyHandle.Create());
            GPUDrivenLODGroupData data = new GPUDrivenLODGroupData
            {
                lodGroup = lodGroup,
                worldSpaceReferencePoint = worldSpaceReferencePoint,
                worldSpaceSize = worldSpaceSize,
                groupSettings = groupSettings,
                forceLODMask = forceLODMask,
                lodBuffer = lodBuffer,
                invalidLODGroup = invalidLODGroup,
                transformOnly = nativeData->transformOnly,
            };

            try
            {
                callback(data);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(lodGroup));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(worldSpaceReferencePoint));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(worldSpaceSize));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(groupSettings));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(forceLODMask));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(lodBuffer));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(invalidLODGroup));
        }

        [RequiredByNativeCode(GenerateProxy = true)]
        public unsafe static void InvokeGPUDrivenRendererDataNativeCallback(IntPtr nativeDataPtr, GPUDrivenRendererDataCallback callback)
        {
            GPUDrivenMeshRendererDataNative* nativeData = (GPUDrivenMeshRendererDataNative*)nativeDataPtr;

            var renderer = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(nativeData->renderer, nativeData->rendererCount, Allocator.Invalid);
            var localBounds = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Bounds>(nativeData->localBounds, nativeData->rendererCount, Allocator.Invalid);
            var lightmapScaleOffset = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector4>(nativeData->lightmapScaleOffset, nativeData->rendererCount, Allocator.Invalid);
            var lodGroup = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(nativeData->lodGroup, nativeData->rendererCount, Allocator.Invalid);
            var lodMask = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(nativeData->lodMask, nativeData->rendererCount, Allocator.Invalid);
            var lightmapIndex = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<short>(nativeData->lightmapIndex, nativeData->rendererCount, Allocator.Invalid);
            var rendererSettings = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<InternalMeshRendererSettings>(nativeData->rendererSettings, nativeData->rendererCount, Allocator.Invalid);
            var rendererUserValues = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<uint>(nativeData->rendererUserValues, nativeData->rendererCount, Allocator.Invalid);
            var rendererPriority = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(nativeData->rendererPriority, nativeData->rendererCount, Allocator.Invalid);
            var localToWorldMatrix = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Matrix4x4>(nativeData->localToWorldMatrix, nativeData->rendererCount, Allocator.Invalid);
            var prevLocalToWorldMatrix = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Matrix4x4>(nativeData->prevLocalToWorldMatrix, nativeData->rendererCount, Allocator.Invalid);
            var mesh = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(nativeData->mesh, nativeData->rendererCount, Allocator.Invalid);
            var meshLodSettings = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<InternalMeshLodRendererSettings>(nativeData->meshLodSettings, nativeData->rendererCount, Allocator.Invalid);
            var subMeshStartIndex = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ushort>(nativeData->subMeshStartIndex, nativeData->rendererCount, Allocator.Invalid);
            var subMaterialRange = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<RangeInt>(nativeData->subMaterialRange, nativeData->rendererCount, Allocator.Invalid);
            var sceneCullingMask = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<ulong>(nativeData->sceneCullingMask, nativeData->rendererCount, Allocator.Invalid);
            var material = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(nativeData->material, nativeData->materialCount, Allocator.Invalid);
            var invalidRenderer = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(nativeData->invalidRenderer, nativeData->invalidRendererCount, Allocator.Invalid);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref renderer, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref localBounds, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref lightmapScaleOffset, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref lodGroup, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref lodMask, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref lightmapIndex, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref rendererSettings, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref rendererUserValues, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref rendererPriority, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref localToWorldMatrix, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref prevLocalToWorldMatrix, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref mesh, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref meshLodSettings, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref subMeshStartIndex, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref subMaterialRange, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref sceneCullingMask, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref material, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref invalidRenderer, AtomicSafetyHandle.Create());
            GPUDrivenMeshRendererData data = new GPUDrivenMeshRendererData
            {
                renderer = renderer,
                localBounds = localBounds,
                lightmapScaleOffset = lightmapScaleOffset,
                lodGroup = lodGroup,
                lodMask = lodMask,
                lightmapIndex = lightmapIndex,
                rendererSettings = rendererSettings,
                rendererUserValues = rendererUserValues,
                rendererPriority = rendererPriority,
                localToWorldMatrix = localToWorldMatrix,
                prevLocalToWorldMatrix = prevLocalToWorldMatrix,
                mesh = mesh,
                meshLodSettings = meshLodSettings,
                subMeshStartIndex = subMeshStartIndex,
                subMaterialRange = subMaterialRange,
                sceneCullingMask = sceneCullingMask,
                material = material,
                invalidRenderer = invalidRenderer,
            };

            try
            {
                callback(data);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(renderer));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(localBounds));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(lightmapScaleOffset));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(lodGroup));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(lodMask));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(lightmapIndex));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(rendererSettings));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(rendererUserValues));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(rendererPriority));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(localToWorldMatrix));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(prevLocalToWorldMatrix));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(mesh));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(meshLodSettings));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(subMeshStartIndex));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(subMaterialRange));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(sceneCullingMask));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(material));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(invalidRenderer));
        }

        [RequiredByNativeCode(GenerateProxy = true)]
        public unsafe static void InvokeOnFetchMeshesData(IntPtr nativeDataPtr, GPUDrivenFetchMeshesDataCallback callback)
        {
            GPUDrivenFetchMeshesDataNative* nativeData = (GPUDrivenFetchMeshesDataNative*)nativeDataPtr;

            var meshes = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<EntityId>(nativeData->meshPtr, nativeData->meshCount, Allocator.Invalid);
            var meshDatas = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<GPUDrivenMeshData>(nativeData->meshDataPtr, nativeData->meshCount, Allocator.Invalid);
            var subMeshOffsets = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(nativeData->subMeshBufferOffsetPtr, nativeData->meshCount, Allocator.Invalid);
            var subMeshDatas = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<GPUDrivenSubMesh>(nativeData->subMeshDataPtr, nativeData->subMeshDataCount, Allocator.Invalid);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref meshes, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref meshDatas, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref subMeshOffsets, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref subMeshDatas, AtomicSafetyHandle.Create());

            try
            {
                callback(meshes, meshDatas, subMeshOffsets, subMeshDatas);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(meshes));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(meshDatas));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(subMeshOffsets));
            AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(subMeshDatas));
        }
    }

    [RequiredByNativeCode]
    [NativeHeader("Runtime/Camera/GPUDrivenProcessor.h")]
    internal class GPUDrivenProcessor
    {
        internal IntPtr m_Ptr;

        public extern bool enablePartialRendering { set; get; }

        public GPUDrivenProcessor()
        {
            m_Ptr = Internal_Create();
        }

        ~GPUDrivenProcessor()
        {
            Destroy();
        }

        public void Dispose()
        {
            Destroy();
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

        // We can't pass collections ownership from C++ to C#, so use a callback to workaround this. 
        public extern void EnableGPUDrivenRenderingAndDispatchRendererData(ReadOnlySpan<EntityId> renderers, GPUDrivenRendererDataCallback callback);

        public extern void DisableGPUDrivenRendering(ReadOnlySpan<EntityId> renderers);

        // We can't pass collections ownership from C++ to C#, so use a callback to workaround this. 
        public extern void DispatchLODGroupData(ReadOnlySpan<EntityId> lodGroups, bool transformOnly, GPUDrivenLODGroupDataCallback callback);

        public static void RegisterMaterials(BatchRendererGroup brg, NativeArray<EntityId> materials, NativeArray<GPUDrivenMaterial> materialDatas)
        {
            RegisterMaterials(brg.Handle, materials, materialDatas);
        }

        [FreeFunction("GPUDrivenProcessor::RegisterMaterials", IsThreadSafe = true)]
        static extern unsafe void RegisterMaterials(IntPtr brg, ReadOnlySpan<EntityId> materials, Span<GPUDrivenMaterial> materialDatas);

        public static void RegisterMeshes(BatchRendererGroup brg, NativeArray<EntityId> meshInstanceIDs, NativeArray<BatchMeshID> batchMeshIDs)
        {
            RegisterMeshes(brg.Handle, meshInstanceIDs, batchMeshIDs);
        }

        [FreeFunction("GPUDrivenProcessor::RegisterMeshes", IsThreadSafe = true)]
        static extern unsafe void RegisterMeshes(IntPtr brg, ReadOnlySpan<EntityId> meshInstanceIDs, Span<BatchMeshID> batchMeshIDs);

        // We can't pass collections ownership from C++ to C#, so use a callback to workaround this. 
        [FreeFunction("GPUDrivenProcessor::FetchMeshDatas")]
        public static extern void FetchMeshDatas(ReadOnlySpan<EntityId> meshIDs, GPUDrivenFetchMeshesDataCallback callback);

        [FreeFunction("GPUDrivenProcessor::ClassifyMaterials")]
        public static extern int ClassifyMaterials(ReadOnlySpan<EntityId> materialIDs, 
            Span<EntityId> unsupportedMaterialIDs, 
            Span<EntityId> supportedMaterialIDs, 
            Span<GPUDrivenMaterialData> supportedPackedMaterialDatas);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(UnityEngine.Rendering.GPUDrivenProcessor obj) => obj.m_Ptr;
        }
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct GPUDrivenMeshRendererDataNative
    {
        public EntityId* renderer;
        public Bounds* localBounds;
        public Vector4* lightmapScaleOffset;
        public EntityId* lodGroup;
        public byte* lodMask;
        public short* lightmapIndex;
        public InternalMeshRendererSettings* rendererSettings;
        public int* rendererPriority;
        public Matrix4x4* localToWorldMatrix;
        public Matrix4x4* prevLocalToWorldMatrix;
        public EntityId* mesh;
        public InternalMeshLodRendererSettings* meshLodSettings;
        public ushort* subMeshStartIndex;
        public RangeInt* subMaterialRange;
        public uint* rendererUserValues;
        public ulong* sceneCullingMask;
        public int rendererCount;

        public int materialCount;
        public EntityId* material;

        public EntityId* invalidRenderer;
        public int invalidRendererCount;
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct GPUDrivenLODGroupDataNative
    {
        public EntityId* lodGroup;
        public Vector3* worldSpaceReferencePoint;
        public float* worldSpaceSize;
        public InternalLODGroupSettings* groupSettings;
        public byte* forceLODMask;
        public EmbeddedLODBuffer* lodBuffer;
        public int lodGroupCount;

        public EntityId* invalidLODGroups;
        public int invalidLODGroupCount;

        public bool transformOnly;
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct GPUDrivenFetchMeshesDataNative
    {
        public EntityId* meshPtr;
        public GPUDrivenMeshData* meshDataPtr;
        public int* subMeshBufferOffsetPtr;
        public GPUDrivenSubMesh* subMeshDataPtr;
        public int meshCount;
        public int subMeshDataCount;
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct GPUDrivenMaterialData
    {
        uint data;

        public bool isTransparent => (data & 1) != 0;
        public bool isMotionVectorsPassEnabled => (data & 1 << 1) != 0;
        public bool isIndirectSupported => (data & 1 << 2) != 0;
        public bool supportsCrossFade => (data & 1 << 3) != 0;

        public GPUDrivenMaterialData()
        {
            data = 0;
        }

        public GPUDrivenMaterialData(bool isTransparent, bool isMotionVectorsPassEnabled, bool isIndirectSupported, bool supportsCrossFade)
        {
            data = isTransparent ? 1u : 0u;
            data |= isMotionVectorsPassEnabled ? 1u << 1 : 0u;
            data |= isIndirectSupported ? 1u << 2 : 0u;
            data |= supportsCrossFade ? 1u << 3 : 0u;
        }

        public bool Equals(GPUDrivenMaterialData other)
        {
            return (other.data & 7) == (data & 7);
        }
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct GPUDrivenMaterial
    {
        public BatchMaterialID materialID;
        public GPUDrivenMaterialData data;

        public bool isTransparent => data.isTransparent;
        public bool isMotionVectorsPassEnabled => data.isMotionVectorsPassEnabled;
        public bool isIndirectSupported => data.isIndirectSupported;
        public bool supportsCrossFade => data.supportsCrossFade;

        public GPUDrivenMaterial(BatchMaterialID materialID, bool isTransparent, bool isMotionVectorsPassEnabled, bool isIndirectSupported, bool supportsCrossFade)
        {
            this.materialID = materialID;
            data = new(isTransparent, isMotionVectorsPassEnabled, isIndirectSupported, supportsCrossFade);
        }
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct GPUDrivenSubMesh : IEquatable<GPUDrivenSubMesh>
    {
        public MeshTopology topology;
        public uint baseVertex;
        public uint indexStart;
        public uint indexCount;

        public bool Equals(GPUDrivenSubMesh other)
        {
            return topology == other.topology
                && baseVertex == other.baseVertex
                && indexStart == other.indexStart
                && indexCount == other.indexCount;
        }
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct GPUDrivenMeshData
    {
        public int subMeshCount;
        public int meshLodCount;
        public Mesh.LodSelectionCurve meshLodSelectionCurve;

        public bool isLodSelectionActive => meshLodCount > 1;
    }

    internal struct GPUDrivenMeshRendererData
    {
        /// <summary>
        /// Renderer data.
        /// </summary>
        public NativeArray<EntityId> renderer;
        public NativeArray<Bounds> localBounds;
        public NativeArray<Vector4> lightmapScaleOffset;
        public NativeArray<EntityId> lodGroup;
        public NativeArray<byte> lodMask;
        public NativeArray<short> lightmapIndex;
        public NativeArray<InternalMeshRendererSettings> rendererSettings;
        public NativeArray<int> rendererPriority;
        public NativeArray<Matrix4x4> localToWorldMatrix;
        public NativeArray<Matrix4x4> prevLocalToWorldMatrix;
        public NativeArray<EntityId> mesh;
        public NativeArray<InternalMeshLodRendererSettings> meshLodSettings;
        public NativeArray<ushort> subMeshStartIndex;
        public NativeArray<RangeInt> subMaterialRange;
        public NativeArray<uint> rendererUserValues;
        public NativeArray<ulong> sceneCullingMask;

        /// <summary>
        /// Material data. Indexed by subMaterialRange.
        /// </summary>
        public NativeArray<EntityId> material;

        /// <summary>
        /// Invalid or disabled Renderers IDs.
        /// </summary>
        public NativeArray<EntityId> invalidRenderer;
    }

    internal struct GPUDrivenLODGroupData
    {
        /// <summary>
        /// LODGroup data.
        /// </summary>
        public NativeArray<EntityId> lodGroup;
        public NativeArray<Vector3> worldSpaceReferencePoint;
        public NativeArray<float> worldSpaceSize;
        public NativeArray<InternalLODGroupSettings> groupSettings;
        public NativeArray<byte> forceLODMask;
        public NativeArray<EmbeddedLODBuffer> lodBuffer;

        /// <summary>
        /// Invalid or disabled LODGroup IDs.
        /// </summary>
        public NativeArray<EntityId> invalidLODGroup;

        /// <summary>
        /// Did we only fetch transform related data?
        /// </summary>
        public bool transformOnly;
    }
}
