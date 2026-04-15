// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace Unity.Profiling.Editor.UI
{
    class FrameSummaryViewController : SummaryViewController
    {
        static class Content
        {
            public static readonly string k_NoDataText = L10n.Tr("Select a frame from the charts above to see its details here.");
            public static readonly string k_MainThreadUtilizationTitle = L10n.Tr("Main thread utilization");
            public static readonly string k_SystemsImpactTitle = L10n.Tr("Systems impact in frame");
        }

        // Model.
        FrameBottlenecksModel m_FrameBottlenecksModel;

        // Children.
        PieChartViewController m_MainThreadUtilizationViewController;
        SingleFrameTimesSectionViewController m_SingleFrameTimesSectionViewController;
        FrameAllocationsSectionViewController m_FrameAllocationsSectionViewController;

        public FrameSummaryViewController(
            IProfilerCaptureDataService dataService,
            IProfilerPersistentSettingsService settingsService,
            ProfilerWindow profilerWindow,
            IResponder responder,
            IDetailsElementBinder detailsBinder) : base(dataService, settingsService, profilerWindow, responder, detailsBinder)
        {

        }

        public void ReloadData(int frameIndex, Action<IDetailsProvider> onDetailsProviderReady = null)
        {
            UnityEngine.Debug.Assert(IsViewLoaded);

            // If we have no data, show the no data view.
            if (m_DataService.FrameCount == 0 || frameIndex < m_DataService.FirstFrameIndex)
            {
                HideContentViewsAndShowNoDataView();
                m_SelectedRange = new Range(0, 0);
                onDetailsProviderReady?.Invoke(null);
                return;
            }

            m_SelectedRange = new Range(frameIndex, frameIndex);
            ReloadDataAsync(m_SelectedRange, onDetailsProviderReady);
        }

        public void CancelReloadDataIfNecessary()
        {
            m_BuildModelCancellation?.Cancel();
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            m_NoDataLabel.text = Content.k_NoDataText;

            // Embed child view controllers.
            m_MainThreadUtilizationViewController = new PieChartViewController(Content.k_MainThreadUtilizationTitle);
            m_BottlenecksContainer.Add(m_MainThreadUtilizationViewController.View);
            AddChild(m_MainThreadUtilizationViewController);

            m_SystemsImpactViewController = new SystemsImpactViewController(m_DataService, Content.k_SystemsImpactTitle, m_ProfilerWindow, m_DetailsBinder);
            m_SystemsImpactContainer.Add(m_SystemsImpactViewController.View);
            AddChild(m_SystemsImpactViewController);

            m_SingleFrameTimesSectionViewController = new SingleFrameTimesSectionViewController(
                m_ProfilerWindow,
                m_SettingsService,
                this,
                m_DetailsBinder);
            m_FrameTimesContainer.Add(m_SingleFrameTimesSectionViewController.View);
            AddChild(m_SingleFrameTimesSectionViewController);

            m_FrameAllocationsSectionViewController = new FrameAllocationsSectionViewController(m_ProfilerWindow, this, m_DetailsBinder);
            m_AllocationsContainer.Add(m_FrameAllocationsSectionViewController.View);
            AddChild(m_FrameAllocationsSectionViewController);
        }

        protected override async Task BuildModelAsync(Range range, CancellationToken cancellationToken)
        {
            var frameIndex = range.Start.Value;
            var modelBuilder = new FrameSummaryModelBuilder(
                m_DataService,
                frameIndex);
            await modelBuilder.BuildAsync(
                cancellationToken,
                OnMainThreadUtilizationCompleted: m_MainThreadUtilizationViewController.RefreshView,
                OnSystemsImpactBuildCompleted: m_SystemsImpactViewController.ReloadData,
                OnFrameBottlenecksBuildCompleted: (model) => {
                    m_FrameBottlenecksModel = model;
                    m_SingleFrameTimesSectionViewController.RefreshFrameBottlenecksView(model);
                },
                OnTopFrameMarkersBuildCompleted: m_SingleFrameTimesSectionViewController.RefreshTopFrameMarkersView,
                OnFrameGCAllocationsBuildCompleted: m_FrameAllocationsSectionViewController.RefreshFrameGCAllocationsView,
                OnTopGCMarkersBuildCompleted: m_FrameAllocationsSectionViewController.RefreshTopGCMarkersView,
                OnFrameGCCollectBuildCompleted: m_FrameAllocationsSectionViewController.RefreshFrameGCCollectView
            );
        }

        protected override IDetailsProvider CreateDetailsProvider(Range range)
        {
            return new FrameSummaryDetailsProvider(
                m_ProfilerWindow,
                m_DataService,
                m_SettingsService,
                range,
                m_FrameBottlenecksModel.CpuDurationNs,
                m_FrameBottlenecksModel.GpuDurationNs);
        }

        protected override void ShowContentActivityIndicators()
        {
            // The purpose of the delay is to avoid visual flickering when very fast
            // asynchronous operations are queued in quick succession, such as when
            // scrubbing a profiler frame selection. Only when the async builder
            // takes longer than k_DelayMs, will a loading spinner be shown.
            const int k_DelayMs = 100;
            m_MainThreadUtilizationViewController.ShowActivityIndicatorAfterDelay(k_DelayMs);
            m_SystemsImpactViewController.ShowActivityIndicatorAfterDelay(k_DelayMs);
            m_SingleFrameTimesSectionViewController.ShowActivityIndicatorAfterDelay(k_DelayMs);
            m_FrameAllocationsSectionViewController.ShowActivityIndicatorAfterDelay(k_DelayMs);
        }

        protected override void HideContentActivityIndicators()
        {
            m_MainThreadUtilizationViewController.SetActivityIndicatorVisible(false);
            m_SystemsImpactViewController.SetActivityIndicatorVisible(false);
            m_SingleFrameTimesSectionViewController.SetActivityIndicatorVisible(false);
            m_FrameAllocationsSectionViewController.SetActivityIndicatorVisible(false);
        }
    }

    class FrameSummaryDetailsProvider : SummaryDetailsProvider
    {
        readonly ulong m_CpuTimeNs;
        readonly ulong m_GpuTimeNs;

        public FrameSummaryDetailsProvider(
            ProfilerWindow profilerWindow,
            IProfilerCaptureDataService dataService,
            IProfilerPersistentSettingsService settingsService,
            Range frameRange,
            ulong cpuTimeNs,
            ulong gpuTimeNs)
            : base(profilerWindow, dataService, settingsService, frameRange)
        {
            m_CpuTimeNs = cpuTimeNs;
            m_GpuTimeNs = gpuTimeNs;
        }

        public override ViewController GetDetailsViewController(IProfilerCaptureDataService dataService)
        {
            return new SummaryDetailsPanelViewController(
                m_ProfilerWindow,
                m_DataService,
                m_SettingsService,
                m_FrameRange,
                m_CpuTimeNs,
                m_GpuTimeNs);
        }
    }
}
