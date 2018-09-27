// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Experimental.UIElements
{
    public class IMGUIContainer : VisualElement
    {
        // Set this delegate to have your IMGUI code execute inside the container
        private readonly Action m_OnGUIHandler;

        // If needed, an IMGUIContainer will allocate native state via this utility object to store control IDs
        ObjectGUIState m_ObjectGUIState;

        internal ObjectGUIState guiState
        {
            get
            {
                Debug.Assert(!useOwnerObjectGUIState);
                if (m_ObjectGUIState == null)
                {
                    m_ObjectGUIState = new ObjectGUIState();
                }
                return m_ObjectGUIState;
            }
        }

        // This is not nice but needed until we properly remove the dependency on GUIView's own ObjectGUIState
        // At least this implementation is not needed for users, only for containers created to wrap each GUIView
        internal bool useOwnerObjectGUIState;
        internal Rect lastWorldClip { get; set; }

        private GUILayoutUtility.LayoutCache m_Cache = null;
        private GUILayoutUtility.LayoutCache cache
        {
            get
            {
                if (m_Cache == null)
                    m_Cache = new GUILayoutUtility.LayoutCache();
                return m_Cache;
            }
        }

        public ContextType contextType { get; set; }
        internal int GUIDepth { get; private set; }

        bool lostFocus = false;
        bool receivedFocus = false;
        FocusChangeDirection focusChangeDirection = FocusChangeDirection.unspecified;
        bool hasFocusableControls = false;

        int newKeyboardFocusControlID = 0;
        public override bool canGrabFocus
        {
            get { return base.canGrabFocus && hasFocusableControls; }
        }

        public IMGUIContainer(Action onGUIHandler)
        {
            m_OnGUIHandler = onGUIHandler;
            contextType = ContextType.Editor;
            focusIndex = 0;
        }

        internal override void DoRepaint(IStylePainter painter)
        {
            base.DoRepaint();

            lastWorldClip = painter.currentWorldClip;
            HandleIMGUIEvent(painter.repaintEvent);
        }

        internal override void ChangePanel(BaseVisualElementPanel p)
        {
            if (elementPanel != null)
            {
                elementPanel.IMGUIContainersCount--;
            }

            base.ChangePanel(p);

            if (elementPanel != null)
            {
                elementPanel.IMGUIContainersCount++;
            }
        }

        // global GUI values.
        // container saves and restores them before doing his thing
        private struct GUIGlobals
        {
            public Matrix4x4 matrix;
            public Color color;
            public Color contentColor;
            public Color backgroundColor;
            public bool enabled;
            public bool changed;
            public int displayIndex;
        }

        private GUIGlobals m_GUIGlobals;

        private void SaveGlobals()
        {
            m_GUIGlobals.matrix = GUI.matrix;
            m_GUIGlobals.color = GUI.color;
            m_GUIGlobals.contentColor = GUI.contentColor;
            m_GUIGlobals.backgroundColor = GUI.backgroundColor;
            m_GUIGlobals.enabled = GUI.enabled;
            m_GUIGlobals.changed = GUI.changed;
            m_GUIGlobals.displayIndex = Event.current.displayIndex;
        }

        private void RestoreGlobals()
        {
            GUI.matrix = m_GUIGlobals.matrix;
            GUI.color = m_GUIGlobals.color;
            GUI.contentColor = m_GUIGlobals.contentColor;
            GUI.backgroundColor = m_GUIGlobals.backgroundColor;
            GUI.enabled = m_GUIGlobals.enabled;
            GUI.changed = m_GUIGlobals.changed;
            Event.current.displayIndex = m_GUIGlobals.displayIndex;
        }

        private void DoOnGUI(Event evt)
        {
            // Extra checks are needed here because client code might have changed the IMGUIContainer
            // since we enter HandleIMGUIEvent()
            if (m_OnGUIHandler == null
                || panel == null)
            {
                return;
            }

            // Save the GUIClip count to make sanity checks after calling the OnGUI handler
            int guiClipCount = GUIClip.Internal_GetCount();

            SaveGlobals();

            UIElementsUtility.BeginContainerGUI(cache, evt, this);

            if (lostFocus)
            {
                GUIUtility.keyboardControl = 0;
                if (focusController != null)
                {
                    focusController.imguiKeyboardControl = 0;
                }
                lostFocus = false;
            }

            if (receivedFocus)
            {
                // If we just received the focus and GUIUtility.keyboardControl is not already one of our control,
                // set the GUIUtility.keyboardControl to our first or last focusable control.
                if (focusChangeDirection != FocusChangeDirection.unspecified && focusChangeDirection != FocusChangeDirection.none)
                {
                    // We assume we are using the VisualElementFocusRing.
                    if (focusChangeDirection == VisualElementFocusChangeDirection.left)
                    {
                        GUIUtility.SetKeyboardControlToLastControlId();
                    }
                    else if (focusChangeDirection == VisualElementFocusChangeDirection.right)
                    {
                        GUIUtility.SetKeyboardControlToFirstControlId();
                    }
                }
                receivedFocus = false;
                focusChangeDirection = FocusChangeDirection.unspecified;
                if (focusController != null)
                {
                    focusController.imguiKeyboardControl = GUIUtility.keyboardControl;
                }
            }
            // We intentionally don't send the NewKeuboardFocus command here since it creates an issue with the AutomatedWindow
            // newKeyboardFocusControlID = GUIUtility.keyboardControl;

            GUIDepth = GUIUtility.Internal_GetGUIDepth();
            EventType originalEventType = Event.current.type;

            bool isExitGUIException = false;

            try
            {
                m_OnGUIHandler();
            }
            catch (Exception exception)
            {
                // only for layout events: we always intercept any exceptions to not interrupt event processing
                if (originalEventType == EventType.Layout)
                {
                    isExitGUIException = GUIUtility.IsExitGUIException(exception);
                    if (!isExitGUIException)
                    {
                        Debug.LogException(exception);
                    }
                }
                else
                {
                    // rethrow event if not in layout
                    throw;
                }
            }
            finally
            {
                int result = GUIUtility.CheckForTabEvent(evt);
                if (focusController != null)
                {
                    if (result < 0)
                    {
                        // If CheckForTabEvent returns -1 or -2, we have reach the end/beginning of its control list.
                        // We should switch the focus to the next VisualElement.
                        KeyDownEvent e = null;
                        if (result == -1)
                        {
                            e = KeyDownEvent.GetPooled('\t', KeyCode.Tab, EventModifiers.None);
                        }
                        else if (result == -2)
                        {
                            e = KeyDownEvent.GetPooled('\t', KeyCode.Tab, EventModifiers.Shift);
                        }

                        var currentFocusedElement = focusController.focusedElement;
                        focusController.SwitchFocusOnEvent(e);

                        KeyDownEvent.ReleasePooled(e);

                        if (currentFocusedElement == this)
                        {
                            if (focusController.focusedElement == this)
                            {
                                // We still have the focus. We should cycle around our controls.
                                if (result == -2)
                                {
                                    GUIUtility.SetKeyboardControlToLastControlId();
                                }
                                else if (result == -1)
                                {
                                    GUIUtility.SetKeyboardControlToFirstControlId();
                                }

                                newKeyboardFocusControlID = GUIUtility.keyboardControl;
                                focusController.imguiKeyboardControl = GUIUtility.keyboardControl;
                            }
                            else
                            {
                                // We lost the focus. Set it to 0 until next IMGUIContainer have a chance to set it to its own control.
                                // Doing this will ensure we draw ourselves without any focused control.
                                GUIUtility.keyboardControl = 0;
                                focusController.imguiKeyboardControl = 0;
                            }
                        }
                    }
                    else if (result > 0)
                    {
                        // A positive result indicates that the focused control has changed.
                        focusController.imguiKeyboardControl = GUIUtility.keyboardControl;
                        newKeyboardFocusControlID = GUIUtility.keyboardControl;
                    }
                    else if (result == 0 && originalEventType == EventType.MouseDown)
                    {
                        // This means the event is not a tab but a MouseDown.
                        // Synchronize our focus info with IMGUI.
                        focusController.SyncIMGUIFocus(this);
                    }
                }

                // Cache the fact that we have focusable controls or not.
                hasFocusableControls = GUIUtility.HasFocusableControls();
            }

            // The Event will probably be nuked with the next function call, so we get its type now.
            EventType eventType = Event.current.type;

            UIElementsUtility.EndContainerGUI();
            RestoreGlobals();

            if (!isExitGUIException)
            {
                // This is the same logic as GUIClipState::EndOnGUI
                if (eventType != EventType.Ignore && eventType != EventType.Used)
                {
                    int currentCount = GUIClip.Internal_GetCount();
                    if (currentCount > guiClipCount)
                        Debug.LogError("GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced)");
                    else if (currentCount < guiClipCount)
                        Debug.LogError("GUI Error: You are popping more GUIClips than you are pushing. Make sure they are balanced)");
                }
            }

            // Clear extraneous GUIClips
            while (GUIClip.Internal_GetCount() > guiClipCount)
                GUIClip.Internal_Pop();

            if (eventType == EventType.Used)
            {
                Dirty(ChangeType.Repaint);
            }
        }

        public override void HandleEvent(EventBase evt)
        {
            base.HandleEvent(evt);

            if (evt.propagationPhase == PropagationPhase.DefaultAction)
            {
                return;
            }

            if (evt.imguiEvent == null)
            {
                return;
            }

            if (evt.isPropagationStopped)
            {
                return;
            }

            if (HandleIMGUIEvent(evt.imguiEvent))
            {
                evt.StopPropagation();
                evt.PreventDefault();
            }
        }

        internal bool HandleIMGUIEvent(Event e)
        {
            if (e == null || m_OnGUIHandler == null || elementPanel == null || elementPanel.IMGUIEventInterests.WantsEvent(e.type) == false)
            {
                return false;
            }

            EventType originalEventType = e.type;
            e.type = EventType.Layout;

            // layout event
            DoOnGUI(e);
            // the actual event
            e.type = originalEventType;
            DoOnGUI(e);

            if (newKeyboardFocusControlID > 0)
            {
                newKeyboardFocusControlID = 0;
                Event focusCommand = new Event();
                focusCommand.type = EventType.ExecuteCommand;
                focusCommand.commandName = "NewKeyboardFocus";

                HandleIMGUIEvent(focusCommand);
            }

            if (e.type == EventType.Used)
            {
                return true;
            }
            else if (e.type == EventType.MouseUp && this.HasCapture())
            {
                // This can happen if a MouseDown was caught by a different IM element but we ended up here on the
                // MouseUp event because no other element consumed it, including the one that had capture.
                // Example case: start text selection in a text field, but drag mouse all the way into another
                // part of the editor, release the mouse button.  Since the mouse up was sent to another container,
                // we end up here and that is perfectly legal (unfortunately unavoidable for now since no IMGUI control
                // used the event), but hot control might still belong to the IM text field at this point.
                // We can safely release the hot control which will release the capture as the same time.
                GUIUtility.hotControl = 0;
            }

            // If we detect that we were removed while processing this event, hi-jack the event loop to early exit
            // In IMGUI/Editor this is actually possible just by calling EditorWindow.Close() for example
            if (elementPanel == null)
            {
                GUIUtility.ExitGUI();
            }

            return false;
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            // no call to base.ExecuteDefaultAction(evt):
            // - we dont want mouse click to directly give focus to IMGUIContainer:
            //   they should be handled by IMGUI and if an IMGUI control grabs the
            //   keyboard, the IMGUIContainer will gain focus via FocusController.SyncIMGUIFocus.
            // - same thing for tabs: IMGUI should handle them.
            // - we dont want to set the PseudoState.Focus flag on IMGUIContainer.
            //   They are focusable, but only for the purpose of focusing their children.

            if (evt.GetEventTypeId() == BlurEvent.TypeId())
            {
                BlurEvent be = evt as BlurEvent;
                if (be.relatedTarget == null || !be.relatedTarget.canGrabFocus)
                {
                    lostFocus = true;
                }
            }
            else if (evt.GetEventTypeId() == FocusEvent.TypeId())
            {
                FocusEvent fe = evt as FocusEvent;
                receivedFocus = true;
                focusChangeDirection = fe.direction;
            }
        }

        protected internal override Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight, MeasureMode heightMode)
        {
            float measuredWidth = float.NaN;
            float measuredHeight = float.NaN;

            if (widthMode != MeasureMode.Exactly || heightMode != MeasureMode.Exactly)
            {
                DoOnGUI(new Event { type = EventType.Layout });
                measuredWidth = m_Cache.topLevel.minWidth;
                measuredHeight = m_Cache.topLevel.minHeight;
            }

            switch (widthMode)
            {
                case MeasureMode.Exactly:
                    measuredWidth = desiredWidth;
                    break;
                case MeasureMode.AtMost:
                    measuredWidth = Mathf.Min(measuredWidth, desiredWidth);
                    break;
            }

            switch (heightMode)
            {
                case MeasureMode.Exactly:
                    measuredHeight = desiredHeight;
                    break;
                case MeasureMode.AtMost:
                    measuredHeight = Mathf.Min(measuredHeight, desiredHeight);
                    break;
            }

            return new Vector2(measuredWidth, measuredHeight);
        }
    }
}
