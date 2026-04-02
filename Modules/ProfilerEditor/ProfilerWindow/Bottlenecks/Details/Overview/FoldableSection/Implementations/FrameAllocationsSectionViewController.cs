// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;

namespace Unity.Profiling.Editor.UI
{
    class FrameAllocationsSectionViewController : FoldableSectionViewController
    {
        // Model.
        const string k_Title = "Allocations";

        // Child view controllers.
        readonly FrameGCAllocationsViewController m_GCAllocationsViewController;
        readonly TopMarkersViewController m_TopGCMarkersViewController;
        readonly FrameGCCollectViewController m_GCCollectViewController;

        public FrameAllocationsSectionViewController(
            ProfilerWindow profilerWindow,
            TopMarkersViewController.IResponder topMarkersResponder,
            IDetailsElementBinder detailsBinder) : base(k_Title)
        {
            m_GCAllocationsViewController = new FrameGCAllocationsViewController("GC allocations in frame",
                profilerWindow,
                detailsBinder);
            m_TopGCMarkersViewController = new TopMarkersViewController(
                "Top contributors to GC allocations in frame (self GC Alloc)",
                profilerWindow,
                TopMarkersViewController.Action.SwitchToCpuModule,
                topMarkersResponder,
                detailsBinder);
            m_GCCollectViewController = new FrameGCCollectViewController("GC Collect (ms)");
        }

        public void RefreshFrameGCAllocationsView(FrameGCAllocationsModel frameGCAllocations)
        {
            m_GCAllocationsViewController.RefreshView(frameGCAllocations);
        }

        public void RefreshTopGCMarkersView(TopMarkersModel topGCMarkers)
        {
            m_TopGCMarkersViewController.RefreshView(topGCMarkers);
        }

        public void RefreshFrameGCCollectView(FrameGCCollectModel frameGCCollect)
        {
            m_GCCollectViewController.RefreshView(frameGCCollect);
        }

        public void ShowActivityIndicatorAfterDelay(int delayMs)
        {
            m_GCAllocationsViewController.ShowActivityIndicatorAfterDelay(delayMs);
            m_TopGCMarkersViewController.ShowActivityIndicatorAfterDelay(delayMs);
            m_GCCollectViewController.ShowActivityIndicatorAfterDelay(delayMs);
        }

        public void SetActivityIndicatorVisible(bool visible)
        {
            m_GCAllocationsViewController.SetActivityIndicatorVisible(visible);
            m_TopGCMarkersViewController.SetActivityIndicatorVisible(visible);
            m_GCCollectViewController.SetActivityIndicatorVisible(visible);
        }

        protected override int NumberOfSections()
        {
            return 3;
        }

        protected override ViewController ViewControllerForSection(int section)
        {
            return section switch
            {
                0 => m_GCAllocationsViewController,
                1 => m_TopGCMarkersViewController,
                2 => m_GCCollectViewController,
                _ => throw new ArgumentOutOfRangeException($"Invalid section {section}"),
            };
        }
    }
}
