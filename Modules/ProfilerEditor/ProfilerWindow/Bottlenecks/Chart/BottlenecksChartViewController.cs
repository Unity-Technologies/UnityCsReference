// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEditor.Accessibility;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    class BottlenecksChartViewController : ViewController, BlocksGraphViewRender.IDataSource, BlocksGraphView.IResponder
    {
        const string k_UxmlResourceName = "BottlenecksChartView.uxml";
        const string k_UssClass_Dark = "bottlenecks-chart-view__dark";
        const string k_UssClass_Light = "bottlenecks-chart-view__light";

        internal static readonly IReadOnlyList<int> k_FPSValues = new[] { 30, 60, 72, 90, 120 };

        // Model.
        readonly IProfilerCaptureDataService m_DataService;
        readonly IProfilerPersistentSettingsService m_SettingsService;
        readonly ProfilerWindow m_ProfilerWindow;
        BottlenecksChartViewModel m_Model;

        // View.
        VisualElement m_KeyContainer;
        Label m_TitleLabel;
        ToolbarMenu m_TargetMenu;
        BlocksGraphView m_BlocksGraphView;
        VisualElement m_FrameIndicator;

        // Tooltip.
        // TODO Consider a proper way to have charts provide tooltips to the parent view controller once we refactor all charts to have tooltips like this (and have a parent view controller to manage the list of charts).
        VisualElement m_TooltipContainer;
        BottlenecksChartTooltipViewController m_TooltipViewController;

        public BottlenecksChartViewController(
            IProfilerCaptureDataService dataService,
            IProfilerPersistentSettingsService settingsService,
            ProfilerWindow profilerWindow,
            IResponder responder,
            VisualElement tooltipContainer)
        {
            m_DataService = dataService;
            m_SettingsService = settingsService;
            m_ProfilerWindow = profilerWindow;
            Responder = responder;
            m_TooltipContainer = tooltipContainer;

            m_DataService.DataCleared += OnNewDataLoadedOrCleared;
            m_DataService.DataLoaded += OnNewDataLoadedOrCleared;
            m_SettingsService.TargetFrameDurationChanged += OnTargetFrameDurationChanged;
            m_SettingsService.MaximumFrameCountChanged += OnMaximumFrameCountChanged;
            m_ProfilerWindow.SelectedFrameIndexChanged += OnNewFrameIndexSelectedInProfilerWindow;
            UserAccessiblitySettings.colorBlindConditionChanged += OnColorBlindSettingChanged;
        }

        public IResponder Responder { get; }

        public void ReloadData()
        {
            if (!IsViewLoaded)
                return;

            var targetFrameDurationNs = m_SettingsService.TargetFrameDurationNs;
            var modelBuilder = new BottlenecksChartViewModelBuilder(m_DataService, m_SettingsService, targetFrameDurationNs);
            modelBuilder.UpdateModel(ref m_Model);

            m_BlocksGraphView.MarkDirtyRepaint();
        }

        public bool SaveHighlightsInfo(string filename)
        {
            return m_Model.ToFile(filename, ProfilerDriver.lastFrameIndex - ProfilerDriver.firstFrameIndex + 1);
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml(k_UxmlResourceName);
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            view.AddToClassList(themeUssClass);

            GatherReferencesInView(view);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            var targetFrameDurationNs = m_SettingsService.TargetFrameDurationNs;
            var modelBuilder = new BottlenecksChartViewModelBuilder(m_DataService, m_SettingsService, targetFrameDurationNs);
            m_Model = modelBuilder.Build();

            m_TitleLabel.text = L10n.Tr("Highlights");
            foreach (var fpsValue in k_FPSValues)
            {
                m_TargetMenu.menu.AppendAction($"{fpsValue} FPS", (action) => { SetTargetFramesPerSecondSetting(fpsValue); });
            }
            m_TargetMenu.menu.AppendAction("Custom", OpenPreferencesFilteredToFpsSetting);
            m_BlocksGraphView.DataSource = this;
            m_BlocksGraphView.Responder = this;
            UpdateTargetLabelText();

            m_KeyContainer.RegisterCallback<ClickEvent>(OnKeyContainerClicked);
            View.RegisterCallback<GeometryChangedEvent>(ViewPerformedLayout);
            View.RegisterCallback<KeyDownEvent>(OnKeyDownInView, TrickleDown.TrickleDown);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UserAccessiblitySettings.colorBlindConditionChanged -= OnColorBlindSettingChanged;
                m_ProfilerWindow.SelectedFrameIndexChanged -= OnNewFrameIndexSelectedInProfilerWindow;
                m_SettingsService.MaximumFrameCountChanged -= OnMaximumFrameCountChanged;
                m_SettingsService.TargetFrameDurationChanged -= OnTargetFrameDurationChanged;
                m_DataService.DataLoaded -= OnNewDataLoadedOrCleared;
                m_DataService.DataCleared -= OnNewDataLoadedOrCleared;
                m_Model?.Dispose();
            }

            base.Dispose(disposing);
        }

        void GatherReferencesInView(VisualElement view)
        {
            const string k_UxmlIdentifier_KeyContainer = "bottlenecks-chart-view__key";
            const string k_UxmlIdentifier_TitleLabel = "bottlenecks-chart-view__key__title-label";
            const string k_UxmlIdentifier_TargetMenu = "bottlenecks-chart-view__key__target-menu";
            const string k_UxmlIdentifier_BlocksGraphView = "bottlenecks-chart-view__chart__blocks-graph-view";
            const string k_UxmlIdentifier_FrameIndicator = "bottlenecks-chart-view__chart__frame-indicator";

            m_KeyContainer = view.Q<VisualElement>(k_UxmlIdentifier_KeyContainer);
            m_TitleLabel = view.Q<Label>(k_UxmlIdentifier_TitleLabel);
            m_TargetMenu = view.Q<ToolbarMenu>(k_UxmlIdentifier_TargetMenu);
            m_BlocksGraphView = view.Q<BlocksGraphView>(k_UxmlIdentifier_BlocksGraphView);
            m_FrameIndicator = view.Q<VisualElement>(k_UxmlIdentifier_FrameIndicator);
        }

        void ViewPerformedLayout(GeometryChangedEvent evt)
        {
            var unitWidth = ComputeGraphUnitWidth();
            m_BlocksGraphView.UnitWidth = unitWidth;

            if (m_ProfilerWindow.SelectedFrameRange != null)
                MoveFrameIndicatorToFrameRange(m_ProfilerWindow.SelectedFrameRange);
        }

        void SetTargetFramesPerSecondSetting(int targetFramesPerSecond)
        {
            var targetFrameDurationNs = Convert.ToUInt64(Math.Round(1e9 / targetFramesPerSecond));
            m_SettingsService.TargetFrameDurationNs = targetFrameDurationNs;
        }

        void OpenPreferencesFilteredToFpsSetting(DropdownMenuAction _)
        {
            var settingsWindow = SettingsWindow.Show(SettingsScope.User, "Preferences/Analysis/Profiler");
            settingsWindow.FilterProviders("Target Frames Per Second");
        }

        void UpdateTargetLabelText()
        {
            var targetFramesPerSecond = Mathf.RoundToInt(1e9f / m_SettingsService.TargetFrameDurationNs);
            m_TargetMenu.text = $"Target Frame Time: {targetFramesPerSecond} FPS";
        }

        void OnNewDataLoadedOrCleared()
        {
            ReloadData();
        }

        void OnTargetFrameDurationChanged()
        {
            m_Model.BottleneckThreshold = m_SettingsService.TargetFrameDurationNs;

            m_BlocksGraphView.MarkDirtyRepaint();
            UpdateTargetLabelText();
        }

        void OnMaximumFrameCountChanged()
        {
            // Dispose the old model.
            m_Model?.Dispose();

            // Rebuild model with the new capacity.
            var targetFrameDurationNs = m_SettingsService.TargetFrameDurationNs;
            var modelBuilder = new BottlenecksChartViewModelBuilder(m_DataService, m_SettingsService, targetFrameDurationNs);
            m_Model = modelBuilder.Build();

            // Re-configure graph view as unit width has changed.
            var unitWidth = ComputeGraphUnitWidth();
            m_BlocksGraphView.UnitWidth = unitWidth;

            // Reload graph.
            ReloadData();

            // Reposition selected frame indicator if necessary.
            MoveFrameIndicatorToFrameRange(m_ProfilerWindow.SelectedFrameRange);
        }

        void OnNewFrameIndexSelectedInProfilerWindow(long selectedFrameIndexLong)
        {
            if (!IsViewLoaded)
                return;

            MoveFrameIndicatorToFrameRange(m_ProfilerWindow.SelectedFrameRange);
        }

        void OnColorBlindSettingChanged()
        {
            if (!IsViewLoaded)
                return;

            m_BlocksGraphView?.MarkDirtyRepaint();
        }

        // This allows the user to click on the key to switch modules.
        void OnKeyContainerClicked(ClickEvent evt)
        {
            var existingSelection = m_ProfilerWindow.SelectedFrameRange;
            Responder?.ChartViewSelectedFrameRange(existingSelection);
        }

        void OnKeyDownInView(KeyDownEvent evt)
        {
            if (m_DataService.FrameCount == 0)
                return;

            var selectionRange = m_ProfilerWindow.SelectedFrameRange;
            if (selectionRange == null)
                return;

            // Shuffle the selection left and right; don't allow the
            // selection to shift beyond the boundaries.
            int frameShift;
            switch (evt.keyCode)
            {
                case KeyCode.LeftArrow:
                    frameShift = -1;
                    evt.StopPropagation();
                    break;

                case KeyCode.RightArrow:
                    frameShift = 1;
                    evt.StopPropagation();
                    break;

                default:
                    return;
            }

            var startFrameIndex = selectionRange.Value.Start.Value + frameShift;
            if (startFrameIndex < m_DataService.FirstFrameIndex)
                return;

            var exclusiveEndFrameIndex = selectionRange.Value.End.Value + frameShift;
            if (exclusiveEndFrameIndex > m_DataService.FirstFrameIndex + m_DataService.FrameCount)
                return;

            Responder?.ChartViewSelectedFrameRange(startFrameIndex..exclusiveEndFrameIndex);
        }

        float ComputeGraphUnitWidth()
        {
            return m_BlocksGraphView.contentRect.width / m_Model.DataSeriesCapacity;
        }

        void MoveFrameIndicatorToFrameRange(Range? frameRange)
        {
            var hasSelection = (frameRange != null);
            UIUtility.SetElementDisplay(m_FrameIndicator, hasSelection);

            if (hasSelection)
            {
                var unitWidth = ComputeGraphUnitWidth();
                var startFrameIndex = frameRange.Value.Start.Value;
                var localStartFrameIndex = startFrameIndex - m_Model.FirstFrameIndex;
                var left = unitWidth * localStartFrameIndex;
                m_FrameIndicator.style.left = left;

                var rangeLength = frameRange.Value.End.Value - startFrameIndex;
                var width = unitWidth * rangeLength;
                m_FrameIndicator.style.width = width;
            }
        }

        int BlocksGraphViewRender.IDataSource.NumberOfDataSeriesForGraphView()
        {
            return m_Model.NumberOfDataSeries;
        }

        Color BlocksGraphViewRender.IDataSource.ColorForDataSeriesInGraphView(int dataSeriesIndex)
        {
            return BottlenecksChartViewModel.GetColorForDataSeries(dataSeriesIndex);
        }

        Color BlocksGraphViewRender.IDataSource.InvalidColorForDataSeriesInGraphView()
        {
            return BottlenecksChartViewModel.InvalidColor;
        }

        float BlocksGraphViewRender.IDataSource.DataValueThresholdInGraphView()
        {
            return m_Model.BottleneckThreshold;
        }

        int BlocksGraphViewRender.IDataSource.LengthForEachDataSeriesInGraphView()
        {
            return m_Model.DataSeriesCapacity;
        }

        NativeSlice<float> BlocksGraphViewRender.IDataSource.ValuesForDataSeriesInGraphView(int dataSeriesIndex)
        {
            return m_Model.DataValueBuffers[dataSeriesIndex];
        }

        public float PercentageFramesOverTarget(int dataSeriesIndex)
        {
            return m_Model.PercentOverThreshold[dataSeriesIndex];
        }

        void BlocksGraphView.IResponder.GraphViewUpdatedPendingSelection(Range unitRange)
        {
            if (m_DataService.FrameCount == 0)
                return;

            // Don't allow range selection if the view is not selected. If the Bottleneck module
            // was not already selected, we do not allow the user to change frame. This matches
            // existing behaviour to help with switching modules without changing frame.
            if (m_ProfilerWindow.IsBottleneckViewVisible() == false)
                return;

            var frameRange = UnitRangeToFrameRange(unitRange);
            MoveFrameIndicatorToFrameRange(frameRange);
        }

        void BlocksGraphView.IResponder.GraphViewSelectedUnitRange(Range unitRange)
        {
            if (m_DataService.FrameCount == 0)
                return;

            var selectedFrameRange = UnitRangeToFrameRange(unitRange);
            Responder?.ChartViewSelectedFrameRange(selectedFrameRange);

            View.Focus();
        }

        void BlocksGraphView.IResponder.GraphViewPointerHoverBegan(int unit, Vector2 position)
        {
            if (m_TooltipViewController != null)
                throw new InvalidOperationException("Tooltip has not been disposed correctly.");

            m_TooltipViewController = new BottlenecksChartTooltipViewController();
            AddChild(m_TooltipViewController);
            m_TooltipContainer.Add(m_TooltipViewController.View);

            UpdateTooltip(unit, position);
        }

        void BlocksGraphView.IResponder.GraphViewPointerHoverMoved(int unit, Vector2 position)
        {
            if (m_TooltipViewController == null)
                return;

            UpdateTooltip(unit, position);
        }

        void BlocksGraphView.IResponder.GraphViewPointerHoverEnded()
        {
            if (m_TooltipViewController == null)
                return;

            RemoveChild(m_TooltipViewController);
            m_TooltipViewController.Dispose();
            m_TooltipViewController = null;
        }

        Range UnitRangeToFrameRange(Range unitRange)
        {
            var model = m_Model;
            var firstProfilerFrameIndex = m_DataService.FirstFrameIndex;
            var exclusiveLastProfilerFrameIndex = firstProfilerFrameIndex + m_DataService.FrameCount;
            var startFrameIndex = Math.Clamp(
                model.FirstFrameIndex + unitRange.Start.Value,
                firstProfilerFrameIndex,
                exclusiveLastProfilerFrameIndex - 1);
            var exclusiveEndFrameIndex = Math.Clamp(
                model.FirstFrameIndex + unitRange.End.Value,
                firstProfilerFrameIndex + 1,
                exclusiveLastProfilerFrameIndex);

            return new Range(startFrameIndex, exclusiveEndFrameIndex);
        }

        void UpdateTooltip(int hoveredUnitIndex, Vector2 position)
        {
            // Clamp hovered unit index.
            hoveredUnitIndex = Math.Max(Math.Min(hoveredUnitIndex, m_Model.DataSeriesCapacity - 1), 0);

            // Update tooltip data.
            var cpuDurations = m_Model.DataValueBuffers[0];
            var cpuDuration = cpuDurations[hoveredUnitIndex];
            var gpuDurations = m_Model.DataValueBuffers[1];
            var gpuDuration = gpuDurations[hoveredUnitIndex];
            var targetFrameDurationNs = m_SettingsService.TargetFrameDurationNs;
            m_TooltipViewController.SetDurations(cpuDuration, gpuDuration, targetFrameDurationNs);

            var positionInContainer = m_TooltipContainer.WorldToLocal(position);

            // Offset the tooltip's position slightly to account for the pointer.
            const int k_TooltipPointerOffset = 12;
            positionInContainer.x += k_TooltipPointerOffset;
            positionInContainer.y += k_TooltipPointerOffset;

            // Clamp the tooltip's position so it never goes outside the container's bounds.
            const int k_TooltipPositionPadding = 16;
            var xMax = m_TooltipContainer.contentRect.xMax - m_TooltipViewController.View.contentRect.width - k_TooltipPositionPadding - k_TooltipPointerOffset;
            var yMax = m_TooltipContainer.contentRect.yMax - m_TooltipViewController.View.contentRect.height - k_TooltipPositionPadding - k_TooltipPointerOffset;
            positionInContainer.x = Mathf.Clamp(positionInContainer.x, k_TooltipPositionPadding, xMax);
            positionInContainer.y = Mathf.Clamp(positionInContainer.y, k_TooltipPositionPadding, yMax);

            m_TooltipViewController.SetPosition(positionInContainer);
        }

        public interface IResponder
        {
            void ChartViewSelectedFrameRange(Range? frameRange);
        }
    }
}
