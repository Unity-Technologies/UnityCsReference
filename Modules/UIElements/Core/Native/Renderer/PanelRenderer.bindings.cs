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
    [AddComponentMenu("UI Toolkit/Panel Renderer (UI Toolkit)")]
    [NativeHeader("Modules/UIElements/Core/Native/Renderer/PanelRenderer.h")]
    public sealed class PanelRenderer : Renderer, IPanelComponent
    {
        #region Fields

        internal static Func<IPanelComponent, ILiveReloadAssetTracker<VisualTreeAsset>> CreateLiveReloadVisualTreeAssetTracker;
        ILiveReloadAssetTracker<VisualTreeAsset> m_LiveReloadVisualTreeAssetTracker;

        /// <summary>
        /// Specifies the PanelSettings instance to connect this PanelRenderer component to.
        /// </summary>
        /// <remarks>
        /// The Panel Settings asset defines the panel that renders UI in the game view. Refer to <see cref="PanelSettings"/> for more information.
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
                        RemoveVisualTreeAssetTracker();

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
        public delegate void UIReloadCallback(PanelRenderer panelRenderer, VisualElement rootElement);

        private UIReloadCallback m_OnUIReloadCallback;

        /// <summary>
        /// Registers a callback to be invoked when the UI is reloaded.
        /// </summary>
        /// <param name="callback">The callback to register.</param>
        public void RegisterUIReloadCallback(UIReloadCallback callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            m_OnUIReloadCallback += callback;

            if (rootVisualElement != null)
                callback(this, rootVisualElement);
        }

        /// <summary>
        /// Unregisters a previously registered UI reload callback.
        /// </summary>
        /// <param name="callback">The callback to unregister.</param>
        public void UnregisterUIReloadCallback(UIReloadCallback callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            m_OnUIReloadCallback -= callback;
        }

        extern private ScriptableObject nativePanelSettings { get; set; }
        extern private ScriptableObject nativeVisualTreeAsset { get; set; }

        extern PanelRenderer nativeParentUI { get; set; }

        extern internal bool hasHierarchyChanged { get; set; }
        extern internal PanelRendererHierarchyOrder hierarchyOrder { get; }

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

        /// <summary>
        /// The order in which this PanelRenderer appears in the hierarchy relative to other PanelRenderer either
        /// attached to the same PanelSettings, or with the same PanelRenderer parent.
        /// </summary>
        public new int sortingOrder
        {
            get => ((Renderer)this).sortingOrder;
            set
            {
                Renderer r = (Renderer)this;
                if (r.sortingOrder != value)
                {
                    r.sortingOrder = value;
                    requiresReinsertion = true;
                }
            }
        }

        float IPanelComponent.sortingOrder => sortingOrder;

        WorldSpaceSizeMode IPanelComponent.worldSpaceSizeMode
        {
            get { return (WorldSpaceSizeMode)nativeWorldSpaceSizeMode; }
            set
            {
                nativeWorldSpaceSizeMode = (int)value;
                SetupWorldSpaceSize();
            }
        }

        Vector2 IPanelComponent.worldSpaceSize
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

        private BoxCollider m_WorldSpaceCollider;

        #endregion

        #region Native Calls

        internal volatile List<CommandList>[] commandLists;
        internal volatile bool skipRendering;

        internal extern void AddDrawCallData(int safeFrameIndex, Material mat, uint textureSlotCount, uint forceRenderType, IntPtr serializedCommandsPtr, int commandCount, CommandListState state);
        internal extern void ResetDrawCallData(int safeFrameIndex);
        internal extern void ResetAllDrawCallData();

        // This will dirty the hierarchy flag of affected PanelRenderers if a hierarchy change occurred
        extern static internal bool CheckHierarchyChanges();
        #endregion

        #region Lifecycle

        internal static bool shouldCheckForRequiredReinsertions = false;

        [RequiredByNativeCode]
        void OnPanelRendererAwake()
        {
            // Add ourselves to the UIElementsRuntimeUtility list
            UIElementsRuntimeUtility.MarkPanelRendererDirty(this);

            SetAllDirty();

            // When instantiated from a prefab, the panel settings may already be set.
            // If so, let's initialize the root visual element.
            if (rootVisualElement == null && panelSettings != null)
                InitRootVisualElement(true);
        }

        [RequiredByNativeCode]
        void OnPanelRendererCleanup()
        {
            OnPanelRendererDeactivated();
            if (m_ReferenceProvider != null)
            {
                m_ReferenceProvider.UnloadReferences();
                m_ReferenceProvider.Dispose();
            }
        }

        [RequiredByNativeCode]
        void OnPanelRendererDeactivated()
        {
            if (rootVisualElement != null)
            {
                RemoveVisualTreeAssetTracker();
                RemoveFromHierarchy();
            }

            UIElementsRuntimeUtility.RemovePanelRenderer(this);
        }

        [RequiredByNativeCode]
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

        internal void RefreshAssets()
        {
            if (panelSettings != previousPanelSettings)
                previousPanelSettings?.DetachPanelComponent(this);

            if (!enabled)
                return;

            bool visualTreeAssetChanged = visualTreeAsset != previousVisualTreeAsset;
            previousVisualTreeAsset = visualTreeAsset;
            previousPanelSettings = panelSettings;
            isAssetDirty = false;

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

                SetupFromHierarchy();

                SetupVisualTreeAssetTracker();

                requiresReinsertion = true;

                m_OnUIReloadCallback?.Invoke(this, rootVisualElement);
            }
            else
            {
                SetupFromHierarchy();
            }

            firstChildInsertIndex = rootVisualElement.hierarchy.childCount;

            SetupRootClassList();
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
            if (!enabled)
                return;

            // If we do have a parent, it will add us.
            if (parentUI != null)
                parentUI.AddChildAndInsertContentToVisualTree(this);
            else
                panelSettings?.AttachAndInsertPanelComponentToVisualTree(this);
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
            if (!enabled)
                return;

            if (hasHierarchyChanged || requiresReinsertion)
            {
                SetupFromHierarchy();
                SetupRootClassList();
                AddRootVisualElementToTree();
                hasHierarchyChanged = false;
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

        float pixelsPerUnit => containerPanel?.pixelsPerUnit ?? 1.0f;

        Vector2 PivotOffset()
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
            skipRendering = (parentUI != null) || (ppu < Mathf.Epsilon);

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

            if (rootVisualElement.isWorldSpaceRootUIDocument != isWorldSpaceRootUIDocument)
            {
                rootVisualElement.isWorldSpaceRootUIDocument = isWorldSpaceRootUIDocument;
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
                m_RootVisualElement.EnableInClassList(k_RootStyleClassName, parentUI == null);

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
        internal extern UIAnimationBinder GetAnimationBinder();


        [RequiredByNativeCode]
        internal void ConnectToAnimationBinder()
        {
            UIAnimationBinder binder = GetAnimationBinder();

            if(binder != null)
            {
                RegisterUIReloadCallback((pr, root) => binder.RegisterRootDocument(root, false)); 
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

    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct CommandListState
    {
        public IntPtr vertexDeclPtr;
        public IntPtr drawRangesPtr;
        public IntPtr constantPropsPtr;
        public IntPtr stencilStatePtr;
    }

    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct PanelRendererHierarchyOrder
    {
        public int ancestorCount;
        public IntPtr ancestorSiblingIndices;

        public static bool operator<(PanelRendererHierarchyOrder first, PanelRendererHierarchyOrder second)
        {
            int minCount = Math.Min(first.ancestorCount, second.ancestorCount);
            unsafe
            {
                int* firstIndices = (int*)first.ancestorSiblingIndices;
                int* secondIndices = (int*)second.ancestorSiblingIndices;
                for (int i = 0; i < minCount; i++)
                {
                    if (firstIndices[i] < secondIndices[i])
                        return true;
                    else if (firstIndices[i] > secondIndices[i])
                        return false;
                }
            }
            return first.ancestorCount < second.ancestorCount;
        }

        public static bool operator>(PanelRendererHierarchyOrder first, PanelRendererHierarchyOrder second)
        {
            return second < first;
        }
    }
}
