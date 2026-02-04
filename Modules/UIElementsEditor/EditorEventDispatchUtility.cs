// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.UIElements.IMGUIContainer;


namespace UnityEditor.UIElements
{
    static class EditorEventDispatchUtility
    {
        internal const int kTestFrameUpdateEvent = 7777;

        static Action testFrameUpdateCallback;

        // This method should only be used by the UI Test Framework
        // It creates a bogus Event that will be used to invoke a callback
        // It is assumed that the caller will immediately call GuiView.SendEvent with the returned Event
        // During the GUIUtility.ProcessEvent call, the callback will be invoked
        //
        // ANY OTHER USAGE IS NOT SUPPORTED AND WILL BRING INFINITE SHAME UPON ITS AUTHOR
        public static Event CreateTestFrameUpdateEvent(Action callback)
        {
            testFrameUpdateCallback = callback;

            Event refreshEvent = new Event();
            refreshEvent.type = (EventType)kTestFrameUpdateEvent;
            return refreshEvent;
        }

        internal static readonly string s_RepaintProfilerMarkerName = "UIElementsUtility.DoDispatch(Repaint Event)";
        internal static readonly string s_EventProfilerMarkerName = "UIElementsUtility.DoDispatch(Non Repaint Event)";
        private static readonly ProfilerMarker s_RepaintProfilerMarker = new ProfilerMarker(ProfilerCategory.UIToolkit, s_RepaintProfilerMarkerName);
        private static readonly ProfilerMarker s_EventProfilerMarker = new ProfilerMarker(ProfilerCategory.UIToolkit, s_EventProfilerMarkerName);

        static internal bool DoDispatch(BaseVisualElementPanel panel, Event eventInstance)
        {
            using var scope = new UITKScope();
            Debug.Assert(panel.contextType == ContextType.Editor, "panel.contextType == ContextType.Editor");

            if (kTestFrameUpdateEvent == (int)eventInstance.type)
            {
                Action cb = testFrameUpdateCallback;
                testFrameUpdateCallback = null;
                cb?.Invoke();
                return true;
            }

            bool usesEvent = false;

            if (eventInstance.type == EventType.Repaint)
            {
                var oldCam = Camera.current;
                var oldRT = RenderTexture.active;

                Camera.SetupCurrent(null);
                RenderTexture.active = null;

                using (s_RepaintProfilerMarker.Auto())
                {
                    panel.Repaint(eventInstance);
                    panel.Render();
                }

                if (panel.panelDebug?.debuggerOverlayPanel is Panel panelDebug)
                {
                    panelDebug.Repaint(eventInstance);
                    panelDebug.Render();
                }

                // TODO get rid of this when we wrap every GUIView inside IMGUIContainers
                // here we pretend to use the repaint event
                // in order to suspend to suspend OnGUI() processing on the native side
                // since we've already run it if we have an IMGUIContainer
                usesEvent = panel.IMGUIContainersCount > 0;

                Camera.SetupCurrent(oldCam);
                RenderTexture.active = oldRT;
            }
            else
            {
                panel.ValidateLayout();

                using (EventBase evt = UIElementsIMGUIUtility.CreateEvent(eventInstance))
                {
                    bool immediate = eventInstance.type == EventType.Used || eventInstance.type == EventType.Layout || eventInstance.type == EventType.ExecuteCommand || eventInstance.type == EventType.ValidateCommand;

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
    }
}
