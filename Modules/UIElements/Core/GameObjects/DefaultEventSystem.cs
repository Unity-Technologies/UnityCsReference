// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Pool;
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

        private RuntimePanel m_FocusedPanel;
        private RuntimePanel m_PreviousFocusedPanel;
        private Focusable m_PreviousFocusedElement;

        public RuntimePanel focusedPanel
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

            m_Raycaster?.Update();

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
                m_InputForUIProcessor.Reset();
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
                if (panels[i] is RuntimePanel { drawsInCameras: false } runtimePanel)
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

        internal void SendPositionBasedEvent<TArg>(Vector3 mousePosition, Vector3 delta, int pointerId,
            int? targetDisplay, Func<Vector3, Vector3, TArg, EventBase> evtFactory, TArg arg,
            bool deselectIfNoTarget = false)
        {
            SendPositionBasedEvent(mousePosition, delta, pointerId,
            targetDisplay, (p, t) =>
            {
                var e = t.evtFactory(p, t.delta, t.arg);
                if (e is IPointerOrMouseEvent pme)
                    pme.deltaPosition = t.delta;
                return e;
            }, (evtFactory, delta, arg), deselectIfNoTarget);
        }

        internal void SendPositionBasedEvent<TArg>(Vector3 mousePosition, Vector3 delta, int pointerId,
            int? targetDisplay, Func<Vector3, TArg, EventBase> evtFactory, TArg arg,
            bool deselectIfNoTarget = false)
        {
            // Allow focus to be lost before processing the event
            if (focusedPanel != null)
            {
                UpdateFocusedPanel(focusedPanel);
            }

            FindTargetAtPosition(mousePosition, delta, pointerId, targetDisplay, out var target, out var targetPanel,
                out var targetPanelPosition, out var elementUnderPointer);

            RuntimePanel lastActivePanel = PointerDeviceState.GetPanel(pointerId, ContextType.Player) as RuntimePanel;

            if (lastActivePanel != targetPanel)
            {
                // Allow last panel the pointer was in to dispatch [Mouse|Pointer][Out|Leave] events if needed.
                lastActivePanel?.PointerLeavesPanel(pointerId);
                targetPanel?.PointerEntersPanel(pointerId, targetPanelPosition);
            }

            if (targetPanel != null)
            {
                using (EventBase evt = evtFactory(targetPanelPosition, arg))
                {
                    if (!targetPanel.isFlat)
                    {
                        // World-space panels can't use the regular RecomputeElementUnderPointer mechanism.
                        //
                        // This behavior is slightly different than screen-space panels and Editor panels: if an
                        // element moves or a collider starts blocking our raycast, the element under pointer will not
                        // change until the pointer moves again or some other event is sent.
                        //
                        // Projects that require an up-to-date element under pointer can implement an InputProvider
                        // (when we make it public) that sends PointerStationaryEvent on every Update.
                        targetPanel.SetTopElementUnderPointer(pointerId, elementUnderPointer, evt);
                    }

                    evt.elementTarget = target;
                    targetPanel.visualTree.SendEvent(evt);

                    if (evt.processedByFocusController)
                    {
                        UpdateFocusedPanel(targetPanel);
                    }

                    if (evt.eventTypeId == PointerDownEvent.TypeId())
                        PointerDeviceState.SetElementWithSoftPointerCapture(pointerId, target ?? targetPanel.visualTree);
                    else if (evt.eventTypeId == PointerUpEvent.TypeId() && ((PointerUpEvent)evt).pressedButtons == 0)
                        PointerDeviceState.SetElementWithSoftPointerCapture(pointerId, null);
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

        internal void SendRayBasedEvent<TArg>(Ray worldRay, int pointerId, Func<Vector3, TArg, EventBase> evtFactory,
            TArg arg, bool deselectIfNoTarget = false)
        {
            // Allow focus to be lost before processing the event
            if (focusedPanel != null)
            {
                UpdateFocusedPanel(focusedPanel);
            }

            FindTargetAtRay(worldRay, pointerId, out var target, out var targetPanel,
                out var targetPanelPosition, out var elementUnderPointer);

            RuntimePanel lastActivePanel = PointerDeviceState.GetPanel(pointerId, ContextType.Player) as RuntimePanel;

            if (lastActivePanel != targetPanel)
            {
                // Allow last panel the pointer was in to dispatch [Mouse|Pointer][Out|Leave] events if needed.
                lastActivePanel?.PointerLeavesPanel(pointerId);
                targetPanel?.PointerEntersPanel(pointerId, targetPanelPosition);
            }

            if (targetPanel != null)
            {
                using (EventBase evt = evtFactory(targetPanelPosition, arg))
                {
                    if (!targetPanel.isFlat)
                    {
                        // World-space panels can't use the regular RecomputeElementUnderPointer mechanism.
                        //
                        // This behavior is slightly different than screen-space panels and Editor panels: if an
                        // element moves or a collider starts blocking our raycast, the element under pointer will not
                        // change until the pointer moves again or some other event is sent.
                        //
                        // Projects that require an up-to-date element under pointer can implement an InputProvider
                        // (when we make it public) that sends PointerStationaryEvent on every Update.
                        targetPanel.SetTopElementUnderPointer(pointerId, elementUnderPointer, evt);
                    }

                    evt.elementTarget = target;
                    targetPanel.visualTree.SendEvent(evt);

                    if (evt.processedByFocusController)
                    {
                        UpdateFocusedPanel(targetPanel);
                    }

                    if (evt.eventTypeId == PointerDownEvent.TypeId())
                        PointerDeviceState.SetElementWithSoftPointerCapture(pointerId, target ?? targetPanel.visualTree);
                    else if (evt.eventTypeId == PointerUpEvent.TypeId() && ((PointerUpEvent)evt).pressedButtons == 0)
                        PointerDeviceState.SetElementWithSoftPointerCapture(pointerId, null);
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

        // Allow unit tests or XR implementations to swap these pieces
        private IScreenRaycaster m_Raycaster;
        public IScreenRaycaster raycaster
        {
            get => m_Raycaster ??= new MainCameraScreenRaycaster();
            set => m_Raycaster = value;
        }

        private readonly PhysicsDocumentPicker m_WorldSpacePicker = new();
        private readonly ScreenOverlayPanelPicker m_ScreenOverlayPicker = new();

        public float worldSpaceMaxDistance = Mathf.Infinity;
        public int worldSpaceLayers = Physics.DefaultRaycastLayers;

        private static readonly Vector3 s_InvalidPanelCoordinates = new (float.NaN, float.NaN, float.NaN);

        internal void FindTargetAtPosition(Vector2 mousePosition, Vector2 delta, int pointerId, int? targetDisplay,
            out VisualElement target, out RuntimePanel targetPanel, out Vector3 targetPanelPosition,
            out VisualElement elementUnderPointer)
        {
            // Try panels from closest to deepest.
            var panels = UIElementsRuntimeUtility.GetSortedScreenOverlayPlayerPanels();
            for (var i = panels.Count - 1; i >= 0; i--)
            {
                if (m_ScreenOverlayPicker.TryPick(panels[i], pointerId, mousePosition, delta,
                        targetDisplay, out _))
                {
                    target = elementUnderPointer = null;
                    targetPanel = (RuntimePanel)panels[i];
                    targetPanel.ScreenToPanel(mousePosition, delta, out targetPanelPosition, true);
                    return;
                }
            }

            foreach (var worldRay in raycaster.MakeRay(mousePosition, targetDisplay))
            {
                if (m_WorldSpacePicker.TryPickWithCapture(pointerId, worldRay, worldSpaceMaxDistance, worldSpaceLayers,
                        out var document, out elementUnderPointer, out _, out _))
                {
                    // We hit a non-UI GameObject
                    if (document == null)
                        break;
                    var capturingElement = RuntimePanel.s_EventDispatcher.pointerState.GetCapturingElement(pointerId) as VisualElement;
                    target = capturingElement ?? elementUnderPointer ?? document.rootVisualElement;
                    targetPanel = document.containerPanel;
                    targetPanelPosition = GetPanelPosition(target, document, worldRay);
                    return;
                }
            }

            target = elementUnderPointer = null;
            targetPanel = null;
            targetPanelPosition = s_InvalidPanelCoordinates;
        }

        internal void FindTargetAtRay(Ray worldRay, int pointerId, out VisualElement target,
            out RuntimePanel targetPanel, out Vector3 targetPanelPosition, out VisualElement elementUnderPointer)
        {
            if (m_WorldSpacePicker.TryPickWithCapture(pointerId, worldRay, worldSpaceMaxDistance, worldSpaceLayers,
                    out var document, out elementUnderPointer, out _, out _))
            {
                var capturingElement = RuntimePanel.s_EventDispatcher.pointerState.GetCapturingElement(pointerId) as VisualElement;
                target = capturingElement ?? elementUnderPointer ?? document.rootVisualElement;
                targetPanel = document.containerPanel;
                targetPanelPosition = GetPanelPosition(target, document, worldRay);
                return;
            }

            target = elementUnderPointer = null;
            targetPanel = null;
            targetPanelPosition = s_InvalidPanelCoordinates;
        }

        Vector3 GetPanelPosition(VisualElement pickedElement, UIDocument document, Ray worldRay)
        {
            var documentRay = document.transform.worldToLocalMatrix.TransformRay(worldRay);
            pickedElement.IntersectWorldRay(documentRay, out var distanceWithinDocument, out _);
            var documentPoint = documentRay.origin + documentRay.direction * distanceWithinDocument;
            return documentPoint;
        }

        private void UpdateFocusedPanel(RuntimePanel runtimePanel)
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
