// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditorInternal.Profiling
{
    [MovedFrom("UnityEditorInternal")]
    public enum ProfilerViewType
    {
        Hierarchy = 0,
        Timeline = 1,
        RawHierarchy = 2
    }

    [MovedFrom("UnityEditorInternal")]
    public enum ProfilerColumn
    {
        DontSort = -1,
        FunctionName = 0,
        TotalPercent,
        SelfPercent,
        Calls,
        GCMemory,
        TotalTime,
        SelfTime,
        DrawCalls,
        TotalGPUTime,
        SelfGPUTime,
        TotalGPUPercent,
        SelfGPUPercent,
        WarningCount,
        ObjectName
    }

    [NativeHeader("Modules/Profiler/Editor/ProfilerHistory/FrameDataView.h")]
    internal class FrameDataView : IDisposable
    {
        private IntPtr m_Ptr;

        public struct MarkerPath
        {
            public readonly List<int> markerIds;

            public MarkerPath(List<int> markerIds)
            {
                this.markerIds = markerIds;
            }

            public override bool Equals(object obj)
            {
                var other = (MarkerPath)obj;

                if (markerIds == other.markerIds)
                    return true;
                if (markerIds == null || other.markerIds == null)
                    return false;
                // Faster than SequenceEqual
                if (markerIds.Count != other.markerIds.Count)
                    return false;
                var count = markerIds.Count;
                for (var i = 0; i < count; ++i)
                {
                    if (markerIds[i] != other.markerIds[i])
                        return false;
                }

                return true;
            }

            public override int GetHashCode()
            {
                if (markerIds == null)
                    return 0;
                int hash = 0;
                for (var i = 0; i < markerIds.Count; ++i)
                    hash ^= markerIds[i].GetHashCode();
                return hash;
            }
        }

        public FrameDataView(ProfilerViewType viewType, int frameIndex, int threadIndex, ProfilerColumn profilerSortColumn, bool sortAscending)
        {
            m_Ptr = Internal_Create(viewType, frameIndex, threadIndex, profilerSortColumn, sortAscending);
        }

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
        private void DisposeInternal()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        [ThreadSafe]
        private static extern IntPtr Internal_Create(ProfilerViewType viewType, int frameIndex, int threadIndex, ProfilerColumn profilerSortColumn, bool sortAscending);

        [ThreadSafe]
        private static extern void Internal_Destroy(IntPtr ptr);


        public extern bool frameDataReady { get; }

        public extern string frameFPS { get; }

        public extern string frameTime { get; }

        public extern string frameGpuTime { get; }

        public extern int frameIndex { get; }

        public extern int threadIndex { get; }

        public extern ProfilerColumn sortColumn { get; }

        public extern bool sortColumnAscending { get; }

        public extern ProfilerViewType viewType { get; }

        public extern int GetRootItemID();

        public extern int GetItemMarkerID(int id);

        public extern int GetItemDepth(int id);

        public extern string GetItemFunctionName(int id);

        public extern string GetItemColumnData(int id, ProfilerColumn column);

        public extern float GetItemColumnDataAsSingle(int id, ProfilerColumn column);

        public extern string GetItemTooltip(int id, ProfilerColumn column);

        public extern int GetItemInstanceID(int id);

        public extern int GetItemSamplesCount(int id);

        public extern string[] GetItemColumnDatas(int id, ProfilerColumn column);

        public extern int[] GetItemInstanceIDs(int id);

        public extern bool HasItemChildren(int id);

        public extern int GetItemChildrenCount(int id);

        public extern void GetItemChildren(int id, List<int> outChildren);

        public extern int[] GetItemAncestors(int id);

        public extern int[] GetItemDescendantsThatHaveChildren(int id);

        public string ResolveItemCallstack(int id)
        {
            return ResolveItemCallstack(id, 0);
        }

        public extern string ResolveItemCallstack(int id, int sampleIndex);

        public extern void Sort(ProfilerColumn profilerSortColumn, bool sortAscending);

        public MarkerPath GetItemMarkerIDPath(int id)
        {
            // Get path as marker ids
            var ancestors = GetItemAncestors(id);
            var markerIds = new List<int>(1 + ancestors.Length);
            for (var i = ancestors.Length - 1; i >= 0; i--)
                markerIds.Add(GetItemMarkerID(ancestors[i]));
            markerIds.Add(GetItemMarkerID(id));

            return new MarkerPath(markerIds);
        }

        public string GetItemPath(int id)
        {
            var ancestors = GetItemAncestors(id);
            var propertyPathBuilder = new StringBuilder();
            for (int i = ancestors.Length - 1; i >= 0; i--)
            {
                propertyPathBuilder.Append(GetItemFunctionName(ancestors[i]));
                propertyPathBuilder.Append('/');
            }
            propertyPathBuilder.Append(GetItemFunctionName(id));
            return propertyPathBuilder.ToString();
        }

        public static extern UnityEngine.Color32 GetMarkerCategoryColor(int category);

        public bool IsValid()
        {
            if (m_Ptr == IntPtr.Zero)
                return false;

            return GetRootItemID() != -1;
        }

        public override bool Equals(object obj)
        {
            if (m_Ptr == IntPtr.Zero)
                return false;

            var frameDataViewObj = obj as FrameDataView;
            if (frameDataViewObj == null)
                return false;

            return frameIndex.Equals(frameDataViewObj.frameIndex) &&
                threadIndex.Equals(frameDataViewObj.threadIndex) &&
                viewType.Equals(frameDataViewObj.viewType);
        }

        public override int GetHashCode()
        {
            return frameIndex.GetHashCode() ^
                threadIndex.GetHashCode() ^
                viewType.GetHashCode();
        }
    }
}
