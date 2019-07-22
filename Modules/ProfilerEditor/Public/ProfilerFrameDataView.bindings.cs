// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling.LowLevel;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Profiling
{
    [NativeHeader("Modules/ProfilerEditor/ProfilerHistory/FrameDataView.h")]
    [StructLayout(LayoutKind.Sequential)]
    public class HierarchyFrameDataView : IDisposable
    {
        IntPtr m_Ptr;

        public const int invalidSampleId = -1;

        [Flags]
        public enum ViewModes
        {
            Default = 0,
            MergeSamplesWithTheSameName = 1 << 0,
            HideEditorOnlySamples = 1 << 1,
        }

        public const int columnDontSort = -1;
        public const int columnName = 0;
        public const int columnTotalPercent = 1;
        public const int columnSelfPercent = 2;
        public const int columnCalls = 3;
        public const int columnGcMemory = 4;
        public const int columnTotalTime = 5;
        public const int columnSelfTime = 6;
        internal const int columnDrawCalls = 7;
        internal const int columnTotalGpuTime = 8;
        internal const int columnSelfGpuTime = 9;
        internal const int columnTotalGpuPercent = 10;
        internal const int columnSelfGpuPercent = 11;
        public const int columnWarningCount = 12;
        public const int columnObjectName = 13;
        public const int columnStartTime = 14;

        internal HierarchyFrameDataView(int frameIndex, int threadIndex, ViewModes viewMode, int sortColumn, bool sortAscending)
        {
            m_Ptr = Internal_Create(frameIndex, threadIndex, viewMode, sortColumn, sortAscending);
        }

        ~HierarchyFrameDataView()
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
        static extern IntPtr Internal_Create(int frameIndex, int threadIndex, ViewModes viewMode, int sortColumn, bool sortAscending);

        [ThreadSafe]
        static extern void Internal_Destroy(IntPtr ptr);


        public bool valid
        {
            get
            {
                if (m_Ptr == IntPtr.Zero)
                    return false;

                return GetRootItemID() != invalidSampleId;
            }
        }

        public extern float frameFps { get; }

        public extern float frameTimeMs { get; }

        public extern float frameGpuTimeMs { get; }

        public extern int frameIndex { get; }

        public extern int threadIndex { get; }

        public extern string threadGroupName { get; }

        public extern string threadName { get; }

        public extern ulong threadId { get; }

        public extern ViewModes viewMode { get; }

        public extern int sortColumn { get; }

        public extern bool sortColumnAscending { get; }

        public extern int GetRootItemID();

        public extern int GetItemMarkerID(int id);

        public extern MarkerFlags GetItemMarkerFlags(int id);

        public extern int GetItemDepth(int id);

        public extern bool HasItemChildren(int id);

        internal extern int GetItemChildrenCount(int id);

        [NativeThrows]
        public extern void GetItemChildren(int id, List<int> outChildren);

        [NativeThrows]
        public extern void GetItemAncestors(int id, List<int> outAncestors);

        [NativeThrows]
        public extern void GetItemDescendantsThatHaveChildren(int id, List<int> outChildren);

        public extern string GetItemName(int id);

        public extern int GetItemInstanceID(int id);

        public extern string GetItemColumnData(int id, int column);

        public float GetItemColumnDataAsSingle(int id, int column)
        {
            return GetItemColumnDataAsFloat(id, column);
        }

        public extern float GetItemColumnDataAsFloat(int id, int column);

        public extern double GetItemColumnDataAsDouble(int id, int column);

        public extern int GetItemMetadataCount(int id);

        public extern string GetItemMetadata(int id, int index);

        public extern float GetItemMetadataAsFloat(int id, int index);

        public extern long GetItemMetadataAsLong(int id, int index);

        internal extern string GetItemTooltip(int id, int column);

        public string ResolveItemCallstack(int id)
        {
            return ResolveItemMergedSampleCallstack(id, 0);
        }

        public void GetItemCallstack(int id, List<ulong> outCallstack)
        {
            GetItemMergedSampleCallstack(id, 0, outCallstack);
        }

        public extern int GetItemMergedSamplesCount(int id);

        public void GetItemMergedSamplesColumnData(int id, int column, List<string> outStrings)
        {
            if (outStrings == null)
                throw new ArgumentNullException(nameof(outStrings));

            GetItemMergedSamplesColumnDataInternal(id, column, outStrings);
        }

        [NativeMethod("GetItemMergedSamplesColumnData")]
        extern void GetItemMergedSamplesColumnDataInternal(int id, int column, List<string> outStrings);

        public void GetItemMergedSamplesColumnDataAsFloats(int id, int column, List<float> outValues)
        {
            if (outValues == null)
                throw new ArgumentNullException(nameof(outValues));

            GetItemMergedSamplesColumnDataAsFloatsInternal(id, column, outValues);
        }

        [NativeMethod("GetItemMergedSamplesColumnDataAsFloats")]
        extern void GetItemMergedSamplesColumnDataAsFloatsInternal(int id, int column, List<float> outValues);

        public void GetItemMergedSamplesInstanceID(int id, List<int> outInstanceIds)
        {
            if (outInstanceIds == null)
                throw new ArgumentNullException(nameof(outInstanceIds));

            GetItemMergedSamplesInstanceIDInternal(id, outInstanceIds);
        }

        [NativeMethod("GetItemMergedSamplesInstanceID")]
        extern void GetItemMergedSamplesInstanceIDInternal(int id, List<int> outInstanceIds);

        public void GetItemMergedSampleCallstack(int id, int sampleIndex, List<ulong> outCallstack)
        {
            if (outCallstack == null)
                throw new ArgumentNullException(nameof(outCallstack));

            GetItemMergedSampleCallstackInternal(id, sampleIndex, outCallstack);
        }

        [NativeMethod("GetItemMergedSampleCallstack")]
        extern void GetItemMergedSampleCallstackInternal(int id, int sampleIndex, List<ulong> outCallstack);

        [StructLayout(LayoutKind.Sequential)]
        [RequiredByNativeCode]
        public struct MethodInfo
        {
            public string methodName;
            public string sourceFileName;
            public uint sourceFileLine;
        };
        public extern MethodInfo ResolveMethodInfo(ulong addr);

        public extern string ResolveItemMergedSampleCallstack(int id, int sampleIndex);

        public void GetItemMarkerIDPath(int id, List<int> outFullIdPath)
        {
            if (outFullIdPath == null)
                throw new ArgumentNullException("outFullIdPath");
            GetItemAncestors(id, outFullIdPath);
            outFullIdPath.Reverse();
            for (int i = 0; i < outFullIdPath.Count; ++i)
                outFullIdPath[i] = GetItemMarkerID(outFullIdPath[i]);
            outFullIdPath.Add(GetItemMarkerID(id));
        }

        public string GetItemPath(int id)
        {
            var ancestors = new List<int>();
            GetItemAncestors(id, ancestors);
            var propertyPathBuilder = new StringBuilder();
            for (int i = ancestors.Count - 1; i >= 0; i--)
            {
                propertyPathBuilder.Append(GetItemName(ancestors[i]));
                propertyPathBuilder.Append('/');
            }
            propertyPathBuilder.Append(GetItemName(id));
            return propertyPathBuilder.ToString();
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Data
        {
            public IntPtr ptr;
            public int size;
        }

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

        extern Data GetFrameMetaData(byte[] statsId, int tag, int index);

        extern int GetFrameMetaDataCount(byte[] statsId, int tag);

        public extern void Sort(int sortColumn, bool sortAscending);

        internal static extern UnityEngine.Color32 GetMarkerCategoryColor(int category);

        public override bool Equals(object obj)
        {
            var dataViewObj = obj as HierarchyFrameDataView;
            if (dataViewObj == null)
                return false;

            if (m_Ptr == dataViewObj.m_Ptr)
                return true;
            if (m_Ptr == IntPtr.Zero || dataViewObj.m_Ptr == IntPtr.Zero)
                return false;

            return frameIndex.Equals(dataViewObj.frameIndex) &&
                threadIndex.Equals(dataViewObj.threadIndex) &&
                viewMode.Equals(dataViewObj.viewMode) &&
                sortColumn.Equals(dataViewObj.sortColumn) &&
                sortColumnAscending.Equals(dataViewObj.sortColumnAscending);
        }

        public override int GetHashCode()
        {
            return frameIndex.GetHashCode() ^
                (threadIndex.GetHashCode() << 8) ^
                (viewMode.GetHashCode() << 24);
        }
    }
}
