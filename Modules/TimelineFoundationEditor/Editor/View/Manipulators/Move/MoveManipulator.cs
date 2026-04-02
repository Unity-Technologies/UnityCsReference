// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace Unity.Timeline.Foundation.View.Internals
{
    class MoveManipulator : PointerManipulator
    {
        static ProfilerMarker s_MoveMarker = new ProfilerMarker($"ManipulatorMoveMarker");
        public bool edgeSnap { get; set; }
        public bool showEdgeSnapDebug { get; set; }

        readonly ICanvas m_Canvas;
        readonly IManipulationContextProvider m_ContextProvider;

        MoveManipulation m_MoveManipulation;
        MoveManipulationTrackInfo m_ManipulationTrackInfo;
        bool m_IsPointerCaptured;
        bool m_ActionKeyEnabled;
        bool m_FirstProcess;

        public MoveManipulator(IManipulationContextProvider contextProvider, ICanvas canvas)
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            m_ContextProvider = contextProvider;
            m_Canvas = canvas;
        }

        public void SetMoveManipulation(MoveManipulation moveManipulation)
        {
            m_MoveManipulation = moveManipulation;
            m_Canvas.overlayManager.AddOverlay(m_MoveManipulation.moveOverlay);
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        void OnPointerDown(PointerDownEvent e)
        {
            if (!m_MoveManipulation.enabled || m_MoveManipulation.moveBehaviour == null || !CanStartManipulation(e))
                return;

            ItemElement itemUnderMouse = MoveManipulatorUtils.FindItemFromTarget(target, e.position);
            var trackUnderMouse = itemUnderMouse?.GetFirstOfType<TrackElement>();
            if (trackUnderMouse == null)
                return;

            BeginCapture(e);

            //delayed manipulation to let the selection settle
            Vector2 mousePosition = e.position;
            EditorApplication.delayCall += () => BeginManipulation(mousePosition, trackUnderMouse);
        }

        void OnPointerMove(PointerMoveEvent e)
        {
            if (!m_IsPointerCaptured || !m_MoveManipulation.manipulationActive)
                return;
            try
            {
                m_ActionKeyEnabled = e.actionKey;
                ProcessManipulation(e.position);
            }
            catch //release capture to avoid keeping a handle on the mouse pointer
            {
                EndManipulation(e);
                throw;
            }
        }

        void OnPointerUp(PointerUpEvent e)
        {
            if (!m_IsPointerCaptured || !CanStopManipulation(e))
                return;

            try
            {
                m_MoveManipulation.ApplyMove();
            }
            finally
            {
                EndManipulation(e);
            }
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (!m_IsPointerCaptured || evt.keyCode != KeyCode.Escape)
                return;

            try
            {
                m_MoveManipulation.CancelMove();
            }
            finally
            {
                EndManipulation(evt);
            }
        }

        void BeginCapture(EventBase evt)
        {
            evt.StopImmediatePropagation();
            target.CaptureMouse();
            m_MoveManipulation.behaviourOverlay.cursorChanged += UpdateCursor;
            m_IsPointerCaptured = true;
            m_FirstProcess = true;
        }

        void EndCapture(EventBase evt)
        {
            if (!m_FirstProcess)
                evt.StopImmediatePropagation();
            m_MoveManipulation.behaviourOverlay.cursorChanged -= UpdateCursor;
            target.ReleaseMouse();
            target.style.cursor = new StyleCursor(StyleKeyword.Initial);
            m_IsPointerCaptured = false;
        }

        void UpdateCursor(Cursor editModeCursor)
        {
            target.style.cursor = editModeCursor;
        }

        void BeginManipulation(Vector2 mousePosition, TrackElement trackElement)
        {
            if (!m_IsPointerCaptured || m_MoveManipulation.manipulationActive)
                return;

            ManipulationContext context = m_ContextProvider.GetManipulationContext();
            ISequenceViewModel viewModel = m_ContextProvider.GetViewModel();
            IManipulationHandler handler = m_ContextProvider.GetManipulationHandler();
            m_MoveManipulation.BeginMove(viewModel, handler, context, m_ContextProvider.GetViewContext(), m_Canvas.WorldPixelToTime(mousePosition.x, ignoreSnapToFrame: true));
            m_ManipulationTrackInfo = new MoveManipulationTrackInfo(mousePosition, trackElement);
            m_MoveManipulation.SetupOverlay(m_ContextProvider);
        }

        void ProcessManipulation(Vector2 mousePosition)
        {
            if (m_FirstProcess)
            {
                m_MoveManipulation.ShowOverlay(showEdgeSnapDebug);
                m_MoveManipulation.ShowBehaviourOverlay();
                m_FirstProcess = false;
            }

            m_MoveManipulation.ApplyPendingBehaviour(m_ContextProvider.GetManipulationContext());
            m_MoveManipulation.UpdateTimeInfo(m_Canvas.WorldPixelToTime(mousePosition.x, true));
            m_ManipulationTrackInfo.UpdateTrackUnderMouse(target, mousePosition);

            if (ShouldDetach())
            {
                m_MoveManipulation.ProcessMoveDetach();
                m_MoveManipulation.HideBehaviourOverlay();
            }

            if (ShouldAttach())
            {
                m_MoveManipulation.ProcessMoveAttach(m_ManipulationTrackInfo.trackUnderMouse, m_ContextProvider.GetViewContext());
                m_MoveManipulation.moveOverlay.AttachTo(m_ManipulationTrackInfo.trackUnderMouseRect);
                m_MoveManipulation.ShowBehaviourOverlay();
            }

            if (m_MoveManipulation.isAttached)
            {
                m_MoveManipulation.ApplySnapping(m_Canvas, edgeSnap ^ m_ActionKeyEnabled, m_Canvas.snapToFrame);
                m_MoveManipulation.ProcessMoveOnTrack();
                m_MoveManipulation.UpdateAttachedOverlay();
            }
            else if (m_MoveManipulation.isDetached)
            {
                m_MoveManipulation.UpdateDetachedOverlay();
                if (m_ManipulationTrackInfo.trackUnderMouse == null)
                    m_MoveManipulation.moveOverlay.SetWorldY(mousePosition.y - m_ManipulationTrackInfo.verticalDetachOffset);
            }
        }

        void EndManipulation(EventBase evt)
        {
            EndCapture(evt);
            m_MoveManipulation.EndMove();
            m_MoveManipulation.HideOverlay();
            m_MoveManipulation.HideBehaviourOverlay();
            m_ManipulationTrackInfo = default;
            m_FirstProcess = false;
        }

        bool ShouldDetach()
        {
            return m_MoveManipulation.isAttached
                && m_MoveManipulation.supportsDetach
                && m_ManipulationTrackInfo.trackUnderMouseHasChanged;
        }

        bool ShouldAttach()
        {
            return m_MoveManipulation.isDetached
                && m_ManipulationTrackInfo.trackUnderMouse != null
                && m_ManipulationTrackInfo.trackUnderMouseHasChanged;
        }
    }
}
