// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Shows a preview of the tab being dragged.
    /// </summary>
    class TabDragPreview : VisualElement
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = TabView.ussClassName + "__drag-preview";

        /// <summary>
        /// Constructor.
        /// </summary>
        public TabDragPreview()
        {
            AddToClassList(ussClassName);

            pickingMode = PickingMode.Ignore;
        }
    }

    /// <summary>
    /// Shows where the tab being moved will be reordered.
    /// </summary>
    class TabDragLocationPreview : VisualElement
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = TabView.ussClassName + "__drag-location-preview";
        public static readonly string visualUssClassName = ussClassName + "__visual";
        public static readonly string verticalUssClassName = ussClassName + "__vertical";
        public static readonly string horizontalUssClassName = ussClassName + "__horizontal";

        VisualElement m_Preview;

        internal VisualElement preview => m_Preview;

        /// <summary>
        /// Constructor.
        /// </summary>
        public TabDragLocationPreview()
        {
            AddToClassList(ussClassName);
            pickingMode = PickingMode.Ignore;

            m_Preview = new VisualElement();
            m_Preview.AddToClassList(visualUssClassName);
            m_Preview.pickingMode = PickingMode.Ignore;
            Add(m_Preview);
        }
    }

    /// <summary>
    /// Shows a preview of the tab being moved.
    /// </summary>
    class TabLayout
    {
        TabView m_TabView;
        List<VisualElement> m_TabHeaders;
        bool m_IsVertical;

        /// <summary>
        /// Constructs with a collection of tabs.
        /// </summary>
        /// <param name="tabView">The tab view managed by the layout.</param>
        /// <param name="isVertical">If the tabs are vertically stacked.</param>
        public TabLayout(TabView tabView, bool isVertical)
        {
            m_TabView = tabView;
            m_TabHeaders = tabView.tabHeaders;
            m_IsVertical = isVertical;
        }

        public static float GetHeight(VisualElement t)
        {
            return t.boundingBox.height;
        }

        public static float GetWidth(VisualElement t)
        {
            return t.boundingBox.width;
        }

        public float GetTabOffset(VisualElement tab)
        {
            if (!tab.visible)
                return float.NaN;

            float pos = 0;

            var visibleIndex = m_TabHeaders.IndexOf(tab);

            for (var i = 0; i < visibleIndex; ++i)
            {
                var otherTab = m_TabHeaders[i];
                var size = m_IsVertical ? GetHeight(otherTab) : GetWidth(otherTab);

                if (float.IsNaN(size))
                    continue;
                pos += size;
            }
            return pos;
        }

        void InitOrderTabs()
        {
            m_TabHeaders ??= new List<VisualElement>();
        }

        /// <summary>
        /// Reorders the display of a tab at the specified source index, to the destination index.
        /// </summary>
        /// <remarks>
        /// This does not change the order in the original tabs data, only in tabs being displayed.</remarks>
        /// <param name="from">The display index of the tab to move.</param>
        /// <param name="to">The display index where the tab will be moved to.</param>
        public void ReorderDisplay(int from, int to)
        {
            InitOrderTabs();

            m_TabView.ReorderTab(from, to);
        }
    }

    /// <summary>
    /// Manipulator used to move tabs in a tab view
    /// </summary>
    class TabDragger : PointerManipulator
    {
        const float k_StartDragDistance = 5f;

        float m_StartPos;
        float m_LastPos;
        bool m_Moving;
        bool m_Cancelled;
        VisualElement m_Header;
        TabView m_TabView;
        VisualElement m_PreviewElement;
        TabDragLocationPreview m_LocationPreviewElement;
        VisualElement m_TabToMove;
        float m_TabToMovePos;
        VisualElement m_DestinationTab;
        bool m_MoveBeforeDestination;

        int m_DraggingPointerId = PointerId.invalidPointerId;

        TabLayout tabLayout { get; set; }

        internal bool active { get; set; }

        internal bool isVertical { get; set; }

        internal bool moving
        {
            get => m_Moving;
            private set
            {
                if (m_Moving == value)
                    return;
                m_Moving = value;
                m_TabToMove.EnableInClassList(Tab.draggingUssClassName, moving);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public TabDragger()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        /// <summary>
        /// This method is called when a PointerDownEvent is sent to the target element.
        /// </summary>
        /// <param name="evt">The event.</param>
        void OnPointerDown(PointerDownEvent evt)
        {
            if (!CanStartManipulation(evt)) return;

            if (active)
            {
                evt.StopImmediatePropagation();
                return;
            }

            ProcessDownEvent(evt, evt.localPosition, evt.pointerId);
        }

        /// <summary>
        /// This method is called when a PointerMoveEvent is sent to the target element.
        /// </summary>
        /// <param name="evt">The event.</param>
        void OnPointerMove(PointerMoveEvent evt)
        {
            if (!active) return;

            ProcessMoveEvent(evt, evt.localPosition);
        }

        /// <summary>
        /// This method is called when a PointerUpEvent is sent to the target element.
        /// </summary>
        /// <param name="evt">The event.</param>
        void OnPointerUp(PointerUpEvent evt)
        {
            if (!active || !CanStopManipulation(evt)) return;

            ProcessUpEvent(evt, evt.localPosition, evt.pointerId);
        }

        /// <summary>
        /// This method is called when a PointerCancelEvent is sent to the target element.
        /// </summary>
        /// <param name="evt">The event.</param>
        void OnPointerCancel(PointerCancelEvent evt)
        {
            if (!active || !CanStopManipulation(evt)) return;

            ProcessCancelEvent(evt, evt.pointerId);
        }

        /// <summary>
        /// This method is called when a PointerCaptureOutEvent is sent to the target element.
        /// </summary>
        /// <param name="evt">The event.</param>
        void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (!active) return;

            ProcessCancelEvent(evt, evt.pointerId);
        }

        /// <summary>
        /// This method processes the up cancel sent to the target Element.
        /// </summary>
        void ProcessCancelEvent(EventBase evt, int pointerId)
        {
            active = false;
            target.ReleasePointer(pointerId);
            if (!(evt is IPointerEvent))
                target.panel.ProcessPointerCapture(pointerId);

            if (moving)
                EndDragMove(true);
        }

        void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.Escape && moving)
            {
                active = false;
                if (m_DraggingPointerId != PointerId.invalidPointerId)
                {
                    target.ReleasePointer(m_DraggingPointerId);
                }
                EndDragMove(true);
                e.StopPropagation();
            }
        }

        void ProcessDownEvent(EventBase evt, Vector2 localPosition, int pointerId)
        {
            var ve = (evt.currentTarget as VisualElement);
            var tabView = ve?.GetFirstAncestorOfType<TabView>();

            if (tabView is not { reorderable: true })
                return;

            target.CapturePointer(pointerId);
            m_DraggingPointerId = pointerId;

            if (!(evt is IPointerEvent))
                target.panel.ProcessPointerCapture(pointerId);

            m_TabView = tabView;
            m_Header = tabView.header;

            isVertical = m_Header.resolvedStyle.flexDirection == FlexDirection.Column;

            tabLayout = new TabLayout(m_TabView, isVertical);

            var pos = ve.ChangeCoordinatesTo(m_Header, localPosition);
            m_Cancelled = false;
            m_StartPos = isVertical ? pos.y : pos.x;
            active = true;
            evt.StopPropagation();
        }

        void ProcessMoveEvent(EventBase e, Vector2 localPosition)
        {
            if (m_Cancelled)
                return;

            var ve = (e.currentTarget as VisualElement);
            var pos = ve.ChangeCoordinatesTo(m_Header, localPosition);

            var currentPos = isVertical ? pos.y : pos.x;

            if (!moving && Mathf.Abs(m_StartPos - currentPos) > k_StartDragDistance)
            {
                BeginDragMove(m_StartPos);
            }

            if (moving)
            {
                DragMove(currentPos);
            }
            e.StopPropagation();
        }

        void ProcessUpEvent(EventBase evt, Vector2 localPosition, int pointerId)
        {
            active = false;
            target.ReleasePointer(pointerId);
            if (!(evt is IPointerEvent))
                target.panel.ProcessPointerCapture(pointerId);

            EndDragMove(false);
            evt.StopPropagation();
        }

        /// <summary>
        /// Called when starting moving using mouse.
        /// </summary>
        /// <param name="pos">The current position of the pointer.</param>
        void BeginDragMove(float pos)
        {
            float destination = 0;
            var tabs = m_TabView.tabHeaders;
            m_TabToMove = m_TabView.tabHeaders[0];

            foreach (var tab in tabs)
            {
                destination += isVertical ? TabLayout.GetHeight(tab) : TabLayout.GetWidth(tab);

                if (destination > pos)
                {
                    m_TabToMove = tab;
                    break;
                }
            }
            moving = true;
            m_LastPos = pos;

            m_PreviewElement = new TabDragPreview();
            m_LocationPreviewElement = new TabDragLocationPreview
            {
                classList = { isVertical ? TabDragLocationPreview.verticalUssClassName :
                    TabDragLocationPreview.horizontalUssClassName }
            };
            m_Header.hierarchy.Add(m_PreviewElement);
            m_Header.Add(m_LocationPreviewElement);

            var index = m_TabView.tabHeaders.IndexOf(m_TabToMove);
            var activatedTab = m_TabView.tabs[index];
            m_TabView.activeTab = activatedTab;

            m_TabToMovePos = tabLayout.GetTabOffset(m_TabToMove);
            UpdateMoveLocation();
        }

        /// <summary>
        /// Called when moving using mouse.
        /// </summary>
        /// <param name="pos">The current position of the pointer.</param>
        void DragMove(float pos)
        {
            m_LastPos = pos;
            UpdateMoveLocation();
        }

        void UpdatePreviewPosition()
        {
            var pos = m_TabToMovePos + m_LastPos - m_StartPos;
            var tabToMoveWidth = TabLayout.GetWidth(m_TabToMove);
            var destinationPos = tabLayout.GetTabOffset(m_DestinationTab);
            var size = isVertical ? TabLayout.GetHeight(m_DestinationTab) : TabLayout.GetWidth(m_DestinationTab);
            var offset = !m_MoveBeforeDestination ? size : 0;

            if (isVertical)
            {
                m_PreviewElement.style.top = pos;
                m_PreviewElement.style.height = TabLayout.GetHeight(m_TabToMove);
                m_PreviewElement.style.width = tabToMoveWidth;

                if (m_DestinationTab != null)
                {
                    m_LocationPreviewElement.preview.style.width = tabToMoveWidth;
                    m_LocationPreviewElement.style.top = destinationPos + offset;
                }
            }
            else
            {
                m_PreviewElement.style.left = pos;
                m_PreviewElement.style.width = tabToMoveWidth;

                if (m_DestinationTab != null)
                {
                    m_LocationPreviewElement.style.left = destinationPos + offset;
                }
            }
        }

        void UpdateMoveLocation()
        {
            float destination = 0;

            m_DestinationTab = null;
            m_MoveBeforeDestination = false;

            foreach (var tab in m_TabView.tabHeaders)
            {
                m_DestinationTab = tab;
                var size = isVertical ? TabLayout.GetHeight(m_DestinationTab) : TabLayout.GetWidth(m_DestinationTab);
                var centerPos = destination + size / 2;

                destination += size;

                if (destination > m_LastPos)
                {
                    m_MoveBeforeDestination = (m_LastPos < centerPos);
                    break;
                }
            }

            UpdatePreviewPosition();
        }

        /// <summary>
        /// Called when finishing dragging using mouse.
        /// </summary>
        /// <param name="cancelled">Indicates whether drag move was cancelled.</param>
        void EndDragMove(bool cancelled)
        {
            if (!moving || m_Cancelled)
                return;

            m_Cancelled = cancelled;

            if (!cancelled)
            {
                int startIndex = m_TabView.tabHeaders.IndexOf(m_TabToMove);
                int destIndex = m_TabView.tabHeaders.IndexOf(m_DestinationTab);

                if (!m_MoveBeforeDestination)
                    destIndex++;

                if (startIndex < destIndex)
                    destIndex--;

                // If we move the tab at the same location then ignore
                if (startIndex != destIndex)
                {
                    tabLayout.ReorderDisplay(startIndex, destIndex);
                }
            }

            m_PreviewElement?.RemoveFromHierarchy();
            m_PreviewElement = null;
            m_LocationPreviewElement?.RemoveFromHierarchy();
            m_LocationPreviewElement = null;
            moving = false;
            m_TabToMove = null;
        }
    }
}
