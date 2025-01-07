// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Manipulator that tracks pointer events on an element and callbacks when the elements is clicked.
    /// </summary>
    /// <remarks>
    /// See <see cref="Clickable.clicked"/> for more information on what it means for an element to be clicked
    /// in the context of this manipulator.
    /// </remarks>
    public class Clickable : PointerManipulator
    {
        /// <summary>
        /// Callback triggered when the target element is clicked, including event data.
        /// </summary>
        /// <remarks>
        /// See <see cref="Clickable.clicked"/> for more information on when the @@clicked@@ and
        /// @@clickedWithEventInfo@@ events are invoked.
        /// The callback methods registered to this event should have an <see cref="EventBase"/> parameter and
        /// return no value.
        /// </remarks>
        /// <seealso cref="Clickable.clicked"/>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// public VisualElement CreateButton()
        /// {
        ///     var button = new Button { text = "Press Me" };
        ///     button.clickedWithEventInfo += (EventBase evt) =>
        ///     {
        ///         int clickCount = ((IPointerEvent)evt).clickCount;
        ///         if (clickCount == 1)
        ///             Debug.Log("Button was single-clicked.");
        ///         else if (clickCount == 2)
        ///             Debug.Log("Button was double-clicked.");
        ///     };
        ///     return button;
        /// }
        /// ]]>
        /// </code>
        /// </example>
        public event Action<EventBase> clickedWithEventInfo;

        /// <summary>
        /// Callback triggered when the target element is clicked.
        /// </summary>
        /// <remarks>
        /// The @@clicked@@ and @@clickedWithEventInfo@@ events are invoked when any of the following conditions occur:
        ///
        ///- The target receives a <see cref="NavigationSubmitEvent"/>.
        ///- The target receives a <see cref="PointerDownEvent"/> followed by a <see cref="PointerUpEvent"/>.
        ///
        /// If the @@delay@@ and @@interval@@ optional constructor parameters are used, then the @@clicked@@ event is
        /// considered repeatable and is instead invoked when any of the following conditions occur:
        ///
        ///- The target receives a <see cref="NavigationSubmitEvent"/>.
        ///- The target just received a <see cref="PointerDownEvent"/>.
        ///- The target has received a <see cref="PointerDownEvent"/> and the pointer button has been held down for a given period of time.
        ///
        /// If the @@clicked@@ event is repeatable, then the first repeated click occurs after an amount of time
        /// corresponding to the @@delay@@ parameter, and subsequent clicks occur after amounts of time corresponding
        /// to the @@interval@@ parameter.
        ///
        /// The callback methods registered to this event should have no parameters and return no value.
        ///
        /// This manipulator makes use of pointer capture.
        /// </remarks>
        /// <seealso cref="Clickable.clickedWithEventInfo"/>
        /// <seealso cref="NavigationSubmitEvent"/>
        /// <seealso cref="PointerDownEvent"/>
        /// <seealso cref="PointerCaptureEvent"/>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// public VisualElement CreateButton()
        /// {
        ///     var button = new Button { text = "Press Me" };
        ///     button.clicked += () =>
        ///     {
        ///         Debug.Log("Button was pressed!");
        ///     };
        ///     return button;
        /// }
        /// ]]>
        /// </code>
        /// </example>
        public event Action clicked;

        private readonly long m_Delay; // in milliseconds
        private readonly long m_Interval; // in milliseconds

        /// <summary>
        /// This property tracks the activation of the manipulator. Set it to true when the manipulator is activated.
        /// </summary>
        protected bool active { get; set; }

        /// <summary>
        /// Specifies the mouse position saved during the last mouse event on the target Element.
        /// </summary>
        public Vector2 lastMousePosition { get; private set; }

        private int m_ActivePointerId = -1; // The pointer that set the active state to true, if any

        private bool m_AcceptClicksIfDisabled;

        internal bool acceptClicksIfDisabled
        {
            get => m_AcceptClicksIfDisabled;
            set
            {
                if (m_AcceptClicksIfDisabled == value)
                    return;

                if (target != null)
                    UnregisterCallbacksFromTarget();

                m_AcceptClicksIfDisabled = value;

                if (target != null)
                    RegisterCallbacksOnTarget();
            }
        }

        private InvokePolicy invokePolicy =>
            acceptClicksIfDisabled ? InvokePolicy.IncludeDisabled : InvokePolicy.Default;

        private IVisualElementScheduledItem m_Repeater;
        private IVisualElementScheduledItem m_PendingActivePseudoStateReset;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// When you use this constructor, a click event is invoked repeatedly at regular intervals
        /// for as long as the pointer is held down on the target element.
        /// </remarks>
        /// <seealso cref="Clickable.clicked"/>
        /// <param name="handler">The method to call when the clickable is clicked.</param>
        /// <param name="delay">Determines when the event begins. Value is defined in milliseconds. Applies if delay is greater than @@0@@.</param>
        /// <param name="interval">Determines the time delta between event repetition. Value is defined in milliseconds. Applies if interval is greater than @@0@@.</param>
        public Clickable(Action handler, long delay, long interval) : this(handler)
        {
            m_Delay = delay;
            m_Interval = interval;
            active = false;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// When you use this constructor, the event (usually a <see cref="PointerUpEvent"/> or a <see cref="NavigationSubmitEvent"/>)
        /// that triggered the click is passed as an argument to the handler method.
        /// </remarks>
        /// <seealso cref="Clickable.clickedWithEventInfo"/>
        /// <seealso cref="Clickable.clicked"/>
        /// <param name="handler">The method to call when the clickable is clicked.</param>
        public Clickable(Action<EventBase> handler)
        {
            clickedWithEventInfo = handler;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
        }

        // Click-once type constructor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <seealso cref="Clickable.clicked"/>
        /// <param name="handler">The method to call when the clickable is clicked.</param>
        public Clickable(Action handler)
        {
            clicked = handler;

            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });

            active = false;
        }

        private void OnTimer(TimerState timerState)
        {
            if ((clicked != null || clickedWithEventInfo != null) && IsRepeatable())
            {
                if (ContainsPointer(m_ActivePointerId) && (target.enabledInHierarchy || acceptClicksIfDisabled))
                {
                    Invoke(null);
                    target.pseudoStates |= PseudoStates.Active;
                }
                else
                {
                    target.pseudoStates &= ~PseudoStates.Active;
                }
            }
        }

        private bool IsRepeatable()
        {
            return (m_Delay > 0 || m_Interval > 0);
        }

        /// <summary>
        /// Called to register mouse event callbacks on the target element.
        /// </summary>
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown, invokePolicy);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove, invokePolicy);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp, InvokePolicy.IncludeDisabled);
            target.RegisterCallback<PointerCancelEvent>(OnPointerCancel, InvokePolicy.IncludeDisabled);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut, InvokePolicy.IncludeDisabled);
        }

        /// <summary>
        /// Called to unregister event callbacks from the target element.
        /// </summary>
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

            ResetActivePseudoState();
        }

        [Obsolete("OnMouseDown has been removed and replaced by its pointer-based equivalent. Please use OnPointerDown.", false)]
        protected void OnMouseDown(MouseDownEvent evt)
        {
            if (CanStartManipulation(evt))
                ProcessDownEvent(evt, evt.localMousePosition, PointerId.mousePointerId);
        }

        [Obsolete("OnMouseMove has been removed and replaced by its pointer-based equivalent. Please use OnPointerMove.", false)]
        protected void OnMouseMove(MouseMoveEvent evt)
        {
            if (active)
                ProcessMoveEvent(evt, evt.localMousePosition);
        }

        [Obsolete("OnMouseUp has been removed and replaced by its pointer-based equivalent. Please use OnPointerUp.", false)]
        protected void OnMouseUp(MouseUpEvent evt)
        {
            if (active && CanStopManipulation(evt))
                ProcessUpEvent(evt, evt.localMousePosition, PointerId.mousePointerId);
        }

        /// <summary>
        /// This method is called when a PointerDownEvent is sent to the target element.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected void OnPointerDown(PointerDownEvent evt)
        {
            if (!CanStartManipulation(evt)) return;

            ProcessDownEvent(evt, evt.localPosition, evt.pointerId);
        }

        /// <summary>
        /// This method is called when a PointerMoveEvent is sent to the target element.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected void OnPointerMove(PointerMoveEvent evt)
        {
            if (!active) return;

            ProcessMoveEvent(evt, evt.localPosition);
        }

        /// <summary>
        /// This method is called when a PointerUpEvent is sent to the target element.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected void OnPointerUp(PointerUpEvent evt)
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

        bool ContainsPointer(int pointerId)
        {
            var elementUnderPointer = target.elementPanel.GetTopElementUnderPointer(pointerId);
            return target == elementUnderPointer || target.Contains(elementUnderPointer);
        }

        /// <summary>
        /// Invokes a click action.
        /// </summary>
        protected void Invoke(EventBase evt)
        {
            clicked?.Invoke();
            clickedWithEventInfo?.Invoke(evt);
        }

        internal void SimulateSingleClick(EventBase evt, int delayMs = 100)
        {
            target.pseudoStates |= PseudoStates.Active;
            m_PendingActivePseudoStateReset = target.schedule.Execute(ResetActivePseudoState);
            m_PendingActivePseudoStateReset.ExecuteLater(delayMs);
            Invoke(evt);
        }

        private void ResetActivePseudoState()
        {
            if (m_PendingActivePseudoStateReset == null)
                return;
            target.pseudoStates &= ~PseudoStates.Active;
            m_PendingActivePseudoStateReset = null;
        }

        /// <summary>
        /// This method processes the down event sent to the target Element.
        /// </summary>
        protected virtual void ProcessDownEvent(EventBase evt, Vector2 localPosition, int pointerId)
        {
            active = true;
            m_ActivePointerId = pointerId;
            target.CapturePointer(pointerId);
            if (!(evt is IPointerEvent))
                target.panel.ProcessPointerCapture(pointerId);

            lastMousePosition = localPosition;
            if (IsRepeatable())
            {
                // Repeatable button clicks are performed on the MouseDown and at timer events
                if (ContainsPointer(pointerId) && (target.enabledInHierarchy || acceptClicksIfDisabled))
                {
                    Invoke(evt);
                }

                if (m_Repeater == null)
                {
                    m_Repeater = target.schedule.Execute(OnTimer).Every(m_Interval).StartingIn(m_Delay);
                }
                else
                {
                    m_Repeater.ExecuteLater(m_Delay);
                }
            }

            target.pseudoStates |= PseudoStates.Active;

            evt.StopImmediatePropagation();
        }

        /// <summary>
        /// This method processes the move event sent to the target Element.
        /// </summary>
        protected virtual void ProcessMoveEvent(EventBase evt, Vector2 localPosition)
        {
            lastMousePosition = localPosition;

            if (ContainsPointer(m_ActivePointerId))
            {
                target.pseudoStates |= PseudoStates.Active;
            }
            else
            {
                target.pseudoStates &= ~PseudoStates.Active;
            }

            evt.StopPropagation();
        }

        /// <summary>
        /// This method processes the up event sent to the target Element.
        /// </summary>
        protected virtual void ProcessUpEvent(EventBase evt, Vector2 localPosition, int pointerId)
        {
            active = false;
            m_ActivePointerId = -1;
            target.ReleasePointer(pointerId);
            if (!(evt is IPointerEvent))
                target.panel.ProcessPointerCapture(pointerId);

            target.pseudoStates &= ~PseudoStates.Active;

            if (IsRepeatable())
            {
                // Repeatable button clicks are performed on the MouseDown and at timer events only
                m_Repeater?.Pause();
            }
            else
            {
                // Non repeatable button clicks are performed on the MouseUp
                if (ContainsPointer(pointerId) && (target.enabledInHierarchy || acceptClicksIfDisabled))
                {
                    Invoke(evt);
                }
            }

            evt.StopPropagation();
        }

        /// <summary>
        /// This method processes the up cancel sent to the target Element.
        /// </summary>
        protected virtual void ProcessCancelEvent(EventBase evt, int pointerId)
        {
            active = false;
            m_ActivePointerId = -1;
            target.ReleasePointer(pointerId);
            if (!(evt is IPointerEvent))
                target.panel.ProcessPointerCapture(pointerId);

            target.pseudoStates &= ~PseudoStates.Active;

            if (IsRepeatable())
            {
                m_Repeater?.Pause();
            }

            evt.StopPropagation();
        }
    }
}
