// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;

namespace Unity.Profiling.Editor.UI
{
    class SingleFrameTimesSectionViewController : FoldableSectionViewController
    {
        // Model.
        const string k_Title = "Frame time";

        // Child view controllers.
        readonly FrameBottlenecksViewController m_FrameBottlenecksViewController;
        readonly TopMarkersViewController m_TopFrameMarkersViewController;

        public SingleFrameTimesSectionViewController(
            ProfilerWindow profilerWindow,
            IProfilerPersistentSettingsService settingsService,
            TopMarkersViewController.IResponder topMarkersResponder,
            IDetailsElementBinder detailsBinder) : base(k_Title)
        {
            m_FrameBottlenecksViewController = new FrameBottlenecksViewController(settingsService, profilerWindow, detailsBinder);
            m_TopFrameMarkersViewController = new TopMarkersViewController(
                "Top markers in frame (self time)",
                profilerWindow,
                TopMarkersViewController.Action.SwitchToCpuModule,
                topMarkersResponder,
                detailsBinder);
        }

        public void RefreshFrameBottlenecksView(FrameBottlenecksModel frameBottlenecks)
        {
            m_FrameBottlenecksViewController.ReloadData(frameBottlenecks);
        }

        public void RefreshTopFrameMarkersView(TopMarkersModel topFrameMarkers)
        {
            m_TopFrameMarkersViewController.RefreshView(topFrameMarkers);
        }

        public void ShowActivityIndicatorAfterDelay(int delayMs)
        {
            m_FrameBottlenecksViewController.ShowActivityIndicatorAfterDelay(delayMs);
            m_TopFrameMarkersViewController.ShowActivityIndicatorAfterDelay(delayMs);
        }

        public void SetActivityIndicatorVisible(bool visible)
        {
            m_FrameBottlenecksViewController.SetActivityIndicatorVisible(visible);
            m_TopFrameMarkersViewController.SetActivityIndicatorVisible(visible);
        }

        protected override int NumberOfSections()
        {
            return 2;
        }

        protected override ViewController ViewControllerForSection(int section)
        {
            return section switch
            {
                0 => m_FrameBottlenecksViewController,
                1 => m_TopFrameMarkersViewController,
                _ => throw new ArgumentOutOfRangeException($"Invalid section {section}"),
            };
        }
    }
}
