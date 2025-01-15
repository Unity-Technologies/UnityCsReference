// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine.Bindings;
using UnityEngine.UIElements.Layout;

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

    /// <summary>
    /// Value used to signify some changes in the VisualElement
    /// </summary>
    // NOTE: if a new VersionChangeType is added, ComputedStyle.CompareChanges may need to be reworked!
    // NOTE: keep in sync with VersionChangeType.h
    [Flags]
    public enum VersionChangeType
    {
        /// <summary>
        /// Some data was bound
        /// </summary>
        Bindings = 1 << 0,
        /// <summary>
        /// persistent data ready
        /// </summary>
        ViewData = 1 << 1,
        /// <summary>
        /// changes to hierarchy
        /// </summary>
        Hierarchy = 1 << 2,
        /// <summary>
        /// changes to properties that may have an impact on layout
        /// </summary>
        Layout = 1 << 3,
        /// <summary>
        /// changes to StyleSheet, USS class
        /// </summary>
        StyleSheet = 1 << 4,
        /// <summary>
        /// changes to styles
        /// </summary>
        Styles = 1 << 5,
        /// <summary>
        /// changes to the overflow
        /// </summary>
        Overflow = 1 << 6,
        /// <summary>
        /// changes to the border radis
        /// </summary>
        BorderRadius = 1 << 7,
        /// <summary>
        /// changes to the border width
        /// </summary>
        BorderWidth = 1 << 8,
        /// <summary>
        /// changes that may impact the world transform (e.g. laid out position, local transform)
        /// </summary>
        Transform = 1 << 9,
        /// <summary>
        /// changes to the size of the element after layout has been performed, without taking the local transform into account
        /// </summary>
        Size = 1 << 10,
        /// <summary>
        /// The visuals of the element have changed
        /// </summary>
        Repaint = 1 << 11,
        /// <summary>
        /// The opacity of the element have changed
        /// </summary>
        Opacity = 1 << 12,
        /// <summary>
        /// Some color of the element has changed (background-color, border-color, etc.)
        /// </summary>
        Color = 1 << 13,
        /// <summary>
        /// Some render hints were changed
        /// </summary>
        RenderHints = 1 << 14,
        /// <summary>
        /// The 'transition-property' style of the element has changed (impacts cancelling of ongoing style transitions)
        /// </summary>
        TransitionProperty = 1 << 15,
        /// <summary>
        /// The combined registered callbacks' EventCategory values has changed
        /// </summary>
        EventCallbackCategories = 1 << 16,
        /// <summary>
        /// The DisableRendering flag has changed
        /// </summary>
        DisableRendering = 1 << 17,
        /// <summary>
        /// Add or remove a binding was requested
        /// </summary>
        BindingRegistration = 1 << 18,
        /// <summary>
        /// The data source of the element was changed
        /// </summary>
        DataSource = 1 << 19,
        /// <summary>
        /// Some property changed that potentially invalidates cached Picking results
        /// </summary>
        Picking = 1 << 20,
    }

    /// <summary>
    /// Offers a set of options that describe the intended usage patterns of a <see cref="VisualElement"/>.
    /// These options serve as guidance for optimizations. You can set multiple
    /// usage hints on an element. For example, if both position and color change, you can set both
    /// <see cref="DynamicTransform"/> and <see cref="DynamicColor"/>.\\
    /// \\
    /// __Note__: Set the usage hints at edit time or before you add the <see cref="VisualElement"/> to a panel.
    /// In the case of transition, when it starts, the system might automatically add missing relevant
    /// usage hints to avoid regenerating geometry in every frame. However, this causes a one-frame performance penalty
    /// because the rendering data for the VisualElement and its descendants is regenerated.
    /// </summary>
    [Flags]
    public enum UsageHints
    {
        /// <summary>
        /// No particular hints applicable.
        /// </summary>
        None = 0,
        /// <summary>
        /// Optimizes rendering of a <see cref="VisualElement"/> for frequent position and
        /// transformation changes.
        /// </summary>
        /// <remarks>
        /// This option uses the GPU instead of CPU to perform the VisualElement's vertex transformation.\\
        /// \\
        /// Use this option on a VisualElement that changes any of the following style properties:
        ///
        ///- `left`
        ///- `top`
        ///- `right`
        ///- `bottom`
        ///- `position`
        ///- `transform-origin`
        ///- `rotate`
        ///- `scale`
        ///- `translate`
        /// </remarks>
        DynamicTransform = 1 << 0,
        /// <summary>
        /// Optimizes rendering of a <see cref="VisualElement"/> that changes its transformation and position
        /// frequently, and has many descendants that have their hints set to <see cref="DynamicTransform"/>.
        /// </summary>
        /// <remarks>
        /// This option is similar to <see cref="DynamicTransform"/> in that it allows GPU transformation of the vertices of
        /// the descendants. However it breaks the batch, and adds an extra draw call. The purpose of this hint is to
        /// avoid having to update the stored matrix of many elements that have <see cref="DynamicTransform"/> set
        /// when a common ancestor changes its transformation or position. In other words, this is an optimisation for
        /// <see cref="DynamicTransform"/>.\\
        /// \\
        /// An example use case is that in Shader Graph, you can
        /// set individual nodes with <see cref="DynamicTransform"/>, and set the ancestor
        /// panner/zoomer with `GroupTransform`, so that when you pan/zoom, you avoid the
        /// need to update hundreds of dynamic transforms.\\
        /// \\
        /// __Note__: Don't use both <see cref="DynamicTransform"/> and GroupTransform at the same time on a single VisualElement.
        /// </remarks>
        GroupTransform = 1 << 1,
        /// <summary>
        /// Optimizes rendering of a <see cref="VisualElement"/> that has multiple descendants with nested masks.
        /// </summary>
        /// <remarks>
        /// This option reduces stencil state changes and capitalizes on consecutive
        /// mask push/pop operations for efficiency.\\
        /// \\
        /// Apply this option to a VisualElement with multiple nested masks among its descendants. For example, a child element
        /// has the `overflow: hidden;` style with rounded corners or SVG background.\\
        /// \\
        /// The following illustration shows the number of batches in a single-level masking, a nested masking, and a nested masking with MaskContainer.
        /// The yellow color indicates the masking elements. The orange color indicates the masking element with MaskContainer applied.
        /// The numbers indicate the number of batches.\\
        /// \\
        /// {img MaskContainer.png}\\
        /// A: Single-level masking (1 batch)\\
        /// B: Nested masking (5 batches)\\
        /// C: Nested masking with MaskContainer (2 batches)\\
        /// \\
        /// __Note__: Don't use GroupTransform in scenarios with many subtrees, where each
        /// subtree uses two or more levels of masking. This helps minimize consecutive
        /// push/push or pop/pop operations.
        /// </remarks>
        MaskContainer = 1 << 2,
        /// <summary>
        /// Optimizes rendering of a <see cref="VisualElement"/> for frequent color changes, such as a built-in style color being animated.
        /// </summary>
        /// <remarks>
        /// This option fetches color from a GPU buffer to prevent re-tessellating geometry or CPU updates when colors change.
        ///
        /// Apply this option on a VisualElement that changes any of the following style properties:
        ///
        ///- `background-color`
        ///- `border-color`
        ///- `color`
        ///- `border-bottom-color`
        ///- `border-left-color`
        ///- `border-right-color`
        ///- `border-top-color`
        ///- `text-color`
        ///- `unity-background-image-tint-color`
        ///
        /// </remarks>
        DynamicColor = 1 << 3,
    }

    //keep in sync with RenderHints in C++ (Modules/UIElements/RenderHints.h)
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
        DirtyDynamicColor = DynamicColor << DirtyOffset,
        DirtyAll = DirtyGroupTransform | DirtyBoneTransform | DirtyClipWithScissors | DirtyMaskContainer | DirtyDynamicColor,
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

    internal delegate void HierarchyEvent(VisualElement ve, HierarchyChangeType changeType, IReadOnlyList<VisualElement> additionalContext = null);

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

        /// <summary>
        /// Checks whether any element within the panel has had any changes to its state since the panel was last rendered.
        /// </summary>
        /// <remarks>
        /// This method doesn't verify if any bindings have been updated, if there are any pending transitions, or if any referenced assets (such as renderTexture, panelSettings, or textSettings) have been modified.
        /// </remarks>
        bool isDirty { get; }

        /// <summary>
        /// Gives the current scaled pixels per point value of the panel.
        /// </summary>
        /// <remarks>
        /// Return the resulting scaling that considers all effective inputs like the screen scaling factor from the operating system and the customizable scaling factor.
        /// The screen scaling factor could be overriden in the editor settings by the user and is not available in the player.
        /// The customizable scaling factor is set in the panel settings for runtime UI and always set to 1 in the editor.
        /// </remarks>
        float scaledPixelsPerPoint { get; }


    }

    /// <summary>
    /// Implement this to receive callbacks for visual element changes.
    /// This interface is exclusively available in development builds and the Editor, as it serves as a debug feature that complements the profiling of an application.
    /// </summary>
    /// <remarks>
    /// Related To <see cref="PanelSettings.SetPanelChangeReceiver"/>
    /// </remarks>
    public interface IDebugPanelChangeReceiver
    {
        /// <summary>
        /// Receives notifications for every change that occurs on the panel's visual elements.
        /// This method is exclusively available in development builds and the Editor, as it serves as a debug feature that complements the profiling of an application.
        /// </summary>
        /// <remarks>
        /// The number of times the callback is called, the value returned, and the order in which they are returned are subject to change between each minor release of Unity, as this is considered internal.
        /// Related to <see cref="PanelSettings.SetPanelChangeReceiver"/>
        /// </remarks>
        /// <param name="element"> The element that changed.</param>
        /// <param name="changeType">A string that contains the comma-separated value of what has changed.</param>
        public void OnVisualElementChange(VisualElement element, VersionChangeType changeType);
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    abstract class BaseVisualElementPanel : IPanel, IGroupBox
    {
    	// TODO: Make sure we do not use new native layout before we fix android 32bit (arm v7) failing test.
        // VisualPanel m_VisualPanel;
        // internal VisualPanel visualPanel => m_VisualPanel;

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
            // m_VisualPanel = VisualManager.SharedManager.CreatePanel();
            // m_VisualPanel.SetOwner(this);

            layoutConfig = LayoutManager.SharedManager.CreateConfig();

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
            // m_VisualPanel.Destroy();
            // m_VisualPanel = default;
            LayoutManager.SharedManager.DestroyConfig(ref layoutConfig);
            disposed = true;
        }

        public abstract void Repaint(Event e);
        public abstract void ValidateFocus();
        public abstract void ValidateLayout();
        public abstract void UpdateAnimations();
        public abstract void UpdateBindings();
        public abstract void UpdateDataBinding();
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
                    layoutConfig.PointScaleFactor = scaledPixelsPerPoint;

                    // if the surface DPI changes we need to invalidate styles
                    visualTree.IncrementVersion(VersionChangeType.StyleSheet);
                }
            }
        }


        internal LayoutConfig layoutConfig;

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
                    layoutConfig.PointScaleFactor = scaledPixelsPerPoint;

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

        internal PanelClearSettings clearSettings { get; set; } = new PanelClearSettings { clearDepthStencil = true, clearColor = true, color = Color.clear };

        internal IPanelRenderer panelRenderer;

        internal bool duringLayoutPhase { get; set; }

        public bool isDirty
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

        // Need virtual for tests
        internal virtual DataBindingManager dataBindingManager { get; set; }

        //IPanel
        public abstract VisualElement visualTree { get; }
        public abstract EventDispatcher dispatcher { get; set; }

        internal void SendEvent(EventBase e, DispatchMode dispatchMode = DispatchMode.Queued)
        {
            Debug.Assert(dispatcher != null, "dispatcher != null");
            dispatcher?.Dispatch(e, this, dispatchMode);
        }

        internal abstract IScheduler scheduler { get; }

        internal abstract IStylePropertyAnimationSystem styleAnimationSystem
        {
            get;
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            set;
        }
        public abstract ContextType contextType { get; }
        public abstract VisualElement Pick(Vector2 point);
        public abstract VisualElement PickAll(Vector2 point, List<VisualElement> picked);

        internal bool disposed { get; private set; }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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

            if (triggerEvent is IPointerEventInternal pe && pe.compatibilityMouseEvent != null)
            {
                m_TopElementUnderPointers.SetElementUnderPointer(element, PointerId.mousePointerId,
                    (EventBase) pe.compatibilityMouseEvent);
            }

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

        internal event Action isFlatChanged;
        bool m_IsFlat = true;

        // Used only for testing. Can be disabled for setting a manual scale
        internal bool UpdateScalingFromEditorWindow;

        public bool isFlat
        {
            get => m_IsFlat;
            set
            {
                if (m_IsFlat == value)
                    return;

                m_IsFlat = value;
                SetSpecializedHierarchyFlagsUpdater();
                isFlatChanged?.Invoke();
            }
        }

        internal void SetSpecializedHierarchyFlagsUpdater()
        {
            var updater = GetUpdater(VisualTreeUpdatePhase.TransformClip);
            var isWorldSpaceUpdater = updater is VisualTreeWorldSpaceHierarchyFlagsUpdater;

            if (isFlat)
            {
                if (isWorldSpaceUpdater)
                    SetUpdater(new VisualTreeHierarchyFlagsUpdater(), VisualTreeUpdatePhase.TransformClip);
            }
            else
            {
                if (!isWorldSpaceUpdater)
                    SetUpdater(new VisualTreeWorldSpaceHierarchyFlagsUpdater(), VisualTreeUpdatePhase.TransformClip);
            }
        }

        internal event Action atlasChanged;
        protected void InvokeAtlasChanged() { atlasChanged?.Invoke(); }
        public abstract AtlasBase atlas { get; set; }

        internal event HierarchyEvent hierarchyChanged;

        internal void InvokeHierarchyChanged(VisualElement ve, HierarchyChangeType changeType, IReadOnlyList<VisualElement> additionalContext = null)
        {
            if (hierarchyChanged != null) hierarchyChanged(ve, changeType, additionalContext);
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
                    //UUM-58503: In some code paths the mousePosition is converted to a Vector2Int, which causes undefined behavior
                    //           when the x,y coords are float.MinValue()
                    m_TopElementUnderPointers.SetElementUnderPointer(null, pointerId, new Vector2((float)int.MinValue, (float)int.MinValue));
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

        public virtual void Render() => panelRenderer.Render();
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
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class Panel : BaseVisualElementPanel
    {
        internal const int k_DefaultPixelsPerUnit = 100;

        private VisualElement m_RootContainer;
        private VisualTreeUpdater m_VisualTreeUpdater;
        private IStylePropertyAnimationSystem m_StylePropertyAnimationSystem;
        private string m_PanelName;
        private uint m_Version = 0;
        private uint m_RepaintVersion = 0;
        private uint m_HierarchyVersion = 0;

        ProfilerMarker m_MarkerBeforeUpdate;
        ProfilerMarker m_MarkerUpdate;
        ProfilerMarker m_MarkerRender;
        ProfilerMarker m_MarkerLayout;
        ProfilerMarker m_MarkerBindings;
        ProfilerMarker m_MarkerDataBinding;
        ProfilerMarker m_MarkerAnimations;
        ProfilerMarker m_MarkerPanelChangeReceiver;
        static ProfilerMarker s_MarkerPickAll = new ProfilerMarker("Panel.PickAll");

        public sealed override VisualElement visualTree
        {
            get { return m_RootContainer; }
        }

        // For UI Test Framework.
        internal class UIFrameState
        {
            internal virtual long[] updatersFrameCount { get; }

            internal virtual long schedulerFrameCount { get; }

            internal virtual bool isPanelDirty { get; }

            internal virtual ContextType panelContextType { get; }

            internal virtual long[] editorUpdatersFrameCount { get; }
            internal static int[] updaterSubsetForEditor = new int[]
            {
                (int)VisualTreeUpdatePhase.Bindings,
                (int)VisualTreeUpdatePhase.DataBinding,
                (int)VisualTreeUpdatePhase.Animation
            };

            internal UIFrameState() { }

            internal UIFrameState(Panel panel)
            {
                isPanelDirty = panel.isDirty;
                panelContextType = panel.contextType;

                schedulerFrameCount = panel.scheduler.FrameCount;
                updatersFrameCount = panel.visualTreeUpdater.GetUpdatersFrameCount();
                editorUpdatersFrameCount = panel.visualTreeUpdater.visualTreeEditorUpdater.GetUpdatersFrameCount();
            }

            public static bool operator >(UIFrameState leftOperand, UIFrameState rightOperand)
            {
                return leftOperand.HasFullUIFrameOccurredSince(rightOperand);
            }

            public static bool operator <(UIFrameState leftOperand, UIFrameState rightOperand)
            {
                return rightOperand.HasFullUIFrameOccurredSince(leftOperand);
            }

            // Returns true if this UIFrameState is greater than the provided reference UIFrameState, false otherwise.
            private bool HasFullUIFrameOccurredSince(UIFrameState reference)
            {
                if (this.panelContextType != reference.panelContextType)
                {
                    throw new NotSupportedException("Comparison is only valid for frames with the same ContextType.");
                }

                // Compare the scheduler frame.
                if (this.schedulerFrameCount <= reference.schedulerFrameCount)
                {
                    return false;
                }

                // Compare the VisualTreeUpdater frames.
                if (!this.isPanelDirty && this.panelContextType == ContextType.Editor)
                {
                    // If the context is Editor and the panel is currently not dirty, only check a subset of the updater frames.
                    for (int i = 0; i < updaterSubsetForEditor.Length; i++)
                    {
                        if (this.updatersFrameCount[updaterSubsetForEditor[i]] <= reference.updatersFrameCount[updaterSubsetForEditor[i]])
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < this.updatersFrameCount.Length; i++)
                    {
                        if (this.updatersFrameCount[i] <= reference.updatersFrameCount[i])
                        {
                            return false;
                        }
                    }
                }

                if (this.panelContextType == ContextType.Editor)
                {
                    // Compare the VisualTreeEditorUpdater frames.
                    for (int i = 0; i < this.editorUpdatersFrameCount.Length; i++)
                    {
                        if (this.editorUpdatersFrameCount[i] <= reference.editorUpdatersFrameCount[i])
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        // For UI Test Framework.
        internal UIFrameState GetFrameState()
        {
            return new UIFrameState(this);
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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

        public override ContextType contextType { get; }

        public override SavePersistentViewData saveViewData { get; set; }

        public override GetViewDataDictionary getViewDataDictionary { get; set; }

        public sealed override FocusController focusController { get; set; }

        public override EventInterests IMGUIEventInterests { get; set; }

        // UUM-60233: Some panels are very expensive to reset and assets do not impact the UI, so in these cases, it is
        // preferable to disable the reset.
        bool m_ResetPanelRenderingOnAssetChange = true;
        public bool resetPanelRenderingOnAssetChange
        {
            get => m_ResetPanelRenderingOnAssetChange;
            set
            {
                if (m_ResetPanelRenderingOnAssetChange != value)
                {
                    m_ResetPanelRenderingOnAssetChange = value;
                    if (m_ResetPanelRenderingOnAssetChange)
                      ResetRendering();
                }
            }
        }

        public void ResetRendering()
        {
            panelRenderer?.Reset();
            atlas?.Reset();
        }

        internal static LoadResourceFunction loadResourceFunc { private get; set; }

        internal static InitEditorUpdaterFunction initEditorUpdaterFunc { private get; set; }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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
        private IDebugPanelChangeReceiver m_PanelChangeReceiver;

        /// <summary>
        /// Sets a custom <see cref="IPanelChangeReceiver"> in the panelChangeReceiver setter to receive every change event.
        /// This method is exclusively available in development builds and the Editor, as it serves as a debug feature that complements the profiling of an application.
        /// </summary>
        /// <remarks>
        /// Note that the values returned might change over time when the underlying architecture is modified.
        ///
        /// Since this is called for every change made to any visual element within the panel, the overhead is not negligible.
        /// </remarks>

        public IDebugPanelChangeReceiver panelChangeReceiver {
            get => m_PanelChangeReceiver;
            set
            {
                m_PanelChangeReceiver = value;
                if (value != null)
                    Debug.LogWarning($"IPanelChangeReceiver suscribed to panel '{name}' and may affect performance. The callback should be used only in debugging scenario and won't work outside development builds");
            }
        }

        void CreateMarkers()
        {
            string appendName = string.IsNullOrEmpty(m_PanelName) ? "" : $".{m_PanelName}";
            m_MarkerBeforeUpdate = new ProfilerMarker($"Panel.BeforeUpdate{appendName}");
            m_MarkerUpdate = new ProfilerMarker($"Panel.Update{appendName}");
            m_MarkerRender = new ProfilerMarker($"Panel.Render{appendName}");
            m_MarkerLayout = new ProfilerMarker($"Panel.Layout{appendName}");
            m_MarkerBindings = new ProfilerMarker($"Panel.Bindings{appendName}");
            m_MarkerDataBinding = new ProfilerMarker($"Panel.DataBinding{appendName}");
            m_MarkerAnimations = new ProfilerMarker($"Panel.Animations{appendName}");
            m_MarkerPanelChangeReceiver = new ProfilerMarker($"Panel.PanelChangeReceiver{appendName}");
        }

        internal static TimeMsFunction TimeSinceStartup { private get; set; }

        public override int IMGUIContainersCount { get; set; }

        public override IMGUIContainer rootIMGUIContainer { get; set; }

        internal override uint version => m_Version;
        internal override uint repaintVersion => m_RepaintVersion;
        internal override uint hierarchyVersion => m_HierarchyVersion;

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
            dataBindingManager = new DataBindingManager(this);

            m_VisualTreeUpdater = new VisualTreeUpdater(this);
            SetSpecializedHierarchyFlagsUpdater();

            var initFunc = initEditorUpdater ?? initEditorUpdaterFunc;
            initFunc.Invoke(this, m_VisualTreeUpdater);
            m_RootContainer = contextType == ContextType.Editor ? new EditorPanelRootElement() : new PanelRootElement();

            // Required!
            visualTree.SetPanel(this);
            focusController = new FocusController(new VisualElementFocusRing(visualTree));
            styleAnimationSystem = new StylePropertyAnimationSystem();

            CreateMarkers();

            InvokeHierarchyChanged(visualTree, HierarchyChangeType.AddedToParent);
            atlas = new DynamicAtlas();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                atlas = null;
                visualTree.Clear();
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
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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

        public override void UpdateDataBinding()
        {
            m_MarkerDataBinding.Begin();
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.DataBinding);
            m_MarkerDataBinding.End();
        }

        public override void ApplyStyles()
        {
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Styles);
        }

        public override void UpdateAssetTrackers()
        {
            m_VisualTreeUpdater.visualTreeEditorUpdater.UpdateVisualTreePhase(VisualTreeEditorUpdatePhase.AssetChange);
        }


        void UpdateForRepaint()
        {
            //Here we don't want to update animation and bindings which are ticked by the scheduler
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.ViewData);
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.DataBinding);
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Styles);
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Layout);
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.TransformClip);
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Repaint);
        }

        internal void UpdateWithoutRepaint()
        {
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.ViewData);
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.Bindings);
            m_VisualTreeUpdater.UpdateVisualTreePhase(VisualTreeUpdatePhase.DataBinding);
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
            (panelDebug?.debuggerOverlayPanel as Panel)?.Repaint(e);
        }

        public override void Render()
        {
            m_MarkerRender.Begin();
            base.Render();
            m_MarkerRender.End();

            (panelDebug?.debuggerOverlayPanel as Panel)?.Render();
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

            if (panelChangeReceiver != null)
                using (m_MarkerPanelChangeReceiver.Auto())
                {
                    panelChangeReceiver.OnVisualElementChange(ve, versionChangeType);
                }

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

        internal virtual Color HyperlinkColor => Color.blue;
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

        internal event Action drawsInCamerasChanged;
        void InvokeDrawsInCamerasChanged() { drawsInCamerasChanged?.Invoke(); }
        bool m_DrawsInCameras;
        internal bool drawsInCameras
        {
            get { return m_DrawsInCameras; }
            set
            {
                if (m_DrawsInCameras != value)
                {
                    m_DrawsInCameras = value;
                    InvokeDrawsInCamerasChanged();
                }
            }
        }

        float m_PixelsPerUnit = k_DefaultPixelsPerUnit;
        internal float pixelsPerUnit {
            get { return m_PixelsPerUnit; }
            set { m_PixelsPerUnit = value; }
        }

        internal RenderTexture targetTexture = null; // Render panel to a texture
        internal int worldSpaceLayer = 0;

        internal int targetDisplay { get; set;}

        internal int screenRenderingWidth => getScreenRenderingWidth(targetDisplay);
        internal int screenRenderingHeight => getScreenRenderingHeight(targetDisplay);

        internal virtual void Update()
        {
            scheduler.UpdateScheduledEvents();
            // This call is already on UIElementsUtility.UpdateSchedulers() but it's also necessary here for Runtime UI
            UpdateAssetTrackers();
            ValidateFocus();
            ValidateLayout();
            UpdateAnimations();
            UpdateBindings();
        }

        // Expose common static method for getting the display/window resolution for calculation in the PanelSetting.
        // Does not consider the gameView, so useless in the editor unless called directly after the render of a camera
        internal static int getScreenRenderingHeight(int display)
        {
            return display >= 0 && display < Display.displays.Length ? Display.displays[display].renderingHeight : Screen.height;
        }

        internal static int getScreenRenderingWidth(int display)
        {
            return display >= 0 && display < Display.displays.Length ? Display.displays[display].renderingWidth : Screen.width;
        }

        public override void Render()
        {
            if (drawsInCameras)
            {
                Debug.LogError("Panel.Render() must not be called on a panel that draws in cameras.");
                return;
            }

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
                base.Render();
                return;
            }

            var oldCam = Camera.current;
            var oldRT = RenderTexture.active;

            Camera.SetupCurrent(null);
            RenderTexture.active = targetTexture;

            GL.Viewport(new Rect(0, 0, targetTexture.width, targetTexture.height));
            base.Render();

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

            using (Pool.ListPool<IRuntimePanelComponent>.Get(out var components))
            {
                selectableGameObject.GetComponents(components);
                foreach (var component in components)
                    component.panel = panel;
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

            // Make sure panel.visualTree is always the last element of the nextParentWithEventInterests chain of all
            // its children. This allows deeper elements to have an active tracking version based on their panel
            // if they would not otherwise have a next parent with interests.
            SetAsNextParentWithEventInterests();
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
            if (evt.propagateToIMGUI)
            {
                // Send to other IMGUIContainers. If target is an IMGUIContainer, already processed, it will be skipped.
                EventDispatchUtilities.PropagateToRemainingIMGUIContainers(evt, this);
                evt.propagateToIMGUI = false;
            }
        }
    }
}
