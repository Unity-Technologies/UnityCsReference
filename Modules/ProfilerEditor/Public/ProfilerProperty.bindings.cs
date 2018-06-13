// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Mono.Cecil.Cil;
using UnityEditorInternal;
using UnityEngine.Bindings;
using UnityEditorInternal.Profiling;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditorInternal
{
    [System.Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct AudioProfilerGroupInfo
    {
        public int assetInstanceId;
        public int objectInstanceId;
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
        public float duration;
        public float frequency;
    }

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
    }

    [System.Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct AudioProfilerClipInfo
    {
        public int assetInstanceId;
        public int assetNameOffset;
        public int loadState;
        public int internalLoadState;
        public int age;
        public int disposed;
        public int numChannelInstances;
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

    [System.Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct UISystemProfilerInfo
    {
        public int objectInstanceId;
        public int objectNameOffset;
        public int parentId;
        public int batchCount;
        public int totalBatchCount;
        public int vertexCount;
        public int totalVertexCount;
        public bool isBatch;
        public BatchBreakingReason batchBreakingReason;
        public int instanceIDsIndex;
        public int instanceIDsCount;
        public int renderDataIndex;
        public int renderDataCount;
    }


    [NativeHeader("Modules/ProfilerEditor/ProfilerHistory/ProfilerProperty.h")]
    [NativeHeader("Runtime/Interfaces/IUISystem.h")]
    [StructLayout(LayoutKind.Sequential)]
    public partial class ProfilerProperty : IDisposable
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

        public extern void SetRoot(int frame, ProfilerColumn profilerSortColumn, ProfilerViewType viewType);

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

        public extern int[] instanceIDs { get; }

        public extern string GetTooltip(ProfilerColumn column);

        public extern int depth { get; }

        public extern string propertyPath
        {
            [NativeMethod("GetFunctionPath")]
            get;
        }

        [NativeMethod("GetProfilerColumn")]
        public extern string GetColumn(ProfilerColumn column);

        [NativeMethod("GetProfilerColumnAsSingle")]
        public extern float GetColumnAsSingle(ProfilerColumn colum);

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

        public extern int[] GetUISystemBatchInstanceIDs();

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

        [ThreadSafe]
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
    }
}
