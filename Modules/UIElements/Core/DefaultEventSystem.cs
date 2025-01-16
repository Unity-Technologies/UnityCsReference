// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using static UnityEngine.InputForUI.PointerEvent;

namespace UnityEngine.UIElements
{
    internal partial class DefaultEventSystem
    {
        private bool isAppFocused => Application.isFocused;

        internal static Func<bool> IsEditorRemoteConnected = () => false;

        private bool ShouldIgnoreEventsOnAppNotFocused()
        {
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.Windows:
                case OperatingSystemFamily.Linux:
                case OperatingSystemFamily.MacOSX:
                    if (IsEditorRemoteConnected())
                        return false;
                    return true;
                default:
                    return false;
            }
        }

        private BaseRuntimePanel m_FocusedPanel;
        private BaseRuntimePanel m_PreviousFocusedPanel;
        private Focusable m_PreviousFocusedElement;

        public BaseRuntimePanel focusedPanel
        {
            get => m_FocusedPanel;
            set
            {
                if (m_FocusedPanel != value)
                {
                    m_FocusedPanel?.Blur();
                    m_FocusedPanel = value;
                    m_FocusedPanel?.Focus();
                }
            }
        }

        public void Reset()
        {
            m_LegacyInputProcessor?.Reset();
            m_InputForUIProcessor?.Reset();
            m_FocusedPanel = null;
        }

        public enum UpdateMode
        {
            Always,
            IgnoreIfAppNotFocused
        }

        public void Update(UpdateMode updateMode = UpdateMode.Always)
        {
            if (!isAppFocused && ShouldIgnoreEventsOnAppNotFocused() && updateMode == UpdateMode.IgnoreIfAppNotFocused)
                return;

            if (m_IsInputForUIActive)
            {
                inputForUIProcessor.ProcessInputForUIEvents();
            }
            else
            {
                legacyInputProcessor.ProcessLegacyInputEvents();
            }
        }

        private LegacyInputProcessor m_LegacyInputProcessor;
        // Internal for Unit Tests
        internal LegacyInputProcessor legacyInputProcessor => m_LegacyInputProcessor ??= new LegacyInputProcessor(this);

        private InputForUIProcessor m_InputForUIProcessor;
        private InputForUIProcessor inputForUIProcessor => m_InputForUIProcessor ??= new InputForUIProcessor(this);

        private bool m_IsInputReady = false;
        /// <summary>
        /// Closely follows the PlayMode state from <see cref="UIElementsRuntimeUtility"/>. Don't clear in Reset().
        /// </summary>
        internal bool isInputReady
        {
            get => m_IsInputReady;
            set
            {
                if (m_IsInputReady == value) return;
                m_IsInputReady = value;

                if (m_IsInputReady)
                {
                    InitInputProcessor();
                }
                else
                {
                    RemoveInputProcessor();
                }
            }
        }

        private bool m_UseInputForUI = true;
        /// <summary>
        /// Set by <see cref="UIToolkitInputConfiguration.SetRuntimeInputBackend"/>. Don't clear in Reset().
        /// </summary>
        internal bool useInputForUI
        {
            get => m_UseInputForUI;
            set
            {
                if (m_UseInputForUI == value) return;

                if (m_IsInputReady)
                {
                    RemoveInputProcessor();
                    m_UseInputForUI = value;
                    InitInputProcessor();
                }
                else
                {
                    m_UseInputForUI = value;
                }
            }
        }

        private bool m_IsInputForUIActive = false;

        internal struct FocusBasedEventSequenceContext : IDisposable
        {
            private DefaultEventSystem es;

            public FocusBasedEventSequenceContext(DefaultEventSystem es)
            {
                this.es = es;
                es.m_PreviousFocusedPanel = es.focusedPanel;
                es.m_PreviousFocusedElement = es.focusedPanel?.focusController.GetLeafFocusedElement();
            }
            public void Dispose()
            {
                es.m_PreviousFocusedPanel = null;
                es.m_PreviousFocusedElement = null;
            }
        }

        internal FocusBasedEventSequenceContext FocusBasedEventSequence()
        {
            return new FocusBasedEventSequenceContext(this);
        }

        void RemoveInputProcessor()
        {
            if (m_IsInputForUIActive)
            {
                UnityEngine.InputForUI.EventProvider.Unsubscribe(inputForUIProcessor.OnEvent);
                UnityEngine.InputForUI.EventProvider.SetEnabled(false);
                m_IsInputForUIActive = false;
            }
        }

        void InitInputProcessor()
        {
            if (m_UseInputForUI)
            {
                m_IsInputForUIActive = true;
                UnityEngine.InputForUI.EventProvider.SetEnabled(true);
                UnityEngine.InputForUI.EventProvider.Subscribe(inputForUIProcessor.OnEvent);
            }
        }

        // Change focused panel to reflect an element being focused by code. However
        // - Do not modify the target of any ongoing focus-based event sequence
        // - Do not unfocus the current focused panel if its element loses focus
        internal void OnFocusEvent(RuntimePanel panel, FocusEvent evt)
        {
            focusedPanel = panel;
        }

        // Internal for Unit Tests
        internal void SendFocusBasedEvent<TArg>(Func<TArg, EventBase> evtFactory, TArg arg)
        {
            // Send all focus-based events to the same previously focused panel if there's one
            // This allows Navigation events to use the same target as related KeyDown (and eventually Gamepad) events
            if (m_PreviousFocusedPanel != null)
            {
                using (EventBase evt = evtFactory(arg))
                {
                    evt.elementTarget = (VisualElement) m_PreviousFocusedElement ?? m_PreviousFocusedPanel.visualTree;
                    m_PreviousFocusedPanel.visualTree.SendEvent(evt);
                    UpdateFocusedPanel(m_PreviousFocusedPanel);
                    return;
                }
            }

            // Send Keyboard events to all panels if none is focused.
            // This is so that navigation with Tab can be started without clicking on an element.

            // Try all the panels, from closest to deepest
            var panels = UIElementsRuntimeUtility.GetSortedPlayerPanels();
            for (var i = panels.Count - 1; i >= 0; i--)
            {
                var panel = panels[i];
                if (panel is BaseRuntimePanel runtimePanel)
                {
                    using (EventBase evt = evtFactory(arg))
                    {
                        // Since there was no focused element, send event to the visualTree to avoid reacting to a
                        // focus change in between events.
                        evt.elementTarget = runtimePanel.visualTree;
                        runtimePanel.visualTree.SendEvent(evt);

                        if (runtimePanel.focusController.focusedElement != null)
                        {
                            focusedPanel = runtimePanel;
                            break;
                        }

                        if (evt.isPropagationStopped)
                        {
                            return;
                        }
                    }
                }
            }
        }

        void SendPositionBasedEvent<TArg>(Vector3 mousePosition, Vector3 delta, int pointerId, int? targetDisplay, Func<Vector3, Vector3, TArg, EventBase> evtFactory, TArg arg, bool deselectIfNoTarget = false)
        {
            // Allow focus to be lost before processing the event
            if (focusedPanel != null)
            {
                UpdateFocusedPanel(focusedPanel);
            }

            var capturingPanel = PointerDeviceState.GetPlayerPanelWithSoftPointerCapture(pointerId);

            // Allow element with pointer capture to update panel soft capture
            var capturing = RuntimePanel.s_EventDispatcher.pointerState.GetCapturingElement(pointerId);
            if (capturing is VisualElement capturingVE)
            {
                capturingPanel = capturingVE.panel;
            }

            BaseRuntimePanel targetPanel = null;
            Vector2 targetPanelPosition = Vector2.zero;
            Vector2 targetPanelDelta = Vector2.zero;

            if (capturingPanel is BaseRuntimePanel capturingRuntimePanel)
            {
                // Panel with soft capture has priority, that is it will receive pointer events until pointer up
                targetPanel = capturingRuntimePanel;
                targetPanel.ScreenToPanel(mousePosition, delta, out targetPanelPosition, out targetPanelDelta);
            }
            else
            {
                // Find a candidate panel for the event
                // Try all the panels, from closest to deepest
                var panels = UIElementsRuntimeUtility.GetSortedPlayerPanels();
                for (var i = panels.Count - 1; i >= 0; i--)
                {
                    if (panels[i] is BaseRuntimePanel runtimePanel && (targetDisplay == null || runtimePanel.targetDisplay == targetDisplay))
                    {
                        if (runtimePanel.ScreenToPanel(mousePosition, delta, out targetPanelPosition, out targetPanelDelta) &&
                            runtimePanel.Pick(targetPanelPosition, pointerId) != null)
                        {
                            targetPanel = runtimePanel;
                            break;
                        }
                    }
                }
            }

            BaseRuntimePanel lastActivePanel = PointerDeviceState.GetPanel(pointerId, ContextType.Player) as BaseRuntimePanel;

            if (lastActivePanel != targetPanel)
            {
                // Allow last panel the pointer was in to dispatch [Mouse|Pointer][Out|Leave] events if needed.
                lastActivePanel?.PointerLeavesPanel(pointerId, lastActivePanel.ScreenToPanel(mousePosition));
                targetPanel?.PointerEntersPanel(pointerId, targetPanelPosition);
            }

            if (targetPanel != null)
            {
                using (EventBase evt = evtFactory(targetPanelPosition, targetPanelDelta, arg))
                {
                    targetPanel.visualTree.SendEvent(evt);

                    if (evt.processedByFocusController)
                    {
                        UpdateFocusedPanel(targetPanel);
                    }

                    if (evt.eventTypeId == PointerDownEvent.TypeId())
                        PointerDeviceState.SetPlayerPanelWithSoftPointerCapture(pointerId, targetPanel);
                    else if (evt.eventTypeId == PointerUpEvent.TypeId() && ((PointerUpEvent)evt).pressedButtons == 0)
                        PointerDeviceState.SetPlayerPanelWithSoftPointerCapture(pointerId, null);
                }
            }
            else
            {
                if (deselectIfNoTarget)
                {
                    focusedPanel = null;
                }
            }
        }

        private void UpdateFocusedPanel(BaseRuntimePanel runtimePanel)
        {
            if (runtimePanel.focusController.focusedElement != null)
            {
                focusedPanel = runtimePanel;
            }
            else if (focusedPanel == runtimePanel)
            {
                focusedPanel = null;
            }
        }

        private static EventBase MakeTouchEvent(Touch touch, int pointerId, EventModifiers modifiers, int targetDisplay)
        {
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    return PointerDownEvent.GetPooled(touch, pointerId, modifiers, targetDisplay);
                case TouchPhase.Moved:
                    return PointerMoveEvent.GetPooled(touch, pointerId, modifiers, targetDisplay);
                case TouchPhase.Ended:
                    return PointerUpEvent.GetPooled(touch, pointerId, modifiers, targetDisplay);
                case TouchPhase.Canceled:
                    return PointerCancelEvent.GetPooled(touch, pointerId, modifiers, targetDisplay);
                default:
                    return null;
            }
        }

        private static EventBase MakePenEvent(PenData pen, EventModifiers modifiers, int targetDisplay)
        {
            switch (pen.contactType)
            {
                case PenEventType.PenDown:
                    return PointerDownEvent.GetPooled(pen, modifiers, targetDisplay);
                case PenEventType.PenUp:
                    return PointerUpEvent.GetPooled(pen, modifiers, targetDisplay);
                case PenEventType.NoContact:
                    return PointerMoveEvent.GetPooled(pen, modifiers, targetDisplay);
                default:
                    return null;
            }
        }

        // For testing purposes
        internal bool verbose = false;
        internal bool logToGameScreen = false;

        private void Log(object o)
        {
            Debug.Log(o);
            if (logToGameScreen)
                LogToGameScreen("" + o);
        }
        private void LogWarning(object o)
        {
            Debug.LogWarning(o);
            if (logToGameScreen)
                LogToGameScreen("Warning! " + o);
        }

        private Label m_LogLabel;
        private List<string> m_LogLines = new List<string>();

        private void LogToGameScreen(string s)
        {
            if (m_LogLabel == null)
            {
                m_LogLabel = new Label {style = {position = Position.Absolute, bottom = 0, color = Color.white}};
                Object.FindFirstObjectByType<UIDocument>().rootVisualElement.Add(m_LogLabel);
            }

            m_LogLines.Add(s + "\n");
            if (m_LogLines.Count > 10)
                m_LogLines.RemoveAt(0);

            m_LogLabel.text = string.Concat(m_LogLines);
        }
    }
}
