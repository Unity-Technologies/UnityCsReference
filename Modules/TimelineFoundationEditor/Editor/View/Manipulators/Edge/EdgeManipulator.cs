// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.IntegerTime;
using Unity.Profiling;
using Unity.Timeline.Foundation.ViewModel;
using Unity.Timeline.Foundation.Widgets;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace Unity.Timeline.Foundation.View.Internals
{
    class EdgeManipulator : PointerManipulator
    {
        static readonly EventModifiers[] k_Modifiers =
        {
            EventModifiers.None,
            EventModifiers.Control,
            EventModifiers.Shift,
            EventModifiers.Command,
            EventModifiers.Alt
        };

        const float k_EdgeAttractionInPixels = 10.0f;
        static ProfilerMarker s_TrimMarker = new($"ManipulatorTrimMarker");
        public bool edgeSnap { get; set; }
        public bool enabled { get; set; } = true;

        readonly IManipulationContextProvider m_ContextProvider;
        readonly ICanvas m_Canvas;
        readonly List<IEdgeManipulation> m_Manipulations = new();
        readonly SnapEngine m_SnapEngine = new();

        ItemElement m_ManipulatedItem;
        EdgeHandle m_ManipulatedEdgeHandle;
        EdgeManipulationTime m_ManipulationTime;
        IEdgeManipulation m_CurrentManipulation;

        public EdgeManipulator(IManipulationContextProvider contextProvider, ICanvas canvas = null)
        {
            m_ContextProvider = contextProvider;
            m_Canvas = canvas;
            foreach (EventModifiers modifier in k_Modifiers)
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = modifier });
        }

        public void AddEdgeManipulation(IEdgeManipulation manipulation)
        {
            m_Manipulations.Add(manipulation);
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
            target.RegisterCallback<KeyUpEvent>(OnKeyUp);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            target.UnregisterCallback<KeyUpEvent>(OnKeyUp);
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (!enabled || !CanStartManipulation(evt) || evt.target is not EdgeHandle edgeHandle)
                return;

            var clipElement = edgeHandle.GetFirstAncestorOfType<ClipElement>();
            if (clipElement == null)
                return;

            IEdgeManipulation manipulation = FindManipulation(m_Manipulations, clipElement.item, edgeHandle.location, evt.modifiers);
            if (manipulation == null)
                return;

            m_ManipulatedItem = clipElement;
            m_ManipulatedEdgeHandle = edgeHandle;
            StartManipulation(manipulation);

            target.CaptureMouse();
            evt.StopImmediatePropagation();
        }

        void StartManipulation(IEdgeManipulation manipulation)
        {
            m_CurrentManipulation = manipulation;
            ISequenceViewModel viewModel = m_ContextProvider.GetViewModel();
            IManipulationHandler handler = m_ContextProvider.GetManipulationHandler();
            m_CurrentManipulation.Begin(viewModel, handler, m_ManipulatedItem.item, m_ManipulatedEdgeHandle.location, out DiscreteTime edgeInitialTime);

            if (m_CurrentManipulation.overlay != null)
                m_CurrentManipulation.overlay.cursorChanged += UpdateCursor;

            m_ManipulationTime = new EdgeManipulationTime(edgeInitialTime) { validRange = m_CurrentManipulation.GetValidRange() };
            ManipulatorUtils.BuildSnapEngine(m_SnapEngine, m_ContextProvider.GetViewContext(), m_CurrentManipulation.GetManipulatedItems());
            m_ManipulatedEdgeHandle.cursor = m_CurrentManipulation.GetCursor();
        }

        void ChangeManipulationIfNecessary(EventModifiers modifiers)
        {
            IEdgeManipulation candidateManipulation = FindManipulation(
                m_Manipulations, m_ManipulatedItem.item, m_ManipulatedEdgeHandle.location, modifiers);
            if (candidateManipulation != m_CurrentManipulation)
            {
                EndCurrentManipulation();
                StartManipulation(candidateManipulation);
            }
        }

        void EndCurrentManipulation()
        {
            m_SnapEngine.RemoveAllEdges();
            if (m_CurrentManipulation.overlay != null)
                m_CurrentManipulation.overlay.cursorChanged -= UpdateCursor;
            m_CurrentManipulation.End();
        }

        static IEdgeManipulation FindManipulation(List<IEdgeManipulation> manipulations, Item item, EdgeHandle.Location location, EventModifiers modifiers)
        {
            //iterate in reverse order to let the last added manipulation have priority
            for (int i = manipulations.Count - 1; i >= 0; i--)
            {
                IEdgeManipulation manipulation = manipulations[i];
                if (manipulation.IsValid(item, location, modifiers))
                    return manipulation;
            }
            return null;
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (m_CurrentManipulation is not { manipulationActive: true })
                return;
            try
            {
                m_ManipulationTime.UpdateTime(m_Canvas.WorldPixelToTime(evt.position.x, true));
                if (m_Canvas.snapToFrame)
                    m_ManipulationTime.SnapTimeToFrame(m_Canvas);
                if (edgeSnap ^ evt.actionKey)
                    m_ManipulationTime.SnapTimeToEdge(m_Canvas, m_SnapEngine, k_EdgeAttractionInPixels);

                m_CurrentManipulation.Process(m_ManipulationTime.initialTime, m_ManipulationTime.previewTime, m_ManipulationTime.manipulationTime);
                UpdateCursor(m_CurrentManipulation.GetCursor());
            }
            catch //release capture to avoid keeping a handle on the mouse pointer
            {
                End(evt);
                throw;
            }
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (m_CurrentManipulation is not { manipulationActive: true })
                return;

            try
            {
                m_CurrentManipulation.Apply();
            }
            finally
            {
                End(evt);
            }
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (m_CurrentManipulation is not { manipulationActive: true })
                return;

            if (evt.keyCode == KeyCode.Escape)
            {
                try
                {
                    m_CurrentManipulation.Cancel();
                }
                finally
                {
                    End(evt);
                }
                return;
            }

            ChangeManipulationIfNecessary(evt.modifiers);
        }

        void OnKeyUp(KeyUpEvent evt)
        {
            if (m_CurrentManipulation is not { manipulationActive: true })
                return;

            ChangeManipulationIfNecessary(evt.modifiers);
        }

        void End(EventBase evt)
        {
            EndCurrentManipulation();
            target.ReleaseMouse();
            m_CurrentManipulation = null;
            m_ManipulatedItem = null;
            evt.StopImmediatePropagation();

            if (m_ManipulatedEdgeHandle != null)
            {
                m_ManipulatedEdgeHandle.UnsetCursor();
                m_ManipulatedEdgeHandle = null;
            }
        }

        void UpdateCursor(Cursor editModeCursor)
        {
            if (m_ManipulatedEdgeHandle == null)
            {
                if (editModeCursor == target.style.cursor)
                    return;

                target.style.cursor = editModeCursor;
            }
            else
                m_ManipulatedEdgeHandle.cursor = editModeCursor;
        }
    }
}
