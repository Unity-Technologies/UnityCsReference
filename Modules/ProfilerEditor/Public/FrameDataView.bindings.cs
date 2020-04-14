// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Profiling
{
    [NativeHeader("Modules/ProfilerEditor/ProfilerHistory/FrameDataView.h")]
    [StructLayout(LayoutKind.Sequential)]
    public abstract class FrameDataView : IDisposable
    {
        protected IntPtr m_Ptr;

        public const int invalidMarkerId = -1;

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

        [ThreadSafe]
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
            [ThreadSafe]
            get;
        }

        public extern int threadIndex
        {
            [ThreadSafe]
            get;
        }

        public extern string threadGroupName
        {
            [ThreadSafe]
            get;
        }

        public extern string threadName
        {
            [ThreadSafe]
            get;
        }

        public extern ulong threadId
        {
            [ThreadSafe]
            get;
        }

        public extern double frameStartTimeMs
        {
            [ThreadSafe]
            get;
        }

        public extern ulong frameStartTimeNs
        {
            [ThreadSafe]
            get;
        }

        public extern float frameTimeMs
        {
            [ThreadSafe]
            get;
        }

        public extern ulong frameTimeNs
        {
            [ThreadSafe]
            get;
        }

        public extern float frameGpuTimeMs
        {
            [ThreadSafe]
            get;
        }

        public extern ulong frameGpuTimeNs
        {
            [ThreadSafe]
            get;
        }

        public extern float frameFps
        {
            [ThreadSafe]
            get;
        }

        public extern int sampleCount
        {
            [ThreadSafe]
            get;
        }

        public extern int maxDepth
        {
            [ThreadSafe]
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
        public extern void GetMarkers(List<MarkerInfo> markerInfoList);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        internal static extern UnityEngine.Color32 GetMarkerCategoryColor(ushort category);

        [StructLayout(LayoutKind.Sequential)]
        struct Data
        {
            public IntPtr ptr;
            public int size;
        }

        [ThreadSafe]
        extern AtomicSafetyHandle GetSafetyHandle();

        public NativeArray<T> GetFrameMetaData<T>(Guid id, int tag) where T : struct
        {
            return GetFrameMetaData<T>(id, tag, 0);
        }

        public unsafe NativeArray<T> GetFrameMetaData<T>(Guid id, int tag, int index) where T : struct
        {
            var stride = UnsafeUtility.SizeOf<T>();
            var data = GetFrameMetaData(id.ToByteArray(), tag, index);
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(data.ptr.ToPointer(), data.size / stride, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, GetSafetyHandle());
            return array;
        }

        public int GetFrameMetaDataCount(Guid id, int tag)
        {
            return GetFrameMetaDataCount(id.ToByteArray(), tag);
        }

        [ThreadSafe]
        extern Data GetFrameMetaData(byte[] statsId, int tag, int index);

        [ThreadSafe]
        extern int GetFrameMetaDataCount(byte[] statsId, int tag);

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
    }
}
