// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Profiling;

namespace UnityEngine.UIElements
{
    // This is the required interface to UIElementsUtility for Runtime game components.
    [NativeHeader("Modules/UIElements/UIElementsRuntimeUtility.h")]
    internal static class UIElementsRuntimeUtility
    {
        static EventDispatcher s_RuntimeDispatcher = new EventDispatcher();

        public static EventBase CreateEvent(Event systemEvent)
        {
            Debug.Assert(s_RuntimeDispatcher != null, "Call UIElementsRuntimeUtility.InitRuntimeEventSystem before sending any event.");
            return UIElementsUtility.CreateEvent(systemEvent, systemEvent.rawType);
        }

        // To be made obsolete once runtime package stop using it.
        public static IPanel CreateRuntimePanel(ScriptableObject ownerObject)
        {
            return FindOrCreateRuntimePanel(ownerObject);
        }

        public static IPanel FindOrCreateRuntimePanel(ScriptableObject ownerObject)
        {
            Panel panel;
            if (!UIElementsUtility.TryGetPanel(ownerObject.GetInstanceID(), out panel))
            {
                panel = new RuntimePanel(ownerObject, s_RuntimeDispatcher)
                {
                    IMGUIEventInterests = new EventInterests { wantsMouseMove = true, wantsMouseEnterLeaveWindow = true }
                };

                RegisterCachedPanelInternal(ownerObject.GetInstanceID(), panel);
            }
            else
            {
                Debug.Assert(ContextType.Player == panel.contextType, "Panel is not a runtime panel.");
            }

            return panel;
        }

        public static void DisposeRuntimePanel(ScriptableObject ownerObject)
        {
            Panel panel;
            if (UIElementsUtility.TryGetPanel(ownerObject.GetInstanceID(), out panel))
            {
                panel.Dispose();
                RemoveCachedPanelInternal(ownerObject.GetInstanceID());
            }
        }

        private static bool s_RegisteredPlayerloopCallback = false;

        // To be made obsolete once runtime package stop using it.
        public static void RegisterCachedPanel(int instanceID, IPanel panel)
        {
            RegisterCachedPanelInternal(instanceID, panel);
        }

        private static void RegisterCachedPanelInternal(int instanceID, IPanel panel)
        {
            UIElementsUtility.RegisterCachedPanel(instanceID, panel as Panel);
            if (!s_RegisteredPlayerloopCallback)
            {
                s_RegisteredPlayerloopCallback = true;
                RegisterPlayerloopCallback();
            }
        }

        // To be made obsolete once runtime package stop using it.
        public static void RemoveCachedPanel(int instanceID)
        {
            RemoveCachedPanelInternal(instanceID);
        }

        private static void RemoveCachedPanelInternal(int instanceID)
        {
            UIElementsUtility.RemoveCachedPanel(instanceID);
            // un-register the playerloop callback as the last panel gets un-registered
            UIElementsUtility.GetAllPanels(panelsIteration, ContextType.Player);
            if (panelsIteration.Count == 0)
            {
                s_RegisteredPlayerloopCallback = false;
                UnregisterPlayerloopCallback();
            }
        }

        static List<Panel> panelsIteration = new List<Panel>();
        internal static readonly string s_RepaintProfilerMarkerName = "UIElementsRuntimeUtility.DoDispatch(Repaint Event)";
        private static readonly ProfilerMarker s_RepaintProfilerMarker = new ProfilerMarker(s_RepaintProfilerMarkerName);

        [RequiredByNativeCode]
        public static void RepaintOverlayPanels()
        {
            UIElementsUtility.GetAllPanels(panelsIteration, ContextType.Player);
            foreach (var panel in panelsIteration)
            {
                // at the moment, all runtime panels who do not use a rendertexure are rendered as overlays.
                // later on, they'll be filtered based on render mode
                if ((panel as RuntimePanel).targetTexture == null)
                {
                    using (s_RepaintProfilerMarker.Auto())
                        panel.Repaint(Event.current);
                }
            }
        }

        public extern static void RegisterPlayerloopCallback();
        public extern static void UnregisterPlayerloopCallback();
    }

    internal static class UIElementsUtility
    {
        private static Stack<IMGUIContainer> s_ContainerStack = new Stack<IMGUIContainer>();
        private static Dictionary<int, Panel> s_UIElementsCache = new Dictionary<int, Panel>();

        private static Event s_EventInstance = new Event(); // event instance reused for ProcessEvent()

        // When not in editor, this will be all white, so no impact on the overall color, except for the multiplication done on the color.
        internal static Color editorPlayModeTintColor = Color.white;

        internal static Action<IMGUIContainer> s_BeginContainerCallback;
        internal static Action<IMGUIContainer> s_EndContainerCallback;

        static UIElementsUtility()
        {
            GUIUtility.takeCapture += TakeCapture;
            GUIUtility.releaseCapture += ReleaseCapture;
            GUIUtility.processEvent += ProcessEvent;
            GUIUtility.cleanupRoots += CleanupRoots;
            GUIUtility.endContainerGUIFromException += EndContainerGUIFromException;

            GUIUtility.guiChanged += MakeCurrentIMGUIContainerDirty;
        }

        internal static IMGUIContainer GetCurrentIMGUIContainer()
        {
            if (s_ContainerStack.Count > 0)
            {
                return s_ContainerStack.Peek();
            }

            return null;
        }

        internal static void MakeCurrentIMGUIContainerDirty()
        {
            if (s_ContainerStack.Count > 0)
            {
                s_ContainerStack.Peek().MarkDirtyLayout();
            }
        }

        private static void TakeCapture()
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
            }
        }

        private static void ReleaseCapture()
        {
            PointerCaptureHelper.ReleaseEditorMouseCapture();
        }

        private static bool ProcessEvent(int instanceID, IntPtr nativeEventPtr)
        {
            Panel panel;
            if (nativeEventPtr != IntPtr.Zero && s_UIElementsCache.TryGetValue(instanceID, out panel))
            {
                if (panel.contextType == ContextType.Editor)
                {
                    // Instead of allocating a new Event object every time
                    // we reuse this instance and copy event data into it
                    s_EventInstance.CopyFromPtr(nativeEventPtr);

                    return DoDispatch(panel);
                }
            }

            return false;
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

        private static void CleanupRoots()
        {
            // see GUI.CleanupRoots
            s_EventInstance = null;
            s_UIElementsCache = null;
            s_ContainerStack = null;
            s_BeginContainerCallback = null;
            s_EndContainerCallback = null;
            EventDispatcher.ClearEditorDispatcher();
        }

        private static bool EndContainerGUIFromException(Exception exception)
        {
            // only End if we have a current container
            if (s_ContainerStack.Count > 0)
            {
                GUIUtility.EndContainer();
                s_ContainerStack.Pop();
            }

            return GUIUtility.ShouldRethrowException(exception);
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
