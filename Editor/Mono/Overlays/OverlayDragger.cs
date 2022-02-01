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
        public static event Action<Overlay> dragStarted;
        public static event Action<Overlay> dragEnded;

        const float k_Epsilon = 0.0001f;

        static readonly List<VisualElement> s_PickingBuffer = new List<VisualElement>();

        bool m_Active;
        bool m_WasFloating;
        OverlayContainer m_StartContainer;
        Vector2 m_InitialLayoutPosition;
        Vector2 m_StartMousePosition;
        readonly Overlay m_Overlay;
        int m_InitialIndex;

        OverlayCanvas canvas => m_Overlay.canvas;
        VisualElement floatingContainer => canvas.floatingContainer;
        VisualElement canvasRoot => canvas.rootVisualElement;

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
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        OverlayDropZoneBase GetOverlayDropZone(Vector2 mousePosition, Overlay ignoreTarget)
        {
            //keep mouse position within bounds for picking, mathf.epsilon is too small
            mousePosition.x = Mathf.Clamp(mousePosition.x, canvasRoot.worldBound.xMin + k_Epsilon,
                canvasRoot.worldBound.xMax - k_Epsilon);
            mousePosition.y = Mathf.Clamp(mousePosition.y, canvasRoot.worldBound.yMin + k_Epsilon,
                canvasRoot.worldBound.yMax - k_Epsilon);

            //get list of items under mouse
            s_PickingBuffer.Clear();
            canvasRoot.panel.PickAll(mousePosition, s_PickingBuffer);

            OverlayDropZoneBase result = null;
            foreach (var visualElement in s_PickingBuffer)
            {
                if (visualElement.parent is OverlayDropZoneBase dropZone
                    && dropZone.CanAcceptTarget(ignoreTarget)
                    && (result == null || dropZone.priority > result.priority))
                {
                    result = dropZone;
                }
            }

            return result;
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
            m_StartContainer = m_Overlay.container;

            var canvasLocalPos = canvasRoot.WorldToLocal(e.mousePosition);
            var constrainedMousePosition = canvas.ClampToOverlayWindow(new Rect(canvasLocalPos, Vector2.zero)).position;
            m_StartMousePosition = constrainedMousePosition;

            m_InitialLayoutPosition = floatingContainer.WorldToLocal(m_Overlay.rootVisualElement.worldBound.position);

            //if docked, convert to floating
            if (!m_Overlay.floating)
            {
                m_Overlay.container.stateLocked = true;
                m_InitialIndex = m_Overlay.container.IndexOf(m_Overlay.rootVisualElement);

                canvas.ShowOriginGhost(m_Overlay);
                m_Overlay.floatingPosition = m_InitialLayoutPosition;
                m_Overlay.Undock();
            }
            else
            {
                //make sure overlay is on top
                m_Overlay.rootVisualElement.BringToFront();
            }

            m_Active = true;
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            target.CaptureMouse();
            e.StopPropagation();

            dragStarted?.Invoke(m_Overlay);
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            var canvasLocalPos = canvasRoot.WorldToLocal(e.mousePosition);
            var constrainedMousePosition = canvas.ClampToOverlayWindow(new Rect(canvasLocalPos, Vector2.zero)).position;
            var diff = (constrainedMousePosition - m_StartMousePosition);

            m_Overlay.rootVisualElement.transform.position = m_InitialLayoutPosition + diff;

            canvas.destinationMarker.SetTarget(IsInOriginGhost(e.mousePosition)
                ? null
                : GetOverlayDropZone(e.mousePosition, m_Overlay));

            e.StopPropagation();
        }

        void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active || !CanStopManipulation(e))
                return;

            e.StopPropagation();

            if (IsInOriginGhost(e.mousePosition))
            {
                CancelDrag(e.mousePosition);
                return;
            }

            var dropZone = GetOverlayDropZone(e.mousePosition, m_Overlay);
            if (dropZone != null)
            {
                m_Overlay.container?.RemoveOverlay(m_Overlay);
                dropZone.DropOverlay(m_Overlay);
            }

            if (m_Overlay.floating)
            {
                var pos = m_Overlay.rootVisualElement.transform.position;
                m_Overlay.floatingPosition = new Vector2(pos.x, pos.y);
            }

            OnDragEnd(e.mousePosition);
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (m_Active && evt.keyCode == KeyCode.Escape)
            {
                CancelDrag(Vector2.negativeInfinity);
                evt.StopPropagation();
            }
        }

        bool IsInOriginGhost(Vector2 mousePosition)
        {
            var isInGhost = canvas.GetOriginGhostWorldBound().Contains(mousePosition);
            canvas.UpdateGhostHover(isInGhost);
            return isInGhost;
        }

        void CancelDrag(Vector2 mousePosition)
        {
            if (m_WasFloating)
            {
                m_Overlay.rootVisualElement.transform.position = m_InitialLayoutPosition;
            }
            else
            {
                m_Overlay.floating = false;
                m_Overlay.container.Insert(m_InitialIndex, m_Overlay.rootVisualElement);
            }

            OnDragEnd(mousePosition);
        }

        void OnDragEnd(Vector2 mousePosition)
        {
            m_Active = false;
            target.ReleaseMouse();

            canvas.HideOriginGhost();
            canvas.destinationMarker.SetTarget(null);
            m_StartContainer.stateLocked = false;
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);

            dragEnded?.Invoke(m_Overlay);
        }
    }
}
