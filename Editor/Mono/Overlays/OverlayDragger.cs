// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    sealed class OverlayDragger : MouseManipulator
    {
        sealed class DockingOperation : IDisposable
        {
            static readonly Type[] s_PickingPriority =
            {
                typeof(ToolbarDropZone),
                typeof(OverlayGhostDropZone),
                typeof(OverlayDropZone),
                typeof(OverlayContainerInsertDropZone),
                typeof(OverlayContainerDropZone),
            };

            const string k_OverlayDraggedState = "unity-overlay--dragged";

            readonly OverlayInsertIndicator m_InsertIndicator;
            readonly List<OverlayDropZoneBase> m_DropZones = new(32);
            readonly List<VisualElement> m_PickingBuffer = new();
            readonly List<OverlayDropZoneBase> m_DropZoneBuffer = new();
            readonly Overlay m_TargetOverlay;
            readonly OverlayContainer m_OriginContainer;
            readonly OverlayGhostDropZone m_OriginGhostDropZone;
            readonly VisualElement m_CanvasRoot;
            OverlayDropZoneBase m_Hovered;

            public DockingOperation(OverlayCanvas canvas, Overlay targetOverlay)
            {
                m_InsertIndicator = new OverlayInsertIndicator();
                m_OriginContainer = targetOverlay.container;
                m_OriginContainer.GetOverlayIndex(targetOverlay, out var section, out var index);
                m_TargetOverlay = targetOverlay;
                m_CanvasRoot = canvas.rootVisualElement;
                targetOverlay.rootVisualElement.AddToClassList(k_OverlayDraggedState);

                if (!targetOverlay.floating)
                {
                    m_OriginGhostDropZone = OverlayGhostDropZone.Create(targetOverlay);
                    m_DropZones.Add(m_OriginGhostDropZone);
                }

                //Collect dropzones
                m_DropZones.AddRange(canvas.dockArea.GetDropZones());

                foreach (var overlay in canvas.overlays)
                {
                    m_DropZones.Add(overlay.insertBeforeDropZone);
                    m_DropZones.Add(overlay.insertAfterDropZone);
                }

                foreach (var container in canvas.containers)
                    m_DropZones.AddRange(container.GetDropZones());

                foreach (var dropZone in m_DropZones)
                {
                    dropZone.Setup(m_InsertIndicator, m_OriginContainer, section);
                    dropZone.Activate(m_TargetOverlay);
                }
            }

            public void UpdateHover(OverlayDropZoneBase hovered)
            {
                if (m_Hovered == hovered)
                    return;

                if (m_Hovered != null)
                    m_Hovered.EndHover();

                m_Hovered = hovered;

                //Remove dropzone f we have a different container
                if (m_Hovered == null || m_Hovered.targetContainer != m_OriginContainer)
                    m_OriginGhostDropZone?.RemoveFromHierarchy();
                
                if (m_Hovered != null)
                    m_Hovered.BeginHover();

                foreach (var dropZone in m_DropZones)
                    dropZone.UpdateHover(hovered);
            }

            public OverlayDropZoneBase GetOverlayDropZoneAtPosition(Vector2 mousePosition)
            {
                //get list of items under mouse
                m_PickingBuffer.Clear();
                m_DropZoneBuffer.Clear();
                m_CanvasRoot.panel.PickAll(mousePosition, m_PickingBuffer);

                foreach (var element in m_PickingBuffer)
                    if (element is OverlayDropZoneBase dropZone && dropZone.CanAcceptTarget(m_TargetOverlay))
                        m_DropZoneBuffer.Add(dropZone);

                if (m_DropZoneBuffer.Count == 0)
                    return null;

                //Sort by priority
                m_DropZoneBuffer.Sort((a, b) => Array.IndexOf(s_PickingPriority, a.GetType()).CompareTo(Array.IndexOf(s_PickingPriority, b.GetType())));

                return m_DropZoneBuffer[0];
            }

            public void Dispose()
            {
                m_TargetOverlay.rootVisualElement.RemoveFromClassList(k_OverlayDraggedState);

                m_Hovered?.EndHover();

                foreach (var dropZone in m_DropZones)
                {
                    dropZone.Deactivate(m_TargetOverlay);
                    dropZone.Cleanup();
                }

                m_OriginGhostDropZone?.RemoveFromHierarchy();
                m_InsertIndicator.RemoveFromHierarchy();
            }
        }


        bool m_Active;
        bool m_WasFloating;
        bool m_WasCollapsed;
        OverlayContainer m_StartContainer;
        Vector2 m_InitialLayoutPosition;
        Vector2 m_StartLeftCornerPosition;
        Vector2 m_StartMousePosition;
        readonly Overlay m_Overlay;
        int m_InitialIndex;
        OverlayContainerSection m_InitialSection;
        DockingOperation m_DockOperation;

        OverlayCanvas canvas => m_Overlay.canvas;
        FloatingOverlayContainer floatingContainer => canvas.floatingContainer;

        public OverlayDragger(Overlay overlay)
        {
            m_Overlay = overlay;
            activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse});
            activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse, modifiers = EventModifiers.Control});
            m_Active = false;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        bool IsInDraggableArea(Vector2 mousePosition)
        {
            return target.worldBound.Contains(mousePosition);
        }

        void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (!IsInDraggableArea(e.mousePosition) || !CanStartManipulation(e))
                return;

            m_WasFloating = m_Overlay.floating;
            m_WasCollapsed = m_Overlay.collapsed;
            m_StartContainer = m_Overlay.container;
            m_DockOperation = new DockingOperation(canvas, m_Overlay);

            m_StartMousePosition = OverlayUtilities.ClampPositionToRect(e.mousePosition, canvas.rootVisualElement.worldBound);
            m_StartLeftCornerPosition = m_Overlay.rootVisualElement.Q(Overlay.draggerName).worldBound.position;

            m_InitialLayoutPosition = floatingContainer.WorldToLocal(m_Overlay.rootVisualElement.worldBound.position);

            //if docked, convert to floating
            if (!m_Overlay.floating)
            {
                m_Overlay.container.GetOverlayIndex(m_Overlay, out m_InitialSection, out m_InitialIndex);
                m_Overlay.Undock();
                m_Overlay.floatingPosition = m_InitialLayoutPosition;
                m_Overlay.UpdateAbsolutePosition();
            }

            m_Overlay.BringToFront();

            m_Active = true;
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            target.CaptureMouse();
            e.StopPropagation();
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            var constrainedMousePosition = OverlayUtilities.ClampPositionToRect(e.mousePosition, canvas.rootVisualElement.worldBound);

            var dropZone = m_DockOperation.GetOverlayDropZoneAtPosition(constrainedMousePosition);
            var targetContainer = dropZone != null ? dropZone.targetContainer : null;

            bool delayPositionUpdate = false;
            if (m_Overlay.tempTargetContainer != targetContainer)
            {
                var prevLayout = m_Overlay.activeLayout;
                m_Overlay.tempTargetContainer = targetContainer;
                m_Overlay.RebuildContent();
                if (m_Overlay.activeLayout != prevLayout)
                    delayPositionUpdate = true;
            }

            var diff = (constrainedMousePosition - (!m_WasCollapsed && m_Overlay.collapsed ? m_StartLeftCornerPosition : m_StartMousePosition));
            var targetPosition = m_InitialLayoutPosition + diff;
            var targetRect = new Rect(targetPosition, m_Overlay.rootVisualElement.layout.size);

            if (delayPositionUpdate)
                m_Overlay.rootVisualElement.RegisterCallback<GeometryChangedEvent, Rect>(DelayedPositionUpdate, targetRect);
            else 
                m_Overlay.rootVisualElement.transform.position = OverlayUtilities.ClampRectToRect(targetRect, floatingContainer.rect).position;

            m_DockOperation.UpdateHover(dropZone);

            e.StopPropagation();
        }

        void DelayedPositionUpdate(GeometryChangedEvent evt, Rect targetRect)
        {
            m_Overlay.rootVisualElement.transform.position = OverlayUtilities.ClampRectToRect(targetRect, floatingContainer.rect).position;
            m_Overlay.rootVisualElement.UnregisterCallback<GeometryChangedEvent, Rect>(DelayedPositionUpdate);
        }

        void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active)
                return;

            if (e.button != (int)MouseButton.RightMouse && !CanStopManipulation(e))
                return;

            e.StopPropagation();

            var dropZone = m_DockOperation.GetOverlayDropZoneAtPosition(OverlayUtilities.ClampPositionToRect(e.mousePosition, canvas.rootVisualElement.worldBound));
            if (dropZone != null)
            {
                if (dropZone is OverlayGhostDropZone)
                {
                    CancelDrag();
                    return;
                }    

                m_Overlay.container?.RemoveOverlay(m_Overlay);
                m_Overlay.rootVisualElement.transform.position = Vector2.zero;
                dropZone.DropOverlay(m_Overlay);
            }

            if (m_Overlay.floating)
            {
                var pos = m_Overlay.rootVisualElement.transform.position;
                m_Overlay.floatingPosition = new Vector2(pos.x, pos.y);
            }

            OnDragEnd();
        }

        void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (!m_Active)
                return;
            CancelDrag();
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (m_Active && evt.keyCode == KeyCode.Escape)
            {
                CancelDrag();
                evt.StopPropagation();
            }
        }

        void CancelDrag()
        {
            if (m_WasFloating)
            {
                m_Overlay.rootVisualElement.transform.position = m_InitialLayoutPosition;
            }
            else
            {
                m_Overlay.DockAt(m_StartContainer, m_InitialSection, m_InitialIndex);
            }

            OnDragEnd();
            m_Overlay.RebuildContent();
        }

        void OnDragEnd()
        {
            m_Active = false;
            target.ReleaseMouse();

            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            m_Overlay.rootVisualElement.UnregisterCallback<GeometryChangedEvent, Rect>(DelayedPositionUpdate); //Ensure we kill any delayed position update that was still in process

            m_Overlay.tempTargetContainer = null;
            m_DockOperation.Dispose();
            m_DockOperation = null;
        }
    }
}
