// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Unity.Profiling.LowLevel;
using UnityEngine;
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
            InvertHierarchy = 1 << 2
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
        

        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr Internal_Create(int frameIndex, int threadIndex, ViewModes viewMode, int sortColumn, bool sortAscending);

        public extern ViewModes viewMode { [NativeMethod(IsThreadSafe = true)] get; }

        public extern int sortColumn { [NativeMethod(IsThreadSafe = true)] get; }

        public extern bool sortColumnAscending { [NativeMethod(IsThreadSafe = true)] get; }

        [NativeMethod(IsThreadSafe = true)]
        public extern int GetRootItemID();

        [NativeMethod(IsThreadSafe = true)]
        public extern int GetItemMarkerID(int id);

        [NativeMethod(IsThreadSafe = true)]
        public extern MarkerFlags GetItemMarkerFlags(int id);

        [NativeMethod(IsThreadSafe = true)]
        public extern ushort GetItemCategoryIndex(int id);

        [NativeMethod(IsThreadSafe = true)]
        public extern int GetItemDepth(int id);

        public extern bool HasItemChildren(int id);

        internal extern int GetItemChildrenCount(int id);

        public extern void GetItemChildren(int id, [NotNull] List<int> outChildren);

        public extern void GetItemAncestors(int id, [NotNull] List<int> outAncestors);

        public extern void GetItemDescendantsThatHaveChildren(int id, [NotNull] List<int> outChildren);

        [NativeMethod(IsThreadSafe = true)]
        public extern string GetItemName(int id);

        [Obsolete("Use GetItemEntityId(int id) instead. This method will be removed in a future version.", true)]
        public int GetItemInstanceID(int id) => GetItemEntityId(id);

        [NativeMethod(IsThreadSafe = true)]
        public extern EntityId GetItemEntityId(int id);

        [NativeMethod(IsThreadSafe = true)]
        public extern string GetItemColumnData(int id, int column);

        public float GetItemColumnDataAsSingle(int id, int column)
        {
            return GetItemColumnDataAsFloat(id, column);
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern float GetItemColumnDataAsFloat(int id, int column);

        [NativeMethod(IsThreadSafe = true)]
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

        [NativeMethod(IsThreadSafe = true)]
        internal extern string GetItemTooltip(int id, int column);

        public string ResolveItemCallstack(int id)
        {
            return ResolveItemMergedSampleCallstack(id, 0);
        }

        public void GetItemCallstack(int id, List<ulong> outCallstack)
        {
            GetItemMergedSampleCallstack(id, 0, outCallstack);
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern int GetItemMergedSamplesCount(int id);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern void GetItemRawFrameDataViewIndices(int id, [NotNull] List<int> outSampleIndices);

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        public extern bool ItemContainsRawFrameDataViewIndex(int id, int sampleIndex);

        public void GetItemMergedSamplesColumnData(int id, int column, List<string> outStrings)
        {
            if (outStrings == null)
                throw new ArgumentNullException(nameof(outStrings));

            GetItemMergedSamplesColumnDataInternal(id, column, outStrings);
        }

        [NativeMethod("GetItemMergedSamplesColumnData", IsThreadSafe = true)]
        extern void GetItemMergedSamplesColumnDataInternal(int id, int column, [Out,NotNull] List<string> outStrings);

        public void GetItemMergedSamplesColumnDataAsFloats(int id, int column, List<float> outValues)
        {
            if (outValues == null)
                throw new ArgumentNullException(nameof(outValues));

            GetItemMergedSamplesColumnDataAsFloatsInternal(id, column, outValues);
        }

        [NativeMethod("GetItemMergedSamplesColumnDataAsFloats", IsThreadSafe = true)]
        extern void GetItemMergedSamplesColumnDataAsFloatsInternal(int id, int column, List<float> outValues);

        public void GetItemMergedSamplesColumnDataAsDoubles(int id, int column, List<double> outValues)
        {
            if (outValues == null)
                throw new ArgumentNullException(nameof(outValues));

            GetItemMergedSamplesColumnDataAsDoublesInternal(id, column, outValues);
        }

        [NativeMethod("GetItemMergedSamplesColumnDataAsDoubles", IsThreadSafe = true)]
        extern void GetItemMergedSamplesColumnDataAsDoublesInternal(int id, int column, List<double> outValues);

        [Obsolete("Deprecated, use GetItemMergedSamplesEntityId instead. This method will be removed in a future version.", true)]
        public void GetItemMergedSamplesInstanceID(int id, List<int> outInstanceIds)
        {
            throw new NotSupportedException("Deprecated, use GetItemMergedSamplesEntityId instead.");
        }

        public void GetItemMergedSamplesEntityId(int id, List<EntityId> outEntityIds)
        {
            if (outEntityIds == null)
                throw new ArgumentNullException(nameof(outEntityIds));

            GetItemMergedSamplesEntityIdInternal(id, outEntityIds);
        }

        [NativeMethod("GetItemMergedSamplesEntityId", IsThreadSafe = true)]
        extern void GetItemMergedSamplesEntityIdInternal(int id, List<EntityId> outEntityIds);

        public void GetItemMergedSampleCallstack(int id, int sampleIndex, List<ulong> outCallstack)
        {
            if (outCallstack == null)
                throw new ArgumentNullException(nameof(outCallstack));

            GetItemMergedSampleCallstackInternal(id, sampleIndex, outCallstack);
        }

        [NativeMethod("GetItemMergedSampleCallstack", IsThreadSafe = true)]
        extern void GetItemMergedSampleCallstackInternal(int id, int sampleIndex, List<ulong> outCallstack);

        public extern string ResolveItemMergedSampleCallstack(int id, int sampleIndex);

        public void GetItemMarkerIDPath(int id, List<int> outFullIdPath)
        {
            if (outFullIdPath == null)
                throw new ArgumentNullException("outFullIdPath");

            if (viewMode.HasFlag(ViewModes.InvertHierarchy))
            {
                // Inverted hierarchy should also report Marker ID path from the top-down perspective.
                // Since callers are represented by children items we do depth first scan to get the first valid markerid path.
                outFullIdPath.Clear();
                List<int> children = null;
                var childId = id;
                while (HasItemChildren(childId))
                {
                    if (children == null)
                        children = new List<int>();
                    GetItemChildren(childId, children);
                    childId = children[0];
                    outFullIdPath.Add(childId);
                }
            }
            else
            {
                GetItemAncestors(id, outFullIdPath);
            }
            outFullIdPath.Reverse();

            for (int i = 0; i < outFullIdPath.Count; ++i)
                outFullIdPath[i] = GetItemMarkerID(outFullIdPath[i]);
            outFullIdPath.Add(GetItemMarkerID(id));
        }

        public string GetItemPath(int id)
        {
            var ancestors = new List<int>();
            if (viewMode.HasFlag(ViewModes.InvertHierarchy))
            {
                // Inverted hierarchy should also report Marker ID path from the top-down perspective.
                // Since callers are represented by children items we do depth first scan to get the first valid markerid path.
                List<int> children = null;
                var childId = id;
                while (HasItemChildren(childId))
                {
                    if (children == null)
                        children = new List<int>();
                    GetItemChildren(childId, children);
                    childId = children[0];
                    ancestors.Add(childId);
                }
            }
            else
            {
                GetItemAncestors(id, ancestors);
            }
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

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(HierarchyFrameDataView frameDataView) => frameDataView.m_Ptr;
        }
    }
}
