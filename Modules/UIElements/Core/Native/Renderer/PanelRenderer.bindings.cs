// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.UIElements.UIR;
using static UnityEngine.UIElements.UIDocument;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Defines a component that connects <see cref="VisualElement"/> to <see cref="GameObject"/>.
    /// </summary>
    /// <example>
    /// The following example uses the Panel Renderer component to display a runtime UI in a scene:
    /// <code source="../../../../../Documentation/ManualDocs/com.unity.documentation-examples/UIToolkit/get-started-runtime-ui/SimpleRuntimeUI.cs"/>
    /// </example>
    [HelpURL("ui-systems/panel-renderer-component")]
    [AddComponentMenu("UI Toolkit/Panel Renderer (UI Toolkit)")]
    [NativeHeader("Modules/UIElements/Core/Native/Renderer/PanelRenderer.h")]
    [ExtensionOfNativeClass]
    public sealed class PanelRenderer : Renderer, IPanelComponent
    {
        #region Fields

        internal static Func<IPanelComponent, ILiveReloadAssetTracker<VisualTreeAsset>> CreateLiveReloadVisualTreeAssetTracker;
        ILiveReloadAssetTracker<VisualTreeAsset> m_LiveReloadVisualTreeAssetTracker;

        /// <summary>
        /// Specifies the PanelSettings instance to connect this PanelRenderer component to.
        /// </summary>
        /// <remarks>
        /// The Panel Settings asset defines the panel that renders UI in the Game view. Refer to <see cref="PanelSettings"/> for more information.
        ///
        /// If this PanelRenderer has a parent PanelRenderer, it uses the parent's PanelSettings automatically.
        /// </remarks>
        public PanelSettings panelSettings
        {
            get
            {
                if (parentUI == null)
                    return nativePanelSettings as PanelSettings;

                var p = parentUI;
                while (p.parentUI != null)
                    p = p.parentUI;

                return p.panelSettings;
            }
            set
            {
                if (nativePanelSettings != value)
                {
                    if (nativePanelSettings != null)
                    {
                        RemoveVisualTreeAssetTracker();
                        RemoveFromHierarchy();
                    }

                    nativePanelSettings = value;
                    isAssetDirty = true;

                    // Setup the root visual element, but don't add it to the tree yet.
                    // It will be added when RefreshAssets is called.
                    InitRootVisualElement(true);
                }
            }
        }

        /// <summary>
        /// The <see cref="VisualTreeAsset"/> automatically loaded into the root visual element.
        /// </summary>
        public VisualTreeAsset visualTreeAsset
        {
            get => nativeVisualTreeAsset as VisualTreeAsset;
            set
            {
                if (nativeVisualTreeAsset != value)
                {
                    nativeVisualTreeAsset = value;
                    isAssetDirty = true;
                }
            }
        }

        private PanelRendererRootElement m_RootVisualElement;

        internal PanelRendererRootElement rootVisualElement
        {
            get => m_RootVisualElement;
            set => m_RootVisualElement = value;
        }

        VisualElement IPanelComponent.GetRootVisualElement() => m_RootVisualElement;
        IEventHandler IPanelComponent.GetRoot() => (this as IPanelComponent).GetRootVisualElement();


        VisualElementReferenceProvider m_ReferenceProvider;

        /// <summary>
        /// The VisualElementReferenceProvider used to resolve <see cref="VisualElementReference"/> instances.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal VisualElementReferenceProvider referenceProvider
        {
            get
            {
                m_ReferenceProvider ??= new VisualElementReferenceProvider();
                return m_ReferenceProvider;
            }

            set => m_ReferenceProvider = value;
        }

        /// <summary>
        /// Callback type for UI reload events.
        /// </summary>
        /// <param name="panelRenderer">The PanelRenderer in which the UI was reloaded.</param>
        /// <param name="rootElement">The root visual element that was reloaded in the PanelRenderer.</param>
        [Obsolete("This callback type is deprecated. Use VersionedUIReloadCallback instead, which provides a version number so the callback can skip redundant work when the UI has not actually changed.")]
        public delegate void UIReloadCallback(PanelRenderer panelRenderer, VisualElement rootElement);

        /// <summary>
        /// Callback type for UI reload events.
        /// </summary>
        /// <param name="panelRenderer">The PanelRenderer in which the UI was reloaded.</param>
        /// <param name="rootElement">The root visual element that was reloaded in the PanelRenderer.</param>
        /// <param name="version">The version of the UI that was reloaded.</param>
        public delegate void VersionedUIReloadCallback(PanelRenderer panelRenderer, VisualElement rootElement, int version);

#pragma warning disable CS0618
        private UIReloadCallback m_OnUIReloadCallback;
#pragma warning restore CS0618

        private VersionedUIReloadCallback m_OnVersionedUIReloadCallback;

        // Start UI version at 1 so that users can rely on version 0 being "not initialized".
        private int m_UIVersion = 1;

        // Set when the root visual element is (re)initialized; cleared once the reload
        // callbacks fire. Prevents firing the callback against a root that has already
        // been processed, for example, AddRootVisualElementToTree() running without a reload.
        private bool m_UIReloadCallbackPending;

        /// <summary>
        /// Registers a callback to be invoked when the UI is reloaded.
        /// </summary>
        /// <param name="callback">The callback to register.</param>
        /// <remarks>
        /// This overload is deprecated. Use <see cref="RegisterUIReloadCallback(VersionedUIReloadCallback)"/>
        /// instead, which provides a version number so the callback can skip redundant work when the UI has not actually changed.
        /// </remarks>
        [Obsolete("Use RegisterUIReloadCallback(VersionedUIReloadCallback) instead, which provides a version number so the callback can skip redundant work when the UI has not actually changed.")]
        public void RegisterUIReloadCallback(UIReloadCallback callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            m_OnUIReloadCallback += callback;

            if (rootVisualElement != null && rootVisualElement.panel != null)
                callback(this, rootVisualElement);
        }

        /// <summary>
        /// Registers a versioned callback to be invoked when the UI is reloaded.
        /// </summary>
        /// <param name="callback">The callback to register.</param>
        /// <remarks>
        /// This callback will be invoked immediately if the root VisualElement is already initialized, and then
        /// every time the UI is reloaded afterwards. Use the version parameter to check if the root VisualElement was
        /// actually reloaded since the last time the callback was invoked.
        /// </remarks>
        /// <example>
        /// The following example registers a versioned callback to initialize the UI. If the component is repeatedly disabled an
        /// re-enabled, the callback version will stay the same. So, the callback can safely skip the initialization logic after the first time.
        /// <code source="../../../../../Documentation/ManualDocs/com.unity.documentation-examples/UIToolkit/get-started-runtime-ui/VersionedPanelRendererCallback.cs"/>
        /// </example>
        public void RegisterUIReloadCallback(VersionedUIReloadCallback callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            m_OnVersionedUIReloadCallback += callback;

            if (rootVisualElement != null && rootVisualElement.panel != null)
                callback(this, rootVisualElement, m_UIVersion);
        }

        /// <summary>
        /// Unregisters a previously registered UI reload callback.
        /// </summary>
        /// <param name="callback">The callback to unregister.</param>
        [Obsolete("Use UnregisterUIReloadCallback(VersionedUIReloadCallback) instead.")]
        public void UnregisterUIReloadCallback(UIReloadCallback callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            m_OnUIReloadCallback -= callback;
        }

        /// <summary>
        /// Unregisters a previously registered versioned UI reload callback.
        /// </summary>
        /// <param name="callback">The callback to unregister.</param>
        public void UnregisterUIReloadCallback(VersionedUIReloadCallback callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            m_OnVersionedUIReloadCallback -= callback;
        }

        void InvokeUIReloadCallbacks()
        {
            if (!m_UIReloadCallbackPending)
                return;

            if (rootVisualElement == null || rootVisualElement.panel == null)
                return;

            m_UIReloadCallbackPending = false;

            m_OnUIReloadCallback?.Invoke(this, rootVisualElement);
            m_OnVersionedUIReloadCallback?.Invoke(this, rootVisualElement, m_UIVersion);
        }

        extern private ScriptableObject nativePanelSettings { get; set; }
        extern private ScriptableObject nativeVisualTreeAsset { get; set; }

        extern PanelRenderer nativeParentUI { get; set; }

        extern private int nativeWorldSpaceSizeMode { get; set; }

        extern private float nativeWorldSpaceSizeWidth { get; set; }
        extern private float nativeWorldSpaceSizeHeight { get; set; }

        extern private int nativePivotReferenceSize { get; set; }

        extern private int nativePivot { get; set; }

        extern private int nativePosition { get; set; }

        private bool m_RequiresReinsertion = false;
        internal bool requiresReinsertion
        {
            get => m_RequiresReinsertion;
            set
            {
                if (m_RequiresReinsertion != value)
                {
                    m_RequiresReinsertion = value;
                    if (value)
                    {
                        shouldCheckForRequiredReinsertions = true;
                        UIElementsRuntimeUtility.MarkPanelRendererDirty(this);
                    }
                }
            }
        }

        private bool m_IsAssetDirty = false;
        internal bool isAssetDirty
        {
            get => m_IsAssetDirty;
            set
            {
                if (m_IsAssetDirty != value)
                {
                    m_IsAssetDirty = value;
                    if (value)
                        UIElementsRuntimeUtility.MarkPanelRendererDirty(this);
                }
            }
        }

        // For dirty tracking
        internal PanelSettings previousPanelSettings { get; set; }
        internal VisualTreeAsset previousVisualTreeAsset { get; set; }
        internal bool previousEnabled { get; set; }
        internal int previousSortingOrder { get; set; }

        float IPanelComponent.sortingOrder => ((Renderer)this).sortingOrder;

        void IPanelComponent.SetComponentEnabled(bool enabled) => this.enabled = enabled;
        bool IPanelComponent.GetComponentEnabled() => this.enabled;

        Vector3 IPanelComponent.GetPanelPosition(IEventHandler pickedElement, Ray worldRay)
        {
            return PanelComponentUtils.GetPanelPosition(gameObject, pickedElement, worldRay);
        }

        private int m_SoftPointerCaptures = 0;
        int IPanelComponent.softPointerCaptures
        {
            get => m_SoftPointerCaptures;
            set => m_SoftPointerCaptures = value;
        }

        VisualElementFocusRing IPanelComponent.focusRing { get; set; }

        /// <summary>
        /// Defines how the size of the root element is calculated for world space.
        /// </summary>
        public WorldSpaceSizeMode worldSpaceSizeMode
        {
            get { return (WorldSpaceSizeMode)nativeWorldSpaceSizeMode; }
            set
            {
                nativeWorldSpaceSizeMode = (int)value;
                SetupWorldSpaceSize();
            }
        }

        /// <summary>
        /// When the <see cref="worldSpaceSizeMode"/> is set to <see cref="WorldSpaceSizeMode.Fixed"/>, this property
        /// determines the size of the PanelRenderer in world space.
        /// </summary>
        public Vector2 worldSpaceSize
        {
            get
            {
                return new Vector2(nativeWorldSpaceSizeWidth, nativeWorldSpaceSizeHeight);
            }
            set
            {
                nativeWorldSpaceSizeWidth = value.x;
                nativeWorldSpaceSizeHeight = value.y;
                SetupWorldSpaceSize();
            }
        }

        /// <summary>
        /// The position (relative or absolute) of the root visual element. Relative only applies for nested PanelRenderers.
        /// </summary>
        public Position position
        {
            get { return (Position)nativePosition; }
            set
            {
                nativePosition = (int)value;
                SetupPosition();
            }
        }

        /// <summary>
        /// Defines how the size of the container is calculated for pivot positioning.
        /// </summary>
        public PivotReferenceSize pivotReferenceSize
        {
            get { return (PivotReferenceSize)nativePivotReferenceSize; }
            set { nativePivotReferenceSize = (int)value; }
        }

        /// <summary>
        /// Defines the pivot point for positioning and transformation, such as rotation and scaling, of the UI Document in world space. The default pivot is the center.
        /// </summary>
        public Pivot pivot
        {
            get { return (Pivot)nativePivot; }
            set { nativePivot = (int)value; }
        }

        /// <summary>
        /// If the GameObject that this PanelRenderer component is attached to has a parent GameObject, and
        /// that parent GameObject also has a PanelRenderer component attached to it, this value is set to
        /// the parent GameObject's PanelRenderer component automatically.
        /// </summary>
        /// <remarks>
        /// If a PanelRenderer has a parent, you cannot add it directly to a panel (PanelSettings). Unity adds it to
        /// the parent's root visual element instead.
        ///
        /// The advantage of placing PanelRenderer GameObjects under other PanelRenderer GameObjects is that you can
        /// have many PanelRenderers all drawing in the same panel (rootVisualElement) and therefore able to batch
        /// together. A typical example is rendering health bars on top of characters, which would be more expensive to
        /// render in their separate panels (and batches) compared to combining them to a single panel, one batch.
        /// </remarks>
        public PanelRenderer parentUI
        {
            get { return nativeParentUI; }
            private set { nativeParentUI = value; }
        }

        IPanelComponent IPanelComponent.parentUI => parentUI;

        private int m_FirstChildInsertIndex;
        internal int firstChildInsertIndex
        {
            get => m_FirstChildInsertIndex;
            set => m_FirstChildInsertIndex = value;
        }

        // We count instances of PanelRenderer to be able to insert PanelRenderers that have the same sort order in a
        // deterministic way (i.e. instances created before will be placed before in the visual tree).
        private static int s_CurrentPanelRendererCounter = 0;
        private int m_PanelRendererCreationIndex = 0;

        int IPanelComponent.creationIndex => m_PanelRendererCreationIndex;

        private BoxCollider m_WorldSpaceCollider;

        #endregion

        #region Native Calls

        internal volatile List<CommandList>[] commandLists;

        internal extern void AddDrawCallData(int safeFrameIndex, Material mat, uint textureSlotCount, uint forceRenderType, IntPtr serializedCommandsPtr, int commandCount, CommandListState state);
        internal extern void ResetDrawCallData(int safeFrameIndex);
        internal extern void ResetAllDrawCallData();

        #endregion

        #region Lifecycle

        internal static bool shouldCheckForRequiredReinsertions = false;

        [RequiredByNativeCode(Optional = true)]
        [RequiredMember]
        void OnPanelRendererAwake()
        {
            if (m_PanelRendererCreationIndex == 0)
                m_PanelRendererCreationIndex = ++s_CurrentPanelRendererCounter;

            // Add ourselves to the UIElementsRuntimeUtility list
            UIElementsRuntimeUtility.MarkPanelRendererDirty(this);

            SetAllDirty();

            // Initialize previous state tracking
            previousSortingOrder = sortingOrder;

            // When instantiated from a prefab, the panel settings may already be set.
            // If so, let's initialize the root visual element.
            if (rootVisualElement == null && panelSettings != null)
                InitRootVisualElement(true);
        }

        [RequiredByNativeCode(Optional = true)]
        [RequiredMember]
        void OnPanelRendererCleanup()
        {
            OnPanelRendererDeactivated();
            if (m_ReferenceProvider != null)
            {
                m_ReferenceProvider.UnloadReferences();
                m_ReferenceProvider.Dispose();
            }
            if (rootVisualElement != null)
            {
                rootVisualElement.Clear(VisualElementClearOptions.RecursiveReleaseResources);
                rootVisualElement.ReleaseResources();
                rootVisualElement = null;
            }
        }

        [RequiredByNativeCode(Optional = true)]
        [RequiredMember]
        void OnPanelRendererDeactivated()
        {
            PointerDeviceState.RemovePanelComponentData(this);

            if (rootVisualElement != null)
            {
                RemoveVisualTreeAssetTracker();
                RemoveFromHierarchy();
            }

            UIElementsRuntimeUtility.RemovePanelRenderer(this);
        }

        [RequiredByNativeCode(Optional = true)]
        [RequiredMember]
        void OnPanelRendererCheckConsistency()
        {
            // Called from ApplyModifiedProperties, so our assets are probably dirty
            SetAllDirty();
        }

        void SetAllDirty()
        {
            isAssetDirty = true;
            requiresReinsertion = true;
        }

        bool IsActiveAndEnabled() => enabled && gameObject.activeInHierarchy;

        internal void RefreshAssets()
        {
            if (panelSettings != previousPanelSettings)
                previousPanelSettings?.DetachPanelComponent(this);

            if (!IsActiveAndEnabled())
                return;

            bool visualTreeAssetChanged = visualTreeAsset != previousVisualTreeAsset;
            InitRootVisualElement(visualTreeAssetChanged);
        }

        void InitRootVisualElement(bool visualTreeAssetChanged = false)
        {
            if (rootVisualElement != null && visualTreeAssetChanged)
                panelSettings?.DetachPanelComponent(this);

            if (visualTreeAssetChanged || rootVisualElement == null)
            {
                if (rootVisualElement != null)
                {
                    RemoveVisualTreeAssetTracker();
                    referenceProvider.UnloadReferences();
                    rootVisualElement.Clear(VisualElementClearOptions.RecursiveReleaseResources);
                    rootVisualElement.ReleaseResources();
                }

                if (visualTreeAsset == null)
                {
                    // Empty container if no UXML is set or if there was an error with cloning the set UXML.
                    rootVisualElement = new PanelRendererRootElement(this, null);
                    rootVisualElement.name = $"{typeof(PanelRendererRootElement).Name}-{gameObject.name}-container";
                }
                else
                {
                    rootVisualElement = new PanelRendererRootElement(this, visualTreeAsset);
                    rootVisualElement.name = $"{typeof(PanelRendererRootElement).Name}-{visualTreeAsset.name}-{gameObject.name}-container";
                    visualTreeAsset.CloneTree(rootVisualElement, out var referenceTable);
                    referenceProvider.ResolveReferences(referenceTable);
                }

                (this as IPanelComponent).focusRing = rootVisualElement != null ? new(rootVisualElement) : null;

                SetupFromHierarchy();

                SetupVisualTreeAssetTracker();

                requiresReinsertion = true;

                // Increase the version to let users know they should re-initialize their UIs.
                ++m_UIVersion;
                m_UIReloadCallbackPending = true;
            }
            else
            {
                SetupFromHierarchy();
            }

            firstChildInsertIndex = rootVisualElement.hierarchy.childCount;

            SetupRootClassList();

            previousVisualTreeAsset = visualTreeAsset;
            previousPanelSettings = panelSettings;
            isAssetDirty = false;
        }

        internal void SetupFromHierarchy()
        {
            if (parentUI != null)
                parentUI.RemoveChild(this);

            parentUI = FindParentPanelRenderer();
        }

        private PanelRenderer FindParentPanelRenderer()
        {
            Transform t = transform;
            Transform parentTransform = t.parent;
            if (parentTransform != null)
            {
                var potentialParents = parentTransform.GetComponentsInParent<PanelRenderer>(true);
                if (potentialParents != null && potentialParents.Length > 0)
                    return potentialParents[0];
            }

            return null;
        }

        internal void AddRootVisualElementToTree()
        {
            if (!IsActiveAndEnabled())
                return;

            // If we do have a parent, it will add us.
            if (parentUI != null)
                parentUI.AddChildAndInsertContentToVisualTree(this);
            else
                panelSettings?.AttachAndInsertPanelComponentToVisualTree(this);

            InvokeUIReloadCallbacks();
        }

        void AddChildAndInsertContentToVisualTree(PanelRenderer child)
        {
            if (m_ChildrenContent == null)
            {
                m_ChildrenContent = new PanelComponentList();
            }
            else
            {
                // Before adding, we need to make sure it's nowhere else in the list (and in the hierarchy) as if we're
                // re-adding, the position probably changed.
                m_ChildrenContent.RemoveFromListAndFromVisualTree(child);
            }

            bool ignoreContentContainer = (child.position == Position.Absolute);
            m_ChildrenContent.AddToListAndToVisualTree(child, m_RootVisualElement, ignoreContentContainer, firstChildInsertIndex);
        }

        internal void RemoveFromHierarchy()
        {
            if (parentUI != null)
                parentUI.RemoveChild(this);
            else
                panelSettings?.DetachPanelComponent(this);
        }

        internal void ReactToHierarchyChanges()
        {
            if (!IsActiveAndEnabled())
                return;

            if (requiresReinsertion)
            {
                SetupFromHierarchy();
                SetupRootClassList();
                AddRootVisualElementToTree();
                requiresReinsertion = false;
            }
        }

        // If this PanelRenderer has PanelRenderer children (1st level only, 2nd level would be the child's
        // children), they're added to this list instead of to the PanelSetting's list.
        private PanelComponentList m_ChildrenContent = null;

        void RemoveChild(PanelRenderer child)
        {
            m_ChildrenContent?.RemoveFromListAndFromVisualTree(child);
        }

        internal void SetupPosition()
        {
            if (m_RootVisualElement == null || parentUI == null)
                return; // The position property is only relevant for nested PanelRenderers.

            if (PanelComponentUtils.IsTransformControlledByGameObject(this))
                m_RootVisualElement.style.position = Position.Absolute;
            else
                m_RootVisualElement.style.position = position;

            // We need to re-add ourselves in the list as the position influences
            // if we're part of the content-container or not.
            requiresReinsertion = true;
        }

        void SetupVisualTreeAssetTracker()
        {
            if (rootVisualElement == null || panelSettings == null)
                return;

            m_LiveReloadVisualTreeAssetTracker ??= CreateLiveReloadVisualTreeAssetTracker?.Invoke(this);

            panelSettings.panel.liveReloadSystem.RegisterVisualTreeAssetTracker(m_LiveReloadVisualTreeAssetTracker, m_RootVisualElement);
        }

        void RemoveVisualTreeAssetTracker()
        {
            if (rootVisualElement == null || panelSettings?.isInitialized != true)
                return;

            panelSettings.panel.liveReloadSystem.UnregisterVisualTreeAssetTracker(rootVisualElement);
        }

        void IPanelComponent.HandleLiveReload()
        {
            if (rootVisualElement == null)
                return;

            InitRootVisualElement(true);
            AddRootVisualElementToTree();
        }

        void IPanelComponent.OnLiveReloadOptionChanged() => ((IPanelComponent)this).HandleLiveReload();

        #endregion

        #region Update
        internal RuntimePanel containerPanel => (RuntimePanel)rootVisualElement?.elementPanel;

        IRuntimePanel IPanelComponent.GetContainerPanel() => containerPanel;

        GameObject IPanelComponent.gameObject => this.gameObject;

        private bool isWorldSpace => (panelSettings != null && panelSettings.renderMode == PanelRenderMode.WorldSpace);

        bool m_RootHasWorldTransform;

        void IPanelComponent.PerformValidation(bool forced)
        {
            OnPanelRendererCheckConsistency();
        }

        void IPanelComponent.PerformUpdate()
        {
            if (rootVisualElement == null)
                return;

            if (isWorldSpace)
            {
                if (PanelComponentUtils.IsTransformControlledByGameObject(this))
                    SetTransform();
                else
                    ClearTransform();

                UpdateLocalBounds();

                if (panelSettings.colliderUpdateMode != ColliderUpdateMode.Keep
                    && Application.isPlaying
                    // UUM-108898: don't add components that get saved while in edit prefab mode and
                    // play mode at the same time, otherwise it won't get removed correctly when leaving play mode.
                    && IsEditingPrefab?.Invoke() != true
    )
                {
                    UpdateWorldSpaceCollider(panelSettings.colliderUpdateMode);
                }
            }
            else
            {
                if (m_RootHasWorldTransform)
                    ClearTransform();
            }

            UpdateIsWorldSpaceRootFlag();
        }

        void SetTransform()
        {
            float ppu = pixelsPerUnit;
            Matrix4x4 matrix;
            if (parentUI == null)
                PanelComponentUtils.ComputeParentTransform(PivotOffset(), ppu, out matrix);
            else
                PanelComponentUtils.ComputeNestedTransform(transform, parentUI.transform, PivotOffset(), parentUI.PivotOffset(), ppu, out matrix);

            rootVisualElement.style.transformOrigin = new TransformOrigin(Vector3.zero);
            rootVisualElement.style.translate = new Translate(matrix.GetPosition());
            rootVisualElement.style.rotate = new Rotate(matrix.rotation);
            rootVisualElement.style.scale = new Scale(matrix.lossyScale);
            m_RootHasWorldTransform = true;
        }

        void ClearTransform()
        {
            rootVisualElement.style.transformOrigin = StyleKeyword.Null;
            rootVisualElement.style.translate = StyleKeyword.Null;
            rootVisualElement.style.rotate = StyleKeyword.Null;
            rootVisualElement.style.scale = StyleKeyword.Null;
            m_RootHasWorldTransform = false;
        }

        internal float pixelsPerUnit => containerPanel?.pixelsPerUnit ?? 1.0f;

        internal Vector2 PivotOffset()
        {
            var pc = (IPanelComponent)this;

            var pivotPercent = PanelComponentUtils.GetPivotAsPercent(pc.pivot);
            var localBounds = PanelComponentUtils.LocalBoundsFromPivotSource(rootVisualElement, pc.pivotReferenceSize);

            return (-(Vector2)localBounds.min) + new Vector2(-localBounds.size.x * pivotPercent.x, -localBounds.size.y * pivotPercent.y);
        }

        void UpdateLocalBounds()
        {
            rootVisualElement.panelRenderer = this;

            // Don't render embedded documents which will be rendered as part of their parents
            // Don't render documents with invalid PPU
            float ppu = pixelsPerUnit;

            BaseRuntimePanel rtp = (BaseRuntimePanel)rootVisualElement.panel;
            if (rtp == null)
                return;

            var bb = PanelComponentUtils.SanitizeRendererBounds(rootVisualElement.localBounds3D);
            var toGameObject = PanelComponentUtils.TransformToGameObjectMatrix(PivotOffset(), ppu);
            VisualElement.TransformAlignedBounds(ref toGameObject, ref bb);

            localBounds = bb;
        }

        internal void UpdateWorldSpaceCollider(ColliderUpdateMode mode)
        {
            if (parentUI != null)
            {
                // The parent UIDoc is responsible to create a global collider that encompasses nested UIDocuments
                return;
            }

            if (containerPanel == null)
            {
                RemoveWorldSpaceCollider();
                return;
            }

            Bounds bb;
            if (mode == ColliderUpdateMode.MatchBoundingBox)
            {
                bb = WorldSpaceInput.GetPicking3DWorldBounds(rootVisualElement);
            }
            else // ColliderUpdateMode.MatchDocumentRect
            {
                Rect wb = rootVisualElement.worldBound;
                bb = new Bounds(wb.center, wb.size);
            }

            if (!PanelComponentUtils.IsValidBounds(bb))
            {
                RemoveWorldSpaceCollider();
                return;
            }

            if (m_WorldSpaceCollider == null)
            {
                m_WorldSpaceCollider = gameObject.AddComponent<BoxCollider>();
                m_WorldSpaceCollider.isTrigger = panelSettings.colliderIsTrigger;
            }

            // Setting BoxCollider.center or BoxCollider.size triggers some work even if the value doesn't change.
            if (bb.center != m_WorldSpaceCollider.center || bb.size != m_WorldSpaceCollider.size)
            {
                m_WorldSpaceCollider.center = bb.center;
                m_WorldSpaceCollider.size = bb.size;
            }
        }

        internal void RemoveWorldSpaceCollider()
        {
            UIRUtility.Destroy(m_WorldSpaceCollider);
            m_WorldSpaceCollider = null;
        }

        void UpdateIsWorldSpaceRootFlag()
        {
            bool isWorldSpacePanel = !(panelSettings?.panel?.isFlat ?? true);
            bool isWorldSpaceRootUIDocument;

            if (isWorldSpacePanel)
            {
                // World-space panel should cut the render chain unless they are nested in another PanelRenderer,
                // in which case they are rendered through their parent.
                isWorldSpaceRootUIDocument = (parentUI == null);
            }
            else
            {
                // For overlay panels, we should never cut the render chain.
                isWorldSpaceRootUIDocument = false;
            }

            if (rootVisualElement.isWorldSpaceRootPanelComponent != isWorldSpaceRootUIDocument)
            {
                rootVisualElement.isWorldSpaceRootPanelComponent = isWorldSpaceRootUIDocument;
                rootVisualElement.MarkDirtyRepaint(); // Necessary to insert a CutRenderChain command
            }
        }

        internal void SetupRootClassList()
        {
            if (m_RootVisualElement == null)
                return;

            if (!isWorldSpace)
            {
                // If we're not a child of any other PanelRenderer stretch to take the full screen.
                m_RootVisualElement.EnableInClassList(rootStyleClassNameUnique, parentUI == null);

                // Reset inline styles thay may have been set if the PanelSetting was
                // previously set to world-space rendering.
                m_RootVisualElement.style.position = StyleKeyword.Null;
                m_RootVisualElement.style.width = StyleKeyword.Null;
                m_RootVisualElement.style.height = StyleKeyword.Null;
            }
            else
            {
                SetupWorldSpaceSize();
            }

            SetupPosition();
        }

        private void SetupWorldSpaceSize()
        {
            if (m_RootVisualElement == null)
                return;

            var pc = (IPanelComponent)this;

            if (!PanelComponentUtils.IsTransformControlledByGameObject(pc))
            {
                // Nested use-case. We shouldn't provide a fixed size.
                m_RootVisualElement.style.width = StyleKeyword.Null;
                m_RootVisualElement.style.height = StyleKeyword.Null;
                return;
            }

            if (pc.worldSpaceSizeMode == WorldSpaceSizeMode.Fixed)
            {
                var size = pc.worldSpaceSize;

                m_RootVisualElement.style.position = Position.Absolute;
                m_RootVisualElement.style.width = size.x;
                m_RootVisualElement.style.height = size.y;
            }
            else
            {
                m_RootVisualElement.style.position = Position.Absolute;
                m_RootVisualElement.style.width = StyleKeyword.Null;
                m_RootVisualElement.style.height = StyleKeyword.Null;
            }
        }
        #endregion

        #region Animation
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal extern UIAnimationBinder GetAnimationBinder();

        // Creates the binder if it does not exist yet. The native animation pipeline calls
        // this when AnimationUtility queries animatable bindings on a PanelRenderer; the
        // authoring module mirrors that behaviour when probing recordability so that
        // newly-created panels answer the "can I record?" question deterministically
        // instead of depending on lazy binder creation.
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal extern UIAnimationBinder GetOrCreateAnimationBinder();

        [NativeMethod("RegisterPanelRendererAnimationBinding")]
        internal static extern void RegisterPanelRendererAnimationBinding();

        [RequiredByNativeCode(Optional = true)]
        [RequiredMember]
        internal void ConnectToAnimationBinder()
        {
            UIAnimationBinder binder = GetAnimationBinder();

            if(binder != null)
            {
                RegisterUIReloadCallback((pr, root, version) => binder.RegisterRootDocument(root, false));
            }
        }

        #endregion
    }

    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    [Icon("UIToolkit/Icons/TemplateContainer.png")]
    internal class PanelRendererRootElement : TemplateContainer, IPanelComponentRootElement
    {
        internal PanelRenderer panelRenderer { get; set; }
        IPanelComponent IPanelComponentRootElement.panelComponent => panelRenderer;

        public PanelRendererRootElement(PanelRenderer panelRenderer, VisualTreeAsset sourceAsset)
            : base(sourceAsset?.name, sourceAsset)
        {
            this.panelRenderer = panelRenderer;
            pickingMode = PickingMode.Ignore;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct CommandListState
    {
        public IntPtr vertexDeclPtr;
        public IntPtr drawRangesPtr;
        public IntPtr constantPropsPtr;
        public IntPtr stencilStatePtr;
    }
}
