// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;

namespace Unity.Profiling.Editor.UI
{
    class AllocationsSectionViewController : FoldableSectionViewController
    {
        // Model.
        const string k_Title = "Allocations";

        // Child view controllers.
        readonly GCAllocationsViewController m_GCAllocationsViewController;
        readonly TopMarkersViewController m_TopGCMarkersViewController;
        readonly BoxPlotViewController m_GCCollectViewController;

        public AllocationsSectionViewController(
            ProfilerWindow profilerWindow,
            string rangeDescriptor,
            TopMarkersViewController.IResponder topMarkersResponder,
            IDetailsElementBinder detailsBinder) : base(k_Title)
        {
            m_GCAllocationsViewController = new GCAllocationsViewController(
                $"GC allocations across {rangeDescriptor}",
                profilerWindow,
                detailsBinder);
            m_TopGCMarkersViewController = new TopMarkersViewController(
                $"Top contributors to GC allocations across {rangeDescriptor}",
                profilerWindow,
                TopMarkersViewController.Action.ChangeSelectedFrame,
                topMarkersResponder,
                detailsBinder);
            m_GCCollectViewController = new BoxPlotViewController(
                "GC Collect (ms)",
                profilerWindow);
        }

        public void RefreshGCAllocationsView(GCAllocationsModel gcAllocations)
        {
            m_GCAllocationsViewController.ReloadData(gcAllocations);
        }

        public void RefreshTopGCMarkersView(TopMarkersModel topGCMarkers)
        {
            m_TopGCMarkersViewController.RefreshView(topGCMarkers);
        }

        public void RefreshGCCollectView(BoxPlotModel gcCollect)
        {
            m_GCCollectViewController.ReloadData(gcCollect);
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
