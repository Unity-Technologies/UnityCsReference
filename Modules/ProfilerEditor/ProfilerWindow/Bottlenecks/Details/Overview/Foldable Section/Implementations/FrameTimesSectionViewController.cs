// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;

namespace Unity.Profiling.Editor.UI
{
    class FrameTimesSectionViewController : FoldableSectionViewController
    {
        // Model.
        const string k_Title = "Frame time";

        // Child view controllers.
        readonly BoxPlotViewController m_FrameTimeBoxPlotViewController;
        readonly TopMarkersViewController m_TopFrameMarkersViewController;
        readonly TopMarkersViewController m_TopRangeMarkersViewController;

        public FrameTimesSectionViewController(
            ProfilerWindow profilerWindow,
            string rangeDescriptor,
            TopMarkersViewController.IResponder topMarkersResponder) : base(k_Title)
        {
            m_FrameTimeBoxPlotViewController = new BoxPlotViewController(
                $"Frame time across {rangeDescriptor}",
                profilerWindow);
            m_TopFrameMarkersViewController = new TopMarkersViewController(
                "Top markers on longest frame (self time)",
                TopMarkersViewController.Action.ChangeSelectedFrame,
                topMarkersResponder);
            m_TopRangeMarkersViewController = new TopMarkersViewController(
                $"Top markers across {rangeDescriptor} (self time)",
                TopMarkersViewController.Action.ChangeSelectedFrame,
                topMarkersResponder);
        }

        public void RefreshFrameTimesView(BoxPlotModel frameTimes)
        {
            m_FrameTimeBoxPlotViewController.ReloadData(frameTimes);
        }

        public void RefreshTopFrameMarkersView(TopMarkersModel topFrameMarkers)
        {
            m_TopFrameMarkersViewController.RefreshView(topFrameMarkers);
        }

        public void RefreshTopRangeMarkersView(TopMarkersModel topCaptureMarkers)
        {
            m_TopRangeMarkersViewController.RefreshView(topCaptureMarkers);
        }

        public void ShowActivityIndicatorAfterDelay(int delayMs)
        {
            m_FrameTimeBoxPlotViewController.ShowActivityIndicatorAfterDelay(delayMs);
            m_TopFrameMarkersViewController.ShowActivityIndicatorAfterDelay(delayMs);
            m_TopRangeMarkersViewController.ShowActivityIndicatorAfterDelay(delayMs);
        }

        public void SetActivityIndicatorVisible(bool visible)
        {
            m_FrameTimeBoxPlotViewController.SetActivityIndicatorVisible(visible);
            m_TopFrameMarkersViewController.SetActivityIndicatorVisible(visible);
            m_TopRangeMarkersViewController.SetActivityIndicatorVisible(visible);
        }

        protected override int NumberOfSections()
        {
            return 3;
        }

        protected override ViewController ViewControllerForSection(int section)
        {
            return section switch
            {
                0 => m_FrameTimeBoxPlotViewController,
                1 => m_TopFrameMarkersViewController,
                2 => m_TopRangeMarkersViewController,
                _ => throw new ArgumentOutOfRangeException($"Invalid section {section}"),
            };
        }
    }
}
