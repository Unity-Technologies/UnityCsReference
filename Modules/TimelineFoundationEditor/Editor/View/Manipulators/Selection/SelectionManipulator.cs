// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using Unity.Timeline.Foundation.Common;
using Unity.Timeline.Foundation.View.Internals;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.View
{
    class SelectionManipulator : PointerManipulator
    {
        public ISequenceViewModel viewModel { get; set; }

        readonly ISelectionBehaviour m_SelectionBehaviour;
        readonly ICanvas m_Canvas;
        readonly SelectionRectangleOverlay<ItemElement, ISelectableElement> m_SelectionRectangleOverlay;

        bool m_SelectionRectangleHasStarted;
        bool m_ShouldProcessPointerUp;

        public SelectionManipulator(ICanvas canvas)
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Shift });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Control });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Command });

            m_SelectionBehaviour = new SelectionBehaviour();
            m_Canvas = canvas;
            m_SelectionRectangleOverlay = new SelectionRectangleOverlay<ItemElement, ISelectableElement>(this);
            canvas.overlayManager.AddOverlay(m_SelectionRectangleOverlay.overlay);
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown_TrickleDown, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerDownEvent>(OnPointerDown_BubbleUp);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown_TrickleDown, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown_BubbleUp);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        void OnPointerDown_TrickleDown(PointerDownEvent e)
        {
            if (!CanStartManipulation(e))
                return;

            ISelectableElement pickedSelectable = PickSelectableElement(e);
            if (pickedSelectable is null)
            {
                bool isLeftMouseButtonActive = (int)MouseButton.LeftMouse == e.button;
                if (isLeftMouseButtonActive && !(e.shiftKey || e.actionKey))
                    m_SelectionBehaviour.ClearSelection(viewModel);
            }
            else
            {
                m_ShouldProcessPointerUp = true;
                if (e.actionKey)
                    DoToggleSelection(pickedSelectable);
                else if (e.shiftKey && pickedSelectable.supportsMultiSelect)
                    DoRangeSelection(pickedSelectable);
                else if (!pickedSelectable.selected)
                    DoSingleSelection(pickedSelectable, GetEdgeLocationClicked(e));
                else
                {
                    var edgeLocation = GetEdgeLocationClicked(e);
                    if (edgeLocation != ISelectionBehaviour.Location.None)
                        DoSingleSelection(pickedSelectable, edgeLocation);
                }
            }
        }

        ISelectionBehaviour.Location GetEdgeLocationClicked(PointerDownEvent e)
        {
            var element = MoveManipulatorUtils.PickElement<EdgeHandle>(target, e.position);
            if (element == null)
                return ISelectionBehaviour.Location.None;

            switch (element.location)
            {
                case EdgeHandle.Location.Left:
                    return ISelectionBehaviour.Location.Start;
                case EdgeHandle.Location.Right:
                    return ISelectionBehaviour.Location.End;
                default:
                    return ISelectionBehaviour.Location.Start;
            }
        }

        void OnPointerDown_BubbleUp(PointerDownEvent e)
        {
            m_ShouldProcessPointerUp = true;
            if (CanStartRectangleSelection(e))
            {
                StartRectangleSelection(e.position);
                e.StopImmediatePropagation();
            }
        }

        void OnPointerMove(PointerMoveEvent e)
        {
            if (m_SelectionRectangleHasStarted)
                UpdateRectangleSelection(e.deltaPosition);
        }

        void OnPointerUp(PointerUpEvent e)
        {
            // Pointer events are not always sent in pairs (mouse down + mouse up).
            // Only process a pointer up event if we have previously received a pointer down event.
            if (!m_ShouldProcessPointerUp)
                return;

            m_ShouldProcessPointerUp = false;

            if (m_SelectionRectangleHasStarted && CanStopManipulation(e))
            {
                IEnumerable<ItemElement> overlappedElements = m_SelectionRectangleOverlay.GetOverlappedElements();
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                IEnumerable<UniqueID> elementIds = overlappedElements.Select(element => element.ID);
#pragma warning restore UA2001
#pragma warning disable UA2002 // System.Linq.Any() usage
                if (elementIds.Any())
#pragma warning restore UA2002 // System.Linq.Any() usage
                {
                    if (!(e.shiftKey || e.actionKey))
                        m_SelectionBehaviour.ClearSelection(viewModel);

                    if (e.actionKey)
                        m_SelectionBehaviour.ToggleSelection(viewModel, elementIds);
                    else
                        m_SelectionBehaviour.Select(viewModel, elementIds);
                }


                EndRectangleSelection();
                e.StopImmediatePropagation();
            }
            //Manage cases where we are selecting a single item out of a selected collection.
            else if (!(e.shiftKey || e.actionKey) && e.button == (int)MouseButton.LeftMouse)
            {
                if (PickSelectableElement(e) is { selected: true } pickedSelectable)
                {
                    // do not re-select an item that is already selected
                    var selectionData = viewModel.GetData<SelectionData>();
                    if (selectionData.selection.Count() > 1)
                        DoSingleSelection(pickedSelectable, ISelectionBehaviour.Location.None);
                }
            }
        }

        void OnKeyDown(KeyDownEvent e)
        {
            if (m_SelectionRectangleHasStarted && e.keyCode == KeyCode.Escape)
            {
                EndRectangleSelection();
                e.StopImmediatePropagation();
            }
        }

        void DoToggleSelection(ISelectableElement pickedSelectable)
        {
            m_SelectionBehaviour.ToggleSelection(viewModel, pickedSelectable.ID);
        }

        void DoRangeSelection(ISelectableElement pickedSelectable)
        {
            IEnumerable<UniqueID> pickedSelection = new[] { pickedSelectable.ID };
            IEnumerable<UniqueID> rangeSelection = ShiftSelection.GetSelectableElements(pickedSelectable, viewModel.selectionData, viewModel.sequenceData);
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_SelectionBehaviour.Select(viewModel, pickedSelection.Concat(rangeSelection));
#pragma warning restore UA2001
        }

        void DoSingleSelection(ISelectableElement pickedSelectable, ISelectionBehaviour.Location edgeLocation)
        {
            m_SelectionBehaviour.ClearSelection(viewModel);
            m_SelectionBehaviour.Select(viewModel, pickedSelectable.ID, edgeLocation);
        }

        bool CanStartRectangleSelection(IPointerEvent e)
        {
            return (int)MouseButton.LeftMouse == e.button &&
                IsPointerWithinCanvas(e)
                && PickSelectableElement(e) is null or TrackElement;
        }

        void StartRectangleSelection(Vector2 mousePosition)
        {
            target.CaptureMouse();
            m_SelectionRectangleHasStarted = true;
            m_SelectionRectangleOverlay?.OnStartDrag(mousePosition);
        }

        void UpdateRectangleSelection(Vector2 mouseDelta)
        {
            m_SelectionRectangleOverlay?.UpdateSize(mouseDelta);
        }

        void EndRectangleSelection()
        {
            target.ReleaseMouse();
            m_SelectionRectangleHasStarted = false;
            m_SelectionRectangleOverlay?.OnEndDrag();
        }

        ISelectableElement PickSelectableElement(IPointerEvent e) => this.PickElemenOfType<ISelectableElement>(e.position);

        bool IsPointerWithinCanvas(IPointerEvent e)
        {
            return m_Canvas.worldBound.Contains(e.position);
        }
    }
}
