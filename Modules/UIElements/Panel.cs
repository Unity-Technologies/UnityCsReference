// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;

namespace UnityEngine.UIElements
{
    public enum ContextType
    {
        Player = 0,
        Editor = 1
    }

    [Flags]
    internal enum VersionChangeType
    {
        // Some data was bound
        Bindings = 1 << 0,
        // persistent data ready
        ViewData = 1 << 1,
        // changes to hierarchy
        Hierarchy = 1 << 2,
        // changes to properties that may have an impact on layout
        Layout = 1 << 3,
        // changes to StyleSheet, USS class
        StyleSheet = 1 << 4,
        // changes to styles, colors and other render properties
        Styles = 1 << 5,
        Overflow = 1 << 6,
        BorderRadius = 1 << 7,
        // changes that may impact the world transform (e.g. laid out position, local transform)
        Transform = 1 << 8,
        // changes to the size of the element after layout has been performed, without taking the local transform into account
        Size = 1 << 9,
        // The visuals of the element have changed
        Repaint = 1 << 10,
        // The opacity of the element have changed
        Opacity = 1 << 11,
    }

    [Flags]
    public enum UsageHints
    {
        None = 0,
        DynamicTransform = 1 << 0,
        GroupTransform = 1 << 1
    }

    [Flags]
    internal enum RenderHints
    {
        None = 0,
        GroupTransform = 1 << 0, // Use uniform matrix to transform children
        BoneTransform = 1 << 1, // Use GPU buffer to store transform matrices
        ClipWithScissors = 1 << 2 // If clipping is requested on this element, prefer scissoring
    }

    enum PanelClearFlags
    {
        None = 0,
        Color = 1 << 0,
        Depth = 1 << 1,
        All = Color | Depth
    }

    internal class RepaintData
    {
        public Matrix4x4 currentOffset { get; set; } = Matrix4x4.identity;
        public Vector2 mousePosition { get; set; }
        public Rect currentWorldClip { get; set; }
        public Event repaintEvent { get; set; }
    }

    internal delegate void HierarchyEvent(VisualElement ve, HierarchyChangeType changeType);

    internal interface IGlobalPanelDebugger
    {
        bool InterceptMouseEvent(IPanel panel, IMouseEvent ev);
        void OnPostMouseEvent(IPanel panel, IMouseEvent ev);
    }

    internal interface IPanelDebugger
    {
        IPanelDebug panelDebug { get; set; }

        void Disconnect();
        void Refresh();
        void OnVersionChanged(VisualElement ele, VersionChangeType changeTypeFlag);

        bool InterceptEvent(EventBase ev);
        void PostProcessEvent(EventBase ev);
    }

    internal interface IPanelDebug
    {
        IPanel panel { get; }

        VisualElement visualTree { get; }

        void AttachDebugger(IPanelDebugger debugger);
        void DetachDebugger(IPanelDebugger debugger);
        void DetachAllDebuggers();
        IEnumerable<IPanelDebugger> GetAttachedDebuggers();

        void MarkDirtyRepaint();

        void Refresh();
        void OnVersionChanged(VisualElement ele, VersionChangeType changeTypeFlag);

        bool InterceptEvent(EventBase ev);
        void PostProcessEvent(EventBase ev);
    }

    // Passed-in to every element of the visual tree
    public interface IPanel : IDisposable
    {
        VisualElement visualTree { get; }
        EventDispatcher dispatcher { get; }
        ContextType contextType { get; }
        FocusController focusController { get; }
        VisualElement Pick(Vector2 point);

        VisualElement PickAll(Vector2 point, List<VisualElement> picked);

        ContextualMenuManager contextualMenuManager { get; }
    }

    abstract class BaseVisualElementPanel : IPanel
    {
        public abstract EventInterests IMGUIEventInterests { get; set; }
        public abstract ScriptableObject ownerObject { get; protected set; }
        public abstract SavePersistentViewData saveViewData { get; set; }
        public abstract GetViewDataDictionary getViewDataDictionary { get; set; }
        public abstract int IMGUIContainersCount { get; set; }
        public abstract FocusController focusController { get; set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                if (panelDebug != null)
                {
                    panelDebug.DetachAllDebuggers();
                    panelDebug = null;
                }
                if (ownerObject != null)
                    UIElementsUtility.RemoveCachedPanel(ownerObject.GetInstanceID());
            }
            else
                DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        public abstract void Repaint(Event e);
        public abstract void ValidateLayout();
        public abstract void UpdateAnimations();
        public abstract void UpdateBindings();
        public abstract void ApplyStyles();

        public abstract void DirtyStyleSheets();
        private float m_Scale = 1;
        internal float scale
        {
            get { return m_Scale; }
            set
            {
                if (!Mathf.Approximately(m_Scale, value))
                {
                    m_Scale = value;
                    // if the surface DPI changes we need to invalidate styles
                    visualTree.IncrementVersion(VersionChangeType.StyleSheet);
                }
            }
        }

        private float m_PixelsPerPoint = 1;
        internal float pixelsPerPoint
        {
            get { return m_PixelsPerPoint; }
            set
            {
                if (!Mathf.Approximately(m_PixelsPerPoint, value))
                {
                    m_PixelsPerPoint = value;
                    // if the surface DPI changes we need to invalidate styles
                    visualTree.IncrementVersion(VersionChangeType.StyleSheet);
                }
            }
        }

        public float scaledPixelsPerPoint
        {
            get { return m_PixelsPerPoint * m_Scale; }
        }

        internal PanelClearFlags clearFlags { get; set; } = PanelClearFlags.All;

        internal bool duringLayoutPhase {get; set;}

        internal bool isDirty
        {
            get { return version != repaintVersion; }
        }

        internal abstract uint version { get; }
        internal abstract uint repaintVersion { get; }

        internal abstract void OnVersionChanged(VisualElement ele, VersionChangeType changeTypeFlag);
        internal abstract void SetUpdater(IVisualTreeUpdater updater, VisualTreeUpdatePhase phase);

        // Need virtual for tests
        internal virtual RepaintData repaintData { get; set; }
        // Need virtual for tests
        internal virtual ICursorManager cursorManager { get; set; }
        public ContextualMenuManager contextualMenuManager { get; internal set; }

        internal Matrix4x4 GetProjection()
        {
            var rect = visualTree.layout;
            return ProjectionUtils.Ortho(rect.xMin, rect.xMax, rect.yMax, rect.yMin, -1, 1);
        }

        internal Rect GetViewport() { return visualTree.layout; }

        //IPanel
        public abstract VisualElement visualTree { get; }
        public abstract EventDispatcher dispatcher { get; protected set; }

        internal void SendEvent(EventBase e, DispatchMode dispatchMode = DispatchMode.Queued)
        {
            Debug.Assert(dispatcher != null);
            dispatcher?.Dispatch(e, this, dispatchMode);
        }

        internal abstract IScheduler scheduler { get; }
        public abstract ContextType contextType { get; protected set; }
        public abstract VisualElement Pick(Vector2 point);
        public abstract VisualElement PickAll(Vector2 point, List<VisualElement> picked);

        internal bool disposed { get; private set; }

        internal abstract IVisualTreeUpdater GetUpdater(VisualTreeUpdatePhase phase);

        internal ElementUnderPointer m_TopElementUnderPointers = new ElementUnderPointer();

        internal VisualElement GetTopElementUnderPointer(int pointerId)
        {
            return m_TopElementUnderPointers.GetTopElementUnderPointer(pointerId);
        }

        void SetElementUnderPointer(VisualElement newElementUnderPointer, int pointerId, Vector2 pointerPos)
        {
            m_TopElementUnderPointers.SetElementUnderPointer(newElementUnderPointer, pointerId, pointerPos);
        }

        internal void SetElementUnderPointer(VisualElement newElementUnderPointer, EventBase triggerEvent)
        {
            m_TopElementUnderPointers.SetElementUnderPointer(newElementUnderPointer, triggerEvent);
        }

        internal void CommitElementUnderPointers()
        {
            m_TopElementUnderPointers.CommitElementUnderPointers(dispatcher);
        }

        internal abstract Shader standardShader { get; set; }

        internal event Action standardShaderChanged;
        protected void InvokeStandardShaderChanged() { if (standardShaderChanged != null) standardShaderChanged(); }

        internal event HierarchyEvent hierarchyChanged;
        internal void InvokeHierarchyChanged(VisualElement ve, HierarchyChangeType changeType) { if (hierarchyChanged != null) hierarchyChanged(ve, changeType); }

        internal void UpdateElementUnderPointers()
        {
            foreach (var pointerId in PointerId.hoveringPointers)
            {
                if (PointerDeviceState.GetPanel(pointerId) != this)
                {
                    SetElementUnderPointer(null, pointerId, new Vector2(float.MinValue, float.MinValue));
                }
                else
                {
                    var pointerPos = PointerDeviceState.GetPointerPosition(pointerId);
                    // Here it's important to call PickAll instead of Pick to ensure we don't use the cached value.
                    VisualElement elementUnderPointer = PickAll(pointerPos, null);
                    SetElementUnderPointer(elementUnderPointer, pointerId, pointerPos);
                }
            }

            CommitElementUnderPointers();
        }

        public IPanelDebug panelDebug { get; set; }

        public void Update()
        {
            scheduler.UpdateScheduledEvents();
            ValidateLayout();
            UpdateBindings();
        }
    }

    // Strategy to load assets must be provided in the context of Editor or Runtime
    internal delegate Object LoadResourceFunction(string pathName, System.Type type, float dpiScaling);

    // Strategy to fetch real time since startup in the context of Editor or Runtime
    internal delegate long TimeMsFunction();

    // Getting the view data dictionary relies on the Editor window.
    internal delegate ISerializableJsonDictionary GetViewDataDictionary();

    // Strategy to save persistent data must be provided in the context of Editor or Runtime
    internal delegate void SavePersistentViewData();

    // Default panel implementation
    internal class Panel : BaseVisualElementPanel
    {
        private VisualElement m_RootContainer;
        private VisualTreeUpdater m_VisualTreeUpdater;
        private string m_PanelName;
        private uint m_Version = 0;
        private uint m_RepaintVersion = 0;

#pragma warning disable CS0649
        internal static Action BeforeUpdaterChange;
        internal static Action AfterUpdaterChange;
#pragma warning restore CS0649

        ProfilerMarker m_MarkerUpdate;
        ProfilerMarker m_MarkerLayout;
        ProfilerMarker m_MarkerBindings;
        ProfilerMarker m_MarkerAnimations;
        static ProfilerMarker s_MarkerPickAll = new ProfilerMarker("Panel.PickAll");

        public override VisualElement visualTree
        {
            get { return m_RootContainer; }
        }

        public override EventDispatcher dispatcher { get; protected set; }

        TimerEventScheduler m_Scheduler;

        public TimerEventScheduler timerEventScheduler
        {
            get { return m_Scheduler ?? (m_Scheduler = new TimerEventScheduler()); }
        }

        internal override IScheduler scheduler
        {
            get { return timerEventScheduler; }
        }

        public override ScriptableObject ownerObject { get; protected set; }

        public override ContextType contextType { get; protected set; }

        public override SavePersistentViewData saveViewData { get; set; }

        public override GetViewDataDictionary getViewDataDictionary { get; set; }

        public override FocusController focusController { get; set; }

        public override EventInterests IMGUIEventInterests { get; set; }

        internal static LoadResourceFunction loadResourceFunc { private get; set; }

        internal static Object LoadResource(string pathName, Type type, float dpiScaling)
        {
            // TODO make the LoadResource function non-static.
            // if (panel.contextType = ContextType.Player)
            //    obj = Resources.Load(pathName, type);
            // else
            //    ...

            Object obj = null;

            if (loadResourceFunc != null)
            {
                obj = loadResourceFunc(pathName, type, dpiScaling);
            }
            else
            {
                obj = Resources.Load(pathName, type);
            }

            return obj;
        }

        private Focusable m_SavedFocusedElement;

        internal void Focus()
        {
            if (m_SavedFocusedElement != null && !(m_SavedFocusedElement is IMGUIContainer))
                m_SavedFocusedElement.Focus();

            m_SavedFocusedElement = null;
        }

        internal void Blur()
        {
            m_SavedFocusedElement = focusController?.GetLeafFocusedElement();

            if (m_SavedFocusedElement != null && !(m_SavedFocusedElement is IMGUIContainer))
                m_SavedFocusedElement.Blur();
        }

        internal string name
        {
            get { return m_PanelName; }
            set
            {
                m_PanelName = value;

                CreateMarkers();
            }
        }

        void CreateMarkers()
        {
            if (!string.IsNullOrEmpty(m_PanelName))
            {
                m_MarkerUpdate = new ProfilerMarker($"Panel.Update.{m_PanelName}");
                m_MarkerLayout = new ProfilerMarker($"Panel.Layout.{m_PanelName}");
                m_MarkerBindings = new ProfilerMarker($"Panel.Bindings.{m_PanelName}");
                m_MarkerAnimations = new ProfilerMarker($"Panel.Animations.{m_PanelName}");
            }
            else
            {
                m_MarkerUpdate = new ProfilerMarker("Panel.Update");
                m_MarkerLayout = new ProfilerMarker("Panel.Layout");
                m_MarkerBindings = new ProfilerMarker("Panel.Bindings");
                m_MarkerAnimations = new ProfilerMarker("Panel.Animations");
            }
        }

        private static TimeMsFunction s_TimeSinceStartup;
        internal static TimeMsFunction TimeSinceStartup
        {
            get { return s_TimeSinceStartup; }
            set
            {
                if (value == null)
                {
                    value = DefaultTimeSinceStartupMs;
                }

                s_TimeSinceStartup = value;
            }
        }

        public override int IMGUIContainersCount { get; set; }

        internal override uint version
        {
            get { return m_Version; }
        }

        internal override uint repaintVersion
        {
            get { return m_RepaintVersion; }
        }

        private Shader m_StandardShader;

        internal override Shader standardShader
        {
            get { return m_StandardShader; }
            set
            {
                if (m_StandardShader != value)
                {
                    m_StandardShader = value;
                    InvokeStandardShaderChanged();
                }
            }
        }

        internal static Panel CreateEditorPanel(ScriptableObject ownerObject)
        {
            return new Panel(ownerObject, ContextType.Editor, EventDispatcher.editorDispatcher);
        }

        public Panel(ScriptableObject ownerObject, ContextType contextType, EventDispatcher dispatcher)
        {
            m_VisualTreeUpdater = new VisualTreeUpdater(this);

            this.ownerObject = ownerObject;
            this.contextType = contextType;
            this.dispatcher = dispatcher;
            repaintData = new RepaintData();
            cursorManager = new CursorManager();
            contextualMenuManager = null;
            m_RootContainer = new VisualElement
            {
                name = VisualElementUtils.GetUniqueName("unity-panel-container"),
                viewDataKey = "PanelContainer"
            };

            // Required!
            visualTree.SetPanel(this);
            focusController = new FocusController(new VisualElementFocusRing(visualTree));

            CreateMarkers();

            InvokeHierarchyChanged(visualTree, HierarchyChangeType.Add);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                m_VisualTreeUpdater.Dispose();

            base.Dispose(disposing);
        }

        public static long TimeSinceStartupMs()
        {
            return (s_TimeSinceStartup == null) ? DefaultTimeSinceStartupMs() : s_TimeSinceStartup();
        }

        internal static long DefaultTimeSinceStartupMs()
        {
            return (long)(Time.realtimeSinceStartup * 1000.0f);
        }

        // For tests only.
        internal static VisualElement PickAllWithoutValidatingLayout(VisualElement root, Vector2 point)
        {
            return PickAll(root, point);
        }

        private static VisualElement PickAll(VisualElement root, Vector2 point, List<VisualElement> picked = null)
        {
            s_MarkerPickAll.Begin();
            var result = PerformPick(root, point, picked);
            s_MarkerPickAll.End();
            return result;
        }

        private static VisualElement PerformPick(VisualElement root, Vector2 point, List<VisualElement> picked = null)
        {
            // Skip picking for elements with display: none
            if (root.resolvedStyle.display == DisplayStyle.None)
                return null;

            if (root.pickingMode == PickingMode.Ignore && root.hierarchy.childCount == 0)
            {
                return null;
            }

            if (!root.worldBoundingBox.Contains(point))
            {
                return null;
            }

            // Problem here: everytime we pick, we need to do that expensive transformation.
            // The default Contains() compares with rect, while we could cache the rect in world space (transform 2 points, 4 if there is rotation) and be done
            // here we have to transform 1 point at every call.
            // Now since this is a virtual, we can't just start to call it with global pos... we could break client code.
            // EdgeControl and port connectors in GraphView overload this.
            Vector2 localPoint = root.WorldToLocal(point);

            bool containsPoint = root.ContainsPoint(localPoint);
            // we only skip children in the case we visually clip them
            if (!containsPoint && root.ShouldClip())
            {
                return null;
            }

            VisualElement returnedChild = null;
            // Depth first in reverse order, do children
            var cCount = root.hierarchy.childCount;
            for (int i = cCount - 1; i >= 0; i--)
            {
                var child = root.hierarchy[i];
                var result = PerformPick(child, point, picked);
                if (returnedChild == null && result != null && result.visible)
                    returnedChild = result;
            }

            if (picked != null && root.enabledInHierarchy && root.visible && root.pickingMode == PickingMode.Position && containsPoint)
            {
                picked.Add(root);
            }

            if (returnedChild != null)
                return returnedChild;

            switch (root.pickingMode)
            {
                case PickingMode.Position:
                {
                    if (containsPoint && root.enabledInHierarchy && root.visible)
                    {
                        return root;
                    }
                }
                break;
                case PickingMode.Ignore:
                    break;
            }
            return null;
        }

        public override VisualElement PickAll(Vector2 point, List<VisualElement> picked)
        {
            ValidateLayout();

            if (picked != null)
                picked.Clear();

            return PickAll(visualTree, point, picked);
        }

        public override VisualElement Pick(Vector2 point)
        {
            ValidateLayout();
            Vector2 mousePos;
            var element = m_TopElementUnderPointers.GetTopElementUnderPointer(PointerId.mousePointerId, out mousePos);
            var diff = mousePos - point;
            // The VisualTreeTransformClipUpdater updates the ElementUnderPointer after each validate layout.
            // small enough to be smaller than 1 pixel
            if (diff.sqrMagnitude < 0.25f)
            {
                return element;
            }

            return PickAll(visualTree, point);
        }

        private bool m_ValidatingLayout = false;
        public override void ValidateLayout()
        {
            // Reentrancy proofing: ValidateLayout() could be in the code path of updaters.
            // Actual case: TransformClip update phase recomputes elements under mouse, which does a pick, which validates layout.
            // Updaters use version numbers for early exit, but it may happen that an updater invalidates a subsequent updater.
            if (!m_ValidatingLayout)
            {
                m_ValidatingLayout = true;

                m_MarkerLayout.Begin();
                m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Styles);
                m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Layout);
                m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.TransformClip);
                m_MarkerLayout.End();

                m_ValidatingLayout = false;
            }
        }

        public override void UpdateAnimations()
        {
            m_MarkerAnimations.Begin();
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Animation);
            m_MarkerAnimations.End();
        }

        public override void UpdateBindings()
        {
            m_MarkerBindings.Begin();
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Bindings);
            m_MarkerBindings.End();
        }

        public override void ApplyStyles()
        {
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Styles);
        }

        void UpdateForRepaint()
        {
            //Here we don't want to update animation and bindings which are ticked by the scheduler
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.ViewData);
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Styles);
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Layout);
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.TransformClip);
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Repaint);
        }

        public override void DirtyStyleSheets()
        {
            m_VisualTreeUpdater.DirtyStyleSheets();
        }


        public override void Repaint(Event e)
        {
            if (contextType == ContextType.Editor)
                Debug.Assert(GUIClip.Internal_GetCount() == 0, "UIElement is not compatible with IMGUI GUIClips, only GUIClip.ParentClipScope");

            m_RepaintVersion = version;

            // in an in-game context, pixelsPerPoint is user driven
            if (contextType == ContextType.Editor)
                pixelsPerPoint = GUIUtility.pixelsPerPoint;

            repaintData.repaintEvent = e;

            using (m_MarkerUpdate.Auto())
            {
                UpdateForRepaint();
            }

            panelDebug?.Refresh();
        }

        internal override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            ++m_Version;
            m_VisualTreeUpdater.OnVersionChanged(ve, versionChangeType);
            panelDebug?.OnVersionChanged(ve, versionChangeType);
        }

        internal override void SetUpdater(IVisualTreeUpdater updater, VisualTreeUpdatePhase phase)
        {
            m_VisualTreeUpdater.SetUpdater(updater, phase);
        }

        internal override IVisualTreeUpdater GetUpdater(VisualTreeUpdatePhase phase)
        {
            return m_VisualTreeUpdater.GetUpdater(phase);
        }
    }

    internal class RuntimePanel : Panel
    {
        public RuntimePanel(ScriptableObject ownerObject, EventDispatcher dispatcher = null)
            : base(ownerObject, ContextType.Player, dispatcher) {}

        // we may provide a rendertexture to be used for world space rendering
        internal RenderTexture targetTexture = null;

        public override void Repaint(Event e)
        {
            // if the renderTarget is not set, we simply render on whatever target is currently set
            if (targetTexture == null)
            {
                clearFlags = PanelClearFlags.Depth;
                base.Repaint(e);
                return;
            }

            var toBeRestoredTarget = RenderTexture.active;
            RenderTexture.active = targetTexture;
            clearFlags = PanelClearFlags.All;
            base.Repaint(e);
            RenderTexture.active = toBeRestoredTarget;
        }
    }
}
