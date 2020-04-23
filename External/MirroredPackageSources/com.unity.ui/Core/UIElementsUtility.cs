using System;
using System.Collections.Generic;
using Unity.Profiling;

namespace UnityEngine.UIElements
{
    internal interface IUIElementsUtility
    {
        bool TakeCapture();
        bool ReleaseCapture();
        bool ProcessEvent(int instanceID,  IntPtr nativeEventPtr, ref bool eventHandled);
        bool CleanupRoots();
        bool EndContainerGUIFromException(Exception exception);
        bool MakeCurrentIMGUIContainerDirty();

        void UpdateSchedulers();
        void RequestRepaintForPanels(Action<ScriptableObject> repaintCallback);
    }

    internal static class UIEventRegistration
    {
        private static List<IUIElementsUtility> s_Utilities = new List<IUIElementsUtility>();

        static UIEventRegistration()
        {
            GUIUtility.takeCapture += () => TakeCapture();
            GUIUtility.releaseCapture += () => ReleaseCapture();
            GUIUtility.processEvent += (i, ptr) => { return ProcessEvent(i, ptr); };

            GUIUtility.cleanupRoots += () => CleanupRoots();
            GUIUtility.endContainerGUIFromException += exception => EndContainerGUIFromException(exception);

            GUIUtility.guiChanged += () => MakeCurrentIMGUIContainerDirty();
        }

        internal static void RegisterUIElementSystem(IUIElementsUtility utility)
        {
            s_Utilities.Insert(0, utility);
        }

        private static void TakeCapture()
        {
            foreach (var uiElementsUtility in s_Utilities)
            {
                if (uiElementsUtility.TakeCapture())
                    return;
            }
        }

        private static void ReleaseCapture()
        {
            foreach (var uiElementsUtility in s_Utilities)
            {
                if (uiElementsUtility.ReleaseCapture())
                    return;
            }
        }

        private static bool EndContainerGUIFromException(Exception exception)
        {
            foreach (var uiElementsUtility in s_Utilities)
            {
                if (uiElementsUtility.EndContainerGUIFromException(exception))
                    return true;
            }
            return GUIUtility.ShouldRethrowException(exception);
        }

        private static bool ProcessEvent(int instanceID, IntPtr nativeEventPtr)
        {
            bool eventHandled = false;
            foreach (var uiElementsUtility in s_Utilities)
            {
                if (uiElementsUtility.ProcessEvent(instanceID, nativeEventPtr, ref eventHandled))
                {
                    return eventHandled;
                }
            }

            return false;
        }

        private static void CleanupRoots()
        {
            foreach (var uiElementsUtility in s_Utilities)
            {
                if (uiElementsUtility.CleanupRoots())
                    return;
            }
        }

        internal static void MakeCurrentIMGUIContainerDirty()
        {
            foreach (var uiElementsUtility in s_Utilities)
            {
                if (uiElementsUtility.MakeCurrentIMGUIContainerDirty())
                    return;
            }
        }

        internal static void UpdateSchedulers()
        {
            foreach (var uiElementsUtility in s_Utilities)
            {
                uiElementsUtility.UpdateSchedulers();
            }
        }

        internal static void RequestRepaintForPanels(Action<ScriptableObject> repaintCallback)
        {
            foreach (var uiElementsUtility in s_Utilities)
            {
                uiElementsUtility.RequestRepaintForPanels(repaintCallback);
            }
        }
    }
    internal class UIElementsUtility : IUIElementsUtility
    {
        private static Stack<IMGUIContainer> s_ContainerStack = new Stack<IMGUIContainer>();
        private static Dictionary<int, Panel> s_UIElementsCache = new Dictionary<int, Panel>();

        private static Event s_EventInstance = new Event(); // event instance reused for ProcessEvent()

        // When not in editor, this will be all white, so no impact on the overall color, except for the multiplication done on the color.
        internal static Color editorPlayModeTintColor = Color.white;

        internal static Action<IMGUIContainer> s_BeginContainerCallback;
        internal static Action<IMGUIContainer> s_EndContainerCallback;

        private static UIElementsUtility s_Instance = new UIElementsUtility();

        private UIElementsUtility()
        {
            UIEventRegistration.RegisterUIElementSystem(this);
//            GUIUtility.takeCapture += () => ((IUIElementsUtility)this).TakeCapture();
//            GUIUtility.releaseCapture += () => ((IUIElementsUtility)this).ReleaseCapture();
//            GUIUtility.processEvent += (i, ptr) =>
//            {
//                bool handled = false;
//                ((IUIElementsUtility)this).ProcessEvent(i, ptr, ref handled);
//                return handled;
//            };
//            GUIUtility.cleanupRoots += () => ((IUIElementsUtility)this).CleanupRoots();
//            GUIUtility.endContainerGUIFromException += exception => ((IUIElementsUtility)this).EndContainerGUIFromException(exception);
//
//            GUIUtility.guiChanged += () => ((IUIElementsUtility)this).MakeCurrentIMGUIContainerDirty();
        }

        internal static IMGUIContainer GetCurrentIMGUIContainer()
        {
            if (s_ContainerStack.Count > 0)
            {
                return s_ContainerStack.Peek();
            }

            return null;
        }

        bool IUIElementsUtility.MakeCurrentIMGUIContainerDirty()
        {
            if (s_ContainerStack.Count > 0)
            {
                s_ContainerStack.Peek().MarkDirtyLayout();
                return true;
            }

            return false;
        }

        bool IUIElementsUtility.TakeCapture()
        {
            if (s_ContainerStack.Count > 0)
            {
                var topmostContainer = s_ContainerStack.Peek();

                var currentCapturingElement = topmostContainer.panel.GetCapturingElement(PointerId.mousePointerId);
                if (currentCapturingElement != null && currentCapturingElement != topmostContainer)
                {
                    Debug.Log("Should not grab hot control with an active capture");
                }
                topmostContainer.CaptureMouse();
                return true;
            }

            return false;
        }

        bool IUIElementsUtility.ReleaseCapture()
        {
            PointerCaptureHelper.ReleaseEditorMouseCapture();
            return false;
        }

        bool IUIElementsUtility.ProcessEvent(int instanceID, IntPtr nativeEventPtr, ref bool eventHandled)
        {
            Panel panel;
            if (nativeEventPtr != IntPtr.Zero && s_UIElementsCache.TryGetValue(instanceID, out panel))
            {
                if (panel.contextType == ContextType.Editor)
                {
                    // Instead of allocating a new Event object every time
                    // we reuse this instance and copy event data into it
                    s_EventInstance.CopyFromPtr(nativeEventPtr);

                    eventHandled =  DoDispatch(panel);
                }
                return true;
            }

            return false;
        }

        bool IUIElementsUtility.CleanupRoots()
        {
            // see GUI.CleanupRoots
            s_EventInstance = null;
            s_UIElementsCache = null;
            s_ContainerStack = null;
            s_BeginContainerCallback = null;
            s_EndContainerCallback = null;
            EventDispatcher.ClearEditorDispatcher();
            return false;
        }

        bool IUIElementsUtility.EndContainerGUIFromException(Exception exception)
        {
            // only End if we have a current container
            if (s_ContainerStack.Count > 0)
            {
                GUIUtility.EndContainer();
                s_ContainerStack.Pop();
            }

            return false;
        }

        static List<Panel> s_PanelsIterationList = new List<Panel>();
        void IUIElementsUtility.UpdateSchedulers()
        {
            // Since updating schedulers jumps into user code, the panels list might change while we're iterating,
            // we make a copy first.
            UIElementsUtility.GetAllPanels(s_PanelsIterationList, ContextType.Editor);

            foreach (var panel in s_PanelsIterationList)
            {
                // Dispatch all timer update messages to each scheduled item
                panel.timerEventScheduler.UpdateScheduledEvents();
                panel.UpdateAnimations();
                panel.UpdateBindings();
            }
        }

        void IUIElementsUtility.RequestRepaintForPanels(Action<ScriptableObject> repaintCallback)
        {
            var iterator = UIElementsUtility.GetPanelsIterator();
            while (iterator.MoveNext())
            {
                var panel = iterator.Current.Value;

                // Game panels' scheduler are ticked by the engine
                if (panel.contextType != ContextType.Editor)
                    continue;

                // Dispatch might have triggered a repaint request.
                if (panel.isDirty)
                {
                    repaintCallback(panel.ownerObject);
                }
            }
        }

        public static void RegisterCachedPanel(int instanceID, Panel panel)
        {
            s_UIElementsCache.Add(instanceID, panel);
        }

        public static void RemoveCachedPanel(int instanceID)
        {
            s_UIElementsCache.Remove(instanceID);
        }

        public static bool TryGetPanel(int instanceID, out Panel panel)
        {
            return s_UIElementsCache.TryGetValue(instanceID, out panel);
        }

        internal static void BeginContainerGUI(GUILayoutUtility.LayoutCache cache, Event evt, IMGUIContainer container)
        {
            if (container.useOwnerObjectGUIState)
            {
                GUIUtility.BeginContainerFromOwner(container.elementPanel.ownerObject);
            }
            else
            {
                GUIUtility.BeginContainer(container.guiState);
            }

            s_ContainerStack.Push(container);
            GUIUtility.s_SkinMode = (int)container.contextType;
            GUIUtility.s_OriginalID = container.elementPanel.ownerObject.GetInstanceID();

            if (Event.current == null)
            {
                Event.current = evt;
            }
            else
            {
                Event.current.CopyFrom(evt);
            }

            // call AFTER setting current event
            if (s_BeginContainerCallback != null)
                s_BeginContainerCallback(container);

            GUI.enabled = container.enabledInHierarchy;
            GUILayoutUtility.BeginContainer(cache);
            GUIUtility.ResetGlobalState();
        }

        // End the 2D GUI.
        internal static void EndContainerGUI(Event evt, Rect layoutSize)
        {
            if (Event.current.type == EventType.Layout
                && s_ContainerStack.Count > 0)
            {
                GUILayoutUtility.LayoutFromContainer(layoutSize.width, layoutSize.height);
            }
            // restore cache
            GUILayoutUtility.SelectIDList(GUIUtility.s_OriginalID, false);
            GUIContent.ClearStaticCache();

            if (s_ContainerStack.Count > 0)
            {
                IMGUIContainer container = s_ContainerStack.Peek();
                if (s_EndContainerCallback != null)
                    s_EndContainerCallback(container);
            }

            evt.CopyFrom(Event.current);

            if (s_ContainerStack.Count > 0)
            {
                GUIUtility.EndContainer();
                s_ContainerStack.Pop();
            }
        }

        internal static EventBase CreateEvent(Event systemEvent)
        {
            return CreateEvent(systemEvent, systemEvent.rawType);
        }

        // In order for tests to run without an EditorWindow but still be able to send
        // events, we sometimes need to force the event type. IMGUI::GetEventType() (native) will
        // return the event type as Ignore if the proper views haven't yet been
        // initialized. This (falsely) breaks tests that rely on the event type. So for tests, we
        // just ensure the event type is what we originally set it to when we sent it.
        internal static EventBase CreateEvent(Event systemEvent, EventType eventType)
        {
            switch (eventType)
            {
                case EventType.MouseMove:
                    return PointerMoveEvent.GetPooled(systemEvent);
                case EventType.MouseDrag:
                    return PointerMoveEvent.GetPooled(systemEvent);
                case EventType.MouseDown:
                    // If some buttons are already down, we generate PointerMove/MouseDown events.
                    // Otherwise we generate PointerDown/MouseDown events.
                    // See W3C pointer events recommendation: https://www.w3.org/TR/pointerevents2
                    if (PointerDeviceState.GetPressedButtons(PointerId.mousePointerId) != 0)
                    {
                        return PointerMoveEvent.GetPooled(systemEvent);
                    }
                    else
                    {
                        return PointerDownEvent.GetPooled(systemEvent);
                    }
                case EventType.MouseUp:
                    // If more buttons are still down, we generate PointerMove/MouseUp events.
                    // Otherwise we generate PointerUp/MouseUp events.
                    // See W3C pointer events recommendation: https://www.w3.org/TR/pointerevents2
                    if (PointerDeviceState.HasAdditionalPressedButtons(PointerId.mousePointerId, systemEvent.button))
                    {
                        return PointerMoveEvent.GetPooled(systemEvent);
                    }
                    else
                    {
                        return PointerUpEvent.GetPooled(systemEvent);
                    }
                case EventType.ContextClick:
                    return ContextClickEvent.GetPooled(systemEvent);
                case EventType.MouseEnterWindow:
                    return MouseEnterWindowEvent.GetPooled(systemEvent);
                case EventType.MouseLeaveWindow:
                    return MouseLeaveWindowEvent.GetPooled(systemEvent);
                case EventType.ScrollWheel:
                    return WheelEvent.GetPooled(systemEvent);
                case EventType.KeyDown:
                    return KeyDownEvent.GetPooled(systemEvent);
                case EventType.KeyUp:
                    return KeyUpEvent.GetPooled(systemEvent);
                case EventType.DragUpdated:
                    return DragUpdatedEvent.GetPooled(systemEvent);
                case EventType.DragPerform:
                    return DragPerformEvent.GetPooled(systemEvent);
                case EventType.DragExited:
                    return DragExitedEvent.GetPooled(systemEvent);
                case EventType.ValidateCommand:
                    return ValidateCommandEvent.GetPooled(systemEvent);
                case EventType.ExecuteCommand:
                    return ExecuteCommandEvent.GetPooled(systemEvent);
                default:// Layout, Ignore, Used
                    return IMGUIEvent.GetPooled(systemEvent);
            }
        }

        internal static readonly string s_RepaintProfilerMarkerName = "UIElementsUtility.DoDispatch(Repaint Event)";
        internal static readonly string s_EventProfilerMarkerName = "UIElementsUtility.DoDispatch(Non Repaint Event)";
        private static readonly ProfilerMarker s_RepaintProfilerMarker = new ProfilerMarker(s_RepaintProfilerMarkerName);
        private static readonly ProfilerMarker s_EventProfilerMarker = new ProfilerMarker(s_EventProfilerMarkerName);

        static bool DoDispatch(BaseVisualElementPanel panel)
        {
            bool usesEvent = false;

            if (s_EventInstance.type == EventType.Repaint)
            {
                using (s_RepaintProfilerMarker.Auto())
                    panel.Repaint(s_EventInstance);

                (panel.panelDebug?.debuggerOverlayPanel as Panel)?.Repaint(s_EventInstance);
                // TODO get rid of this when we wrap every GUIView inside IMGUIContainers
                // here we pretend to use the repaint event
                // in order to suspend to suspend OnGUI() processing on the native side
                // since we've already run it if we have an IMGUIContainer
                usesEvent = panel.IMGUIContainersCount > 0;
            }
            else
            {
                panel.ValidateLayout();

                using (EventBase evt = CreateEvent(s_EventInstance))
                {
                    bool immediate = s_EventInstance.type == EventType.Used || s_EventInstance.type == EventType.Layout || s_EventInstance.type == EventType.ExecuteCommand || s_EventInstance.type == EventType.ValidateCommand;

                    using (s_EventProfilerMarker.Auto())
                        panel.SendEvent(evt, immediate ? DispatchMode.Immediate : DispatchMode.Queued);

                    // The dispatcher should have finished processing the event,
                    // otherwise we cannot return a value for usesEvent.
                    // FIXME: this makes GUIPointWindowConversionOnMultipleWindow fails because the event sent in the OnGUI is put in the queue.
                    // Debug.Assert(evt.processed);

                    // FIXME: we dont always have to repaint if evt.isPropagationStopped.
                    if (evt.isPropagationStopped)
                    {
                        panel.visualTree.IncrementVersion(VersionChangeType.Repaint);
                        usesEvent = true;
                    }
                }
            }

            return usesEvent;
        }

        internal static void GetAllPanels(List<Panel> panels, ContextType contextType)
        {
            panels.Clear();
            var iterator = GetPanelsIterator();
            while (iterator.MoveNext())
            {
                if (iterator.Current.Value.contextType == contextType)
                {
                    panels.Add(iterator.Current.Value);
                }
            }
        }

        internal static Dictionary<int, Panel>.Enumerator GetPanelsIterator()
        {
            return s_UIElementsCache.GetEnumerator();
        }

        internal static Panel FindOrCreateEditorPanel(ScriptableObject ownerObject)
        {
            Panel panel;
            if (!s_UIElementsCache.TryGetValue(ownerObject.GetInstanceID(), out panel))
            {
                panel = Panel.CreateEditorPanel(ownerObject);
                RegisterCachedPanel(ownerObject.GetInstanceID(), panel);
            }
            else
            {
                Debug.Assert(ContextType.Editor == panel.contextType, "Panel is not an editor panel.");
            }

            return panel;
        }
    }
}
