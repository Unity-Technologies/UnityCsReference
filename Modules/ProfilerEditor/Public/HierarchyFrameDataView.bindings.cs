// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling.LowLevel;
using UnityEngine.Bindings;

namespace UnityEditor.Profiling
{
    [NativeHeader("Modules/ProfilerEditor/ProfilerHistory/HierarchyFrameDataView.h")]
    [StructLayout(LayoutKind.Sequential)]
    public class HierarchyFrameDataView : FrameDataView
    {
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

        [ThreadSafe]
        static extern IntPtr Internal_Create(int frameIndex, int threadIndex, ViewModes viewMode, int sortColumn, bool sortAscending);

        public extern ViewModes viewMode { [ThreadSafe] get; }

        public extern int sortColumn { [ThreadSafe] get; }

        public extern bool sortColumnAscending { [ThreadSafe] get; }

        [ThreadSafe]
        public extern int GetRootItemID();

        [ThreadSafe]
        public extern int GetItemMarkerID(int id);

        [ThreadSafe]
        public extern MarkerFlags GetItemMarkerFlags(int id);

        [ThreadSafe]
        public extern ushort GetItemCategoryIndex(int id);

        [ThreadSafe]
        public extern int GetItemDepth(int id);

        public extern bool HasItemChildren(int id);

        internal extern int GetItemChildrenCount(int id);

        [NativeThrows]
        public extern void GetItemChildren(int id, List<int> outChildren);

        [NativeThrows]
        public extern void GetItemAncestors(int id, List<int> outAncestors);

        [NativeThrows]
        public extern void GetItemDescendantsThatHaveChildren(int id, List<int> outChildren);

        [ThreadSafe]
        public extern string GetItemName(int id);

        [ThreadSafe]
        public extern int GetItemInstanceID(int id);

        [ThreadSafe]
        public extern string GetItemColumnData(int id, int column);

        public float GetItemColumnDataAsSingle(int id, int column)
        {
            return GetItemColumnDataAsFloat(id, column);
        }

        [ThreadSafe]
        public extern float GetItemColumnDataAsFloat(int id, int column);

        [ThreadSafe]
        public extern double GetItemColumnDataAsDouble(int id, int column);

        public int GetItemMetadataCount(int id) { return GetItemMergedSamplesMetadataCount(id, 0); }

        public string GetItemMetadata(int id, int index) { return GetItemMergedSamplesMetadata(id, 0, index); }

        public float GetItemMetadataAsFloat(int id, int index) { return GetItemMergedSamplesMetadataAsFloat(id, 0, index); }

        public long GetItemMetadataAsLong(int id, int index) { return GetItemMergedSamplesMetadataAsLong(id, 0, index); }

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern int GetItemMergedSamplesMetadataCount(int id, int sampleIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern string GetItemMergedSamplesMetadata(int id, int sampleIndex, int metadataIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern float GetItemMergedSamplesMetadataAsFloat(int id, int sampleIndex, int metadataIndex);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern long GetItemMergedSamplesMetadataAsLong(int id, int sampleIndex, int metadataIndex);

        [ThreadSafe]
        internal extern string GetItemTooltip(int id, int column);

        public string ResolveItemCallstack(int id)
        {
            return ResolveItemMergedSampleCallstack(id, 0);
        }

        public void GetItemCallstack(int id, List<ulong> outCallstack)
        {
            GetItemMergedSampleCallstack(id, 0, outCallstack);
        }

        [ThreadSafe]
        public extern int GetItemMergedSamplesCount(int id);

        public void GetItemMergedSamplesColumnData(int id, int column, List<string> outStrings)
        {
            if (outStrings == null)
                throw new ArgumentNullException(nameof(outStrings));

            GetItemMergedSamplesColumnDataInternal(id, column, outStrings);
        }

        [NativeMethod("GetItemMergedSamplesColumnData")]
        [ThreadSafe]
        extern void GetItemMergedSamplesColumnDataInternal(int id, int column, List<string> outStrings);

        public void GetItemMergedSamplesColumnDataAsFloats(int id, int column, List<float> outValues)
        {
            if (outValues == null)
                throw new ArgumentNullException(nameof(outValues));

            GetItemMergedSamplesColumnDataAsFloatsInternal(id, column, outValues);
        }

        [NativeMethod("GetItemMergedSamplesColumnDataAsFloats")]
        [ThreadSafe]
        extern void GetItemMergedSamplesColumnDataAsFloatsInternal(int id, int column, List<float> outValues);

        public void GetItemMergedSamplesColumnDataAsDoubles(int id, int column, List<double> outValues)
        {
            if (outValues == null)
                throw new ArgumentNullException(nameof(outValues));

            GetItemMergedSamplesColumnDataAsDoublesInternal(id, column, outValues);
        }

        [NativeMethod("GetItemMergedSamplesColumnDataAsDoubles")]
        [ThreadSafe]
        extern void GetItemMergedSamplesColumnDataAsDoublesInternal(int id, int column, List<double> outValues);

        public void GetItemMergedSamplesInstanceID(int id, List<int> outInstanceIds)
        {
            if (outInstanceIds == null)
                throw new ArgumentNullException(nameof(outInstanceIds));

            GetItemMergedSamplesInstanceIDInternal(id, outInstanceIds);
        }

        [NativeMethod("GetItemMergedSamplesInstanceID")]
        [ThreadSafe]
        extern void GetItemMergedSamplesInstanceIDInternal(int id, List<int> outInstanceIds);

        public void GetItemMergedSampleCallstack(int id, int sampleIndex, List<ulong> outCallstack)
        {
            if (outCallstack == null)
                throw new ArgumentNullException(nameof(outCallstack));

            GetItemMergedSampleCallstackInternal(id, sampleIndex, outCallstack);
        }

        [NativeMethod("GetItemMergedSampleCallstack")]
        [ThreadSafe]
        extern void GetItemMergedSampleCallstackInternal(int id, int sampleIndex, List<ulong> outCallstack);

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

        public extern void Sort(int sortColumn, bool sortAscending);

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
