// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Serialization;

namespace UnityEditorInternal
{
    [Obsolete("AudioProfilerGroupInfo type is deprecated.")]
    [System.Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct AudioProfilerGroupInfo
    {
        [FormerlySerializedAs("assetInstanceId")]
        public EntityId assetEntityId;
        [Obsolete("assetInstanceId is deprecated. Use assetEntityId instead.", true)]
        public int assetInstanceId { get => assetEntityId; set => assetEntityId = value; }

        [FormerlySerializedAs("objectInstanceId")]
        public EntityId objectEntityId;
        [Obsolete("objectInstanceId is deprecated. Use objectEntityId instead.", true)]
        public int objectInstanceId { get => objectEntityId; set => objectEntityId = value; }

        public int assetNameOffset;
        public int objectNameOffset;
        public int parentId;
        public int uniqueId;
        public int flags;
        public int playCount;
        public float distanceToListener;
        public float volume;
        public float audibility;
        public float minDist;
        public float maxDist;
        public float time;
        public float maxRMSLevelOrDuration;
        public float frequency;
    }

    [Obsolete("AudioProfilerDSPInfo type is deprecated.")]
    [System.Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct AudioProfilerDSPInfo
    {
        public int id;
        public int target;
        public int targetPort;
        public int numChannels;
        public int nameOffset;
        public float weight;
        public float cpuLoad;
        public float level1;
        public float level2;
        public int numLevels;
        public int flags;
        public int audibilityVisitOrder;
        public float relativeAudibility;
        public float absoluteAudibility;
    }

    [Obsolete("AudioProfilerClipInfo type is deprecated.")]
    [System.Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct AudioProfilerClipInfo
    {
        [FormerlySerializedAs("assetInstanceId")]
        public EntityId assetEntityId;
        [Obsolete("assetInstanceId is deprecated. Use assetEntityId instead.")]
        public int assetInstanceId { get => assetEntityId; set => assetEntityId = value; }

        public int assetNameOffset;
        public int loadState;
        public int internalLoadState;
        public int age;
        public int disposed;
        public int numChannelInstances;
        public int numClones;
        public int refCount;
        public UInt64 instancePtr;
    }

    [Flags]
    public enum BatchBreakingReason
    {
        NoBreaking,
        NotCoplanarWithCanvas = 1,
        CanvasInjectionIndex = 2,
        DifferentMaterialInstance = 4,
        DifferentRectClipping = 8,
        DifferentTexture = 16,
        DifferentA8TextureUsage = 32,
        DifferentClipRect = 64,
        Unknown = 128,
    }

    [Obsolete("UISystemProfilerInfo type is deprecated.")]
    [System.Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct UISystemProfilerInfo
    {
        [Obsolete("objectInstanceId is deprecated. Use objectEntityId instead.")]
        public int objectInstanceId {get => objectEntityId; set => objectEntityId = value; }
        [FormerlySerializedAs("objectInstanceId")]
        public EntityId objectEntityId;
        public int objectNameOffset;
        [Obsolete("objectInstanceId is deprecated. Use objectEntityId instead.")]
        public int parentId {get => parentEntityId; set => parentEntityId = value; }
        [FormerlySerializedAs("parentId")]
        public EntityId parentEntityId;
        public int batchCount;
        public int totalBatchCount;
        public int vertexCount;
        public int totalVertexCount;
        public bool isBatch;
        public BatchBreakingReason batchBreakingReason;
        [Obsolete("instanceIDsIndex is deprecated. Use entityIdsIndex instead.")]
        public int instanceIDsIndex {get => entityIdsIndex; set => entityIdsIndex = value; }
        [FormerlySerializedAs("instanceIDsIndex")]
        public int entityIdsIndex;

        [Obsolete("instanceIDsCount is deprecated. Use entityIdsCount instead.")]
        public int instanceIDsCount {get => entityIdsCount; set => entityIdsCount = value; }
        [FormerlySerializedAs("instanceIDsCount")]

        public int entityIdsCount;
        public int renderDataIndex;
        public int renderDataCount;
    }

    [Obsolete("ProfilerProperty is deprecated. Use UnityEditor.Profiling.RawFrameDataView or UnityEditor.Profiling.HierarchyFrameDataView instead.")]
    [NativeHeader("Modules/ProfilerEditor/ProfilerHistory/ProfilerProperty.h")]
    [NativeHeader("Runtime/Interfaces/IUISystem.h")]
    [StructLayout(LayoutKind.Sequential)]
    public class ProfilerProperty : IDisposable
    {
        private IntPtr m_Ptr;

        public ProfilerProperty()
        {
            m_Ptr = Internal_Create();
        }

        [NativeMethod("CleanupProperty")]
        public extern void Cleanup();

        [NativeMethod("GetNext")]
        public extern bool Next(bool enterChildren);

        public extern void SetRoot(int frame, int profilerSortColumn, int viewType);

        public extern void ResetToRoot();

        public extern void InitializeDetailProperty(ProfilerProperty source);


        [NativeMethod("FunctionName")]
        public extern string propertyName { get; }

        public extern bool HasChildren
        {
            [NativeMethod("HasChildren")]
            get;
        }

        public extern bool onlyShowGPUSamples { get; set; }

        [Obsolete("instanceIDs is deprecated. Use entityIds instead.", true)]
        public int[] instanceIDs => throw new InvalidOperationException("instanceIDs obsolete, use entityIds instead.");
        public extern EntityId[] entityIds { get; }

        public extern string GetTooltip(int column);

        public extern int depth { get; }

        public extern string propertyPath
        {
            [NativeMethod("GetFunctionPath")]
            get;
        }

        [NativeMethod("GetProfilerColumn")]
        public extern string GetColumn(int column);

        [NativeMethod("GetProfilerColumnAsSingle")]
        public extern float GetColumnAsSingle(int colum);

        public extern string frameFPS { get; }

        public string frameTime { get; }

        public extern string frameGpuTime { get; }

        public extern bool frameDataReady { get; }

        public extern AudioProfilerGroupInfo[] GetAudioProfilerGroupInfo();


        public extern AudioProfilerDSPInfo[] GetAudioProfilerDSPInfo();

        public extern AudioProfilerClipInfo[] GetAudioProfilerClipInfo();

        public extern string GetAudioProfilerNameByOffset(int offset);

        public extern UISystemProfilerInfo[] GetUISystemProfilerInfo();

        public extern string GetUISystemProfilerNameByOffset(int offset);

        public extern EventMarker[] GetUISystemEventMarkers();

        public extern string GetUISystemEventMarkerNameByOffset(int offset);

        public extern EntityId[] GetUISystemBatchEntityIds();
        [Obsolete("GetUISystemBatchInstanceIDs is deprecated. Use GetUISystemBatchEntityIds instead.", true)]
        public int[] GetUISystemBatchInstanceIDs() => throw new NotImplementedException("Use GetUISystemBatchEntityIds instead.");

        [StaticAccessor("GetIUISystem()", StaticAccessorType.Arrow)]
        [NativeMethod("ReleaseTexture")]
        public extern static void ReleaseUISystemProfilerRender(Texture2D t);

        public static Texture2D UISystemProfilerRender(int frameIndex, int renderDataIndex, int renderDataCount, bool renderOverdraw)
        {
            return UISystemProfilerRender_Internal(IntPtr.Zero, frameIndex, renderDataIndex, renderDataCount, renderOverdraw);
        }

        [StaticAccessor("GetIUISystem()", StaticAccessorType.Arrow)]
        [NativeMethod("ProfilerRenderBatch")]
        private static extern Texture2D UISystemProfilerRender_Internal(IntPtr ptr, int frameIndex, int renderDataIndex, int renderDataCount, bool renderOverdraw);

        private static extern IntPtr Internal_Create();

        [NativeMethod(IsThreadSafe = true)]
        private static extern void Internal_Delete(IntPtr iPtr);

        public void Dispose()
        {
            FreeNativeResources();
            GC.SuppressFinalize(this);
        }

        private void FreeNativeResources()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Delete(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        ~ProfilerProperty()
        {
            FreeNativeResources();
        }

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(ProfilerProperty prop) => prop.m_Ptr;
        }
    }
}
