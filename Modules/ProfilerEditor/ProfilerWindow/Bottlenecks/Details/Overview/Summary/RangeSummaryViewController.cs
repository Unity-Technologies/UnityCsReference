// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using UnityEditor;

namespace Unity.Profiling.Editor.UI
{
    class RangeSummaryViewController : SummaryViewController
    {
        // Model.
        readonly bool m_RangeIsWholeCapture;
        readonly string m_RangeDescriptor;
        readonly string m_NoDataText;
        protected CancellationTokenSource m_BuildModelCancellation;

        // Children.
        RangeBottlenecksViewController m_BottlenecksViewController;

        public RangeSummaryViewController(
            IProfilerCaptureDataService dataService,
            IProfilerPersistentSettingsService settingsService,
            ProfilerWindow profilerWindow,
            IResponder responder,
            IDetailsElementBinder detailsBinder,
            bool rangeIsWholeCapture = false) : base(dataService, settingsService, profilerWindow, responder, detailsBinder)
        {
            m_RangeIsWholeCapture = rangeIsWholeCapture;
            m_RangeDescriptor = rangeIsWholeCapture ? "capture" : "selection";
            m_NoDataText = rangeIsWholeCapture ?
                "Record a new capture or load an existing one to see its details here." :
                "Select a frame from the charts above to see its details here.";
        }

        public void ReloadData(Range frameRange)
        {
            UnityEngine.Debug.Assert(IsViewLoaded);

            // If we have no data, show the no data view.
            if (m_DataService.FrameCount == 0)
            {
                HideContentViewsAndShowNoDataView();
                m_SelectedRange = new Range(0, 0);
                return;
            }

            m_SelectedRange = frameRange;
            ReloadDataAsync(frameRange);
        }

        public void CancelReloadDataIfNecessary()
        {
            m_BuildModelCancellation?.Cancel();
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            m_NoDataLabel.text = m_NoDataText;

            // Embed child view controllers.
            m_BottlenecksViewController = new RangeBottlenecksViewController(m_SettingsService);
            m_BottlenecksContainer.Add(m_BottlenecksViewController.View);
            AddChild(m_BottlenecksViewController);

            var systemsImpactTitle = $"Systems impact across {m_RangeDescriptor} (mean time)";
            m_SystemsImpactViewController = new SystemsImpactViewController(m_DataService, systemsImpactTitle);
            m_SystemsImpactContainer.Add(m_SystemsImpactViewController.View);
            AddChild(m_SystemsImpactViewController);

            m_FrameTimesSectionViewController = new FrameTimesSectionViewController(
                m_ProfilerWindow,
                m_RangeDescriptor,
                this,
                m_DetailsBinder);
            m_FrameTimesContainer.Add(m_FrameTimesSectionViewController.View);
            AddChild(m_FrameTimesSectionViewController);

            m_AllocationsSectionViewController = new AllocationsSectionViewController(
                m_ProfilerWindow,
                m_RangeDescriptor,
                this,
                m_DetailsBinder);
            m_AllocationsContainer.Add(m_AllocationsSectionViewController.View);
            AddChild(m_AllocationsSectionViewController);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_BuildModelCancellation?.Cancel();
            }

            base.Dispose(disposing);
        }

        async void ReloadDataAsync(Range frameRange)
        {
            // If there is already a builder in flight, cancel it.
            m_BuildModelCancellation?.Cancel();

            // Show the content views.
            ShowContentViewsAndHideNoDataView();
            ShowContentActivityIndicators();

            // Build the data model asynchronously. Refresh UI as models are built.
            var success = true;
            using (var buildModelCancellation = new CancellationTokenSource())
            {
                // Store a reference to the source so we can cancel it.
                m_BuildModelCancellation = buildModelCancellation;

                try
                {
                    var modelBuilder = new RangeSummaryModelBuilder(
                        m_DataService,
                        frameRange);
                    await modelBuilder.BuildAsync(
                        buildModelCancellation.Token,
                        OnRangeBottlenecksBuildCompleted: m_BottlenecksViewController.ReloadData,
                        OnSystemsImpactBuildCompleted: m_SystemsImpactViewController.ReloadData,
                        OnFrameTimesBuildCompleted: m_FrameTimesSectionViewController.RefreshFrameTimesView,
                        OnTopFrameMarkersBuildCompleted: m_FrameTimesSectionViewController.RefreshTopFrameMarkersView,
                        OnTopRangeMarkersBuildCompleted: m_FrameTimesSectionViewController.RefreshTopRangeMarkersView,
                        OnGCAllocationsBuildCompleted: m_AllocationsSectionViewController.RefreshGCAllocationsView,
                        OnTopGCMarkersBuildCompleted: m_AllocationsSectionViewController.RefreshTopGCMarkersView,
                        OnGCCollectBuildCompleted: m_AllocationsSectionViewController.RefreshGCCollectView
                    );
                }
                catch (OperationCanceledException e) when (e.CancellationToken == buildModelCancellation.Token)
                {
                    // The operation was cancelled.
                    success = false;
                }
                catch (ProfilerFrameIndexOutOfBounds)
                {
                    // The frame index is invalid, cancel the operation.
                    buildModelCancellation.Cancel();
                    success = false;
                }
                catch (Exception e)
                {
                    success = false;
                    UnityEngine.Debug.LogException(e);
                }

                // It's possible for an async reload operation to reach here after another has
                // been started, i.e. it is not the current builder.
                var isCurrentBuilder = m_BuildModelCancellation == buildModelCancellation;
                if (isCurrentBuilder)
                {
                    // Nullify the source reference if it is the current one.
                    m_BuildModelCancellation = null;

                    if (success == false)
                    {
                        HideContentActivityIndicators();
                        HideContentViewsAndShowNoDataView();
                    }
                }
            }
        }

        void ShowContentViewsAndHideNoDataView()
        {
            SetContentViewsVisible(true);
        }

        void HideContentViewsAndShowNoDataView()
        {
            SetContentViewsVisible(false);
        }

        void SetContentViewsVisible(bool visible)
        {
            UIUtility.SetElementDisplay(m_TopSection, visible);
            UIUtility.SetElementDisplay(m_FrameTimesContainer, visible);
            UIUtility.SetElementDisplay(m_AllocationsContainer, visible);

            UIUtility.SetElementDisplay(m_NoDataLabel, !visible);
        }

        void ShowContentActivityIndicators()
        {
            // The purpose of the delay is to avoid visual flickering when very fast
            // asynchronous operations are queued in quick succession, such as when
            // scrubbing a profiler frame selection. Only when the async builder
            // takes longer than k_DelayMs, will a loading spinner be shown.
            const int k_DelayMs = 100;
            m_BottlenecksViewController.ShowActivityIndicatorAfterDelay(k_DelayMs);
            m_SystemsImpactViewController.ShowActivityIndicatorAfterDelay(k_DelayMs);
            m_FrameTimesSectionViewController.ShowActivityIndicatorAfterDelay(k_DelayMs);
            m_AllocationsSectionViewController.ShowActivityIndicatorAfterDelay(k_DelayMs);
        }

        void HideContentActivityIndicators()
        {
            m_BottlenecksViewController.SetActivityIndicatorVisible(false);
            m_SystemsImpactViewController.SetActivityIndicatorVisible(false);
            m_FrameTimesSectionViewController.SetActivityIndicatorVisible(false);
            m_AllocationsSectionViewController.SetActivityIndicatorVisible(false);
        }
    }
}
