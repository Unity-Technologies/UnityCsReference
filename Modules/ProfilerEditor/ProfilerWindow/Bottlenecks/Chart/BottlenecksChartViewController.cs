// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor.UI
{
    class BottlenecksChartViewController : ViewController, BlocksGraphView.IDataSource, BlocksGraphView.IResponder
    {
        const string k_UxmlResourceName = "BottlenecksChartView.uxml";
        const string k_UssClass_Dark = "bottlenecks-chart-view__dark";
        const string k_UssClass_Light = "bottlenecks-chart-view__light";
        const string k_UxmlIdentifier_TitleLabel = "bottlenecks-chart-view__key__title-label";
        const string k_UxmlIdentifier_TargetMenu = "bottlenecks-chart-view__key__target-menu";
        const string k_UxmlIdentifier_BlocksGraphView = "bottlenecks-chart-view__chart__blocks-graph-view";
        const string k_UxmlIdentifier_FrameIndicator = "bottlenecks-chart-view__chart__frame-indicator";

        // Model.
        readonly IProfilerCaptureDataService m_DataService;
        readonly IProfilerPersistentSettingsService m_SettingsService;
        readonly ProfilerWindow m_ProfilerWindow;
        BottlenecksChartViewModel m_Model;

        // View.
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

            m_DataService.NewDataLoadedOrCleared += OnNewDataLoadedOrCleared;
            m_SettingsService.TargetFrameDurationChanged += OnTargetFrameDurationChanged;
            m_SettingsService.MaximumFrameCountChanged += OnMaximumFrameCountChanged;
            m_ProfilerWindow.SelectedFrameIndexChanged += OnNewFrameIndexSelectedInProfilerWindow;
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
            m_TargetMenu.menu.AppendAction("30 FPS", (action) => { SetTargetFramesPerSecondSetting(30); });
            m_TargetMenu.menu.AppendAction("60 FPS", (action) => { SetTargetFramesPerSecondSetting(60); });
            m_TargetMenu.menu.AppendAction("72 FPS", (action) => { SetTargetFramesPerSecondSetting(72); });
            m_TargetMenu.menu.AppendAction("90 FPS", (action) => { SetTargetFramesPerSecondSetting(90); });
            m_TargetMenu.menu.AppendAction("120 FPS", (action) => { SetTargetFramesPerSecondSetting(120); });
            m_TargetMenu.menu.AppendAction("Custom", OpenPreferencesFilteredToFpsSetting);
            m_BlocksGraphView.DataSource = this;
            m_BlocksGraphView.Responder = this;
            UpdateTargetLabelText();

            View.RegisterCallback<GeometryChangedEvent>(ViewPerformedLayout);
            View.RegisterCallback<ClickEvent>(OnViewClicked);
            View.RegisterCallback<KeyDownEvent>(OnKeyDownInView, TrickleDown.TrickleDown);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_ProfilerWindow.SelectedFrameIndexChanged -= OnNewFrameIndexSelectedInProfilerWindow;
                m_SettingsService.MaximumFrameCountChanged -= OnMaximumFrameCountChanged;
                m_SettingsService.TargetFrameDurationChanged -= OnTargetFrameDurationChanged;
                m_DataService.NewDataLoadedOrCleared -= OnNewDataLoadedOrCleared;
                m_Model?.Dispose();
            }

            base.Dispose(disposing);
        }

        void GatherReferencesInView(VisualElement view)
        {
            m_TitleLabel = view.Q<Label>(k_UxmlIdentifier_TitleLabel);
            m_TargetMenu = view.Q<ToolbarMenu>(k_UxmlIdentifier_TargetMenu);
            m_BlocksGraphView = view.Q<BlocksGraphView>(k_UxmlIdentifier_BlocksGraphView);
            m_FrameIndicator = view.Q<VisualElement>(k_UxmlIdentifier_FrameIndicator);
        }

        void ViewPerformedLayout(GeometryChangedEvent evt)
        {
            var unitWidth = ComputeGraphUnitWidth();
            m_BlocksGraphView.UnitWidth = unitWidth;
            m_FrameIndicator.style.width = unitWidth;

            if (m_ProfilerWindow.selectedFrameIndex != -1)
                MoveFrameIndicatorToFrameIndex(Convert.ToInt32(m_ProfilerWindow.selectedFrameIndex));
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
            m_FrameIndicator.style.width = unitWidth;

            // Reload graph.
            ReloadData();

            // Reposition selected frame indicator if necessary.
            MoveFrameIndicatorToFrameIndex(Convert.ToInt32(m_ProfilerWindow.selectedFrameIndex));
        }

        void OnNewFrameIndexSelectedInProfilerWindow(long selectedFrameIndexLong)
        {
            if (!IsViewLoaded)
                return;

            var selectedFrameIndex = Convert.ToInt32(selectedFrameIndexLong);
            MoveFrameIndicatorToFrameIndex(selectedFrameIndex);
        }

        void OnViewClicked(ClickEvent evt)
        {
            var selectedFrameIndex = Convert.ToInt32(m_ProfilerWindow.selectedFrameIndex);
            Responder?.ChartViewSelectedFrameIndex(selectedFrameIndex);
        }

        void OnKeyDownInView(KeyDownEvent evt)
        {
            if (m_DataService.FrameCount == 0)
                return;

            int frameIndex;
            switch (evt.keyCode)
            {
                case KeyCode.LeftArrow:
                    frameIndex = Convert.ToInt32(m_ProfilerWindow.selectedFrameIndex) - 1;
                    evt.StopPropagation();
                    break;

                case KeyCode.RightArrow:
                    frameIndex = Convert.ToInt32(m_ProfilerWindow.selectedFrameIndex) + 1;
                    evt.StopPropagation();
                    break;

                default:
                    return;
            }

            var lastFrameIndex = (m_DataService.FirstFrameIndex + m_DataService.FrameCount) - 1;
            frameIndex = Math.Clamp(frameIndex, m_DataService.FirstFrameIndex, lastFrameIndex);
            Responder?.ChartViewSelectedFrameIndex(frameIndex);
        }

        float ComputeGraphUnitWidth()
        {
            return m_BlocksGraphView.contentRect.width / m_Model.DataSeriesCapacity;
        }

        void MoveFrameIndicatorToFrameIndex(int selectedFrameIndex)
        {
            var isFrameSelected = (m_ProfilerWindow.selectedFrameIndex != -1);
            UIUtility.SetElementDisplay(m_FrameIndicator, isFrameSelected);

            if (isFrameSelected)
            {
                var unitWidth = ComputeGraphUnitWidth();
                var localFrameIndex = selectedFrameIndex - m_Model.FirstFrameIndex;
                var left = unitWidth * localFrameIndex;
                m_FrameIndicator.style.left = left;
            }
        }

        int BlocksGraphView.IDataSource.NumberOfDataSeriesForGraphView()
        {
            return m_Model.NumberOfDataSeries;
        }

        Color BlocksGraphView.IDataSource.ColorForDataSeriesInGraphView(int dataSeriesIndex)
        {
            return m_Model.Colors[dataSeriesIndex];
        }

        Color BlocksGraphView.IDataSource.InvalidColorForDataSeriesInGraphView()
        {
            return m_Model.InvalidColor;
        }

        float BlocksGraphView.IDataSource.DataValueThresholdInGraphView()
        {
            return m_Model.BottleneckThreshold;
        }

        int BlocksGraphView.IDataSource.LengthForEachDataSeriesInGraphView()
        {
            return m_Model.DataSeriesCapacity;
        }

        NativeSlice<float> BlocksGraphView.IDataSource.ValuesForDataSeriesInGraphView(int dataSeriesIndex)
        {
            return m_Model.DataValueBuffers[dataSeriesIndex];
        }

        void BlocksGraphView.IResponder.GraphViewSelectedUnit(int unit)
        {
            if (m_DataService.FrameCount == 0)
                return;

            var selectedFrameIndex = m_Model.FirstFrameIndex + unit;
            var lastFrameIndex = (m_DataService.FirstFrameIndex + m_DataService.FrameCount) - 1;
            selectedFrameIndex = Math.Clamp(selectedFrameIndex, m_DataService.FirstFrameIndex,  lastFrameIndex);
            Responder?.ChartViewSelectedFrameIndex(selectedFrameIndex);

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
            void ChartViewSelectedFrameIndex(int frameIndex);
        }
    }
}
