// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine.Yoga;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Describes in which context a VisualElement hierarchy is being ran.
    /// </summary>
    public enum ContextType
    {
        /// <summary>
        /// Currently running in an Unity Player.
        /// </summary>
        Player = 0,
        /// <summary>
        /// Currently running in the Unity Editor.
        /// </summary>
        Editor = 1
    }

    // NOTE: if a new VersionChangeType is added, ComputedStyle.CompareChanges may need to be reworked!
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
        BorderWidth = 1 << 8,
        // changes that may impact the world transform (e.g. laid out position, local transform)
        Transform = 1 << 9,
        // changes to the size of the element after layout has been performed, without taking the local transform into account
        Size = 1 << 10,
        // The visuals of the element have changed
        Repaint = 1 << 11,
        // The opacity of the element have changed
        Opacity = 1 << 12,
        // Some color of the element has changed (background-color, border-color, etc.)
        Color = 1 << 13,
        RenderHints = 1 << 14,
        // The 'transition-property' style of the element has changed (impacts cancelling of ongoing style transitions)
        TransitionProperty = 1 << 15,
        // The combined registered callbacks' EventCategory values has changed
        EventCallbackCategories = 1 << 16,
        // The DisableRendering flag has changed
        DisableRendering = 1 << 17,
    }

    /// <summary>
    /// Offers a set of values that describe the intended usage patterns of a specific <see cref="VisualElement"/>.
    /// </summary>
    [Flags]
    public enum UsageHints
    {
        /// <summary>
        /// No particular hints applicable.
        /// </summary>
        None = 0,
        /// <summary>
        /// Marks a <see cref="VisualElement"/> that changes its transformation often (i.e. position, rotation or scale).
        /// When specified, this flag hints the system to optimize rendering of the <see cref="VisualElement"/> for recurring transformation changes. The VisualElement's vertex transformation will be done by the GPU when possible on the target platform.
        /// Please note that the number of VisualElements to which this hint effectively applies can be limited by target platform capabilities. For such platforms, it is recommended to prioritize use of this hint to only the VisualElements with the highest frequency of transformation changes.
        /// </summary>
        DynamicTransform = 1 << 0,
        /// <summary>
        /// Marks a <see cref="VisualElement"/> that hosts many children with <see cref="DynamicTransform"/> applied on them.
        /// A common use-case of this hint is a VisualElement that represents a "viewport" within which there are many <see cref="DynamicTransform"/> VisualElements that can move individually in addition to the "viewport" element also often changing its transformation. However, if the contents of the aforementioned "viewport" element are mostly static (not moving) then it is enough to use the <see cref="DynamicTransform"/> hint on that element instead of <see cref="GroupTransform"/>.
        /// Internally, an element hinted with <see cref="GroupTransform"/> will force a separate draw batch with its world transformation value, but in the same time it will avoid changing the transforms of all its descendants whenever a transformation change occurs on the <see cref="GroupTransform"/> element.
        /// </summary>
        GroupTransform = 1 << 1,
        /// <summary>
        /// Marks a <see cref="VisualElement"/> that hosts non-rectangular descendants using the "overflow: hidden;"
        /// style. Non-rectangular masks are implemented with the stencil. If applicable, the renderer breaks the batch
        /// to preemptively set the stencil state, before and after drawing the descendants, so that the descendants
        /// won't have to set them at the next masking level. When using this flag, consecutive stencil push/pop
        /// operations are cheap and don't require modifying the stencil reference. As a result, the batch doesn't need
        /// to be broken for each push/pop operation. Consecutive push/push or pop/pop operations are still expensive.
        /// Avoid cases that involve many subtrees, where each subtree uses 2 or more levels of masking, to avoid
        /// consecutive push/push or pop/pop operations.
        /// </summary>
        /// <remarks>
        /// This flag is implicitly set on the root VisualElement, making single-level masks non-batch-breaking by default.
        /// </remarks>
        MaskContainer = 1 << 2,
        /// <summary>
        /// Marks a <see cref="VisualElement"/> that changes its color often (background-color, border-color, etc.).
        /// This will store the element's colors in an optimized storage suitable for frequent changes, such as animation.
        /// </summary>
        DynamicColor = 1 << 3,
    }

    [Flags]
    internal enum RenderHints
    {
        None = 0,

        GroupTransform = 1 << 0, // Use uniform matrix to transform children
        BoneTransform = 1 << 1, // Use GPU buffer to store transform matrices
        ClipWithScissors = 1 << 2, // If clipping is requested on this element, prefer scissoring
        MaskContainer = 1 << 3, // Use to prevent the next nested masks from modifying the stencil ref
        DynamicColor = 1 << 4, // Use to store the color in shaderInfo storage

        // Whenever we change a render hint, we also set a dirty flag to indicate that it has changed
        // This way, we don't need an additional field to store pending changes
        DirtyOffset = 5,
        DirtyGroupTransform = GroupTransform << DirtyOffset,
        DirtyBoneTransform = BoneTransform << DirtyOffset,
        DirtyClipWithScissors = ClipWithScissors << DirtyOffset,
        DirtyMaskContainer = MaskContainer << DirtyOffset,
        DirtyAll = DirtyGroupTransform | DirtyBoneTransform | DirtyClipWithScissors | DirtyMaskContainer,
    }

    // For backwards compatibility with debugger in 2020.1
    enum PanelClearFlags
    {
        None = 0,
        Color = 1 << 0,
        Depth = 1 << 1,
        All = Color | Depth
    }

    struct PanelClearSettings
    {
        public bool clearDepthStencil;
        public bool clearColor;
        public Color color;
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
        void OnContextClick(IPanel panel, ContextClickEvent ev);
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
        IPanel debuggerOverlayPanel { get; }

        VisualElement visualTree { get; }
        VisualElement debugContainer { get; }

        bool hasAttachedDebuggers { get; }

        void AttachDebugger(IPanelDebugger debugger);
        void DetachDebugger(IPanelDebugger debugger);
        void DetachAllDebuggers();
        IEnumerable<IPanelDebugger> GetAttachedDebuggers();

        void MarkDirtyRepaint();
        void MarkDebugContainerDirtyRepaint();
        void Refresh();
        void OnVersionChanged(VisualElement ele, VersionChangeType changeTypeFlag);

        bool InterceptEvent(EventBase ev);
        void PostProcessEvent(EventBase ev);
    }

    // Passed-in to every element of the visual tree
    /// <summary>
    /// Interface for classes implementing UI panels.
    /// </summary>
    public interface IPanel : IDisposable
    {
        /// <summary>
        /// Root of the VisualElement hierarchy.
        /// </summary>
        VisualElement visualTree { get; }
        /// <summary>
        /// This Panel EventDispatcher.
        /// </summary>
        EventDispatcher dispatcher { get; }
        /// <summary>
        /// Describes in which context a VisualElement hierarchy is being ran.
        /// </summary>
        ContextType contextType { get; }
        /// <summary>
        /// Return the focus controller for this panel.
        /// </summary>
        FocusController focusController { get; }
        /// <summary>
        /// Returns the top element at this position. Will not return elements with pickingMode set to <see cref="PickingMode.Ignore"/>.
        /// </summary>
        /// <param name="point">World coordinates.</param>
        /// <returns>Top VisualElement at the position. Null if none was found.</returns>
        VisualElement Pick(Vector2 point);

        /// <summary>
        /// Returns all elements at this position. Will not return elements with pickingMode set to <see cref="PickingMode.Ignore"/>.
        /// </summary>
        /// <param name="point">World coordinates.</param>
        /// <param name="picked">All Visualelements overlapping this position.</param>
        /// <returns>Top VisualElement at the position. Null if none was found.</returns>
        VisualElement PickAll(Vector2 point, List<VisualElement> picked);

        /// <summary>
        /// The Contextual menu manager for the panel.
        /// </summary>
        ContextualMenuManager contextualMenuManager { get; }
    }

    abstract class BaseVisualElementPanel : IPanel, IGroupBox
    {
        public abstract EventInterests IMGUIEventInterests { get; set; }
        public abstract ScriptableObject ownerObject { get; protected set; }
        public abstract SavePersistentViewData saveViewData { get; set; }
        public abstract GetViewDataDictionary getViewDataDictionary { get; set; }
        public abstract int IMGUIContainersCount { get; set; }
        public abstract FocusController focusController { get; set; }
        public abstract IMGUIContainer rootIMGUIContainer { get; set; }

        internal event Action<BaseVisualElementPanel> panelDisposed;

        private UIElementsBridge m_UIElementsBridge;

        internal UIElementsBridge uiElementsBridge
        {
            get
            {
                if (m_UIElementsBridge != null)
                    return m_UIElementsBridge;

                throw new Exception("Panel has no UIElementsBridge.");
            }

            set => m_UIElementsBridge = value;
        }

        protected BaseVisualElementPanel()
        {
            yogaConfig = new YogaConfig();
            yogaConfig.UseWebDefaults = YogaConfig.Default.UseWebDefaults;
            m_UIElementsBridge = new RuntimeUIElementsBridge();
        }

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

                PointerDeviceState.RemovePanelData(this);
            }
            else
                DisposeHelper.NotifyMissingDispose(this);

            panelDisposed?.Invoke(this);
            yogaConfig = null;
            disposed = true;
        }

        public abstract void Repaint(Event e);
        public abstract void ValidateFocus();
        public abstract void ValidateLayout();
        public abstract void UpdateAnimations();
        public abstract void UpdateBindings();
        public abstract void ApplyStyles();

        public abstract void UpdateAssetTrackers();
        public abstract void DirtyStyleSheets();
        internal abstract void UpdateInlineStylesRecursively(VisualElement root = null);

        public bool enableAssetReload
        {
            get => liveReloadSystem.enable;
            set => liveReloadSystem.enable = value;
        }

        private float m_Scale = 1;
        internal float scale
        {
            get { return m_Scale; }
            set
            {
                if (!Mathf.Approximately(m_Scale, value))
                {
                    m_Scale = value;

                    //we need to update the yoga config
                    visualTree.IncrementVersion(VersionChangeType.Layout);
                    yogaConfig.PointScaleFactor = scaledPixelsPerPoint;

                    // if the surface DPI changes we need to invalidate styles
                    visualTree.IncrementVersion(VersionChangeType.StyleSheet);
                }
            }
        }


        internal YogaConfig yogaConfig;

        private float m_PixelsPerPoint = 1;
        internal float pixelsPerPoint
        {
            get { return m_PixelsPerPoint; }
            set
            {
                if (!Mathf.Approximately(m_PixelsPerPoint, value))
                {
                    m_PixelsPerPoint = value;

                    //we need to update the yoga config
                    visualTree.IncrementVersion(VersionChangeType.Layout);
                    yogaConfig.PointScaleFactor = scaledPixelsPerPoint;

                    // if the surface DPI changes we need to invalidate styles
                    visualTree.IncrementVersion(VersionChangeType.StyleSheet);
                }
            }
        }

        public float scaledPixelsPerPoint
        {
            get { return m_PixelsPerPoint * m_Scale; }
        }

        public float referenceSpritePixelsPerUnit { get; set; } = 100.0f;

        // For backwards compatibility with debugger in 2020.1
        public PanelClearFlags clearFlags
        {
            get
            {
                PanelClearFlags flags = PanelClearFlags.None;

                if (clearSettings.clearColor)
                {
                    flags |= PanelClearFlags.Color;
                }

                if (clearSettings.clearDepthStencil)
                {
                    flags |= PanelClearFlags.Depth;
                }

                return flags;
            }
            set
            {
                var settings = clearSettings;
                settings.clearColor = (value & PanelClearFlags.Color) == PanelClearFlags.Color;
                settings.clearDepthStencil = (value & PanelClearFlags.Depth) == PanelClearFlags.Depth;
                clearSettings = settings;
            }
        }

        internal PanelClearSettings clearSettings { get; set; } = new PanelClearSettings { clearDepthStencil = true, clearColor = true, color = Color.clear };

        uint m_VertexBudget = 0;


        internal uint vertexBudget
        {
            get
            {
                return m_VertexBudget;
            }
            set
            {
                //early return to prevent trashing the render chain.
                if (m_VertexBudget == value)
                    return;

                m_VertexBudget = value;
                (GetUpdater(VisualTreeUpdatePhase.Repaint) as UIRRepaintUpdater).DestroyRenderChain();
            }
        }

        internal bool duringLayoutPhase { get; set; }

        internal bool isDirty
        {
            get
            {
                return (version != repaintVersion) || (((Panel)panelDebug?.debuggerOverlayPanel)?.isDirty ?? false);
            }
        }

        internal abstract uint version { get; }
        internal abstract uint repaintVersion { get; }
        internal abstract uint hierarchyVersion { get; }

        // Updaters can request an panel invalidation when some callbacks aren't coming from UIElements internally
        internal abstract void RequestUpdateAfterExternalEvent(IVisualTreeUpdater updater);
        internal abstract void OnVersionChanged(VisualElement ele, VersionChangeType changeTypeFlag);
        internal abstract void SetUpdater(IVisualTreeUpdater updater, VisualTreeUpdatePhase phase);

        // Need virtual for tests
        internal virtual RepaintData repaintData { get; set; }

        // Need virtual for tests
        internal virtual ICursorManager cursorManager { get; set; }
        public ContextualMenuManager contextualMenuManager { get; internal set; }

        //IPanel
        public abstract VisualElement visualTree { get; }
        public abstract EventDispatcher dispatcher { get; set; }

        internal void SendEvent(EventBase e, DispatchMode dispatchMode = DispatchMode.Queued)
        {
            Debug.Assert(dispatcher != null);
            dispatcher?.Dispatch(e, this, dispatchMode);
        }

        internal abstract IScheduler scheduler { get; }
        internal abstract IStylePropertyAnimationSystem styleAnimationSystem { get; set; }
        public abstract ContextType contextType { get; protected set; }
        public abstract VisualElement Pick(Vector2 point);
        public abstract VisualElement PickAll(Vector2 point, List<VisualElement> picked);

        internal bool disposed { get; private set; }

        internal abstract IVisualTreeUpdater GetUpdater(VisualTreeUpdatePhase phase);

        internal abstract IVisualTreeUpdater GetEditorUpdater(VisualTreeEditorUpdatePhase phase);

        internal ElementUnderPointer m_TopElementUnderPointers = new ElementUnderPointer();

        internal VisualElement GetTopElementUnderPointer(int pointerId)
        {
            return m_TopElementUnderPointers.GetTopElementUnderPointer(pointerId);
        }

        internal VisualElement RecomputeTopElementUnderPointer(int pointerId, Vector2 pointerPos, EventBase triggerEvent)
        {
            VisualElement element = null;

            if (PointerDeviceState.GetPanel(pointerId, contextType) == this &&
                !PointerDeviceState.HasLocationFlag(pointerId, contextType, PointerDeviceState.LocationFlag.OutsidePanel))
            {
                element = Pick(pointerPos);
            }

            m_TopElementUnderPointers.SetElementUnderPointer(element, pointerId, triggerEvent);
            return element;
        }

        internal void ClearCachedElementUnderPointer(int pointerId, EventBase triggerEvent)
        {
            m_TopElementUnderPointers.SetTemporaryElementUnderPointer(null, pointerId, triggerEvent);
        }

        internal void CommitElementUnderPointers()
        {
            m_TopElementUnderPointers.CommitElementUnderPointers(dispatcher, contextType);
        }

        internal abstract Shader standardShader { get; set; }

        internal virtual Shader standardWorldSpaceShader
        {
            get { return null; }
            set {}
        }

        internal event Action standardShaderChanged, standardWorldSpaceShaderChanged;

        protected void InvokeStandardShaderChanged()
        {
            if (standardShaderChanged != null) standardShaderChanged();
        }

        protected void InvokeStandardWorldSpaceShaderChanged()
        {
            if (standardWorldSpaceShaderChanged != null) standardWorldSpaceShaderChanged();
        }

        internal event Action atlasChanged;
        protected void InvokeAtlasChanged() { atlasChanged?.Invoke(); }
        public abstract AtlasBase atlas { get; set; }

        internal event Action<Material> updateMaterial;
        internal void InvokeUpdateMaterial(Material mat) { updateMaterial?.Invoke(mat); } // TODO: Actually call this!

        internal event HierarchyEvent hierarchyChanged;

        internal void InvokeHierarchyChanged(VisualElement ve, HierarchyChangeType changeType)
        {
            if (hierarchyChanged != null) hierarchyChanged(ve, changeType);
        }

        internal event Action<IPanel> beforeUpdate;
        internal void InvokeBeforeUpdate() { beforeUpdate?.Invoke(this); }

        internal void UpdateElementUnderPointers()
        {
            foreach (var pointerId in PointerId.hoveringPointers)
            {
                if (PointerDeviceState.GetPanel(pointerId, contextType) != this ||
                    PointerDeviceState.HasLocationFlag(pointerId, contextType, PointerDeviceState.LocationFlag.OutsidePanel))
                {
                    m_TopElementUnderPointers.SetElementUnderPointer(null, pointerId, new Vector2(float.MinValue, float.MinValue));
                }
                else
                {
                    var pointerPos = PointerDeviceState.GetPointerPosition(pointerId, contextType);

                    // Here it's important to call PickAll instead of Pick to ensure we don't use the cached value.
                    VisualElement elementUnderPointer = PickAll(pointerPos, null);
                    m_TopElementUnderPointers.SetElementUnderPointer(elementUnderPointer, pointerId, pointerPos);
                }
            }

            CommitElementUnderPointers();
        }

        void IGroupBox.OnOptionAdded(IGroupBoxOption option) { /* Nothing to do here. */ }
        void IGroupBox.OnOptionRemoved(IGroupBoxOption option) { /* Nothing to do here. */ }

        public IPanelDebug panelDebug { get; set; }
        public ILiveReloadSystem liveReloadSystem { get; set; }

        public virtual void Update()
        {
            scheduler.UpdateScheduledEvents();
            // This call is already on UIElementsUtility.UpdateSchedulers() but it's also necessary here for Runtime UI
            UpdateAssetTrackers();
            ValidateFocus();
            ValidateLayout();
            UpdateAnimations();
            UpdateBindings();
        }
    }

    // Strategy to initialize the editor updater
    internal delegate void InitEditorUpdaterFunction(BaseVisualElementPanel panel, VisualTreeUpdater visualTreeUpdater);

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
        private IStylePropertyAnimationSystem m_StylePropertyAnimationSystem;
        private string m_PanelName;
        private uint m_Version = 0;
        private uint m_RepaintVersion = 0;
        private uint m_HierarchyVersion = 0;

        ProfilerMarker m_MarkerBeforeUpdate;
        ProfilerMarker m_MarkerUpdate;
        ProfilerMarker m_MarkerLayout;
        ProfilerMarker m_MarkerBindings;
        ProfilerMarker m_MarkerAnimations;
        static ProfilerMarker s_MarkerPickAll = new ProfilerMarker("Panel.PickAll");

        public sealed override VisualElement visualTree
        {
            get { return m_RootContainer; }
        }

        public sealed override EventDispatcher dispatcher { get; set; }

        TimerEventScheduler m_Scheduler;

        public TimerEventScheduler timerEventScheduler
        {
            get { return m_Scheduler ?? (m_Scheduler = new TimerEventScheduler()); }
        }

        internal override IScheduler scheduler
        {
            get { return timerEventScheduler; }
        }

        internal VisualTreeUpdater visualTreeUpdater
        {
            get { return m_VisualTreeUpdater; }
        }

        internal override IStylePropertyAnimationSystem styleAnimationSystem
        {
            get => m_StylePropertyAnimationSystem;
            set
            {
                if (m_StylePropertyAnimationSystem == value)
                    return;

                m_StylePropertyAnimationSystem?.CancelAllAnimations();
                m_StylePropertyAnimationSystem = value;
            }
        }

        public override ScriptableObject ownerObject { get; protected set; }

        public override ContextType contextType { get; protected set; }

        public override SavePersistentViewData saveViewData { get; set; }

        public override GetViewDataDictionary getViewDataDictionary { get; set; }

        public sealed override FocusController focusController { get; set; }

        public override EventInterests IMGUIEventInterests { get; set; }

        internal static LoadResourceFunction loadResourceFunc { private get; set; }

        internal static InitEditorUpdaterFunction initEditorUpdaterFunc { private get; set; }

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

        private bool m_JustReceivedFocus;
        internal void Focus()
        {
            // Case 1345260: wait for the next editor update to set focus back to the element that had it before the
            // panel last lost the focus. This delay avoids setting the focus and immediately setting it again when the
            // panel is selected through clicking it.
            m_JustReceivedFocus = true;
        }

        internal void Blur()
        {
            focusController?.BlurLastFocusedElement();
        }

        public override void ValidateFocus()
        {
            if (m_JustReceivedFocus)
            {
                m_JustReceivedFocus = false;
                focusController?.SetFocusToLastFocusedElement();
            }

            focusController?.ValidateInternalState(this);
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
                m_MarkerBeforeUpdate = new ProfilerMarker($"Panel.BeforeUpdate.{m_PanelName}");
                m_MarkerUpdate = new ProfilerMarker($"Panel.Update.{m_PanelName}");
                m_MarkerLayout = new ProfilerMarker($"Panel.Layout.{m_PanelName}");
                m_MarkerBindings = new ProfilerMarker($"Panel.Bindings.{m_PanelName}");
                m_MarkerAnimations = new ProfilerMarker($"Panel.Animations.{m_PanelName}");
            }
            else
            {
                m_MarkerBeforeUpdate = new ProfilerMarker($"Panel.BeforeUpdate");
                m_MarkerUpdate = new ProfilerMarker("Panel.Update");
                m_MarkerLayout = new ProfilerMarker("Panel.Layout");
                m_MarkerBindings = new ProfilerMarker("Panel.Bindings");
                m_MarkerAnimations = new ProfilerMarker("Panel.Animations");
            }
        }

        internal static TimeMsFunction TimeSinceStartup { private get; set; }

        public override int IMGUIContainersCount { get; set; }

        public override IMGUIContainer rootIMGUIContainer { get; set; }

        internal override uint version => m_Version;
        internal override uint repaintVersion => m_RepaintVersion;
        internal override uint hierarchyVersion => m_HierarchyVersion;

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

        private AtlasBase m_Atlas;

        public override AtlasBase atlas
        {
            get { return m_Atlas; }
            set
            {
                if (m_Atlas != value)
                {
                    m_Atlas?.InvokeRemovedFromPanel(this);
                    m_Atlas = value;
                    InvokeAtlasChanged();
                    m_Atlas?.InvokeAssignedToPanel(this);
                }
            }
        }

        // Used by tests
        internal static Panel CreateEditorPanel(ScriptableObject ownerObject)
        {
            return new Panel(ownerObject, ContextType.Editor, EventDispatcher.editorDispatcher);
        }

        public Panel(ScriptableObject ownerObject, ContextType contextType, EventDispatcher dispatcher, InitEditorUpdaterFunction initEditorUpdater = null)
        {
            this.ownerObject = ownerObject;
            this.contextType = contextType;
            this.dispatcher = dispatcher;
            repaintData = new RepaintData();
            cursorManager = new CursorManager();
            contextualMenuManager = null;
            m_VisualTreeUpdater = new VisualTreeUpdater(this);
            var initFunc = initEditorUpdater ?? initEditorUpdaterFunc;
            initFunc.Invoke(this, m_VisualTreeUpdater);
            m_RootContainer = contextType == ContextType.Editor ? new EditorPanelRootElement() : new PanelRootElement();

            // Required!
            visualTree.SetPanel(this);
            focusController = new FocusController(new VisualElementFocusRing(visualTree));
            styleAnimationSystem = new StylePropertyAnimationSystem();

            CreateMarkers();

            InvokeHierarchyChanged(visualTree, HierarchyChangeType.Add);
            atlas = new DynamicAtlas();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                atlas = null;
                m_VisualTreeUpdater.Dispose();
            }

            base.Dispose(disposing);
        }

        public static long TimeSinceStartupMs()
        {
            return TimeSinceStartup?.Invoke() ?? DefaultTimeSinceStartupMs();
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

        private static VisualElement PickAll(VisualElement root, Vector2 point, List<VisualElement> picked = null, bool includeIgnoredElement = false)
        {
            s_MarkerPickAll.Begin();
            var result = PerformPick(root, point, picked, includeIgnoredElement);
            s_MarkerPickAll.End();
            return result;
        }

        private static VisualElement PerformPick(VisualElement root, Vector2 point, List<VisualElement> picked = null, bool includeIgnoredElement = false)
        {
            // Skip picking for elements with display: none
            if (root.resolvedStyle.display == DisplayStyle.None)
                return null;

            if (root.pickingMode == PickingMode.Ignore && root.hierarchy.childCount == 0 && !includeIgnoredElement)
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
                var result = PerformPick(child, point, picked, includeIgnoredElement);
                if (returnedChild == null && result != null)
                {
                    if (picked == null)
                    {
                        return result;
                    }

                    returnedChild = result;
                }
            }

            if (root.visible && (root.pickingMode == PickingMode.Position || includeIgnoredElement) && containsPoint)
            {
                picked?.Add(root);
                if (returnedChild == null)
                    returnedChild = root;
            }

            return returnedChild;
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
            // The VisualTreeHierarchyFlagsUpdater updates the ElementUnderPointer after each validate layout.
            ValidateLayout();
            var element = m_TopElementUnderPointers.GetTopElementUnderPointer(PointerId.mousePointerId,
                out Vector2 mousePos, out bool isTemporary);

            // Assume same pixel means same element, given nothing has changed in the layout
            Vector2Int PixelOf(Vector2 p) => Vector2Int.FloorToInt(p);
            if (!isTemporary && PixelOf(mousePos) == PixelOf(point))
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

        public override void UpdateAssetTrackers()
        {
            liveReloadSystem.Update();
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

        internal void UpdateWithoutRepaint()
        {
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.ViewData);
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Bindings);
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Animation);
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Styles);
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Layout);
        }

        public override void DirtyStyleSheets()
        {
            m_VisualTreeUpdater.DirtyStyleSheets();
        }

        internal override IVisualTreeUpdater GetEditorUpdater(VisualTreeEditorUpdatePhase phase)
        {
            return m_VisualTreeUpdater.visualTreeEditorUpdater.GetUpdater(phase);
        }

        internal override void UpdateInlineStylesRecursively(VisualElement rootElement = null)
        {
            var root = rootElement ?? m_RootContainer;

            if (root.visualTreeAssetSource?.inlineSheet?.rules.Length == 0)
                return;

            foreach (var element in root.Children())
            {
                if (element.visualTreeAssetSource?.inlineSheet != null && element.inlineStyleAccess?.inlineRule.rule != null)
                    element.UpdateInlineRule(element.visualTreeAssetSource.inlineSheet, element.inlineStyleAccess.inlineRule.rule);

                UpdateInlineStylesRecursively(element);
            }
        }

        static internal event Action<Panel> beforeAnyRepaint;

        public override void Repaint(Event e)
        {
            m_RepaintVersion = version;

            // in an in-game context, pixelsPerPoint is user driven
            if (contextType == ContextType.Editor)
                pixelsPerPoint = GUIUtility.pixelsPerPoint;

            repaintData.repaintEvent = e;

            using (m_MarkerBeforeUpdate.Auto())
            {
                InvokeBeforeUpdate();
            }

            beforeAnyRepaint?.Invoke(this);

            using (m_MarkerUpdate.Auto())
            {
                UpdateForRepaint();
            }

            panelDebug?.Refresh();
        }

        // Updaters can request an panel invalidation when some callbacks aren't coming from UIElements internally
        internal override void RequestUpdateAfterExternalEvent(IVisualTreeUpdater updater)
        {
            if (updater == null)
                throw new ArgumentNullException(nameof(updater));
            ++m_Version;
        }

        internal override void OnVersionChanged(VisualElement ve, VersionChangeType versionChangeType)
        {
            ++m_Version;
            m_VisualTreeUpdater.OnVersionChanged(ve, versionChangeType);

            if ((versionChangeType & VersionChangeType.Hierarchy) == VersionChangeType.Hierarchy)
                ++m_HierarchyVersion;
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

    internal abstract class BaseRuntimePanel : Panel
    {
        private GameObject m_SelectableGameObject;
        public GameObject selectableGameObject
        {
            get => m_SelectableGameObject;
            set
            {
                if (m_SelectableGameObject != value)
                {
                    AssignPanelToComponents(null);
                    m_SelectableGameObject = value;
                    AssignPanelToComponents(this);
                }
            }
        }

        // We count instances of Runtime panels to be able to insert panels that have the same sort order in a deterministic
        // way throughout the same session (i.e. instances created before will be placed before in the visual tree).
        private static int s_CurrentRuntimePanelCounter = 0;
        internal readonly int m_RuntimePanelCreationIndex;

        private float m_SortingPriority = 0;
        public float sortingPriority
        {
            get => m_SortingPriority;

            set
            {
                if (!Mathf.Approximately(m_SortingPriority, value))
                {
                    m_SortingPriority = value;
                    if (contextType == ContextType.Player)
                    {
                        UIElementsRuntimeUtility.SetPanelOrderingDirty();
                    }
                }
            }
        }

        internal int resolvedSortingIndex = 0;

        public event Action destroyed;

        protected BaseRuntimePanel(ScriptableObject ownerObject, EventDispatcher dispatcher = null)
            : base(ownerObject, ContextType.Player, dispatcher)
        {
            m_RuntimePanelCreationIndex = s_CurrentRuntimePanelCounter++;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                destroyed?.Invoke();
            }

            base.Dispose(disposing);
        }

        private Shader m_StandardWorldSpaceShader;

        internal override Shader standardWorldSpaceShader
        {
            get { return m_StandardWorldSpaceShader; }
            set
            {
                if (m_StandardWorldSpaceShader != value)
                {
                    m_StandardWorldSpaceShader = value;
                    InvokeStandardWorldSpaceShaderChanged();
                }
            }
        }

        bool m_DrawToCameras;

        internal bool drawToCameras
        {
            get { return m_DrawToCameras; }
            set
            {
                if (m_DrawToCameras != value)
                {
                    m_DrawToCameras = value;
                    (GetUpdater(VisualTreeUpdatePhase.Repaint) as UIRRepaintUpdater)?.DestroyRenderChain();
                }
            }
        }

        internal RenderTexture targetTexture = null; // Render panel to a texture
        internal Matrix4x4 panelToWorld = Matrix4x4.identity;

        internal int targetDisplay { get; set;}

        internal int screenRenderingWidth => targetDisplay > 0 && targetDisplay < Display.displays.Length
        ? Display.displays[targetDisplay].renderingWidth : Screen.width;
        internal int screenRenderingHeight => targetDisplay > 0 && targetDisplay < Display.displays.Length
        ? Display.displays[targetDisplay].renderingHeight : Screen.height;

        public override void Repaint(Event e)
        {
            // if the targetTexture is not set, we simply render on whatever target is currently set
            if (targetTexture == null)
            {
                // This is called after the camera(s) are done rendering, so the
                // last camera viewport will leak here.  The "overlay" panels should
                // render on the whole framebuffer, so we force a fullscreen viewport here.
                var rt = RenderTexture.active;
                int width = rt != null ? rt.width : screenRenderingWidth;
                int height = rt != null ? rt.height : screenRenderingHeight;
                GL.Viewport(new Rect(0, 0, width, height));
                base.Repaint(e);
                return;
            }

            var oldCam = Camera.current;
            var oldRT = RenderTexture.active;

            Camera.SetupCurrent(null);
            RenderTexture.active = targetTexture;

            GL.Viewport(new Rect(0, 0, targetTexture.width, targetTexture.height));
            base.Repaint(e);

            Camera.SetupCurrent(oldCam);
            RenderTexture.active = oldRT;
        }

        internal static readonly Func<Vector2, Vector2> DefaultScreenToPanelSpace = (p) => (p);
        private Func<Vector2, Vector2> m_ScreenToPanelSpace = DefaultScreenToPanelSpace;

        public Func<Vector2, Vector2> screenToPanelSpace
        {
            get => m_ScreenToPanelSpace;
            set => m_ScreenToPanelSpace = value ?? DefaultScreenToPanelSpace;
        }

        internal Vector2 ScreenToPanel(Vector2 screen)
        {
            return screenToPanelSpace(screen) / scale;
        }

        internal bool ScreenToPanel(Vector2 screenPosition, Vector2 screenDelta,
            out Vector2 panelPosition, out Vector2 panelDelta, bool allowOutside = false)
        {
            panelPosition = ScreenToPanel(screenPosition);

            Vector2 panelPrevPosition;

            // We don't allow pointer events outside of a panel to be considered
            // unless it is capturing the mouse (see SendPositionBasedEvent).
            if (!allowOutside)
            {
                var panelRect = visualTree.layout;
                if (!panelRect.Contains(panelPosition))
                {
                    panelDelta = screenDelta;
                    return false;
                }

                panelPrevPosition = ScreenToPanel(screenPosition - screenDelta);
                if (!panelRect.Contains(panelPrevPosition))
                {
                    panelDelta = screenDelta;
                    return true;
                }
            }
            else
            {
                panelPrevPosition = ScreenToPanel(screenPosition - screenDelta);
            }

            panelDelta = panelPosition - panelPrevPosition;
            return true;
        }

        private void AssignPanelToComponents(BaseRuntimePanel panel)
        {
            if (selectableGameObject == null)
                return;

            var components = ObjectListPool<IRuntimePanelComponent>.Get();
            try // Going through potential user code
            {
                selectableGameObject.GetComponents(components);
                foreach (var component in components)
                    component.panel = panel;
            }
            finally
            {
                ObjectListPool<IRuntimePanelComponent>.Release(components);
            }
        }

        internal void PointerLeavesPanel(int pointerId, Vector2 position)
        {
            ClearCachedElementUnderPointer(pointerId, null);
            CommitElementUnderPointers();
            PointerDeviceState.SavePointerPosition(pointerId, position, null, contextType);
        }

        internal void PointerEntersPanel(int pointerId, Vector2 position)
        {
            PointerDeviceState.SavePointerPosition(pointerId, position, this, contextType);
        }
    }

    internal interface IRuntimePanelComponent
    {
        IPanel panel { get; set; }
    }

    internal class PanelRootElement : VisualElement
    {
        public PanelRootElement()
        {
            name = VisualElementUtils.GetUniqueName("unity-panel-container");
            viewDataKey = "PanelContainer";
            pickingMode = PickingMode.Ignore;

            // Setting eventCallbackCategories will ensure panel.visualTree is always a valid next parent.
            // This allows deeper elements to have an active tracking version based on their panel
            // if they would not otherwise have a next parent with callbacks.
            eventCallbackCategories = 1 << (int) EventCategory.Reserved;
        }
    }

    internal class EditorPanelRootElement : PanelRootElement
    {
        public EditorPanelRootElement()
        {
            pickingMode = PickingMode.Position;

            RegisterCallback<ExecuteCommandEvent>(OnEventCompletedAtAnyTarget);
            RegisterCallback<ValidateCommandEvent>(OnEventCompletedAtAnyTarget);
            RegisterCallback<DragExitedEvent>(OnEventCompletedAtAnyTarget);
            RegisterCallback<MouseEnterWindowEvent>(OnEventCompletedAtAnyTarget);
            RegisterCallback<MouseLeaveWindowEvent>(OnEventCompletedAtAnyTarget);
            RegisterCallback<IMGUIEvent>(OnEventCompletedAtAnyTarget);
        }

        private void OnEventCompletedAtAnyTarget(EventBase evt)
        {
            // Send to other IMGUIContainers. If target is an IMGUIContainer, already processed, it will be skipped.
            EventDispatchUtilities.PropagateToRemainingIMGUIContainers(evt, this);

            // Stop propagateToIMGUI to prevent ExecuteDefaultActionAtTarget below from also kicking in.
            evt.propagateToIMGUI = false;
        }

        [EventInterest(EventInterestOptionsInternal.TriggeredByOS)]
        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            // Send to all IMGUIContainers. Current target is an EditorPanelRootElement so it doesn't specially need to be skipped.
            if (!evt.isPropagationStopped && evt.propagateToIMGUI)
                EventDispatchUtilities.PropagateToRemainingIMGUIContainers(evt, this);
        }
    }
}
