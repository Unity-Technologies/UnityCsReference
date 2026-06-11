// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using static UnityEngine.UIElements.IRuntimePanel;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Interface for classes implementing UI runtime panels.
    /// </summary>
    public interface IRuntimePanel : IPanel
    {
        /// <summary>
        /// The <see cref="UnityEngine.UIElements.PanelSettings"/> asset associated with this panel.
        /// </summary>
        PanelSettings panelSettings { get; }

        /// <summary>
        /// A GameObject from the Scene that can be used by <see cref="UnityEngine.EventSystems.EventSystem"/>
        /// to get and set focus to this panel. If null, panel focus will be handled independently of
        /// Event System selection.
        /// </summary>
        GameObject selectableGameObject { get; set; }

        internal event Action destroyed;
        internal void Focus();
        internal void Blur();

        // Element is a VisualElement but use object as type for code stripping reasons
        internal void SetTopElementUnderPointer(int pointerId, IEventHandler element, EventBase triggerEvent);

        // Element is a VisualElement but use object as type for code stripping reasons
        internal void SetTopElementUnderPointer(int pointerId, IEventHandler element, Vector2 position);
        internal void PointerEntersPanel(int pointerId, Vector3 position);
        internal void PointerLeavesPanel(int pointerId);
        internal void CommitElementUnderPointers();
        internal bool isFlat { get; }
        internal void SendEvent(EventBase e);

        internal bool ScreenToPanel(Vector2 screenPosition, Vector2 screenDelta,
           out Vector3 panelPosition, bool allowOutside = false);

        internal bool disposed { get; }
        internal int targetDisplay { get; }
        internal float sortingPriority { get; }
        internal int resolvedSortingIndex { get; }
        internal IEventHandler Pick(Vector2 point, int pointerId);

        internal static CodeStrippingSafeUIElementsRuntimeUtility uIElementsRuntimeUtility { get; set; }
        internal static IPointerDeviceState pointerDeviceState { get => (IPointerDeviceState)uIElementsRuntimeUtility; }
        internal bool drawsInCameras { get; }
        internal static IDefaultEventSystem defaultEventSystem { get => (IDefaultEventSystem)uIElementsRuntimeUtility; }

        internal IEventHandler visualTree_as_IEventHandler { get; }

        internal void RegisterRootFocusCallback(Action callback);
        internal void UnregisterRootFocusCallback(Action callback);
        internal IEventHandler GetLeafFocusedElement();

        internal string name { get; }

        // These methods allow UGUI to send events without depending on EventBase types

        internal bool SendPointerMoveEvent(IPointerEvent eventData, IEventHandler target, IEventHandler elementUnderPointer);
        internal bool SendPointerDownEvent(IPointerEvent eventData, IEventHandler target, IEventHandler elementUnderPointer, Camera camera);
        internal bool SendPointerUpEvent(IPointerEvent eventData, IEventHandler target, IEventHandler elementUnderPointer);
        internal bool SendPointerCancelEvent(IPointerEvent eventData, IEventHandler target, IEventHandler elementUnderPointer);
        internal bool SendWheelEvent(Vector2 scrollDelta,IPointerEvent eventData);

        internal bool SendNavigationEvent(
            NavigationEventType eventType,
            IEventHandler target,
            NavigationDeviceType deviceType,
            EventModifiers modifiers,
            Vector2 moveVector = default,
            NavigationMoveDirection moveDirection = 0);

        internal bool SendKeyboardEvent(
            bool isKeyDown,
            char character,
            KeyCode keyCode,
            EventModifiers modifiers,
            IEventHandler target);

        internal bool SendIMEEvent(string compositionString, IEventHandler target);
    }

    /// <summary>
    /// Type of navigation event for SendNavigationEvent.
    /// </summary>
    internal enum NavigationEventType
    {
        Submit,
        Cancel,
        Move
    }

    internal interface CodeStrippingSafeUIElementsRuntimeUtility
    {
        bool HasActiveDocuments();

        Object activeEventSystem { get; }

        int s_ResolvedSortingIndexMax { get; }

        List<IRuntimePanel> GetSortedPlayerPanelsInternal();

        public void AddOnCreatePanelAction(Action<IRuntimePanel> action);
        public void RemoveOnCreatePanelAction(Action<IRuntimePanel> action);

        public void AddOnWillDestroyPanelAction(Action<IRuntimePanel> action);
        public void RemoveOnWillDestroyPanelAction(Action<IRuntimePanel> action);

        public void RegisterEventSystem(Object eventSystem);
        public void UnregisterEventSystem(Object eventSystem);

        void GetCapturingElement(int pointerId, out IRuntimePanel panel, out IEventHandler element);

        int s_DoubleClickTime{get;}

        bool TryPick(IRuntimePanel panel, int pointerId, Vector2 screenPosition, Vector2 delta, int? targetDisplay, out bool captured);
        bool TryPickWithCapture(int pointerId, Ray worldRay, float maxDistance, int layerMask, out Collider collider, out IPanelComponent panelComponent, out IEventHandler elementUnderPointer, out float distance, out bool captured);
        Vector3 PanelToScreenBottomLeftPosition(Vector2 mousePosition, int targetDisplay);
    }

    //Interface referenced by the UGUI interop for code stripping reason
    internal interface IPointerDeviceState
    {
        void SetElementWithSoftPointerCapture(int pointerId, IEventHandler element, Camera camera);

        IRuntimePanel GetPlayerPanelWithSoftPointerCapture(int pointerId);

        void PressButton(int pointerId, int buttonId);
        void ReleaseButton(int pointerId, int buttonId);

        int GetPressedButtons(int pointerId);
        Vector3 GetPointerDeltaPosition(int pointerId, ContextType contextType, Vector3 newPosition);

        Camera GetCameraWithSoftPointerCapture(int pointerId);

        public IEventHandler GetCapturingElement(int pointerId);
        IPanelComponent GetWorldSpacePanelComponentWithSoftPointerCapture(int pointerId);
    }

    internal interface IDefaultEventSystem
    {
        bool? overrideUseDefaultEventSystem { get; set; }
        LayerMask worldSpaceLayers { get; set; }
        float worldSpaceMaxDistance { get; set; }
        IScreenRaycaster raycaster { get; set; }

        void ApplyRaycasterAsDefault(bool processWorldSpaceInput, bool defaultEventCameraIsMainCamera, Camera[] eventCameras);
    }

    internal class CodeStrippingSafeUIElementsRuntimeUtilityImpl : CodeStrippingSafeUIElementsRuntimeUtility, IPointerDeviceState, IDefaultEventSystem
    {
        public Object activeEventSystem => UIElementsRuntimeUtility.activeEventSystem;

        public int s_ResolvedSortingIndexMax => UIElementsRuntimeUtility.s_ResolvedSortingIndexMax;


        bool? IDefaultEventSystem.overrideUseDefaultEventSystem
        {
            get => UIElementsRuntimeUtility.overrideUseDefaultEventSystem;
            set => UIElementsRuntimeUtility.overrideUseDefaultEventSystem = value;
        }
        LayerMask IDefaultEventSystem.worldSpaceLayers { get => UIElementsRuntimeUtility.defaultEventSystem.worldSpaceLayers; set => UIElementsRuntimeUtility.defaultEventSystem.worldSpaceLayers = value; }
        float IDefaultEventSystem.worldSpaceMaxDistance { get => UIElementsRuntimeUtility.defaultEventSystem.worldSpaceMaxDistance; set => UIElementsRuntimeUtility.defaultEventSystem.worldSpaceMaxDistance = value; }
        IScreenRaycaster IDefaultEventSystem.raycaster { get => UIElementsRuntimeUtility.defaultEventSystem.raycaster; set => UIElementsRuntimeUtility.defaultEventSystem.raycaster = value; }

        int CodeStrippingSafeUIElementsRuntimeUtility.s_DoubleClickTime => ClickDetector.s_DoubleClickTime;

        public void AddOnCreatePanelAction(Action<IRuntimePanel> action)
        {
            UIElementsRuntimeUtility.onCreatePanel += action;
        }
        public void RemoveOnCreatePanelAction(Action<IRuntimePanel> action)
        {
            UIElementsRuntimeUtility.onCreatePanel -= action;
        }

        public void AddOnWillDestroyPanelAction(Action<IRuntimePanel> action)
        {
            UIElementsRuntimeUtility.onWillDestroyPanel += action;
        }

        public void RemoveOnWillDestroyPanelAction(Action<IRuntimePanel> action)
        {
            UIElementsRuntimeUtility.onWillDestroyPanel -= action;
        }

        public void GetCapturingElement(int pointerId, out IRuntimePanel panel, out IEventHandler element)
        {
            var ve = RuntimePanel.s_EventDispatcher.pointerState.GetCapturingElement(pointerId);
            element = ve;
            panel = (IRuntimePanel)(ve as VisualElement)?.panel;
        }

        public void RegisterEventSystem(Object eventSystem)
        {
            UIElementsRuntimeUtility.RegisterEventSystem(eventSystem);
        }

        public void UnregisterEventSystem(Object eventSystem)
        {
            UIElementsRuntimeUtility.UnregisterEventSystem(eventSystem);
        }

        public List<IRuntimePanel> GetSortedPlayerPanelsInternal()
        {
            return UIElementsRuntimeUtility.GetSortedPlayerPanelsInternal();
        }

        public void SetElementWithSoftPointerCapture(int pointerId, IEventHandler element, Camera camera)
        {
            PointerDeviceState.SetElementWithSoftPointerCapture(pointerId, (VisualElement)element, camera);
        }

        public IRuntimePanel GetPlayerPanelWithSoftPointerCapture(int pointerId) => PointerDeviceState.GetPlayerPanelWithSoftPointerCapture(pointerId);

        public void PressButton(int pointerId, int buttonId) => PointerDeviceState.PressButton(pointerId, buttonId);

        public void ReleaseButton(int pointerId, int buttonId) => PointerDeviceState.ReleaseButton(pointerId, buttonId);

        public int GetPressedButtons(int pointerId) => PointerDeviceState.GetPressedButtons(pointerId);

        public Vector3 GetPointerDeltaPosition(int pointerId, ContextType contextType, Vector3 newPosition)
        {
            return PointerDeviceState.GetPointerDeltaPosition(pointerId, contextType, newPosition);
        }

        public Camera GetCameraWithSoftPointerCapture(int pointerId)
        {
            return PointerDeviceState.GetCameraWithSoftPointerCapture(pointerId);
        }

        public IEventHandler GetCapturingElement(int pointerId)
        {
            return RuntimePanel.s_EventDispatcher.pointerState.GetCapturingElement(pointerId);
        }

        public IPanelComponent GetWorldSpacePanelComponentWithSoftPointerCapture(int pointerId)
        {
            return PointerDeviceState.GetWorldSpacePanelComponentWithSoftPointerCapture(pointerId);
        }

        public bool HasActiveDocuments()
        {
            // This should be close to a no-op if the panels are already sorted.
            return UIElementsRuntimeUtility.GetSortedPlayerPanels()?.Count > 0;
        }

        bool CodeStrippingSafeUIElementsRuntimeUtility.TryPickWithCapture(int pointerId, Ray worldRay, float maxDistance, int layerMask, out Collider collider, out IPanelComponent panelComponent, out IEventHandler elementUnderPointer, out float distance, out bool captured)
        {
            return PhysicsDocumentPicker.TryPickWithCapture(pointerId, worldRay, maxDistance, layerMask, out collider, out panelComponent, out elementUnderPointer, out distance, out captured);
    }

        bool CodeStrippingSafeUIElementsRuntimeUtility.TryPick(IRuntimePanel panel, int pointerId, Vector2 screenPosition, Vector2 delta, int? targetDisplay, out bool captured)
        {
            return ScreenOverlayPanelPicker.TryPick(panel, pointerId, screenPosition, delta, targetDisplay, out captured);
        }

        Vector3 CodeStrippingSafeUIElementsRuntimeUtility.PanelToScreenBottomLeftPosition(Vector2 mousePosition, int targetDisplay)
        {
            return UIElementsRuntimeUtility.PanelToScreenBottomLeftPosition(mousePosition, targetDisplay);
        }

        void IDefaultEventSystem.ApplyRaycasterAsDefault(bool processWorldSpaceInput, bool defaultEventCameraIsMainCamera, Camera[] eventCameras)
        {
            IRuntimePanel.defaultEventSystem.raycaster =  processWorldSpaceInput
            ? defaultEventCameraIsMainCamera
                ? new MainCameraScreenRaycaster()
                : new CameraScreenRaycaster { cameras = (Camera[])eventCameras.Clone() }
                : new CameraScreenRaycaster { cameras = Array.Empty<Camera>() };
        }
    }


    internal class RuntimePanel : BaseRuntimePanel, IRuntimePanel
    {
        internal static readonly EventDispatcher s_EventDispatcher = RuntimeEventDispatcher.Create();

        private readonly PanelSettings m_PanelSettings;
        public PanelSettings panelSettings => m_PanelSettings;

        bool IRuntimePanel.drawsInCameras => base.drawsInCameras;
        bool IRuntimePanel.isFlat => base.isFlat;

        private static readonly List<IPanelComponent> s_EmptyPanelComponentList = new();

        internal List<IPanelComponent> panelComponents =>
            m_PanelSettings.m_AttachedPanelComponentsList?.m_AttachedPanelComponents ?? s_EmptyPanelComponentList;

        bool IRuntimePanel.disposed => disposed;

        int IRuntimePanel.targetDisplay => targetDisplay;

        float IRuntimePanel.sortingPriority => sortingPriority;
        int IRuntimePanel.resolvedSortingIndex => resolvedSortingIndex;

        IEventHandler IRuntimePanel.visualTree_as_IEventHandler => visualTree;

        string IRuntimePanel.name => name;

        IEventHandler IRuntimePanel.Pick(Vector2 point, int pointerId)
        {
            return Pick(point, pointerId);
        }

        public static RuntimePanel Create(ScriptableObject ownerObject)
        {
            return new RuntimePanel(ownerObject);
        }

        protected RuntimePanel(ScriptableObject ownerObject)
            : base(ownerObject, s_EventDispatcher)
        {
            CreateMenuFunctor = () => new GenericDropdownMenu();
            IRuntimePanel.uIElementsRuntimeUtility ??= new CodeStrippingSafeUIElementsRuntimeUtilityImpl();

            focusController = new FocusController(new NavigateFocusRing(visualTree));
            m_PanelSettings = ownerObject as PanelSettings;
            name = m_PanelSettings != null ? m_PanelSettings.name : "RuntimePanel";

            visualTree.RegisterCallback<FocusEvent, RuntimePanel>((e, p) => p.OnElementFocus(e), this,
                TrickleDown.TrickleDown);
        }

        internal override void Update()
        {
            if (m_PanelSettings != null)
            {
                m_PanelSettings.ApplyPanelSettings();
            }

            base.Update();
        }

        private void OnElementFocus(FocusEvent evt)
        {
            UIElementsRuntimeUtility.defaultEventSystem.OnFocusEvent(this, evt);
        }

        void IRuntimePanel.Blur()
        {
            Blur();
        }

        void IRuntimePanel.SetTopElementUnderPointer(int pointerId, IEventHandler element, EventBase triggerEvent)
        {
            SetTopElementUnderPointer(pointerId, (VisualElement)element, triggerEvent);
        }

        void IRuntimePanel.SetTopElementUnderPointer(int pointerId, IEventHandler element, Vector2 position)
        {
            SetTopElementUnderPointer(pointerId, (VisualElement)element, position);
        }

        void IRuntimePanel.PointerEntersPanel(int pointerId, Vector3 position)
        {
            PointerEntersPanel(pointerId, position);
        }

        void IRuntimePanel.PointerLeavesPanel(int pointerId)
        {
            PointerLeavesPanel(pointerId);
        }

        void IRuntimePanel.CommitElementUnderPointers()
        {
            CommitElementUnderPointers();
        }

        void IRuntimePanel.SendEvent(EventBase e)
        {
            SendEvent(e);
        }

        void IRuntimePanel.Focus()
        {
            Focus();
        }

        bool IRuntimePanel.ScreenToPanel(Vector2 screenPosition, Vector2 screenDelta, out Vector3 panelPosition, bool allowOutside)
        {
            return ScreenToPanel(screenPosition, screenDelta, out panelPosition, allowOutside);
        }

        private record EventCallbackWrapper
        {
            internal EventCallbackWrapper(Action a)
            {
                WrappedCallback = (e) => a();
            }
            public readonly EventCallback<FocusEvent> WrappedCallback;
        }

        static Dictionary<Action, EventCallbackWrapper> registeredCallback;

        void IRuntimePanel.RegisterRootFocusCallback(Action callback)
        {
            registeredCallback ??= new();

            EventCallbackWrapper w = new(callback);
            registeredCallback.Add(callback,w);

            visualTree.RegisterCallback<FocusEvent>(w.WrappedCallback, TrickleDown.TrickleDown);
        }

        void IRuntimePanel.UnregisterRootFocusCallback(Action callback)
        {
            if (registeredCallback?.TryGetValue(callback, out var w) ?? false)
            {
                registeredCallback.Remove(callback);
                visualTree.UnregisterCallback<FocusEvent>(w.WrappedCallback, TrickleDown.TrickleDown);
            }
        }

        IEventHandler IRuntimePanel.GetLeafFocusedElement()
        {
            return focusController.GetLeafFocusedElement();
        }

        private void UpdatePointerEventTarget<TPointerEvent>(TPointerEvent e, IPointerEvent eventData, IEventHandler target, IEventHandler underPointer)
    where TPointerEvent : PointerEventBase<TPointerEvent>, new()
        {
            e.target = target;

            if (!isFlat)
            {
                // World-space panels set their top element manually instead of using RecomputeElementUnderPointer.
                SetTopElementUnderPointer(eventData.pointerId, (VisualElement) underPointer, e);
    }
}

        bool IRuntimePanel.SendPointerMoveEvent(IPointerEvent eventData, IEventHandler target, IEventHandler elementUnderPointer)
        {
            using (var evt = PointerMoveEvent.GetPooled(eventData))
            {
                UpdatePointerEventTarget(evt, eventData, target, elementUnderPointer);
                SendEvent(evt);
                return evt.isPropagationStopped;
            }
        }

        bool IRuntimePanel.SendPointerDownEvent(IPointerEvent eventData, IEventHandler target, IEventHandler elementUnderPointer, Camera camera)
        {
            using (var evt = PointerDownEvent.GetPooled(eventData))
            {
                UpdatePointerEventTarget(evt, eventData, target, elementUnderPointer);
                SendEvent(evt);

                // Handle soft pointer capture for Down events
                PointerDeviceState.SetElementWithSoftPointerCapture(
                    eventData.pointerId, (VisualElement)evt.target, camera);

                return evt.isPropagationStopped;
            }
        }

        bool IRuntimePanel.SendPointerUpEvent(IPointerEvent eventData, IEventHandler target, IEventHandler elementUnderPointer)
        {
            using (var evt = PointerUpEvent.GetPooled(eventData))
            {
                UpdatePointerEventTarget(evt, eventData, target, elementUnderPointer);
                SendEvent(evt);

                // Release soft pointer capture if no buttons are pressed
                if (eventData.pressedButtons == 0)
                {
                    PointerDeviceState.SetElementWithSoftPointerCapture(eventData.pointerId, null, null);
                }

                return evt.isPropagationStopped;
            }
        }

        bool IRuntimePanel.SendPointerCancelEvent(IPointerEvent eventData, IEventHandler target, IEventHandler elementUnderPointer)
        {
            using (var evt = PointerCancelEvent.GetPooled(eventData))
            {
                UpdatePointerEventTarget(evt, eventData, target, elementUnderPointer);
                SendEvent(evt);

                // Release soft pointer capture if no buttons are pressed
                if (eventData.pressedButtons == 0)
                {
                    PointerDeviceState.SetElementWithSoftPointerCapture(eventData.pointerId, null, null);
                }

                return evt.isPropagationStopped;
            }
        }

        bool IRuntimePanel.SendWheelEvent(Vector2 uitkScrollDelta, IPointerEvent eventData)
        {
            using (var evt = WheelEvent.GetPooled(uitkScrollDelta, eventData))
            {
                SendEvent(evt);
                return evt.isPropagationStopped;
            }
        }

        bool IRuntimePanel.SendNavigationEvent(NavigationEventType eventType, IEventHandler target,
            NavigationDeviceType deviceType, EventModifiers modifiers, Vector2 moveVector,
            NavigationMoveDirection moveDirection)
        {
            EventBase evt = eventType switch
            {
                NavigationEventType.Submit => NavigationSubmitEvent.GetPooled(deviceType, modifiers),
                NavigationEventType.Cancel => NavigationCancelEvent.GetPooled(deviceType, modifiers),
                NavigationEventType.Move when moveVector != default =>
                    NavigationMoveEvent.GetPooled(moveVector, deviceType, modifiers),
                NavigationEventType.Move =>
                    NavigationMoveEvent.GetPooled((NavigationMoveEvent.Direction)moveDirection, deviceType, modifiers), // TabEvent
                _ => null
            };

            if (evt == null)
                return false;

            using (evt)
            {
                evt.target = target;
                SendEvent(evt);
                return evt.isPropagationStopped;
            }
        }

        bool IRuntimePanel.SendKeyboardEvent(bool isKeyDown, char character, KeyCode keyCode, EventModifiers modifiers,
            IEventHandler target)
        {
            EventBase evt = isKeyDown
                ? KeyDownEvent.GetPooled(character, keyCode, modifiers)
                : KeyUpEvent.GetPooled(character, keyCode, modifiers);

            using (evt)
            {
                evt.target = target;
                SendEvent(evt);
                return evt.isPropagationStopped;
            }
        }
        bool IRuntimePanel.SendIMEEvent(string compositionString, IEventHandler target)
        {
            using (var imeEvt = IMEEvent.GetPooled(compositionString ?? string.Empty))
            {
                imeEvt.target = target;
                SendEvent(imeEvt);
                return imeEvt.isPropagationStopped;
            }
        }

    }
}
