// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    public class IMGUIContainer : VisualElement, IDisposable
    {
        public new class UxmlFactory : UxmlFactory<IMGUIContainer, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            public UxmlTraits()
            {
                focusIndex.defaultValue = 0;
                focusable.defaultValue = true;
            }

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }

        // Set this delegate to have your IMGUI code execute inside the container
        private Action m_OnGUIHandler;
        public Action onGUIHandler
        {
            get { return m_OnGUIHandler; }
            set
            {
                if (m_OnGUIHandler != value)
                {
                    m_OnGUIHandler = value;
                    IncrementVersion(VersionChangeType.Layout);
                    IncrementVersion(VersionChangeType.Repaint);
                }
            }
        }

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

        // We cache the clipping rect during regular painting so that we can reuse it
        // during the DoMeasure call to DoOnGUI(). It's still important to not
        // pass Rect.zero for the clipping rect as this eventually sets the
        // global GUIClip.visibleRect which IMGUI code could be using to influence
        // size. See case 1111923.
        private Rect m_CachedClippingRect;

        private float layoutMeasuredWidth
        {
            get
            {
                return Mathf.Ceil(cache.topLevel.maxWidth);
            }
        }

        private float layoutMeasuredHeight
        {
            get
            {
                return Mathf.Ceil(cache.topLevel.maxHeight);
            }
        }

        public ContextType contextType { get; set; }

        // The following 2 flags indicate the following :
        // 1) lostFocus : a blur event occured and we need to make sure the actual keyboard focus from IMGUI is really un-focused
        bool lostFocus = false;
        // 2) receivedFocus : a Focus event occured and we need to focus the actual IMGUIContainer as being THE element focused.
        bool receivedFocus = false;
        FocusChangeDirection focusChangeDirection = FocusChangeDirection.unspecified;
        bool hasFocusableControls = false;

        int newKeyboardFocusControlID = 0;

        internal bool focusOnlyIfHasFocusableControls { get; set; } = true;

        public override bool canGrabFocus => focusOnlyIfHasFocusableControls ? hasFocusableControls && base.canGrabFocus : base.canGrabFocus;

        public static readonly string ussClassName = "unity-imgui-container";

        public IMGUIContainer()
            : this(null)
        {
        }

        public IMGUIContainer(Action onGUIHandler)
        {
            AddToClassList(ussClassName);

            this.onGUIHandler = onGUIHandler;
            contextType = ContextType.Editor;
            focusable = true;

            requireMeasureFunction = true;
            generateVisualContent += OnGenerateVisualContent;
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            lastWorldClip = elementPanel.repaintData.currentWorldClip;

            // Access to the painter is internal and is not exposed to public
            // The IStylePainter is kept as an interface rather than a concrete class for now to support tests
            mgc.painter.DrawImmediate(DoIMGUIRepaint);
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
            if (Event.current != null)
            {
                m_GUIGlobals.displayIndex = Event.current.displayIndex;
            }
        }

        private void RestoreGlobals()
        {
            GUI.matrix = m_GUIGlobals.matrix;
            GUI.color = m_GUIGlobals.color;
            GUI.contentColor = m_GUIGlobals.contentColor;
            GUI.backgroundColor = m_GUIGlobals.backgroundColor;
            GUI.enabled = m_GUIGlobals.enabled;
            GUI.changed = m_GUIGlobals.changed;
            if (Event.current != null)
            {
                Event.current.displayIndex = m_GUIGlobals.displayIndex;
            }
        }

        private void DoOnGUI(Event evt, Matrix4x4 parentTransform, Rect clippingRect, bool isComputingLayout, Rect layoutSize, bool eventIsPropagatedFromNonFocusableVisualElement = false)
        {
            // If we are computing the layout, we should not try to get the worldTransform...
            // it is dependant on the layout, which is being calculated (thus, not good)
            // therefore, we should be passing in the identity matrix as our parent transform
            // we use this below to push a clip scope, we used to simply not push this assuming it had already been done somewhere,
            // this is not the case for all IMGUIContainers which can lead to attempts to access the root clipping scope
            Debug.Assert(!isComputingLayout || parentTransform == Matrix4x4.identity);

            // Extra checks are needed here because client code might have changed the IMGUIContainer
            // since we enter HandleIMGUIEvent()
            if (onGUIHandler == null
                || panel == null)
            {
                return;
            }

            // Save the GUIClip count to make sanity checks after calling the OnGUI handler
            int guiClipCount = GUIClip.Internal_GetCount();

            SaveGlobals();

            // Save a copy of the container size.
            var previousMeasuredWidth = layoutMeasuredWidth;
            var previousMeasuredHeight = layoutMeasuredHeight;

            UIElementsUtility.BeginContainerGUI(cache, evt, this);

            // For the IMGUI, we need to update the GUI.color with the actual play mode tint ...
            // In fact, this is taken from EditorGUIUtility.ResetGUIState().
            // Here, the play mode tint is either white (no tint, or not in play mode) or the right color (if in play mode)
            GUI.color = UIElementsUtility.editorPlayModeTintColor;
            // From now on, Event.current is either evt or a copy of evt.
            // Since Event.current may change while being processed, we do not rely on evt below but use Event.current instead.

            if (Event.current.type != EventType.Layout)
            {
                if (lostFocus)
                {
                    if (focusController != null)
                    {
                        // We dont want to clear the GUIUtility.keyboardControl if another IMGUIContainer
                        // just set it in the if (receivedFocus) block below. So we only clear it if own it.
                        if (GUIUtility.OwnsId(GUIUtility.keyboardControl))
                        {
                            GUIUtility.keyboardControl = 0;
                            focusController.imguiKeyboardControl = 0;
                        }
                    }
                    lostFocus = false;
                }

                if (receivedFocus)
                {
                    if (hasFocusableControls)
                    {
                        if (focusChangeDirection != FocusChangeDirection.unspecified && focusChangeDirection != FocusChangeDirection.none)
                        {
                            // We got here by tabbing.

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
                        else if (GUIUtility.keyboardControl == 0)
                        {
                            // Since GUIUtility.keyboardControl == 0, we got focused in some other way than by clicking inside us
                            // (for example it could be by clicking in an element that delegates focus to us).
                            // Give GUIUtility.keyboardControl to our first control.
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
                // We intentionally don't send the NewKeyboardFocus command here since it creates an issue with the AutomatedWindow
                // newKeyboardFocusControlID = GUIUtility.keyboardControl;
            }

            EventType originalEventType = Event.current.type;

            bool isExitGUIException = false;

            try
            {
                using (new GUIClip.ParentClipScope(parentTransform, clippingRect))
                {
                    onGUIHandler();
                }
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
                if (Event.current.type != EventType.Layout && !eventIsPropagatedFromNonFocusableVisualElement)
                {
                    int currentKeyboardFocus = GUIUtility.keyboardControl;
                    int result = GUIUtility.CheckForTabEvent(Event.current);
                    if (focusController != null)
                    {
                        if (result < 0)
                        {
                            // If CheckForTabEvent returns -1 or -2, we have reach the end/beginning of its control list.
                            // We should switch the focus to the next VisualElement.
                            Focusable currentFocusedElement = focusController.GetLeafFocusedElement();
                            Focusable nextFocusedElement = null;
                            using (KeyDownEvent e = KeyDownEvent.GetPooled('\t', KeyCode.Tab, result == -1 ? EventModifiers.None : EventModifiers.Shift))
                            {
                                nextFocusedElement = focusController.SwitchFocusOnEvent(e);
                            }

                            if (currentFocusedElement == this)
                            {
                                if (nextFocusedElement == this)
                                {
                                    // We will still have the focus. We should cycle around our controls.
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
                                    // We will lose the focus. Set the focused element ID to 0 until next
                                    // IMGUIContainer have a chance to set it to its own control.
                                    // Doing this will ensure we draw ourselves without any focused control.
                                    GUIUtility.keyboardControl = 0;
                                    focusController.imguiKeyboardControl = 0;
                                }
                            }
                        }
                        else if (result > 0)
                        {
                            // A positive result indicates that the focused control has changed to one of our elements; result holds the control id.
                            focusController.imguiKeyboardControl = GUIUtility.keyboardControl;
                            newKeyboardFocusControlID = GUIUtility.keyboardControl;
                        }
                        else if (result == 0)
                        {
                            // This means the event is not a tab. Synchronize our focus info with IMGUI.

                            if (originalEventType == EventType.MouseDown && !focusOnlyIfHasFocusableControls)
                            {
                                focusController.SyncIMGUIFocus(GUIUtility.keyboardControl, this, true);
                            }
                            else if ((currentKeyboardFocus != GUIUtility.keyboardControl) || (originalEventType == EventType.MouseDown))
                            {
                                focusController.SyncIMGUIFocus(GUIUtility.keyboardControl, this, false);
                            }
                            else if (GUIUtility.keyboardControl != focusController.imguiKeyboardControl)
                            {
                                // Here we want to resynchronize our internal state ...
                                newKeyboardFocusControlID = GUIUtility.keyboardControl;

                                if (focusController.GetLeafFocusedElement() == this)
                                {
                                    // In this case, the focused element is the right one in the Focus Controller... we are just updating the internal imguiKeyboardControl
                                    focusController.imguiKeyboardControl = GUIUtility.keyboardControl;
                                }
                                else
                                {
                                    // In this case, the focused element is NOT the right one in the Focus Controller... we also have to refocus...
                                    focusController.SyncIMGUIFocus(GUIUtility.keyboardControl, this, false);
                                }
                            }
                        }
                    }
                    // Cache the fact that we have focusable controls or not.
                    hasFocusableControls = GUIUtility.HasFocusableControls();
                }
            }

            // This will copy Event.current into evt.
            UIElementsUtility.EndContainerGUI(evt, layoutSize);
            RestoreGlobals();

            // See if the container size has changed. This is to make absolutely sure the VisualElement resizes
            // if the IMGUI content resizes.
            if (evt.type == EventType.Layout &&
                (!Mathf.Approximately(previousMeasuredWidth, layoutMeasuredWidth) || !Mathf.Approximately(previousMeasuredHeight, layoutMeasuredHeight)))
            {
                IncrementVersion(VersionChangeType.Layout);
            }

            if (!isExitGUIException)
            {
                // This is the same logic as GUIClipState::EndOnGUI
                if (evt.type != EventType.Ignore && evt.type != EventType.Used)
                {
                    int currentCount = GUIClip.Internal_GetCount();
                    if (currentCount > guiClipCount)
                        Debug.LogError("GUI Error: You are pushing more GUIClips than you are popping. Make sure they are balanced.");
                    else if (currentCount < guiClipCount)
                        Debug.LogError("GUI Error: You are popping more GUIClips than you are pushing. Make sure they are balanced.");
                }
            }

            // Clear extraneous GUIClips
            while (GUIClip.Internal_GetCount() > guiClipCount)
                GUIClip.Internal_Pop();

            if (evt.type == EventType.Used)
            {
                IncrementVersion(VersionChangeType.Repaint);
            }
        }

        public void MarkDirtyLayout()
        {
            IncrementVersion(VersionChangeType.Layout);
        }

        public override void HandleEvent(EventBase evt)
        {
            base.HandleEvent(evt);

            if (evt == null)
            {
                return;
            }

            if (evt.propagationPhase != PropagationPhase.TrickleDown &&
                evt.propagationPhase != PropagationPhase.AtTarget &&
                evt.propagationPhase != PropagationPhase.BubbleUp)
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

            if (SendEventToIMGUI(evt))
            {
                evt.StopPropagation();
                evt.PreventDefault();
            }
        }

        // This is the IStylePainterInternal.DrawImmediate callback
        private void DoIMGUIRepaint()
        {
            var offset = elementPanel.repaintData.currentOffset;
            HandleIMGUIEvent(elementPanel.repaintData.repaintEvent, offset * worldTransform, ComputeAAAlignedBound(worldClip, offset), false);
        }

        internal bool SendEventToIMGUI(EventBase evt)
        {
            if (evt is IPointerEvent)
            {
                // Pointer events are not handled by IMGUI. The compatibility mouse event will eventually come.
                return false;
            }

            bool result;
            using (new EventDebuggerLogIMGUICall(evt))
            {
                result = HandleIMGUIEvent(evt.imguiEvent);
            }
            return result;
        }

        internal bool HandleIMGUIEvent(Event e, bool eventIsPropagatedFromNonFocusableVisualElement = false)
        {
            Matrix4x4 currentTransform;
            GetCurrentTransformAndClip(this, e, out currentTransform, out m_CachedClippingRect);

            return HandleIMGUIEvent(e, currentTransform, m_CachedClippingRect, eventIsPropagatedFromNonFocusableVisualElement);
        }

        private bool HandleIMGUIEvent(Event e, Matrix4x4 worldTransform, Rect clippingRect, bool eventIsPropagatedFromNonFocusableVisualElement)
        {
            if (e == null || onGUIHandler == null || elementPanel == null || elementPanel.IMGUIEventInterests.WantsEvent(e.type) == false)
            {
                return false;
            }

            EventType originalEventType = e.type;
            e.type = EventType.Layout;

            // layout event
            DoOnGUI(e, worldTransform, clippingRect, false, layout, eventIsPropagatedFromNonFocusableVisualElement);
            // the actual event
            e.type = originalEventType;
            DoOnGUI(e, worldTransform, clippingRect, false, layout, eventIsPropagatedFromNonFocusableVisualElement);

            if (newKeyboardFocusControlID > 0)
            {
                newKeyboardFocusControlID = 0;
                Event focusCommand = new Event
                {
                    type = EventType.ExecuteCommand,
                    commandName = EventCommandNames.NewKeyboardFocus
                };

                HandleIMGUIEvent(focusCommand);
            }

            if (e.type == EventType.Used)
            {
                return true;
            }
            else if (e.type == EventType.MouseUp && this.HasMouseCapture())
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

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            if (evt == null)
            {
                return;
            }

            // no call to base.ExecuteDefaultAction(evt):
            // - we dont want mouse click to directly give focus to IMGUIContainer:
            //   they should be handled by IMGUI and if an IMGUI control grabs the
            //   keyboard, the IMGUIContainer will gain focus via FocusController.SyncIMGUIFocus.
            // - same thing for tabs: IMGUI should handle them.
            // - we dont want to set the PseudoState.Focus flag on IMGUIContainer.
            //   They are focusable, but only for the purpose of focusing their children.

            // Here, we set flags that will be acted upon in DoOnGUI(), since we need to change IMGUI state.
            if (evt.eventTypeId == BlurEvent.TypeId())
            {
                // A lost focus event is ... a lost focus event.
                // The specific handling of the IMGUI will be done in the DoOnGUI() above...
                lostFocus = true;

                // On lost focus, we need to repaint to remove any focused element blue borders.
                IncrementVersion(VersionChangeType.Repaint);
            }
            else if (evt.eventTypeId == FocusEvent.TypeId())
            {
                FocusEvent fe = evt as FocusEvent;
                receivedFocus = true;
                focusChangeDirection = fe.direction;
            }
            else if (evt.eventTypeId == DetachFromPanelEvent.TypeId())
            {
                if (elementPanel != null)
                {
                    elementPanel.IMGUIContainersCount--;
                }
            }
            else if (evt.eventTypeId == AttachToPanelEvent.TypeId())
            {
                if (elementPanel != null)
                {
                    elementPanel.IMGUIContainersCount++;
                }
            }
        }

        protected internal override Vector2 DoMeasure(float desiredWidth, MeasureMode widthMode, float desiredHeight, MeasureMode heightMode)
        {
            float measuredWidth = float.NaN;
            float measuredHeight = float.NaN;

            if (widthMode != MeasureMode.Exactly || heightMode != MeasureMode.Exactly)
            {
                var evt = new Event { type = EventType.Layout };
                var layoutRect = layout;
                // Make sure the right width/height will be used at the final stage of the calculation
                switch (widthMode)
                {
                    case MeasureMode.Exactly:
                        layoutRect.width = desiredWidth;
                        break;
                }
                switch (heightMode)
                {
                    case MeasureMode.Exactly:
                        layoutRect.height = desiredHeight;
                        break;
                }
                // When computing layout it's important to not call GetCurrentTransformAndClip
                // because it will remove the dirty flag on the container transform which might
                // set the transform in an invalid state. That's why we have to pass
                // identity and a cached clipping state here. It's still important to not
                // pass Rect.zero for the clipping rect as this eventually sets the
                // global GUIClip.visibleRect which IMGUI code could be using to influence
                // size. See case 1111923.
                DoOnGUI(evt, Matrix4x4.identity, m_CachedClippingRect, true, layoutRect);
                measuredWidth = layoutMeasuredWidth;
                measuredHeight = layoutMeasuredHeight;
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

        private static void GetCurrentTransformAndClip(IMGUIContainer container, Event evt, out Matrix4x4 transform, out Rect clipRect)
        {
            clipRect = container.lastWorldClip;
            if (clipRect.width == 0.0f || clipRect.height == 0.0f)
            {
                // lastWorldClip will be empty until the first repaint occurred,
                // we fall back on the worldBound in this case.
                clipRect = container.worldBound;
            }

            transform = container.worldTransform;
            if (evt.type == EventType.Repaint
                && container.elementPanel != null)
            {
                // during repaint, we must use in case the current transform is not relative to Panel
                // this is to account for the pixel caching feature
                transform =  container.elementPanel.repaintData.currentOffset * container.worldTransform;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                m_ObjectGUIState?.Dispose();
            }
        }
    }
}
