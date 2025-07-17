// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    class FrameSummaryViewController : SummaryViewController
    {
        // Model.
        CancellationTokenSource m_BuildModelCancellation;

        // View.
        VisualElement m_TopSection;
        VisualElement m_MainThreadUtilizationContainer;
        VisualElement m_SystemsImpactContainer;
        VisualElement m_FrameTimesContainer;
        VisualElement m_AllocationsContainer;
        Label m_NoDataLabel;

        // Children.
        PieChartViewController m_MainThreadUtilizationViewController;
        SystemsImpactViewController m_SystemsImpactViewController;
        SingleFrameTimesSectionViewController m_SingleFrameTimesSectionViewController;
        FrameAllocationsSectionViewController m_FrameAllocationsSectionViewController;

        public FrameSummaryViewController(
            IProfilerCaptureDataService dataService,
            IProfilerPersistentSettingsService settingsService,
            ProfilerWindow profilerWindow,
            IResponder responder) : base(dataService, settingsService, profilerWindow, responder)
        {

        }

        public void ReloadData(int frameIndex)
        {
            UnityEngine.Debug.Assert(IsViewLoaded);
            ReloadDataAsync(frameIndex);
        }

        public void CancelReloadDataIfNecessary()
        {
            m_BuildModelCancellation?.Cancel();
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml("FrameSummaryView.uxml");
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            const string k_UssClass_Dark = "frame-summary-view__dark";
            const string k_UssClass_Light = "frame-summary-view__light";
            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            GatherReferencesInView(view);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            m_NoDataLabel.text = "Select a frame from the charts above to see its details here.";

            // Embed child view controllers.
            m_MainThreadUtilizationViewController = new PieChartViewController("Main thread utilization");
            m_MainThreadUtilizationContainer.Add(m_MainThreadUtilizationViewController.View);
            AddChild(m_MainThreadUtilizationViewController);

            m_SystemsImpactViewController = new SystemsImpactViewController("Systems impact in frame");
            m_SystemsImpactContainer.Add(m_SystemsImpactViewController.View);
            AddChild(m_SystemsImpactViewController);

            m_SingleFrameTimesSectionViewController = new SingleFrameTimesSectionViewController(
                m_ProfilerWindow,
                m_SettingsService,
                topMarkersResponder: this);
            m_FrameTimesContainer.Add(m_SingleFrameTimesSectionViewController.View);
            AddChild(m_SingleFrameTimesSectionViewController);

            m_FrameAllocationsSectionViewController = new FrameAllocationsSectionViewController(topMarkersResponder: this);
            m_AllocationsContainer.Add(m_FrameAllocationsSectionViewController.View);
            AddChild(m_FrameAllocationsSectionViewController);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_BuildModelCancellation?.Cancel();
            }

            base.Dispose(disposing);
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_TopSection = view.Q<VisualElement>("frame-summary-view__top-section");
            m_MainThreadUtilizationContainer = view.Q<VisualElement>("frame-summary-view__main-thread-utilization-container");
            m_SystemsImpactContainer = view.Q<VisualElement>("frame-summary-view__systems-impact-container");
            m_FrameTimesContainer = view.Q<VisualElement>("frame-summary-view__frame-times-container");
            m_AllocationsContainer = view.Q<VisualElement>("frame-summary-view__allocations-container");
            m_NoDataLabel = view.Q<Label>("frame-summary-view__no-data-label");
        }

        async void ReloadDataAsync(int frameIndex)
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
                    var modelBuilder = new FrameSummaryModelBuilder(
                        m_DataService,
                        frameIndex);
                    await modelBuilder.BuildAsync(
                        buildModelCancellation.Token,
                        OnMainThreadUtilizationCompleted: m_MainThreadUtilizationViewController.RefreshView,
                        OnSystemsImpactBuildCompleted: m_SystemsImpactViewController.ReloadData,
                        OnFrameBottlenecksBuildCompleted: m_SingleFrameTimesSectionViewController.RefreshFrameBottlenecksView,
                        OnTopFrameMarkersBuildCompleted: m_SingleFrameTimesSectionViewController.RefreshTopFrameMarkersView,
                        OnFrameGCAllocationsBuildCompleted: m_FrameAllocationsSectionViewController.RefreshFrameGCAllocationsView,
                        OnTopGCMarkersBuildCompleted: m_FrameAllocationsSectionViewController.RefreshTopGCMarkersView,
                        OnFrameGCCollectBuildCompleted: m_FrameAllocationsSectionViewController.RefreshFrameGCCollectView
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
            m_MainThreadUtilizationViewController.ShowActivityIndicatorAfterDelay(k_DelayMs);
            m_SystemsImpactViewController.ShowActivityIndicatorAfterDelay(k_DelayMs);
            m_SingleFrameTimesSectionViewController.ShowActivityIndicatorAfterDelay(k_DelayMs);
            m_FrameAllocationsSectionViewController.ShowActivityIndicatorAfterDelay(k_DelayMs);
        }

        void HideContentActivityIndicators()
        {
            m_MainThreadUtilizationViewController.SetActivityIndicatorVisible(false);
            m_SystemsImpactViewController.SetActivityIndicatorVisible(false);
            m_SingleFrameTimesSectionViewController.SetActivityIndicatorVisible(false);
            m_FrameAllocationsSectionViewController.SetActivityIndicatorVisible(false);
        }
    }
}
