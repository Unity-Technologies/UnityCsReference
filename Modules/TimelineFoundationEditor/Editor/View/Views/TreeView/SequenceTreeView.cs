// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.Time;
using Unity.Timeline.Foundation.View.Internals;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View
{
    [UxmlElement]
    internal partial class SequenceTreeView : SequenceElement, ISequenceTreeView
    {
        const string k_ControlsBackground = "controlsBackground";
        const string k_HideFoldoutStyle = "hideTreeViewFoldout";
        const string k_ControlsColumnResizer = "controls-column-resizer";

        static readonly TemplateResource k_Template = Internals.UIResources.TemplateFactory.Get<SequenceTreeView>();
        static readonly StylesheetResource k_Stylesheet = Internals.UIResources.StylesheetFactory.Get<SequenceTreeView>();

        public VisualElement ControlsHeader => m_Header.ControlsHeader;
        public VisualElement ContentsHeader => m_Header.ContentsHeader;
        public VisualElement ContentsViewport => m_TreeView.Q<ScrollView>().contentViewport;
        public TimeRangeScroller HorizontalScroller => m_TimeRangeScroller;
        public Scroller VerticalScroller => m_VerticalScroller;

        public ContentLookup ContentLookup => m_ContentManager.Lookup;
        protected IContentCreator ContentCreator => m_ContentManager;
        public ICanvas Canvas => m_CanvasManager;
        public IOverlayManager ContentsOverlay => m_CanvasManager.overlayManager;
        public IOverlayManager ControlsOverlay => m_ControlsOverlay;

        public TimeRange DisplayRange => m_TimeArea.DisplayRange;
        public TimeFormat TimeFormat => m_TimeArea.TimeFormat;
        public FrameRate FrameRate => m_TimeArea.FrameRate;
        public DiscreteTime Time => m_PlayHead.time;

        public event Action<DiscreteTime> TimeChanged;
        public event Action<float> ControlsWidthChanged;

        protected Func<Track, TrackElement> CreateTrackElement { get; set; }
        protected Func<Track, TrackHeaderElement> CreateTrackHeaderElement { get; set; }
        protected Func<Item, ItemElement> CreateItemElement { get; set; }
        protected TimeArea timeArea => m_TimeArea;

        protected ITreeViewDragAndDropHandler dragAndDropHandler { get; set; }

        [UxmlAttribute]
        public bool ShowControls
        {
            get => m_Header.GetControlsVisibility();
            set => m_Header.SetControlsVisibility(value);
        }

        [UxmlAttribute("show-playhead")]
        public bool ShowPlayHead
        {
            get => m_PlayHead.isShown;
            set => SetPlayHeadVisibility(value);
        }

        [UxmlAttribute]
        public float ControlsWidth
        {
            get => m_Header.GetControlsWidth();
            set => m_Header.SetControlsWidth(value);
        }

        public bool Reorderable
        {
            get => m_TreeView.reorderable;
            set => m_TreeView.reorderable = value;
        }

        readonly TreeView m_TreeView;
        readonly TreeViewHeader m_Header;
        readonly VisualElement m_ControlsBackground;
        readonly TimeGrid m_TimeGrid;
        readonly TimeArea m_TimeArea;
        readonly TimeRangeScroller m_TimeRangeScroller;
        readonly CanvasManager m_CanvasManager;
        readonly OverlayManager m_ControlsOverlay;
        protected readonly PlayHeadOverlay m_PlayHead;
        readonly Scroller m_VerticalScroller;
        readonly VisualElement m_HeaderResizeElement;
        readonly HeaderResizeManipulator m_HeaderResizeManipulator;

        ContentManager m_ContentManager;
        SequenceTreeViewController m_Controller;
        Sequence m_CurrentSequence;

        public SequenceTreeView()
        {
            k_Template.CloneInto(this);
            k_Stylesheet.ApplyTo(this);

            m_TreeView = this.Q<TreeView>();
            m_Header = this.Q<TreeViewHeader>();

            m_TreeView.canStartDrag += CanStartDrag;
            m_TreeView.setupDragAndDrop += SetupDragAndDrop;
            m_TreeView.dragAndDropUpdate += DragAndDropUpdate;
            m_TreeView.handleDrop += HandleDrop;
            Reorderable = true;

            m_ControlsBackground = this.Q(k_ControlsBackground);

            var scrollView = m_TreeView.Q<ScrollView>();
            m_VerticalScroller = scrollView.verticalScroller;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;

            m_TimeGrid = this.Q<TimeGrid>();
            m_TimeRangeScroller = this.Q<TimeRangeScroller>();
            m_ControlsOverlay = this.Q<OverlayManager>();
            var contentsOverlayManager = this.Q<CanvasOverlayManager>();
            m_HeaderResizeElement = new VisualElement();
            m_CanvasManager = new CanvasManager(this, contentsOverlayManager);

            ContentsHeader.Add(m_TimeArea = new TimeArea());
            ContentsOverlay.AddOverlay(m_PlayHead = new PlayHeadOverlay());
            m_HeaderResizeManipulator = new HeaderResizeManipulator(m_HeaderResizeElement);

            SetupTreeView();
            SetupControlsResize();
            SetupZoomAndPanManipulators();
            SetupTimeDragManipulator();
        }

        public void SetSequence(Sequence sequence)
        {
            Reset();
            if (sequence != null)
                ScheduleSequenceChange(sequence);
        }

        public void ApplySequenceDiff(SequenceDiff diff)
        {
            m_ContentManager.ProcessSequenceChanges(diff);
            m_Controller.ApplySequenceDiff(diff);
            SetContentDuration(diff.sequence?.duration ?? DiscreteTime.Zero);
            SetFrameRate(diff.sequence?.frameRate ?? FrameRate.k_60Fps);
        }

        public void ApplySelection(SelectionData selectionData)
        {
            m_Controller.ApplySelectionData(selectionData);
            m_ContentManager.ProcessSelectionChanges(selectionData);
        }

        public void SetDisplayRange(TimeRange range)
        {
            m_TimeArea.SetDisplayRangeWithoutNotify(range);
            m_TimeGrid.SetTimeRange(range);
            m_TimeRangeScroller.SetRange(range);
            m_CanvasManager.SetDisplayRange(range);
        }

        public void SetContentDuration(DiscreteTime duration)
        {
            m_TimeRangeScroller.SetContentDuration(duration);
        }

        public void SetTimeFormat(TimeFormat timeFormat)
        {
            m_TimeArea.TimeFormat = timeFormat;
            m_CanvasManager.SetTimeFormat(timeFormat);
        }

        public void SetDisplayTransform(TimeTransform displayTransform)
        {
            m_TimeArea.DisplayRangeTransform = displayTransform;
            m_CanvasManager.SetDisplayTransform(displayTransform);
        }

        public void SetFrameRate(FrameRate frameRate)
        {
            m_TimeArea.FrameRate = frameRate;
            m_TimeGrid.SetFrameRate(frameRate);
            m_CanvasManager.SetFrameRate(frameRate);
        }

        public void SetTime(DiscreteTime time)
        {
            m_PlayHead.time = time;
            m_TimeRangeScroller.SetCurrentTime(time);
        }

        public void SetSnapToFrame(bool snapToFrame)
        {
            m_CanvasManager.snapToFrame = snapToFrame;
        }

        public IReadOnlyList<Track> GetVisibleTracksInViewport()
        {
            var visibleTracks = new List<Track>();
            //using a query here to avoid tracks in cache that are no longer in the hierarchy
            //querying visible tracks to filter out tracks in collapsed groups
            Rect worldRect = worldBound;
            foreach (TrackElement trackHeader in this.Query<TrackElement>().Visible().Build())
            {
                //track could be in hierarchy and visible (styling) and not be actually visible in the viewport
                if(worldRect.Contains(trackHeader.worldBound.position))
                    visibleTracks.Add(trackHeader.track);
            }
            return visibleTracks;
        }

        public void Refresh()
        {
            m_TreeView.RefreshItems();
        }

        public void Rebuild() => m_TreeView.Rebuild();

        protected void SetHiddenTracks(params Track[] tracks) => m_Controller?.SetHiddenTracks(tracks);

        Stack GetItemDataForId(int id)
        {
            Stack itemDataForId = m_TreeView.GetItemDataForId<Track>(id);
            if (itemDataForId == null)
            {
                return ViewModel?.sequenceData.sequence;
            }

            return itemDataForId;
        }

        List<Track> GetItemDataFromIDs(IEnumerable<int> ids)
        {
            var tracks = new List<Track>();
            foreach (int id in ids)
            {
                tracks.Add(m_TreeView.GetItemDataForId<Track>(id));
            }

            return tracks;
        }

        public void SetItems(IList<TreeViewItemData<Track>> items) =>
            m_TreeView.SetRootItems(items);

        public void SetSelection(IEnumerable<int> ids) =>
            m_TreeView.SetSelectionByIdWithoutNotify(ids);

        public void SetExpanded(int id, bool expanded)
        {
            if (expanded)
                m_TreeView.viewController.ExpandItem(id, false, false);
            else
                m_TreeView.viewController.CollapseItem(id, false);
        }

        public void FrameVertically(UniqueID trackID)
        {
            int treeViewId = m_Controller.GetTreeViewId(trackID);
            m_TreeView.ScrollToItemById(treeViewId);
        }

        void Reset()
        {
            m_CurrentSequence = null;
            m_ContentManager = new ContentManager(CreateTrackHeaderElement, CreateTrackElement, CreateItemElement);
            m_TreeView.SetRootItems(new List<TreeViewItemData<Track>>(0));
            m_TreeView.RefreshItems();
            m_Controller = new SequenceTreeViewController(this);
        }

        void SetupTreeView()
        {
            m_ContentManager = new ContentManager(CreateTrackHeaderElement, CreateTrackElement, CreateItemElement);
            m_TreeView.makeItem = () => new TrackCell();
            m_TreeView.bindItem = BindCellContents;
        }

        void SetupControlsResize()
        {
            Add(m_HeaderResizeElement);
            m_HeaderResizeElement.AddToClassList(k_ControlsColumnResizer);
            m_HeaderResizeManipulator.OnDrag += OnControlsWidthManipulatorDragged;
            m_Header.ContentsHeader.RegisterCallback<GeometryChangedEvent>(_ => OnLayoutChanged());
        }

        void SetupTimeDragManipulator()
        {
            var timeAreaDragManipulator = new TimeDragManipulator(m_CanvasManager);
            AddDragCallbacks(timeAreaDragManipulator);
            m_TimeArea.AddManipulator(timeAreaDragManipulator);

            var playHeadDragManipulator = new TimeDragManipulator(m_CanvasManager);
            AddDragCallbacks(playHeadDragManipulator);
            m_PlayHead.AddManipulator(playHeadDragManipulator);

            void AddDragCallbacks(TimeDragManipulator manipulator)
            {
                manipulator.StartDrag += _ => m_PlayHead.SetTooltipDisplayState(true);
                manipulator.EndDrag += _ => m_PlayHead.SetTooltipDisplayState(false);
                manipulator.SetTime += time => TimeChanged?.Invoke(time);
            }
        }

        void SetupZoomAndPanManipulators()
        {
            this.AddManipulator(new CanvasZoomManipulator(m_CanvasManager));
            this.AddManipulator(new CanvasPanManipulator(m_CanvasManager));
            RegisterCallback<ZoomEvent>(OnZoom);
            RegisterCallback<PanEvent>(OnPan);
            RegisterCallback<DisplayRangeChangeEvent>(OnDisplayRangeChanged);
        }

        void BindCellContents(VisualElement ve, int idx)
        {
            var track = m_TreeView.GetItemDataForIndex<Track>(idx);
            if (track != null && ve is TrackCell trackCell)
            {
                ITrackHeaderElement header = m_Header.GetControlsVisibility() ? m_ContentManager.GetOrCreateTrackHeaderElement(track) : null;
                trackCell.BindTo(header);
                ITrackElement content = m_ContentManager.GetOrCreateTrackContentElement(track);
                trackCell.BindTo(content);
                trackCell.ResizeContents(m_Header.GetContentsWidth());
            }

            m_CanvasManager.RepositionTargetAndDescendants(ve);
        }

        void OnLayoutChanged()
        {
            EnableInClassList(k_HideFoldoutStyle, !ShowControls);

            float headerWidth = m_Header.GetControlsWidth();
            m_ControlsBackground.style.width = headerWidth;
            m_ControlsOverlay.style.width = headerWidth;
            m_HeaderResizeElement.style.left = headerWidth;
            m_TimeRangeScroller.style.paddingLeft = headerWidth;
            TrackCell.ResizeAllCells(this, m_Header.GetContentsWidth());
        }

        bool CanStartDrag(CanStartDragArgs args) => dragAndDropHandler?.CanStartDrag(args, GetItemDataFromIDs(args.selectedIds)) ?? false;
        StartDragArgs SetupDragAndDrop(SetupDragAndDropArgs args) => dragAndDropHandler?.SetupDragAndDrop(args, GetItemDataFromIDs(args.selectedIds)) ?? args.startDragArgs;
        DragVisualMode DragAndDropUpdate(HandleDragAndDropArgs args) => dragAndDropHandler?.DragAndDropUpdate(args, GetItemDataForId(args.parentId)) ?? args.dragAndDropData.visualMode;
        DragVisualMode HandleDrop(HandleDragAndDropArgs args) => dragAndDropHandler?.HandleDrop(args, GetItemDataForId(args.parentId)) ?? args.dragAndDropData.visualMode;

        void OnZoom(ZoomEvent evt)
        {
            TimeRange previousRange = m_CanvasManager.canvasTransform.displayRange;
            TimeRange newDisplayRange = evt.ApplyToTimeRange(previousRange);
            DisplayRangeChangeEvent.Send(this, previousRange, newDisplayRange);
        }

        void OnPan(PanEvent evt)
        {
            TimeRange previousRange = m_CanvasManager.canvasTransform.displayRange;
            TimeRange newDisplayRange = evt.ApplyToTimeRange(previousRange);
            DisplayRangeChangeEvent.Send(this, previousRange, newDisplayRange);
        }

        void OnControlsWidthManipulatorDragged(float delta)
        {
            float newWidth = Mathf.Max(0, ControlsWidth + delta);
            ControlsWidth = newWidth;
            ControlsWidthChanged?.Invoke(newWidth);
        }

        void OnDisplayRangeChanged(DisplayRangeChangeEvent evt)
        {
            SetDisplayRange(evt.newValue);
        }

        void SetPlayHeadVisibility(bool isVisible)
        {
            if (isVisible)
                m_PlayHead.Show();
            else
                m_PlayHead.Hide();

            m_TimeRangeScroller.SetShowCurrentTime(isVisible);
        }

        void ScheduleSequenceChange(Sequence sequence)
        {
            m_CurrentSequence = sequence;
            schedule.Execute(SetCurrentSequence);
        }

        void SetCurrentSequence()
        {
            if (m_CurrentSequence != null)
                m_Controller.SetSequence(m_CurrentSequence);
        }
    }
}
