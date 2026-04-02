// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using Unity.Profiling.Editor;
using Unity.Profiling.LowLevel;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Profiling;
using UnityEngine.Scripting;

namespace UnityEditor.Profiling
{
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public struct ProfilerCategoryInfo
    {
        UInt16 m_Id;
        UnityEngine.Color32 m_Color;
        string m_Name;
        ProfilerCategoryFlags m_Flags;

        public UInt16 id
        {
            get => m_Id;
        }

        public UnityEngine.Color32 color
        {
            get => m_Color;
        }

        public string name
        {
            get => m_Name;
        }

        public ProfilerCategoryFlags flags
        {
            get => m_Flags;
        }
    };

    [NativeHeader("Modules/ProfilerEditor/ProfilerHistory/FrameDataView.h")]
    [StructLayout(LayoutKind.Sequential)]
    public abstract class FrameDataView : IDisposable
    {
        protected IntPtr m_Ptr;

        public const int invalidMarkerId = -1;

        public const int invalidThreadIndex = -1;
        public const ulong invalidThreadId = 0;

        internal const int invalidOrCurrentFrameIndex = -1;

        ~FrameDataView()
        {
            DisposeInternal();
        }

        public void Dispose()
        {
            DisposeInternal();
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        void DisposeInternal()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern void Internal_Destroy(IntPtr ptr);

        public bool valid
        {
            get
            {
                return m_Ptr != IntPtr.Zero;
            }
        }

        public extern int frameIndex
        {
            [NativeMethod(IsThreadSafe = true)]
            get;
        }


        public extern int threadIndex
        {
            [NativeMethod(IsThreadSafe = true)]
            get;
        }

        public extern string threadGroupName
        {
            [NativeMethod(IsThreadSafe = true)]
            get;
        }

        public extern string threadName
        {
            [NativeMethod(IsThreadSafe = true)]
            get;
        }

        public extern ulong threadId
        {
            [NativeMethod(IsThreadSafe = true)]
            get;
        }

        public extern double frameStartTimeMs
        {
            [NativeMethod(IsThreadSafe = true)]
            get;
        }

        public extern ulong frameStartTimeNs
        {
            [NativeMethod(IsThreadSafe = true)]
            get;
        }

        public extern float frameTimeMs
        {
            [NativeMethod(IsThreadSafe = true)]
            get;
        }

        public extern ulong frameTimeNs
        {
            [NativeMethod(IsThreadSafe = true)]
            get;
        }

        public extern float frameGpuTimeMs
        {
            [NativeMethod(IsThreadSafe = true)]
            get;
        }

        public extern ulong frameGpuTimeNs
        {
            [NativeMethod(IsThreadSafe = true)]
            get;
        }

        public extern float frameFps
        {
            [NativeMethod(IsThreadSafe = true)]
            get;
        }

        public extern int sampleCount
        {
            [NativeMethod(IsThreadSafe = true)]
            get;
        }

        public extern int maxDepth
        {
            [NativeMethod(IsThreadSafe = true)]
            get;
        }

        // the current runtime (Editor or Player) session id. This is different from the
        internal extern uint runtimeSessionId
        {
            [NativeMethod(IsThreadSafe = true)]
            get;
        }

        [StructLayout(LayoutKind.Sequential)]
        [RequiredByNativeCode]
        public struct MarkerMetadataInfo
        {
            public ProfilerMarkerDataType type;
            public ProfilerMarkerDataUnit unit;
            public string name;
        };

        [StructLayout(LayoutKind.Sequential)]
        [RequiredByNativeCode]
        public struct MarkerInfo
        {
            public int id;
            public ushort category;
            public MarkerFlags flags;
            public string name;
            public MarkerMetadataInfo[] metadataInfo;
        };

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern ushort GetMarkerCategoryIndex(int markerId);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern MarkerFlags GetMarkerFlags(int markerId);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern string GetMarkerName(int markerId);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern MarkerMetadataInfo[] GetMarkerMetadataInfo(int markerId);

        [NativeMethod(IsThreadSafe = true)]
        public extern int GetMarkerId(string markerName);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern void GetMarkers([Out,UnityEngine.Bindings.NotNull] List<MarkerInfo> markerInfoList);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern ProfilerCategoryInfo GetCategoryInfo(UInt16 id);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern void GetAllCategories([Out,UnityEngine.Bindings.NotNull] List<ProfilerCategoryInfo> categoryInfoList);

        [NativeMethod(Name = "profiling::FrameDataView::GetBuiltinMarkerCategoryColor", IsFreeFunction = true, IsThreadSafe = true, ThrowsException = true)]
        internal static extern UnityEngine.Color32 GetMarkerCategoryColor(ushort category);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Data
        {
            public IntPtr ptr;
            public int size;
        }

        [NativeMethod(IsThreadSafe = true)]
        extern AtomicSafetyHandle GetSafetyHandle();

        public NativeArray<T> GetFrameMetaData<T>(Guid id, int tag) where T : struct
        {
            return GetFrameMetaData<T>(id, tag, 0);
        }

        public unsafe NativeArray<T> GetFrameMetaData<T>(Guid id, int tag, int index) where T : struct
        {
            return GetFrameMetaData<T>(id.ToByteArray(), tag, index);
        }

        internal NativeArray<T> GetFrameMetaData<T>(byte[] statsId, int tag) where T : struct
        {
            return GetFrameMetaData<T>(statsId, tag, 0);
        }

        internal unsafe NativeArray<T> GetFrameMetaData<T>(byte[] statsId, int tag, int index) where T : struct
        {
            var stride = UnsafeUtility.SizeOf<T>();
            var data = GetFrameMetaData(statsId, tag, index);
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(data.ptr.ToPointer(), data.size / stride, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, GetSafetyHandle());
            return array;
        }

        public int GetFrameMetaDataCount(Guid id, int tag)
        {
            return GetFrameMetaDataCount(id.ToByteArray(), tag);
        }

        [NativeMethod(IsThreadSafe = true)]
        extern Data GetFrameMetaData(byte[] statsId, int tag, int index);

        [NativeMethod(IsThreadSafe = true)]
        extern int GetFrameMetaDataCount(byte[] statsId, int tag);

        public NativeArray<T> GetSessionMetaData<T>(Guid id, int tag) where T : struct
        {
            return GetSessionMetaData<T>(id, tag, 0);
        }

        public unsafe NativeArray<T> GetSessionMetaData<T>(Guid id, int tag, int index) where T : struct
        {
            var stride = UnsafeUtility.SizeOf<T>();
            var data = GetSessionMetaData(id.ToByteArray(), tag, index);
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(data.ptr.ToPointer(), data.size / stride, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, GetSafetyHandle());
            return array;
        }

        public int GetSessionMetaDataCount(Guid id, int tag)
        {
            return GetSessionMetaDataCount(id.ToByteArray(), tag);
        }

        [NativeMethod(IsThreadSafe = true)]
        extern Data GetSessionMetaData(byte[] statsId, int tag, int index);

        [NativeMethod(IsThreadSafe = true)]
        extern int GetSessionMetaDataCount(byte[] statsId, int tag);

        internal T GetProfilingSessionMetaData<T>(ProfilingSessionMetaDataEntry entry) where T : unmanaged
        {
            using (var ret = GetSessionMetaData<T>(ProfilerDriver.profilerInternalSessionMetaDataGuid, (int)entry))
            {
                Debug.Assert(ret.Length > 0, $"A ProfilingSessionMetaDataEntry {entry} of type {typeof(T)} does not exist for this session.");
                return ret[ret.Length-1];
            }
        }

        internal T? GetProfilingSessionMetaDataLatest<T>(ProfilingSessionMetaDataEntry entry) where T : unmanaged
        {
            var metaDataCount = GetSessionMetaDataCount(ProfilerDriver.profilerInternalSessionMetaDataGuid, (int)entry);
            if (metaDataCount <= 0)
                return null;

            using (var ret = GetSessionMetaData<T>(ProfilerDriver.profilerInternalSessionMetaDataGuid, (int)entry, metaDataCount -1))
            {
                if (ret.Length > 0)
                    return ret[ret.Length-1];
            }

            return null;
        }

        internal string GetProfilingSessionMetaDataString(ProfilingSessionMetaDataEntry entry)
        {
            using (var ret = GetSessionMetaData<byte>(ProfilerDriver.profilerInternalSessionMetaDataGuid, (int)entry))
            {
                Debug.Assert(ret.Length > 0, $"A ProfilingSessionMetaDataEntry {entry} of type string does not exist for this session.");
                unsafe
                {
                    return System.Text.Encoding.UTF8.GetString((byte*)ret.GetUnsafePtr(), ret.Length);
                }
            }
        }

        internal string GetProfilingSessionMetaDataStringLatest(ProfilingSessionMetaDataEntry entry)
        {
            // NB: If an empty string ("") was written as metadata, it will be added to
            // the metadata count, but the length of the returned NativeArray will be zero.
            var metaDataCount = GetSessionMetaDataCount(ProfilerDriver.profilerInternalSessionMetaDataGuid, (int)entry);
            if (metaDataCount <= 0)
                return null;

            using (var ret = GetSessionMetaData<byte>(ProfilerDriver.profilerInternalSessionMetaDataGuid, (int)entry, metaDataCount -1))
            {
                if (ret.Length > 0)
                {
                    unsafe
                    {
                        return System.Text.Encoding.UTF8.GetString((byte*)ret.GetUnsafePtr(), ret.Length);
                    }
                }
            }

            return null;
        }

        [StructLayout(LayoutKind.Sequential)]
        [RequiredByNativeCode]
        public struct MethodInfo
        {
            public string methodName;
            public string sourceFileName;
            public uint sourceFileLine;
        }

        public extern MethodInfo ResolveMethodInfo(ulong addr);

        [NativeMethod(IsThreadSafe = true)]
        public unsafe extern void* GetCounterValuePtr(int markerId);

        public unsafe bool HasCounterValue(int markerId)
        {
            return GetCounterValuePtr(markerId) != null;
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern int GetCounterValueAsInt(int markerId);

        [NativeMethod(IsThreadSafe = true)]
        public extern long GetCounterValueAsLong(int markerId);

        [NativeMethod(IsThreadSafe = true)]
        public extern float GetCounterValueAsFloat(int markerId);

        [NativeMethod(IsThreadSafe = true)]
        public extern double GetCounterValueAsDouble(int markerId);

        [NativeMethod(IsThreadSafe = true)]
        internal extern float GetLegacyStatisticValueAsFloat(ProfilerArea area, string name);

        [StructLayout(LayoutKind.Sequential)]
        [RequiredByNativeCode]
        public struct UnityObjectInfo
        {
            [NativeName("name")]
            readonly string m_Name;
            [NativeName("relatedGameObjectInstanceId")]
            readonly EntityId m_RelatedGameObjectEntityId;
            [NativeName("nativeTypeIndex")]
            readonly int m_NativeTypeIndex;
            [NativeName("rootId")]
            readonly ulong m_RootId;

            public string name => m_Name;
            public int nativeTypeIndex => m_NativeTypeIndex;
            [Obsolete("relatedGameObjectInstanceId is obsolete. Use relatedGameObjectEntityId instead.", true)]
            public int relatedGameObjectInstanceId => m_RelatedGameObjectEntityId;
            public EntityId relatedGameObjectEntityId => m_RelatedGameObjectEntityId;
            public ulong allocationRootId => m_RootId;
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern bool GetUnityObjectInfo(EntityId entityId, out UnityObjectInfo info);

        [Obsolete("GetUnityObjectInfo(int, out UnityObjectInfo) is obsolete. Use GetUnityObjectInfo(EntityId, out UnityObjectInfo) instead.", true)]
        public bool GetUnityObjectInfo(int instanceId, out UnityObjectInfo info) => GetUnityObjectInfo((EntityId)instanceId, out info);

        [StructLayout(LayoutKind.Sequential)]
        [RequiredByNativeCode]
        public struct UnityObjectNativeTypeInfo
        {
            [NativeName("name")]
            readonly string m_Name;
            [NativeName("baseNativeTypeIndex")]
            readonly int m_BaseNativeTypeIndex;

            public string name => m_Name;
            public int baseNativeTypeIndex => m_BaseNativeTypeIndex;
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern bool GetUnityObjectNativeTypeInfo(int nativeTypeIndex, out UnityObjectNativeTypeInfo info);

        [NativeMethod(IsThreadSafe = true)]
        public extern int GetUnityObjectNativeTypeInfoCount();

        [StructLayout(LayoutKind.Sequential)]
        [RequiredByNativeCode]
        public struct GfxResourceInfo
        {
            [NativeName("rootId")]
            readonly ulong m_RootId;
            [NativeName("instanceId")]
            readonly EntityId m_EntityId;

            public ulong relatedAllocationRootId => m_RootId;
            public EntityId relatedEntityId => m_EntityId;
            [Obsolete("relatedInstanceId is obsolete. Use relatedEntityId instead.", true)]
            public int relatedInstanceId => m_EntityId;
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern bool GetGfxResourceInfo(ulong gfxResourceId, out GfxResourceInfo info);

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(FrameDataView frameDataView) => frameDataView.m_Ptr;
        }
    }
}
