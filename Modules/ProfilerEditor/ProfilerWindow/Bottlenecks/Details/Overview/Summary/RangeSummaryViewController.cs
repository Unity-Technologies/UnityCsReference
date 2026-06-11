// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace Unity.Profiling.Editor.UI
{
    class RangeSummaryViewController : SummaryViewController
    {
        static class Content
        {
            public static readonly string k_SystemsImpactTitleFormat = L10n.Tr("Systems impact across {0} (mean time)");
            public static readonly string k_CaptureDescriptor = L10n.Tr("capture");
            public static readonly string k_SelectionDescriptor = L10n.Tr("selection");
            public static readonly string k_WholeCaptureNoDataText = L10n.Tr("Record a new capture or load an existing one to see its details here.");
            public static readonly string k_SelectionNoDataText = L10n.Tr("Select a frame from the charts above to see its details here.");
        }

        // Model.
        readonly bool m_RangeIsWholeCapture;
        readonly string m_RangeDescriptor;
        readonly string m_NoDataText;
        RangeBottlenecksModel m_RangeBottlenecksModel;

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
            m_RangeDescriptor = rangeIsWholeCapture ? Content.k_CaptureDescriptor : Content.k_SelectionDescriptor;
            m_NoDataText = rangeIsWholeCapture ? Content.k_WholeCaptureNoDataText : Content.k_SelectionNoDataText;
        }

        public void ReloadData(Range frameRange, Action<IDetailsProvider> onDetailsProviderReady = null)
        {
            UnityEngine.Debug.Assert(IsViewLoaded);

            var hasData = m_DataService.FrameCount != 0;

            // Clamp the frame range to the bounds of available profiler data, if necessary
            if (hasData)
            {
                if (frameRange.Equals(Range.All))
                {
                    // If the range is the whole capture, we want to show data as long as there is at least 1 frame in the capture.
                    frameRange = new Range(m_DataService.FirstFrameIndex, m_DataService.FirstFrameIndex + m_DataService.FrameCount);
                }

                var (start, end) = (frameRange.Start.Value, frameRange.End.Value);
                if (end < m_DataService.FirstFrameIndex || start >= m_DataService.FirstFrameIndex + m_DataService.FrameCount)
                {
                    hasData = false;
                }
                else
                {
                    if (start < m_DataService.FirstFrameIndex)
                        start = m_DataService.FirstFrameIndex;
                    if (end > m_DataService.FirstFrameIndex + m_DataService.FrameCount)
                        end = m_DataService.FirstFrameIndex + m_DataService.FrameCount;
                    frameRange = new Range(start, end);
                }
            }

            // If we have no data, show the no data view.
            if (!hasData)
            {
                HideContentViewsAndShowNoDataView();
                m_SelectedRange = new Range(0, 0);
                if (onDetailsProviderReady != null)
                    View.schedule.Execute(() => onDetailsProviderReady(null));
                return;
            }

            m_SelectedRange = frameRange;
            ReloadDataAsync(frameRange, onDetailsProviderReady);
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

            var systemsImpactTitle = string.Format(Content.k_SystemsImpactTitleFormat, m_RangeDescriptor);
            m_SystemsImpactViewController = new SystemsImpactViewController(m_DataService, systemsImpactTitle, m_ProfilerWindow, m_DetailsBinder);
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

        protected override async Task BuildModelAsync(Range range, CancellationToken cancellationToken)
        {
            var modelBuilder = new RangeSummaryModelBuilder(
                m_DataService,
                range);
            await modelBuilder.BuildAsync(
                cancellationToken,
                OnRangeBottlenecksBuildCompleted: model => DeferIfNotCancelled(() =>
                {
                    m_RangeBottlenecksModel = model;
                    m_BottlenecksViewController.ReloadData(model);
                }, cancellationToken),
                OnSystemsImpactBuildCompleted: model => DeferIfNotCancelled(() => m_SystemsImpactViewController.ReloadData(model), cancellationToken),
                OnFrameTimesBuildCompleted: model => DeferIfNotCancelled(() => m_FrameTimesSectionViewController.RefreshFrameTimesView(model), cancellationToken),
                OnTopFrameMarkersBuildCompleted: model => DeferIfNotCancelled(() => m_FrameTimesSectionViewController.RefreshTopFrameMarkersView(model), cancellationToken),
                OnTopRangeMarkersBuildCompleted: model => DeferIfNotCancelled(() => m_FrameTimesSectionViewController.RefreshTopRangeMarkersView(model), cancellationToken),
                OnGCAllocationsBuildCompleted: model => DeferIfNotCancelled(() => m_AllocationsSectionViewController.RefreshGCAllocationsView(model), cancellationToken),
                OnTopGCMarkersBuildCompleted: model => DeferIfNotCancelled(() => m_AllocationsSectionViewController.RefreshTopGCMarkersView(model), cancellationToken),
                OnGCCollectBuildCompleted: model => DeferIfNotCancelled(() => m_AllocationsSectionViewController.RefreshGCCollectView(model), cancellationToken)
            );
        }

        protected override IDetailsProvider CreateDetailsProvider(Range range)
        {
            return new RangeSummaryDetailsProvider(
                m_ProfilerWindow,
                m_DataService,
                m_SettingsService,
                range,
                m_RangeBottlenecksModel);
        }

        protected override void ShowContentActivityIndicators()
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

        protected override void HideContentActivityIndicators()
        {
            m_BottlenecksViewController.SetActivityIndicatorVisible(false);
            m_SystemsImpactViewController.SetActivityIndicatorVisible(false);
            m_FrameTimesSectionViewController.SetActivityIndicatorVisible(false);
            m_AllocationsSectionViewController.SetActivityIndicatorVisible(false);
        }
    }

    class RangeSummaryDetailsProvider : SummaryDetailsProvider
    {
        readonly RangeBottlenecksModel m_RangeBottlenecksModel;

        public RangeSummaryDetailsProvider(
            ProfilerWindow profilerWindow,
            IProfilerCaptureDataService dataService,
            IProfilerPersistentSettingsService settingsService,
            Range frameRange,
            RangeBottlenecksModel rangeBottlenecksModel)
            : base(profilerWindow, dataService, settingsService, frameRange)
        {
            m_RangeBottlenecksModel = rangeBottlenecksModel;
        }

        public override ViewController GetDetailsViewController(IProfilerCaptureDataService dataService)
        {
            return new RangeSummaryDetailsPanelViewController(
                m_ProfilerWindow,
                m_DataService,
                m_SettingsService,
                m_FrameRange,
                m_RangeBottlenecksModel);
        }
    }

    class RangeSummaryDetailsPanelViewController : SummaryDetailsPanelViewController
    {
        static class RangeContent
        {
            // Range-specific bottleneck descriptions (with target set)
            public static readonly string k_BoundTitleFormat = L10n.Tr("<b>The {0} exceeded your target frame time in this frame range.</b>");
            public static readonly string k_NotBoundTitle = L10n.Tr("<b>The CPU and the GPU are within your target frame time in this frame range.</b>");
        }

        public RangeSummaryDetailsPanelViewController(
            ProfilerWindow profilerWindow,
            IProfilerCaptureDataService dataService,
            IProfilerPersistentSettingsService settingsService,
            Range frameRange,
            RangeBottlenecksModel model)
            : base(profilerWindow, dataService, settingsService, frameRange, cpuTimeNs: 0, gpuTimeNs: 0)
        {
            (_, m_CpuTimeNs) = ComputeAvgAndMax(model.CpuDurationsNs);
            (_, m_GpuTimeNs) = ComputeAvgAndMax(model.GpuDurationsNs);
        }

        static (ulong avg, ulong max) ComputeAvgAndMax(ulong[] values)
        {
            if (values == null || values.Length == 0)
                return (0, 0);

            ulong sum = 0;
            ulong max = 0;
            int nonZeroCount = 0;

            foreach (var value in values)
            {
                // Skip empty/incomplete frames, matching RangeBottlenecksModel.ComputePercentageOfValuesOverBudget pattern
                if (value == 0)
                    continue;

                sum += value;
                if (value > max)
                    max = value;
                nonZeroCount++;
            }

            if (nonZeroCount == 0)
                return (0, 0);

            return (sum / (ulong)nonZeroCount, max);
        }

        protected override string GetBoundTitleFormat()
        {
            return RangeContent.k_BoundTitleFormat;
        }

        protected override string GetNotBoundTitle()
        {
            return RangeContent.k_NotBoundTitle;
        }
    }
}
