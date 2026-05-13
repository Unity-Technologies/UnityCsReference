// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    [Icon("UIToolkit/Icons/TemplateContainer.png")]
    internal class UIDocumentRootElement : TemplateContainer, IPanelComponentRootElement
    {
        public readonly UIDocument document;
        internal UIRenderer uiRenderer { get; set; }

        IPanelComponent IPanelComponentRootElement.panelComponent => document;

        public UIDocumentRootElement(UIDocument document, VisualTreeAsset sourceAsset) : base(sourceAsset?.name,
            sourceAsset)
        {
            this.document = document;
        }
    }

    /// <summary>
    /// An enum describing how the world-space UIDocument will be sized.
    /// </summary>
    public enum WorldSpaceSizeMode
    {
        /// <summary>
        /// The size of the UIDocument will be determined from the layout size of the root element.
        /// </summary>
        Dynamic,

        /// <summary>
        /// The size of the UIDocument will be fixed to the values provided in <see cref="worldSpaceSize"/>.
        /// </summary>
        Fixed
    }


    /// <summary>
    /// Enum value used to specify the size used to compute the <see cref="Pivot"/> position.
    /// </summary>
    public enum PivotReferenceSize
    {
        /// <summary>The size of the full bounding-box of the UIDocument will be used to determine the pivot position.</summary>
        BoundingBox = 0,

        /// <summary>The layout size of the root element will be used to determine the pivot position.</summary>
        Layout = 1,
    }


    /// <summary>
    /// Enum value used to specify the origin point of a <see cref="UIDocument"/>.
    /// </summary>
    public enum Pivot
    {
        /// <summary>Center pivot.</summary>
        Center = 0,

        /// <summary>Top-left pivot.</summary>
        TopLeft = 1,

        /// <summary>Top-center pivot.</summary>
        TopCenter = 2,

        /// <summary>Top-right pivot.</summary>
        TopRight = 3,

        /// <summary>Left-center pivot.</summary>
        LeftCenter = 4,

        /// <summary>Right-center pivot.</summary>
        RightCenter = 5,

        /// <summary>Bottom-left pivot.</summary>
        BottomLeft = 6,

        /// <summary>Bottom-center pivot.</summary>
        BottomCenter = 7,

        /// <summary>Bottom-right pivot.</summary>
        BottomRight = 8,
    }

    /// <summary>
    /// Defines a Component that connects <see cref="VisualElement">VisualElements</see> to <see cref="GameObject">GameObjects</see>.
    /// </summary>
    /// <remarks>
    /// This makes it possible to render UI defined in UXML documents in the Game view.
    /// </remarks>
    /// <example>
    /// The following example shows how to query a UIDocument component and interact with its elements.
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/UIDocument_Example.cs"/>
    /// </example>
    [HelpURL("UIE-get-started-with-runtime-ui")]
    [AddComponentMenu(@"UI Toolkit/Legacy/UI Document (UI Toolkit)"), ExecuteAlways, DisallowMultipleComponent] // Hide in Add Component menu
    [DefaultExecutionOrder(-100)] // UIDocument's OnEnable should run before user's OnEnable
    public sealed class UIDocument : MonoBehaviour, IPanelComponent
    {
        internal static readonly UniqueStyleString rootStyleClassNameUnique = new("unity-ui-document__root");

        internal const string k_VisualElementNameSuffix = "-container";

        internal const string k_EditorElementsWarningMessage = "The VisualTreeAsset contains editor-only elements that are incompatible at runtime.\nTo fix this, remove the editor elements from the VisualTreeAsset.";

        private const int k_DefaultSortingOrder = 0;

        // We count instances of UIDocument to be able to insert UIDocuments that have the same sort order in a
        // deterministic way (i.e. instances created before will be placed before in the visual tree).
        private static int s_CurrentUIDocumentCounter = 0;
        internal readonly int m_UIDocumentCreationIndex;

        int IPanelComponent.creationIndex => m_UIDocumentCreationIndex;

        internal static Func<bool> IsEditorPlaying;
        internal static Func<bool> IsEditorPlayingOrWillChangePlaymode;
        internal static Func<bool> IsEditingPrefab;

        internal static int EnabledDocumentCount = 0;

        [SerializeField]
        private PanelSettings m_PanelSettings;

        // For Reset, we need to always keep track of what our previous PanelSettings was so we can react to being
        // removed from it (as our PanelSettings becomes null in that operation).
        private PanelSettings m_PreviousPanelSettings = null;

        /// <summary>
        /// Specifies the PanelSettings instance to connect this UIDocument component to.
        /// </summary>
        /// <remarks>
        /// The Panel Settings asset defines the panel that renders UI in the game view. See <see cref="PanelSettings"/>.
        ///
        /// If this UIDocument has a parent UIDocument, it uses the parent's PanelSettings automatically.
        /// </remarks>
        public PanelSettings panelSettings
        {
            get
            {
                return m_PanelSettings;
            }
            set
            {
                if (parentUI == null)
                {
                    if (m_PanelSettings == value)
                    {
                        m_PreviousPanelSettings = m_PanelSettings;
                        return;
                    }

                    if (m_PanelSettings != null)
                    {
                        m_PanelSettings.DetachPanelComponent(this);
                    }
                    m_PanelSettings = value;

                    if (m_PanelSettings != null)
                    {
                        SetupVisualTreeAssetTracker();
                        m_PanelSettings.AttachAndInsertPanelComponentToVisualTree(this);
                    }
                }
                else
                {
                    // Children only hold the same instance as the parent, they don't attach themselves directly.
                    Assert.AreEqual(parentUI.m_PanelSettings, value);
                    m_PanelSettings = parentUI.m_PanelSettings;
                }

                if (m_ChildrenContent != null)
                {
                    // Guarantee changes to panel settings trickles down the hierarchy.
                    foreach (var child in m_ChildrenContent.m_AttachedPanelComponents)
                    {
                        child.panelSettings = m_PanelSettings;
                    }
                }

                m_PreviousPanelSettings = m_PanelSettings;
            }
        }

        /// <summary>
        /// If the GameObject that this UIDocument component is attached to has a parent GameObject, and
        /// that parent GameObject also has a UIDocument component attached to it, this value is set to
        /// the parent GameObject's UIDocument component automatically.
        /// </summary>
        /// <remarks>
        /// If a UIDocument has a parent, you cannot add it directly to a panel (PanelSettings). Unity adds it to
        /// the parent's root visual element instead.
        ///
        /// The advantage of placing UIDocument GameObjects under other UIDocument GameObjects is that you can
        /// have many UIDocuments all drawing in the same panel (rootVisualElement) and therefore able to batch
        /// together. A typical example is rendering health bars on top of characters, which would be more expensive to
        /// render in their separate panels (and batches) compared to combining them to a single panel, one batch.
        /// </remarks>
        public UIDocument parentUI
        {
            get => m_ParentUI;
            private set => m_ParentUI = value;
        }

        IPanelComponent IPanelComponent.parentUI => m_ParentUI;

        [SerializeField]
        private UIDocument m_ParentUI;


        // If this UIDocument has UIDocument children (1st level only, 2nd level would be the child's
        // children), they're added to this list instead of to the PanelSetting's list.
        private PanelComponentList m_ChildrenContent = null;
        private List<IPanelComponent> m_ChildrenContentCopy = null;

        [SerializeField]
        private VisualTreeAsset sourceAsset;

        /// <summary>
        /// The <see cref="VisualTreeAsset"/> loaded into the root visual element automatically.
        /// </summary>
        /// <remarks>
        /// If you leave this empty, the root visual element is also empty.
        /// </remarks>
        public VisualTreeAsset visualTreeAsset
        {
            get { return sourceAsset; }
            set
            {
                sourceAsset = value;
                RecreateUI();
            }
        }

        private UIDocumentRootElement m_RootVisualElement;

        /// <summary>
        /// The root visual element where the UI hierarchy starts.
        /// </summary>
        public VisualElement rootVisualElement
        {
            get { return m_RootVisualElement; }
            private set
            {
                m_RootVisualElement = (UIDocumentRootElement)value;
                (this as IPanelComponent).focusRing = value != null ? new(value) : null;
            }
        }

        VisualElement IPanelComponent.GetRootVisualElement() => m_RootVisualElement;
        IEventHandler IPanelComponent.GetRoot() => (this as IPanelComponent).GetRootVisualElement();


        VisualElementFocusRing IPanelComponent.focusRing { get; set; }

        private int m_SoftPointerCaptures = 0;
        int IPanelComponent.softPointerCaptures
        {
            get => m_SoftPointerCaptures;
            set => m_SoftPointerCaptures = value;
        }

        private int m_FirstChildInsertIndex;

        internal int firstChildInsertIndex
        {
            get => m_FirstChildInsertIndex;
        }

        [SerializeField]
        private float m_SortingOrder = k_DefaultSortingOrder;

        /// <summary>
        /// An enum describing how the world-space UIDocument will be sized.
        /// </summary>
        [Obsolete("UIDocument.WorldSpaceSizeMode has been moved to WorldSpaceSizeMode . (UnityUpgradable) -> WorldSpaceSizeMode", true)]
        public enum WorldSpaceSizeMode
        {
            /// <summary>
            /// The size of the UIDocument will be determined from the layout size of the root element.
            /// </summary>
            Dynamic,

            /// <summary>
            /// The size of the UIDocument will be fixed to the values provided in <see cref="worldSpaceSize"/>.
            /// </summary>
            Fixed
        }

        [SerializeField]
        private Position m_Position = Position.Relative;

        /// <summary>
        /// The position (relative or absolute) of the root visual element. Relative only applies for nested UIDocuments.
        /// </summary>
        public Position position
        {
            get { return m_Position; }
            set
            {
                if (m_Position == value)
                    return;
                m_Position = value;
                SetupPosition();
            }
        }

        [SerializeField]
        private UIElements.WorldSpaceSizeMode m_WorldSpaceSizeMode = UIElements.WorldSpaceSizeMode.Fixed;

        /// <summary>
        /// Defines how the size of the root element is calculated for world space.
        /// </summary>
        public UIElements.WorldSpaceSizeMode worldSpaceSizeMode
        {
            get { return m_WorldSpaceSizeMode; }
            set
            {
                if (m_WorldSpaceSizeMode == value)
                    return;
                m_WorldSpaceSizeMode = value;
                SetupWorldSpaceSize();
            }
        }

        [SerializeField]
        private float m_WorldSpaceWidth = 1920;

        [SerializeField]
        private float m_WorldSpaceHeight = 1080;

        /// <summary>
        /// When the <see cref="worldSpaceSizeMode"/> is set to <see cref="WorldSpaceSizeMode.Fixed"/>, this property
        /// determines the size of the UIDocument in world space.
        /// </summary>
        public Vector2 worldSpaceSize
        {
            get { return new Vector2(m_WorldSpaceWidth, m_WorldSpaceHeight); }
            set
            {
                if (m_WorldSpaceWidth == value.x && m_WorldSpaceHeight == value.y)
                    return;
                m_WorldSpaceWidth = value.x;
                m_WorldSpaceHeight = value.y;
                SetupWorldSpaceSize();
            }
        }

        [SerializeField]
        private PivotReferenceSize m_PivotReferenceSize;

        GameObject IPanelComponent.gameObject => this.gameObject;

        private bool isWorldSpace => (m_PanelSettings != null && m_PanelSettings.renderMode == PanelRenderMode.WorldSpace);

        /// <summary>
        /// Defines how the size of the container is calculated for pivot positioning.
        /// </summary>
        public PivotReferenceSize pivotReferenceSize
        {
            get { return m_PivotReferenceSize; }
            set { m_PivotReferenceSize = value; }
        }

        [SerializeField]
        private Pivot m_Pivot;

        /// <summary>
        /// Defines the pivot point for positioning and transformation, such as rotation and scaling, of the UI Document in world space. The default pivot is the center.
        /// </summary>
        public Pivot pivot
        {
            get { return m_Pivot; }
            set { m_Pivot = value; }
        }

        [SerializeField, HideInInspector] private BoxCollider m_WorldSpaceCollider;

        /// <summary>
        /// The order in which this UIDocument will show up on the hierarchy in relation to other UIDocuments either
        /// attached to the same PanelSettings, or with the same UIDocument parent.
        /// </summary>
        /// <remarks>
        /// A UIDocument with a higher sorting order is displayed above one with a lower sorting order. In the case of identical sorting order,
        /// an older UIDocument is drawn first, appearing behind new ones.
        ///\\
        ///\\
        /// SA: [[PanelSettings.sortingOrder]]
        /// </remarks>
        public float sortingOrder
        {
            get => m_SortingOrder;
            set
            {
                if (m_SortingOrder == value)
                {
                    return;
                }

                m_SortingOrder = value;
                ApplySortingOrder();
            }
        }

        float IPanelComponent.sortingOrder => sortingOrder;

        internal void ApplySortingOrder()
        {
            AddRootVisualElementToTree();
        }

        internal static Func<IPanelComponent, ILiveReloadAssetTracker<VisualTreeAsset>> CreateLiveReloadVisualTreeAssetTracker;
        private ILiveReloadAssetTracker<VisualTreeAsset> m_LiveReloadVisualTreeAssetTracker;


        // Private constructor so it's not present on the public API file.
        private UIDocument()
        {
            m_UIDocumentCreationIndex = s_CurrentUIDocumentCounter++;
        }

        private void Awake()
        {
            if (IsEditorPlayingOrWillChangePlaymode.Invoke() && !IsEditorPlaying.Invoke())
            {
                // We're in a weird transition state that causes an error with the logic below so let's skip it.
                return;
            }

            // By default, the UI Content will try to attach itself to a parent somewhere in the hierarchy.
            // This is done to mimic the behaviour we get from UGUI's Canvas/Game Object relationship.
            SetupFromHierarchy();
        }

        private void OnEnable()
        {
            _Enable();
            EnabledDocumentCount++;
        }

        private void _Enable()
        {
            if (IsEditorPlayingOrWillChangePlaymode.Invoke() && !IsEditorPlaying.Invoke())
            {
                // We're in a weird transition state that causes an error with the logic below so let's skip it.
                return;
            }
            if (parentUI != null && m_PanelSettings == null)
            {
                // Ensures we have the same PanelSettings set as our parent, as the
                // initialization of the parent may have happened after ours.
                m_PanelSettings = parentUI.m_PanelSettings;
            }

            if (m_RootVisualElement == null)
            {
                RecreateUI();
            }
            else
            {
                AddRootVisualElementToTree();
            }

            if (TryGetComponent<UIRenderer>(out var renderer))
                renderer.enabled = true;
        }

        void IPanelComponent.SetComponentEnabled(bool enabled) => this.enabled = enabled;
        bool IPanelComponent.GetComponentEnabled() => this.enabled;

        //This need to be in the implementation for code stripping reason,is duplicated in PanelRenderer
        Vector3 IPanelComponent.GetPanelPosition(IEventHandler pickedElement, Ray worldRay)
        {
            return PanelComponentUtils.GetPanelPosition(gameObject, pickedElement, worldRay);
        }

        /// <summary>
        /// The runtime panel whose visualTree contains this document's rootVisualElement, if any.
        /// </summary>
        /// <remarks>
        /// Null will be returned if the UIDocument is not enabled or if the panel has not been created yet.
        /// </remarks>
        public IRuntimePanel runtimePanel => containerPanel;

        /// <summary>
        /// Strongly-typed equivalent of <see cref="runtimePanel"/>.
        /// </summary>
        internal RuntimePanel containerPanel => (RuntimePanel)rootVisualElement?.elementPanel;

        IRuntimePanel IPanelComponent.GetContainerPanel() => containerPanel;

        bool m_RootHasWorldTransform;

        void LateUpdate()
        {
            ((IPanelComponent) this).PerformUpdate();
        }

        // Used by unit tests.
        void IPanelComponent.PerformUpdate()
        {
            if (m_RootVisualElement == null || panelSettings == null || panelSettings.panel == null)
            {
                RemoveWorldSpaceCollider();
                return;
            }

            AddOrRemoveRendererComponent();

            if (isWorldSpace)
            {
                if (PanelComponentUtils.IsTransformControlledByGameObject(this))
                    SetTransform();
                else
                    ClearTransform();

                UpdateRenderer();
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
                RemoveWorldSpaceCollider();
                if (m_RootHasWorldTransform)
                    ClearTransform();
            }

            UpdateIsWorldSpaceRootFlag();
        }

        void UpdateRenderer()
        {
            UIRenderer renderer;
            if (!TryGetComponent<UIRenderer>(out renderer))
            {
                m_RootVisualElement.uiRenderer = null;
                return;
            }

            renderer.hideFlags = HideFlags.HideInInspector | HideFlags.DontSave;
            if (renderer.sharedMaterial)
            {
                renderer.sharedMaterial.hideFlags = HideFlags.HideInInspector;
            }

            m_RootVisualElement.uiRenderer = renderer;

            BaseRuntimePanel rtp = (BaseRuntimePanel)m_RootVisualElement.panel;
            if (rtp == null)
                return;

            var bb = SanitizeRendererBounds(rootVisualElement.localBounds3D);
            var toGameObject = PanelComponentUtils.TransformToGameObjectMatrix(PivotOffset(), pixelsPerUnit);
            VisualElement.TransformAlignedBounds(ref toGameObject, ref bb);

            renderer.localBounds = bb;

            UpdateIsWorldSpaceRootFlag();
        }

        Bounds SanitizeRendererBounds(Bounds b)
        {
            // The bounds may be invalid if the element is not layed out yet
            if (float.IsNaN(b.size.x) || float.IsNaN(b.size.y) || float.IsNaN(b.size.z))
                b = new Bounds(Vector3.zero, Vector3.zero);
            if (b.size.x < 0.0f || b.size.y < 0.0f)
                b.size = Vector3.zero;
            return b;
        }

        void AddOrRemoveRendererComponent()
        {
            // Automatically add the UIRenderer component when working in world-space
            TryGetComponent<UIRenderer>(out var renderer);
            if (isWorldSpace)
            {
                if (renderer == null)
                    gameObject.AddComponent<UIRenderer>();
            }
            else
            {
                UIRUtility.Destroy(renderer);
            }
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
            bool isWorldSpacePanel = !panelSettings.panel.isFlat;
            bool isWorldSpaceRootUIDocument;

            if (isWorldSpacePanel)
            {
                // World-space panel should cut the render chain unless they are nested in another UIDocument,
                // in which case they are rendered through their parent.
                isWorldSpaceRootUIDocument = (parentUI == null);
            }
            else
            {
                // For overlay panels, we should never cut the render chain.
                isWorldSpaceRootUIDocument = false;
            }

            if (m_RootVisualElement.isWorldSpaceRootPanelComponent != isWorldSpaceRootUIDocument)
            {
                m_RootVisualElement.isWorldSpaceRootPanelComponent = isWorldSpaceRootUIDocument;
                m_RootVisualElement.MarkDirtyRepaint(); // Necessary to insert a CutRenderChain command
            }
        }

        void SetTransform()
        {
            float ppu = pixelsPerUnit;
            Matrix4x4 matrix;
            if (m_ParentUI == null)
                PanelComponentUtils.ComputeParentTransform(PivotOffset(), ppu, out matrix);
            else
                PanelComponentUtils.ComputeNestedTransform(transform, m_ParentUI.transform, PivotOffset(), m_ParentUI.PivotOffset(), ppu, out matrix);

            m_RootVisualElement.style.transformOrigin = new TransformOrigin(Vector3.zero);
            m_RootVisualElement.style.translate = new Translate(matrix.GetPosition());
            m_RootVisualElement.style.rotate = new Rotate(matrix.rotation);
            m_RootVisualElement.style.scale = new Scale(matrix.lossyScale);
            m_RootHasWorldTransform = true;
        }

        void ClearTransform()
        {
            m_RootVisualElement.style.transformOrigin = StyleKeyword.Null;
            m_RootVisualElement.style.translate = StyleKeyword.Null;
            m_RootVisualElement.style.rotate = StyleKeyword.Null;
            m_RootVisualElement.style.scale = StyleKeyword.Null;
            m_RootHasWorldTransform = false;
        }

        float pixelsPerUnit => containerPanel?.pixelsPerUnit ?? 1.0f;

        Vector2 PivotOffset()
        {
            var pivotPercent = PanelComponentUtils.GetPivotAsPercent(m_Pivot);
            var localBounds = PanelComponentUtils.LocalBoundsFromPivotSource(rootVisualElement, pivotReferenceSize);

            return (-(Vector2)localBounds.min) + new Vector2(-localBounds.size.x * pivotPercent.x, -localBounds.size.y * pivotPercent.y);
        }

        /// <summary>
        /// Orders UIDocument components based on the way their GameObjects are ordered in the Hierarchy View.
        /// </summary>
        private void SetupFromHierarchy()
        {
            if (parentUI != null)
            {
                parentUI.RemoveChild(this);
            }
            parentUI = FindUIDocumentParent();
        }

        private UIDocument FindUIDocumentParent()
        {
            // Go up looking for a parent UIDocument, which we'd add ourselves too.
            // If that fails, we'll just add ourselves to the runtime panel through the PanelSettings
            // (assuming one is set, otherwise nothing gets drawn so it's pointless to not be
            // parented by another UIDocument OR have a PanelSettings set).
            Transform t = transform;
            Transform parentTransform = t.parent;
            if (parentTransform != null)
            {
                // We need to make sure we can get a parent even if they're disabled/inactive to reflect the good values.
                var potentialParents = parentTransform.GetComponentsInParent<UIDocument>(true);
                if (potentialParents != null && potentialParents.Length > 0)
                {
                    return potentialParents[0];
                }
            }

            return null;
        }

        internal void Reset()
        {
            if (parentUI == null)
            {
                m_PreviousPanelSettings?.DetachPanelComponent(this);
                panelSettings = null;
            }

            SetupFromHierarchy();

            if (parentUI != null)
            {
                m_PanelSettings = parentUI.m_PanelSettings;
                AddRootVisualElementToTree();
            }
            else if (m_PanelSettings != null)
            {
                AddRootVisualElementToTree();
            }
            OnValidate();
        }

        internal void AddChildAndInsertContentToVisualTree(UIDocument child)
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
            m_ChildrenContent.AddToListAndToVisualTree(child, m_RootVisualElement, ignoreContentContainer, m_FirstChildInsertIndex);
        }

        private void RemoveChild(UIDocument child)
        {
            m_ChildrenContent?.RemoveFromListAndFromVisualTree(child);
        }

        /// <summary>
        /// Force rebuild the UI from UXML (if one is attached) and of all children (if any).
        /// </summary>
        private void RecreateUI()
        {
            if (m_RootVisualElement != null)
            {
                RemoveFromHierarchy();
                if (m_PanelSettings != null)
                    m_PanelSettings.panel.liveReloadSystem.UnregisterVisualTreeAssetTracker(m_RootVisualElement);
                rootVisualElement = null;
            }

            // Even though the root element is of type VisualElement, we use a TemplateContainer internally
            // because we still want to use it as a TemplateContainer.
            if (sourceAsset != null)
            {
                rootVisualElement = new UIDocumentRootElement(this, sourceAsset);
                try
                {
                    if (sourceAsset.hasEditorElements)
                        Debug.LogWarning(k_EditorElementsWarningMessage, this);
                    sourceAsset.CloneTree(m_RootVisualElement);
                }
                catch (Exception e)
                {
                    // This shouldn't happen but if it does we don't fail silently.
                    Debug.LogError("The UXML file set for the UIDocument could not be cloned.");
                    Debug.LogException(e);
                }
            }

            m_OldUxml = sourceAsset;

            if (m_RootVisualElement == null)
            {
                // Empty container if no UXML is set or if there was an error with cloning the set UXML.
                rootVisualElement = new UIDocumentRootElement(this, null)
                    { name = gameObject.name + k_VisualElementNameSuffix };
            }
            else
            {
                m_RootVisualElement.name = gameObject.name + k_VisualElementNameSuffix;
            }
            m_RootVisualElement.pickingMode = PickingMode.Ignore;

            // Setting the live reload tracker has to be done prior to attaching to panel in order to work properly
            SetupVisualTreeAssetTracker();

            if (isActiveAndEnabled)
            {
                AddRootVisualElementToTree();
            }

            // Save the last VisualElement before we start adding children so we can guarantee
            // the order from the game object hierarchy.
            m_FirstChildInsertIndex = m_RootVisualElement.childCount;

            // Finally, we re-add our known children's element.
            // This makes sure the hierarchy of game objects reflects on the order of VisualElements.
            if (m_ChildrenContent != null)
            {
                // We need a copy to iterate because in the process of creating the children UI we modify the list.
                if (m_ChildrenContentCopy == null)
                {
                    m_ChildrenContentCopy = new List<IPanelComponent>(m_ChildrenContent.m_AttachedPanelComponents);
                }
                else
                {
                    m_ChildrenContentCopy.AddRange(m_ChildrenContent.m_AttachedPanelComponents);
                }

                foreach (var panelComponentChild in m_ChildrenContentCopy)
                {
                    var child = (UIDocument)panelComponentChild;
                    if (child.isActiveAndEnabled)
                    {
                        if (child.m_RootVisualElement == null)
                        {
                            child.RecreateUI();
                        }
                        else
                        {
                            // Since the root is already created, make sure it's inserted into the right position.
                            AddChildAndInsertContentToVisualTree(child);
                        }
                    }
                }

                m_ChildrenContentCopy.Clear();
            }

            SetupRootClassList();
        }

        internal void SetupPosition()
        {
            if (m_RootVisualElement == null || m_ParentUI == null)
                return; // The position property is only relevant for nested UIDocuments

            if (PanelComponentUtils.IsTransformControlledByGameObject(this))
                m_RootVisualElement.style.position = Position.Absolute;
            else
                m_RootVisualElement.style.position = m_Position;

            // We need to re-add ourselves in the list as the position influences
            // if we're part of the content-container or not.
            m_ParentUI.AddChildAndInsertContentToVisualTree(this);
        }

        private void SetupRootClassList()
        {
            if (m_RootVisualElement == null)
                return;

            if (!isWorldSpace)
            {
                // If we're not a child of any other UIDocument stretch to take the full screen.
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

            if (!PanelComponentUtils.IsTransformControlledByGameObject(this))
            {
                // Nested use-case. We shouldn't provide a fixed size.
                m_RootVisualElement.style.width = StyleKeyword.Null;
                m_RootVisualElement.style.height = StyleKeyword.Null;
                return;
            }

            if (m_WorldSpaceSizeMode == UIElements.WorldSpaceSizeMode.Fixed)
            {
                m_RootVisualElement.style.position = Position.Absolute;
                m_RootVisualElement.style.width = m_WorldSpaceWidth;
                m_RootVisualElement.style.height = m_WorldSpaceHeight;
            }
            else
            {
                m_RootVisualElement.style.position = Position.Absolute;
                m_RootVisualElement.style.width = StyleKeyword.Null;
                m_RootVisualElement.style.height = StyleKeyword.Null;
            }
        }

        private void AddRootVisualElementToTree()
        {
            if (!enabled)
                return; // Case 1388963, don't add the root if the component is disabled

            // If we do have a parent, it will add us.
            if (parentUI != null)
            {
                parentUI.AddChildAndInsertContentToVisualTree(this);
            }
            else if (m_PanelSettings != null)
            {
                m_PanelSettings.AttachAndInsertPanelComponentToVisualTree(this);
            }
        }

        private void RemoveFromHierarchy()
        {
            if (parentUI != null)
            {
                parentUI.RemoveChild(this);
            }
            else if (m_PanelSettings != null)
            {
                m_PanelSettings.DetachPanelComponent(this);
            }
        }

        private void OnDisable()
        {
            EnabledDocumentCount--;
            PointerDeviceState.RemovePanelComponentData(this);
            RemoveWorldSpaceCollider();

            if (m_RootVisualElement != null)
            {
                RemoveFromHierarchy();
                // Unhook tracking, we're going down (but only after we detach from the panel).
                if (m_PanelSettings != null)
                    m_PanelSettings.panel.liveReloadSystem.UnregisterVisualTreeAssetTracker(m_RootVisualElement);
                rootVisualElement = null;
            }

            if (TryGetComponent<UIRenderer>(out var renderer))
                renderer.enabled = false;
        }

        private void OnTransformChildrenChanged()
        {
            // In Editor, when not playing, we let a watcher listen for hierarchy changed events, except if
            // we're disabled in which case the watcher can't find us.
            if (!IsEditorPlaying.Invoke() && isActiveAndEnabled)
            {
                return;
            }
            if (m_ChildrenContent != null)
            {
                // The list may change inside the call to ReactToHierarchyChanged so we need a copy.
                if (m_ChildrenContentCopy == null)
                {
                    m_ChildrenContentCopy = new List<IPanelComponent>(m_ChildrenContent.m_AttachedPanelComponents);
                }
                else
                {
                    m_ChildrenContentCopy.AddRange(m_ChildrenContent.m_AttachedPanelComponents);
                }
                foreach (var panelComponentChild in m_ChildrenContentCopy)
                {
                    var child = (UIDocument)panelComponentChild;
                    child.ReactToHierarchyChanged();
                }
                m_ChildrenContentCopy.Clear();
            }
        }

        private void OnTransformParentChanged()
        {
            // In Editor, when not playing, we let a watcher listen for hierarchy changed events, except if
            // we're disabled in which case the watcher can't find us.
            if (!IsEditorPlaying.Invoke() && isActiveAndEnabled)
            {
                return;
            }

            ReactToHierarchyChanged();
        }

        internal void ReactToHierarchyChanged()
        {
            SetupFromHierarchy();

            if (parentUI != null)
            {
                // Using the property guarantees the change trickles down the hierarchy (if there is one).
                panelSettings = parentUI.m_PanelSettings;
            }

            m_RootVisualElement?.RemoveFromHierarchy();
            AddRootVisualElementToTree();

            SetupRootClassList();
        }


        private void SetupVisualTreeAssetTracker()
        {
            if (m_RootVisualElement == null)
                return;

            if (m_LiveReloadVisualTreeAssetTracker == null)
            {
                m_LiveReloadVisualTreeAssetTracker = CreateLiveReloadVisualTreeAssetTracker.Invoke(this);
            }

            if (m_PanelSettings != null)
                m_PanelSettings.panel.liveReloadSystem.RegisterVisualTreeAssetTracker(m_LiveReloadVisualTreeAssetTracker, m_RootVisualElement);
        }

        void IPanelComponent.OnLiveReloadOptionChanged()
        {
            // We not only have to recreate ourselves but also our children (and their children).
            ClearChildrenRecursively();

            ((IPanelComponent)this).HandleLiveReload();
        }

        private void ClearChildrenRecursively()
        {
            if (m_ChildrenContent == null)
            {
                return;
            }

            foreach (var panelComponentChild in m_ChildrenContent.m_AttachedPanelComponents)
            {
                var child = (UIDocument)panelComponentChild;
                if (child.m_RootVisualElement != null)
                {
                    child.m_RootVisualElement.RemoveFromHierarchy();
                    child.rootVisualElement = null;
                }

                child.ClearChildrenRecursively();
            }
        }

        void IPanelComponent.HandleLiveReload()
        {
            if (rootVisualElement == null)
                return;

            var disabledCompanions = DisableCompanions();

            RecreateUI();

            if (disabledCompanions != null && disabledCompanions.Count > 0)
            {
                EnableCompanions(disabledCompanions);
            }
            else if (IsEditorPlaying.Invoke())
            {
                Debug.LogWarning("UI was recreated and no companion MonoBehaviour found, some UI functionality may have been lost.");
            }
        }

        private HashSet<MonoBehaviour> DisableCompanions()
        {
            HashSet<MonoBehaviour> disabledCompanions = null;

            var companions = GetComponents<MonoBehaviour>();

            if (companions != null && companions.Length > 1) // If only one is found, it's this UIDocument.
            {
                disabledCompanions = new HashSet<MonoBehaviour>();

                foreach (var companion in companions)
                {
                    if (companion != null && companion != this && companion.isActiveAndEnabled)
                    {
                        companion.enabled = false;
                        disabledCompanions.Add(companion);
                    }
                }
            }

            return disabledCompanions;
        }

        private void EnableCompanions(HashSet<MonoBehaviour> disabledCompanions)
        {
            foreach (var companion in disabledCompanions)
            {
                companion.enabled = true;
            }
        }

        private VisualTreeAsset m_OldUxml = null;
        private float m_OldSortingOrder = k_DefaultSortingOrder;

        // For unit tests
        internal static int s_OnValidateCalled = 0;

        private void OnValidate()
        {
            s_OnValidateCalled++;
            ((IPanelComponent)this).PerformValidation(false);
        }

        void IPanelComponent.PerformValidation(bool forced)
        {
            // UUM-57741. Don't try to validate the UI Document if the panel isn't initialized. Otherwise,
            // the assignment of the visualTreeAsset below will indirectly create the panel. There are other
            // systems listening to the panel creation to initialize themselves, which may do invalid
            // operations for an OnValidate() call.
            // Note that starting in 6000.2, the EventSystem doesn't create GameObjects on panel creation,
            // but wait for the next call to Update to do it.
            // UUM-119306: Sort order is also undefined if not initialized.
            // UUM-120369: Direct assignation from the Inspector needs to update the panelSettings property
            // immediately, otherwise we will not get another chance later.
            if (m_PanelSettings != null && !m_PanelSettings.isInitialized && !forced)
            {
                return;
            }

            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            if (m_PreviousPanelSettings != m_PanelSettings &&
                (m_PanelSettings == null || m_RootVisualElement?.panel != null || forced))
            {
                // We'll use the setter as it guarantees the right behavior.
                // It's necessary for the setter that the old value is still in place.
                var tempPanelSettings = m_PanelSettings;
                m_PanelSettings = m_PreviousPanelSettings;
                panelSettings = tempPanelSettings;
            }

            if (m_OldUxml != sourceAsset || !m_OldUxml)
            {
                visualTreeAsset = sourceAsset;
                m_OldUxml = sourceAsset;
            }

            if (m_OldSortingOrder != m_SortingOrder)
            {
                if (m_RootVisualElement != null && m_RootVisualElement.panel != null)
                {
                    ApplySortingOrder();
                }

                m_OldSortingOrder = m_SortingOrder;
            }

            if (isWorldSpace)
            {
                SetupWorldSpaceSize();
            }

            SetupPosition();
        }

        private void OnDrawGizmosSelected()
        {
            if (rootVisualElement == null)
                return;

            PanelComponentUtils.DrawGizmoBounds(this, PivotOffset(), pixelsPerUnit);
        }

    }
}
