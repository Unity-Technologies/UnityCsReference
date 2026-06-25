// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling.Editor.UI;
using UnityEditor;
using UnityEditorInternal.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor
{
    internal class ChartViewController : ViewController
    {
        const string k_UxmlIdentifier_Uxml = "ProfilerChartView.uxml";
        const string k_UxmlIdentifier_LegendItemUxml = "ProfilerChartViewLegendItem.uxml";
        const string k_UxmlIdentifier_UssClass_Dark = "profiler-chart-view__dark";
        const string k_UxmlIdentifier_UssClass_Light = "profiler-chart-view__light";
        const string k_UxmlIdentifier_UssClass_Active = "profiler-chart-view__active";
        const string k_UxmlIdentifier_Header = "profiler-chart-view__legend__header";
        const string k_UxmlIdentifier_HeaderLabel = "profiler-chart-view__legend__header__label";
        const string k_UxmlIdentifier_HeaderIcon = "profiler-chart-view__legend__header__icon";
        const string k_UxmlIdentifier_Warning = "profiler-chart-view__legend__header__warning";
        const string k_UxmlIdentifier_Legend = "profiler-chart-view__legend__series";
        const string k_UxmlIdentifier_LegendItem_Toggle = "profiler-chart-view__legend__series-item__toggle";
        const string k_UxmlIdentifier_LegendItem_Icon = "profiler-chart-view__legend__series-item__icon";
        const string k_UxmlIdentifier_LegendItem_Label = "profiler-chart-view__legend__series-item__name";
        const string k_UxmlIdentifier_Chart = "profiler-chart-view__chart";
        const string k_UxmlIdentifier_Chart_Selection = "profiler-chart-view__chart__selection";
        const string k_UxmlIdentifier_Chart_Mesh = "profiler-chart-view__chart__mesh";

        readonly ProfilerModule m_Module;
        readonly ProfilerModuleChartType m_ChartType;
        readonly ChartModel m_Model;

        bool m_Selected;

        VisualElement m_Header;
        Label m_HeaderLabel;
        Image m_HeaderIcon;
        Image m_WarningIcon;
        Image m_PinIndicatorIcon;
        ListView m_Legend;
        VisualElement m_Chart;
        VisualElement m_FrameSelection;
        ChartSelectionManipulator m_ChartSelectionManipulator;
        ChartSelectionLabelsWidget m_ChartSelectionLabels;
        ChartGuidesWidget m_ChartGuides;
        ChartWidget m_ChartWidget;
        FrameWarningsOverlayWidget m_MissingFramesWidget;
        SelectedMarkerOverlayWidget m_SelectedMarkerOverlay;

        long m_CurrentFrame;
        int[] m_LegendItems;

        public ChartViewController(ProfilerModule module, ProfilerModuleChartType type, ChartModel model)
        {
            m_Module = module;
            m_ChartType = type;
            m_Model = model;

            m_Selected = false;
            m_CurrentFrame = -1;
        }

        public virtual void Clear()
        {
            m_CurrentFrame = -1;
        }

        internal long CurrentFrame => m_CurrentFrame;

        public Action ModuleSelected { get; set; }
        public Action CountersEnabledStateChanged { get; set; }
        public Action CountersOrderChanged { get; set; }
        public Action<bool> SeriesEnabledStateChanged { get; set; }
        public Action<int> SelectedFrameChanged { get; set; }
        public Func<int, string> StatisticsAvailabilityMessageFactory { get; set; }
        public VisualElement Chart => m_Chart;
        public Action OnViewLoaded { get; set; }

        public void Update()
        {
            if (!m_Module.active)
                return;

            m_Chart.MarkDirtyRepaint();

            m_ChartGuides.Update();
            m_MissingFramesWidget.Update();

            // Update frame selector style
            UpdateSelection();
        }

        public virtual void SetActiveState(bool active)
        {
            UIUtility.SetElementDisplay(View, active);

            if (active)
                Update();
        }

        public void SetSelected(bool selected)
        {
            m_Selected = selected;

            // Chart highlight
            if (selected)
                View.AddToClassList(k_UxmlIdentifier_UssClass_Active);
            else
                View.RemoveFromClassList(k_UxmlIdentifier_UssClass_Active);

            // Prevent frame selection manipulator from consuming events
            // if module isn't selected
            m_ChartSelectionManipulator.Disabled = !selected;
        }

        public void NotifySelectedFrameIndexChanged(long selectedFrameIndex)
        {
            m_CurrentFrame = selectedFrameIndex;
            UpdateSelection();
        }

        protected override VisualElement LoadView()
        {
            var view = ViewControllerUtility.LoadVisualTreeFromBuiltInUxml(k_UxmlIdentifier_Uxml);
            if (view == null)
                throw new InvalidViewDefinedInUxmlException();

            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UxmlIdentifier_UssClass_Dark : k_UxmlIdentifier_UssClass_Light;
            view.AddToClassList(themeUssClass);

            return view;
        }

        protected override void ViewLoaded()
        {
            base.ViewLoaded();

            // Legend
            m_Header = View.Q(k_UxmlIdentifier_Header);
            m_HeaderLabel = View.Q<Label>(k_UxmlIdentifier_HeaderLabel);
            m_HeaderIcon = View.Q<Image>(k_UxmlIdentifier_HeaderIcon);
            m_WarningIcon = View.Q<Image>(k_UxmlIdentifier_Warning);
            m_Legend = View.Q<ListView>(k_UxmlIdentifier_Legend);
            MakeLegend();

            m_Header.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));

            m_PinIndicatorIcon = new Image
            {
                image = EditorGUIUtility.LoadIcon("SceneTemplate/pin"),
                scaleMode = ScaleMode.ScaleToFit,
                tooltip = "Pinned to top. Right-click to unpin.",
                style =
                {
                    width = 12,
                    height = 12,
                    flexShrink = 0,
                    marginLeft = 4,
                    marginRight = 2
                }
            };
            // After header icon (index 1), before header label (index 2). Clamp against
            // childCount so a future UXML edit that prunes a header child can't trip a throw.
            m_Header.Insert(Mathf.Min(2, m_Header.childCount), m_PinIndicatorIcon);
            UpdatePinIndicator();

            // Chart graph
            m_Chart = View.Q(k_UxmlIdentifier_Chart_Mesh);
            m_Chart.RegisterCallback<GeometryChangedEvent>((x) => {
                if (!m_Module.active)
                    return;
                if (m_CurrentFrame == -1)
                {
                    var domain = m_Model.GetDataDomain();
                    var domainSpan = (int)(domain.y - domain.x);
                    var frameIndex = m_Model.chartDomainOffset + domainSpan;
                    m_ChartSelectionLabels.UpdateChartLabels(frameIndex, m_Chart.layout);
                }
                else
                    m_ChartSelectionLabels.UpdateChartLabels(m_CurrentFrame, x.newRect);
            });
            m_ChartSelectionManipulator = new ChartSelectionManipulator(SelectFrameByPosition);
            var chartContainer = View.Q(k_UxmlIdentifier_Chart);
            chartContainer.AddManipulator(m_ChartSelectionManipulator);
            MakeChartGeometry();

            // Chart frame selection
            m_FrameSelection = View.Q(k_UxmlIdentifier_Chart_Selection);

            m_ChartGuides = new ChartGuidesWidget(m_Model, m_Chart);
            m_ChartSelectionLabels = new ChartSelectionLabelsWidget(m_ChartType, m_Model, m_Chart);
            m_MissingFramesWidget = new FrameWarningsOverlayWidget(m_Model, m_Chart, StatisticsAvailabilityMessageFactory);
            if (m_Module is CPUProfilerModule)
                m_SelectedMarkerOverlay = new SelectedMarkerOverlayWidget(m_Chart);

            View.RegisterCallback<PointerUpEvent>(ModuleActivationCallback);
            View.RegisterCallback<KeyDownEvent>(SelectionManipulationCallback);

            OnViewLoaded?.Invoke();
        }

        void UpdateSelection()
        {
            if (!m_Module.active)
                return;

            var domainSize = m_Model.GetDataDomainLength();
            var frameIndex = m_CurrentFrame;
            var frame = m_CurrentFrame - m_Model.chartDomainOffset;

            if (m_CurrentFrame == -1)
            {
                var domain = m_Model.GetDataDomain();
                var domainSpan = (int)(domain.y - domain.x);
                frameIndex = m_Model.chartDomainOffset + domainSpan;
                frame = frameIndex - m_Model.chartDomainOffset;
            }

            m_FrameSelection.style.left = new Length(100.0f * frame / domainSize, LengthUnit.Percent);
            m_FrameSelection.style.width = new Length(100.0f / domainSize, LengthUnit.Percent);
            UIUtility.SetElementDisplay(m_FrameSelection, m_Model.firstSelectableFrame != -1);
            m_ChartSelectionLabels.UpdateChartLabels(frameIndex, m_Chart.layout);
        }

        public void SetWarningIconVisible(bool visibility, string message)
        {
            UIUtility.SetElementDisplay(m_WarningIcon, visibility);
            m_WarningIcon.tooltip = message;
        }

        public void SetSelectedMarkerOverlay(string selectionText, string tooltipText)
        {
            m_SelectedMarkerOverlay?.Update(selectionText, tooltipText);
        }

        public void ClearSelectedMarkerOverlay()
        {
            m_SelectedMarkerOverlay?.Clear();
        }

        void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            if (m_Module.pinned)
            {
                evt.menu.AppendAction(L10n.Tr("Unpin from Top"),
                    _ => m_Module.pinned = false,
                    DropdownMenuAction.AlwaysEnabled);
            }
            else
            {
                var atLimit = m_Module.ProfilerWindow.GetPinnedModuleCount() >= ProfilerWindow.k_MaximumPinnedModules;
                evt.menu.AppendAction(L10n.Tr("Pin to Top"),
                    _ => m_Module.pinned = true,
                    atLimit ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
            }
        }

        void UpdatePinIndicator()
        {
            UIUtility.SetElementDisplay(m_PinIndicatorIcon, m_Module.pinned);
        }

        public void NotifyPinnedStateChanged()
        {
            UpdatePinIndicator();
        }

        void MakeLegend()
        {
            m_Header.tooltip = m_Model.Tooltip + "\n\nRight-click to pin/unpin this module.";

            m_HeaderLabel.text = m_Model.Header;
            m_HeaderLabel.tooltip = m_Model.Header;
            m_HeaderIcon.image = EditorGUIUtility.LoadIcon(m_Model.HeaderIconName);

            if (!string.IsNullOrEmpty(m_Model.WarningMsg))
            {
                UIUtility.SetElementDisplay(m_WarningIcon, true);
                m_WarningIcon.tooltip = m_Model.WarningMsg;
            }
            else
                UIUtility.SetElementDisplay(m_WarningIcon, false);

            // Reverse order, as ListView starts at bottom
            m_LegendItems = new int[m_Model.numSeries];
            for (int i = 0; i < m_Model.numSeries; i++)
                m_LegendItems[i] = m_Model.order[m_Model.numSeries - i - 1];

            m_Legend.showBoundCollectionSize = false;
            m_Legend.showFoldoutHeader = false;
            m_Legend.showAddRemoveFooter = false;
            m_Legend.horizontalScrollingEnabled = false;
            m_Legend.fixedItemHeight = 20;
            m_Legend.reorderable = m_ChartType.IsStackedChartType();
            m_Legend.selectionType = SelectionType.None;
            m_Legend.reorderMode = m_ChartType.IsStackedChartType() ? ListViewReorderMode.Animated : ListViewReorderMode.Simple;
            m_Legend.itemsSource = m_LegendItems;
            m_Legend.makeItem = MakeLegendItem;
            m_Legend.bindItem = BindLegendItem;
            m_Legend.unbindItem = UnbindLegendItem;
            m_Legend.itemIndexChanged += ReorderLegendItem;
        }

        VisualElement MakeLegendItem()
        {
            return ViewControllerUtility.LoadVisualTreeFromBuiltInUxml(k_UxmlIdentifier_LegendItemUxml);
        }

        void BindLegendItem(VisualElement item, int index)
        {
            var seriesIndex = m_LegendItems[index];

            var name = m_Model.series[seriesIndex].name;
            item.Q<Label>(k_UxmlIdentifier_LegendItem_Label).text = name;
            var description = m_Model.series[seriesIndex].description;
            item.tooltip = string.IsNullOrEmpty(description) ? name : description;

            var counterToggle = item.Q<VisualElement>(k_UxmlIdentifier_LegendItem_Toggle);
            counterToggle.userData = seriesIndex;
            counterToggle.pickingMode = PickingMode.Position;
            UpdateCounterToggleVisualState(counterToggle, seriesIndex);
            counterToggle.RegisterCallback<MouseUpEvent>(CounterToggleEnableCallback, TrickleDown.TrickleDown);
        }

        void UnbindLegendItem(VisualElement item, int index)
        {
            var seriesIndex = m_LegendItems[index];
            var counterToggle = item.Q<VisualElement>(k_UxmlIdentifier_LegendItem_Toggle);
            counterToggle.UnregisterCallback<MouseUpEvent>(CounterToggleEnableCallback, TrickleDown.TrickleDown);
        }

        unsafe void ReorderLegendItem(int l, int r)
        {
            for (int i = 0; i < m_Model.numSeries; i++)
                m_Model.order[i] = m_LegendItems[m_Model.numSeries - i - 1];

            Update();
        }

        void CounterToggleEnableCallback(MouseUpEvent evt)
        {
            var ve = evt.currentTarget as VisualElement;
            var seriesIndex = ve.userData as int?;
            if (!seriesIndex.HasValue)
                return;

            m_Model.series[seriesIndex.Value].enabled = !m_Model.series[seriesIndex.Value].enabled;
            CountersEnabledStateChanged?.Invoke();
            UpdateCounterToggleVisualState(ve, seriesIndex.Value);
            m_Module.Update();
        }

        void UpdateCounterToggleVisualState(VisualElement toggle, int seriesIndex)
        {
            var state = m_Model.series[seriesIndex].enabled;
            var counterIcon = toggle.Q<VisualElement>(k_UxmlIdentifier_LegendItem_Icon);
            counterIcon.style.backgroundColor = state ? m_Model.series[seriesIndex].color : Color.black;
        }

        void ModuleActivationCallback(PointerUpEvent evt)
        {
            if (m_Selected)
                return;

            ModuleSelected?.Invoke();
            evt.StopImmediatePropagation();
            return;
        }

        void SelectionManipulationCallback(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.LeftArrow)
            {
                MoveSelectedFrame(-1);
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.RightArrow)
            {
                MoveSelectedFrame(1);
                evt.StopPropagation();
            }
        }

        void SelectFrameByPosition(float pos)
        {
            if (m_Model.firstSelectableFrame == -1)
                return;

            var contentRect = m_Chart.contentRect;
            if (contentRect.width == 0)
                return;

            // Convert position from Chart local coordinate to frame index
            var domainSize = m_Model.GetDataDomainLength();
            var frameIndex = Mathf.CeilToInt(pos / contentRect.width * domainSize - 1.0f);
            var domain = m_Model.GetDataDomain();
            frameIndex = Math.Clamp(frameIndex, (int)domain.x, (int)domain.y);
            frameIndex += m_Model.chartDomainOffset;
            if (frameIndex < m_Model.firstSelectableFrame)
                frameIndex = m_Model.firstSelectableFrame;

            // Notify if changed
            if (frameIndex != m_CurrentFrame)
                SelectedFrameChanged?.Invoke(frameIndex);
        }

        void MoveSelectedFrame(int direction)
        {
            var length = m_Model.GetDataDomainLength();
            var newSelectedFrame = m_CurrentFrame + direction;
            if (newSelectedFrame < m_Model.firstSelectableFrame || newSelectedFrame > m_Model.chartDomainOffset + length - 1)
                return;

            // Notify if changed
            SelectedFrameChanged?.Invoke((int)newSelectedFrame);
        }

        void MakeChartGeometry()
        {
            if (m_ChartWidget != null)
                return;

            switch (m_ChartType)
            {
                case ProfilerModuleChartType.Line:
                    m_ChartWidget = new LineChartWidget(m_Model, m_Chart);
                    break;
                case ProfilerModuleChartType.StackedTimeArea:
                case ProfilerModuleChartType.StackedArea:
                    m_ChartWidget = new BarChartWidget(m_Model, m_Chart);
                    break;
            }
        }
    }
}
