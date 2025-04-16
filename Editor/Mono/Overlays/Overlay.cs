// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Overlays
{
    [Flags]
    public enum Layout
    {
        HorizontalToolbar = 1 << 0,
        VerticalToolbar = 1 << 1,
        Panel = 1 << 2,

        All = Panel | HorizontalToolbar | VerticalToolbar,
    }

    // See also OverlayPlacement.cs
    public abstract partial class Overlay
    {
        const string k_UxmlPath = "UXML/Overlays/overlay.uxml";
        public static readonly string ussClassName = "unity-overlay";
        const string k_Highlight = "overlay-box-highlight";
        const string k_Floating = "overlay--floating";
        internal const string headerTitle = "overlay-header__title";
        const string k_Collapsed = "unity-overlay--collapsed";
        internal const string k_Header = "overlay-header";
        const string k_Expanded = "unity-overlay--expanded";
        internal const string k_CollapsedContent = "overlay-collapsed-content";
        const string k_CollapsedIconButton = "unity-overlay-collapsed-dropdown__icon";
        internal const string k_ToolbarHorizontalLayout = "overlay-layout--toolbar-horizontal";
        internal const string k_ToolbarVerticalLayout = "overlay-layout--toolbar-vertical";
        const string k_PanelLayout = "overlay-layout--freesize";
        internal const string draggerName = "unity-overlay-collapse__dragger";

        string m_Id, m_RootVisualElementName, m_DisplayName;
        Layout m_ActiveLayout = Layout.Panel;

        internal bool dontSaveInLayout {get; set;}
        internal bool m_HasMenuEntry = true;

        [SerializeField]
        Layout m_Layout = Layout.Panel;
        [SerializeField]
        bool m_Collapsed;
        [SerializeField]
        bool m_Floating;
        [SerializeField]
        Vector2 m_FloatingSnapOffset;
        [SerializeField]
        internal Vector2 m_SnapOffsetDelta = Vector2.zero;
        [SerializeField]
        SnapCorner m_FloatingSnapCorner = SnapCorner.TopLeft;

        //Min and Max being 0 means the resizing is disabled for that axis without enforcing an actual size
        //Resizing is disabled by default
        [SerializeField]
        Vector2 m_Size;
        [SerializeField]
        bool m_SizeOverridden;
        Vector2 m_MinSize = Vector2.zero;
        Vector2 m_MaxSize = Vector2.zero;
        Vector2 m_DefaultSize = Vector2.zero;

        // Temporary Variables
        bool m_LockAnchor = false;
        bool m_ContentsChanged = true;
        internal bool m_DisableContentModification = false;

        // Connections
        public EditorWindow containerWindow => canvas.containerWindow;
        internal OverlayCanvas canvas { get; set; }
        internal bool isPopup { get; set; }
        OverlayContainer m_Container;
        internal OverlayContainer tempTargetContainer { get; set; }

        // Instantiated VisualElement contents
        VisualElement m_CurrentContent;
        VisualElement m_CollapsedContent;
        OverlayPopup m_ModalPopup; // collapsed popup root
        VisualElement m_RootVisualElement;
        VisualElement m_ResizeTarget;

        internal VisualElement resizeTarget => m_ResizeTarget;

        OverlayDropZone m_BeforeDropZone;
        OverlayDropZone m_AfterDropZone;

        internal OverlayDropZone insertBeforeDropZone => m_BeforeDropZone;
        internal OverlayDropZone insertAfterDropZone => m_AfterDropZone;

        // Callbacks
        public event Action<Layout> layoutChanged;
        public event Action<bool> collapsedChanged;
        public event Action<bool> displayedChanged;
        internal event Action<OverlayContainer> containerChanged;
        internal event Action minSizeChanged;
        internal event Action maxSizeChanged;
        internal event Action defaultSizeChanged;
        internal event Action sizeOverridenChanged;

        // Invoked in partial class OverlayPlacement.cs
#pragma warning disable 67
        public event Action<bool> floatingChanged;
        public event Action<Vector3> floatingPositionChanged;
#pragma warning restore 67

        public string id
        {
            get => m_Id;
            internal set { m_Id = value; }
        }

        static VisualTreeAsset s_TreeAsset;
        event Action displayNameChanged;
        VisualElement m_ContentRoot;

        // Properties
        internal bool hasMenuEntry => m_HasMenuEntry;
        internal Rect collapsedButtonRect => collapsedContent.worldBound;

        Texture2D m_CollapsedIcon = null;
        public Texture2D collapsedIcon
        {
            set
            {
                if(m_CollapsedIcon != null && m_CollapsedIcon.Equals(value))
                    return;

                m_CollapsedIcon = value;

                if (m_CollapsedContent == null)
                    return;

                var iconElement = collapsedContent.Q<Label>(classes: k_CollapsedIconButton);
                if(iconElement != null)
                {
                    var iconTexture = m_CollapsedIcon == null ?
                                        EditorGUIUtility.LoadIcon(EditorGUIUtility.GetIconPathFromAttribute(GetType())) :
                                        m_CollapsedIcon;

                    var collapsedIcon = GetCollapsedIconContent();
                    var image = collapsedIcon.image as Texture2D;
                    iconElement.style.backgroundImage = image;
                    iconElement.text = image != null ? null : OverlayUtilities.GetSignificantLettersForIcon(displayName);
                }
            }
        }

        VisualElement collapsedContent
        {
            get
            {
                if (m_CollapsedContent != null)
                    return m_CollapsedContent;

                m_CollapsedContent = rootVisualElement.Q(k_CollapsedContent);
                m_CollapsedContent.Q<Button>().clicked += ToggleCollapsedPopup;

                var iconElement = rootVisualElement.Q<Label>(classes: k_CollapsedIconButton);

                var collapsedIcon = GetCollapsedIconContent();
                if (collapsedIcon.image != null)
                    iconElement.style.backgroundImage = collapsedIcon.image as Texture2D;
                else
                    iconElement.text = collapsedIcon.text;

                return m_CollapsedContent;
            }
        }

        public Layout layout
        {
            get => m_Layout;

            internal set
            {
                if(m_DisableContentModification)
                {
                    Debug.LogError(GetEventTypeErrorMessage("Overlay.layout"));
                    return;
                }

                if (m_Layout == value)
                    return;

                m_Layout = value;
                RebuildContent();
            }
        }

        // layout is the preferred layout, active layout is what this overlay is actually using
        protected internal Layout activeLayout => m_ActiveLayout;

        public bool collapsed
        {
            get => m_CollapsedContent != null && m_CollapsedContent.parent == contentRoot;

            set
            {
                if(m_DisableContentModification)
                {
                    Debug.LogError(GetEventTypeErrorMessage("Overlay.collapsed"));
                    return;
                }

                m_Collapsed = value;
                if (m_Collapsed != (m_CollapsedContent != null && m_CollapsedContent.parent == contentRoot))
                    RebuildContent();
            }
        }

        public string displayName
        {
            get
            {
                if (String.IsNullOrEmpty(m_DisplayName))
                    return rootVisualElement.name;
                return m_DisplayName;
            }
            set
            {
                if (m_DisplayName != value)
                {
                    m_DisplayName = value;
                    displayNameChanged?.Invoke();
                }
            }
        }

        internal bool userControlledVisibility => !(this is IControlVisibility || this is ITransientOverlay);

        internal OverlayContainer container
        {
            get => m_Container;
            set
            {
                if (m_Container == value)
                    return;

                m_Container = value;
                containerChanged?.Invoke(m_Container);
            }
        }

        internal DockZone dockZone => floating ? DockZone.Floating : OverlayCanvas.GetDockZone(container);

        internal DockPosition dockPosition => container.GetSection(OverlayContainerSection.BeforeSpacer).Contains(this) ? DockPosition.Top : DockPosition.Bottom;

        internal static VisualTreeAsset treeAsset
        {
            get
            {
                if (s_TreeAsset != null)
                    return s_TreeAsset;
                return s_TreeAsset = (VisualTreeAsset)EditorGUIUtility.Load(k_UxmlPath);
            }
        }

        public bool displayed
        {
            get => rootVisualElement.style.display == DisplayStyle.Flex;
            set
            {
                if (rootVisualElement.style.display != (value ? DisplayStyle.Flex : DisplayStyle.None))
                {
                    if(m_DisableContentModification)
                    {
                        Debug.LogError(GetEventTypeErrorMessage("Overlay.displayed"));
                        return;
                    }

                    rootVisualElement.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;

                    RebuildContent();
                    displayedChanged?.Invoke(value);
                }
            }
        }

        // Externally supported layouts are enforced by implementing ICreate interfaces. Internally we need a dynamic
        // solution to handle CustomEditors. This isn't exposed because it would require much more validation to ensure
        // that `supportedLayouts` is correct when coming from external code, whereas internally we can trust that this
        // value is correct.
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected internal virtual Layout supportedLayouts
        {
            get
            {
                var supported = Layout.Panel;
                if (this is ICreateToolbar)
                    supported |= Layout.HorizontalToolbar | Layout.VerticalToolbar;
                if (this is ICreateHorizontalToolbar)
                    supported |= Layout.HorizontalToolbar;
                if (this is ICreateVerticalToolbar)
                    supported |= Layout.VerticalToolbar;
                return supported;
            }
        }

        sealed class GlobalMouseBehaviourForOverlays : MouseManipulator
        {
            Overlay m_Overlay;

            public GlobalMouseBehaviourForOverlays(Overlay overlay)
            {
                m_Overlay = overlay;
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<MouseDownEvent>(OnMouseDownTrickleDown, TrickleDown.TrickleDown);
                target.RegisterCallback<MouseDownEvent>(OnMouseDownBubbleUp, TrickleDown.NoTrickleDown);
                target.RegisterCallback<MouseUpEvent>(OnMouseUp);
                target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<MouseDownEvent>(OnMouseDownTrickleDown, TrickleDown.TrickleDown);
                target.UnregisterCallback<MouseDownEvent>(OnMouseDownBubbleUp, TrickleDown.NoTrickleDown);
                target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
                target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            }

            void OnMouseDownTrickleDown(MouseDownEvent e)
            {
                m_Overlay.BringToFront();
            }

            void OnMouseDownBubbleUp(MouseDownEvent e)
            {
                e.StopPropagation();
            }

            void OnMouseUp(MouseUpEvent e)
            {
                e.StopPropagation();
            }

            void OnMouseMove(MouseMoveEvent e)
            {
                e.StopPropagation();
            }
        }

        public VisualElement rootVisualElement
        {
            get
            {
                if (m_RootVisualElement != null)
                    return m_RootVisualElement;

                m_RootVisualElement = new VisualElement();
                treeAsset.CloneTree(m_RootVisualElement);

                m_RootVisualElement.name = m_RootVisualElementName;
                m_RootVisualElement.usageHints = UsageHints.DynamicTransform;
                m_RootVisualElement.AddToClassList(ussClassName);
                m_RootVisualElement.AddManipulator(new GlobalMouseBehaviourForOverlays(this));

                var dragger = new OverlayDragger(this);
                var contextClick = new ContextualMenuManipulator(BuildContextMenu);

                var header = m_RootVisualElement.Q(null, k_Header);
                header.AddManipulator(contextClick);
                header.AddManipulator(dragger);

                var title = m_RootVisualElement.Q<Label>(headerTitle);
                title.text = displayName;
                displayNameChanged += () => title.text = displayName;

                var dockArea = new VisualElement() { name = "OverlayDockArea" };
                dockArea.pickingMode = PickingMode.Ignore;
                dockArea.StretchToParentSize();
                m_RootVisualElement.Add(dockArea);

                dockArea.Add(m_BeforeDropZone = new OverlayDropZone(this, OverlayDropZone.Placement.Before));
                dockArea.Add(m_AfterDropZone = new OverlayDropZone(this, OverlayDropZone.Placement.After));

                m_RootVisualElement.tooltip = L10n.Tr(displayName);

                m_ResizeTarget = m_RootVisualElement.Q("unity-overlay");
                m_ResizeTarget.style.overflow = Overflow.Hidden;
                m_ResizeTarget.Add(new OverlayResizerGroup(this));

                return m_RootVisualElement;
            }
        }

        // used by tests
        internal VisualElement contentRoot
        {
            get
            {
                if(m_ContentRoot != null)
                    return m_ContentRoot;
                m_ContentRoot = rootVisualElement.Q("overlay-content");
                m_ContentRoot.renderHints = RenderHints.ClipWithScissors;
                return m_ContentRoot;
            }
        }

        public bool isInToolbar => container is ToolbarOverlayContainer;

        internal bool sizeOverridden
        {
            get => m_SizeOverridden;

            set
            {
                if (m_SizeOverridden == value)
                    return;

                m_SizeOverridden = value;
                sizeOverridenChanged?.Invoke();
            }
        }

        internal Vector2 sizeToSave => m_Size;

        public Vector2 size
        {
            get
            {
                return m_ResizeTarget.layout.size;
            }
            set
            {
                if (m_Size == value && sizeOverridden)
                    return;

                m_Size = value;
                sizeOverridden = true;
                UpdateSize();
            }
        }

        public Vector2 minSize
        {
            get => m_MinSize;
            set
            {
                if (m_MinSize == value)
                    return;

                m_MinSize = value;
                minSizeChanged?.Invoke();
                UpdateSize();
            }
        }

        public Vector2 maxSize
        {
            get => m_MaxSize;
            set
            {
                if (m_MaxSize == value)
                    return;

                m_MaxSize = value;
                maxSizeChanged?.Invoke();
                UpdateSize();
            }
        }

        public Vector2 defaultSize
        {
            get => m_DefaultSize;
            set
            {
                if (m_DefaultSize == value)
                    return;

                m_DefaultSize = value;
                defaultSizeChanged?.Invoke();
                UpdateSize();
            }
        }

        internal static string GetEventTypeErrorMessage(string errorEvent)
        {
            return L10n.Tr($"Cannot modify {errorEvent} during event of type EventType.Layout");
        }

        internal void ResetSize()
        {
            if (!sizeOverridden)
                return;

            sizeOverridden = false;
            UpdateSize();
        }

        bool CanCreateRequestedLayout(Layout requested)
        {
            // Is layout requesting a toolbar while Overlay is not implementing ICreateToolbar?
            if ((int)(requested & supportedLayouts) < 1)
                return false;

            if (tempTargetContainer != null)
                return tempTargetContainer.IsOverlayLayoutSupported(requested);

            return floating || container == null || container.IsOverlayLayoutSupported(requested);
        }

        Layout GetBestLayoutForState()
        {
            // Always prefer the user-set layout
            if (CanCreateRequestedLayout(layout))
                return layout;

            for (int i = 0; i < 3; i++)
            {
                if (CanCreateRequestedLayout((Layout)(1 << i)))
                    return (Layout)(1 << i);
            }

            return 0;
        }

        internal VisualElement GetSimpleHeader()
        {
            var header = new VisualElement();
            var title = new Label(displayName);
            title.name = headerTitle;
            header.Add(title);

            return header;
        }

        // Rebuild the Overlay contents, taking into account the container and layout. If the container does not support
        // the requested layout, the overlay will be collapsed (the collapsed property will not be modified, and the
        // next time RebuildContent is invoked this method will try again to build the requested layout and un-collapse
        // the Overlay).
        internal void RebuildContent()
        {
            if (m_Container == null)
                return;

            // We need to invoke a callback if the collapsed state changes (either from user request or invalid layout)
            bool wasCollapsed = collapsedContent.parent == contentRoot;
            var prevLayout = m_ActiveLayout;
            m_ActiveLayout = GetBestLayoutForState();

            // Clear any existing contents.
            m_CurrentContent?.RemoveFromHierarchy();
            collapsedContent.RemoveFromHierarchy();
            m_CurrentContent = null;

            if (!displayed)
                return;

            // An Overlay can collapsed by request, or by necessity. If collapsed due to invalid layout/container match,
            // the collapsed property is not modified. The next time a content rebuild is requested we'll try again to
            // create the contents with the stored state.
            bool isCollapsed = m_Collapsed || activeLayout == 0;
            var targetContainer = tempTargetContainer ?? container;

            m_ContentsChanged = true;

            m_ActiveLayout = m_ActiveLayout == 0 ? targetContainer.preferredLayout : m_ActiveLayout;

            // Update styling
            if (floating)
            {
                rootVisualElement.style.position = Position.Absolute;
                rootVisualElement.AddToClassList(k_Floating);
            }
            else
            {
                rootVisualElement.style.position = Position.Relative;
#pragma warning disable CS0618 // Type or member is obsolete
                rootVisualElement.transform.position = Vector3.zero;
#pragma warning restore CS0618 // Type or member is obsolete
                rootVisualElement.RemoveFromClassList(k_Floating);
            }

            rootVisualElement.EnableInClassList(k_ToolbarVerticalLayout, m_ActiveLayout == Layout.VerticalToolbar);
            rootVisualElement.EnableInClassList(k_ToolbarHorizontalLayout, m_ActiveLayout == Layout.HorizontalToolbar);
            rootVisualElement.EnableInClassList(k_PanelLayout, m_ActiveLayout == Layout.Panel);
            rootVisualElement.EnableInClassList(k_Collapsed, isCollapsed);
            rootVisualElement.EnableInClassList(k_Expanded, !isCollapsed);

            if (isCollapsed)
            {
                if (collapsedContent.parent != contentRoot)
                    contentRoot.Add(collapsedContent);
            }
            else
            {
                m_CurrentContent = CreateContent(m_ActiveLayout);
                contentRoot.Add(m_CurrentContent);
            }

            // Disable drop zone previews when floating
            var dropZonesDisplay = !floating ? DisplayStyle.Flex : DisplayStyle.None;
            m_BeforeDropZone.style.display = dropZonesDisplay;
            m_AfterDropZone.style.display = dropZonesDisplay;

            UpdateSize();

            // Invoke callbacks after content is created and styling has been applied
            if(wasCollapsed != isCollapsed)
                collapsedChanged?.Invoke(isCollapsed);

            if (prevLayout != m_ActiveLayout)
                layoutChanged?.Invoke(m_ActiveLayout);
        }

        internal GUIContent GetCollapsedIconContent()
        {
            var icon = m_CollapsedIcon == null
                ? EditorGUIUtility.LoadIcon(EditorGUIUtility.GetIconPathFromAttribute(GetType()))
                : m_CollapsedIcon;

            if (icon != null)
                return new GUIContent(icon);

            return new GUIContent(OverlayUtilities.GetSignificantLettersForIcon(displayName));
        }

        internal bool IsResizable()
        {
            return activeLayout == Layout.Panel && !collapsed;
        }

        bool IsSizeAuto(float size)
        {
            return size <= 0 || float.IsNegativeInfinity(size);
        }

        void UpdateSize()
        {
            if (m_ResizeTarget == null)
                return;

            ApplySize(m_ResizeTarget, IsResizable(), sizeOverridden);

            var position = canvas.ClampToOverlayWindow(new Rect(floatingPosition, m_Size)).position;
            UpdateSnapping(position);
        }

        void ApplySize(VisualElement element, bool resizableLayout, bool sizeOverridden)
        {
            element.style.minWidth = resizableLayout && !IsSizeAuto(m_MinSize.x) ? m_MinSize.x : new StyleLength(StyleKeyword.Auto);
            element.style.minHeight = resizableLayout && !IsSizeAuto(m_MinSize.y) ? m_MinSize.y : new StyleLength(StyleKeyword.Auto);
            element.style.maxWidth = resizableLayout && !IsSizeAuto(m_MaxSize.x) ? m_MaxSize.x : new StyleLength(StyleKeyword.Auto);
            element.style.maxHeight = resizableLayout && !IsSizeAuto(m_MaxSize.y) ? m_MaxSize.y : new StyleLength(StyleKeyword.Auto);

            if (resizableLayout && sizeOverridden)
            {
                element.style.width = m_Size.x;
                element.style.height = m_Size.y;
            }
            else
            {
                element.style.width = defaultSize.x > 0 && resizableLayout ? defaultSize.x : new StyleLength(StyleKeyword.Auto);
                element.style.height = defaultSize.y > 0 && resizableLayout ? defaultSize.y : new StyleLength(StyleKeyword.Auto);
            }
        }

        // CreateContent always returns a new VisualElement tree with the Overlay contents. It does not modify the
        // m_CurrentContent or m_ContentRoot properties. Use RebuildContent() to update an Overlay contents.
        // CreateContent will try to return content in the requested layout, regardless of whether the container
        // supports it. The only reason that content would not be created with the requested layout is if the Overlay
        // does not implement the correct ICreate{Horizontal, Vertical}Toolbar interface.
        // To rebuild content taking into account the parent container, use RebuildContent().
        public VisualElement CreateContent(Layout requestedLayout)
        {
            var previousContent = m_ContentRoot;

            try
            {
                VisualElement content = null;

                switch (requestedLayout)
                {
                    case Layout.HorizontalToolbar:
                        if (!(this is ICreateHorizontalToolbar horizontal)
                            || (content = horizontal.CreateHorizontalToolbarContent()) == null)
                            goto default;
                        break;

                    case Layout.VerticalToolbar:
                        if (!(this is ICreateVerticalToolbar vertical)
                            || (content = vertical.CreateVerticalToolbarContent()) == null)
                            goto default;
                        break;

                    case Layout.Panel:
                        content = CreatePanelContent();
                        break;

                    default:
                        if (!(this is ICreateToolbar toolbar))
                        {
                            Debug.LogError($"Overlay {GetType()} attempting to set unsupported layout: {requestedLayout}");
                            goto case Layout.Panel;
                        }
                        content = new EditorToolbar(toolbar.toolbarElements, canvas.containerWindow).rootVisualElement;
                        break;
                }

                if (content != null)
                {
                    // When this happens styling isn't applied correctly when transitioning between popup window and
                    // floating/docked.
                    if (content == previousContent)
                        Debug.LogError($"Overlay named \"{displayName}\" returned a reference to the previous " +
                            $" content. This is not allowed; CreateContent() must return a new instance.");
                    return content;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed loading overlay \"{displayName}\"!\n{e}");
                return new Label($"{displayName} failed to load");
            }

            Debug.LogError($"Overlay \"{displayName}\" returned a null VisualElement.");
            return new Label($"{displayName} failed to load");
        }

        public abstract VisualElement CreatePanelContent();

        // Invoked when an Overlay is added to an OverlayCanvas
        public virtual void OnCreated() {}

        public virtual void OnWillBeDestroyed() {}

        public void Close() => canvas?.Remove(this);

        // Used by tests
        internal VisualElement popup => m_ModalPopup;

        // Used by tests
        internal void ToggleCollapsedPopup()
        {
            if (m_ModalPopup != null)
            {
                ClosePopup();
                return;
            }

            m_ModalPopup = OverlayPopup.CreateUnderOverlay(this);
            ApplySize(m_ModalPopup.Q(className: "overlay-box-background"), true, sizeOverridden);

            m_ModalPopup.RegisterCallback<FocusOutEvent>(evt =>
            {
                if (evt.relatedTarget is VisualElement target && (m_ModalPopup == target || m_ModalPopup.Contains(target)))
                    return;

                // When the new focus is an embedded IMGUIContainer or popup window, give focus back to the modal
                // popup so that the next focus out event has the opportunity to close the element.
                if (evt.relatedTarget == null && m_ModalPopup.containsCursor)
                    EditorApplication.delayCall += m_ModalPopup.Focus;
                else
                    ClosePopup();
            });

            canvas.rootVisualElement.Add(m_ModalPopup);
            m_ModalPopup.Focus();
        }

        public void RefreshPopup()
        {
            m_ModalPopup?.Refresh();
        }

        void ClosePopup()
        {
            m_ModalPopup?.RemoveFromHierarchy();
            m_ModalPopup = null;
        }

        static DropdownMenuAction.Status GetMenuItemState(bool isChecked)
        {
            return isChecked ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
        }

        void BuildContextMenu(ContextualMenuPopulateEvent evt)
        {
            var menu = evt.menu;

            menu.AppendAction(L10n.Tr("Hide"),
                (action) => displayed = false,
                userControlledVisibility ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            if (collapsed)
            {
                if (container == null || container.IsOverlayLayoutSupported(supportedLayouts))
                    menu.AppendAction(L10n.Tr("Expand"), (action) => collapsed = false);
            }
            else
                menu.AppendAction(L10n.Tr("Collapse"), (action) => collapsed = true);

            if (!isInToolbar)
            {
                menu.AppendAction(L10n.Tr("Reset Size"), action => ResetSize());

                menu.AppendSeparator();
                var layouts = supportedLayouts;

                // Panel layout is always supported by default, we only add this option in the menu if other options are available
                if ((layouts & Layout.HorizontalToolbar) != 0 || (layouts & Layout.VerticalToolbar) != 0)
                    menu.AppendAction(L10n.Tr("Panel"), action => { layout = Layout.Panel; collapsed = false; }, GetMenuItemState(layout == Layout.Panel));
                if ((layouts & Layout.HorizontalToolbar) != 0)
                    menu.AppendAction(L10n.Tr("Horizontal"), action => { layout = Layout.HorizontalToolbar; collapsed = false; }, GetMenuItemState(layout == Layout.HorizontalToolbar));
                if ((layouts & Layout.VerticalToolbar) != 0)
                    menu.AppendAction(L10n.Tr("Vertical"), action => { layout = Layout.VerticalToolbar; collapsed = false; }, GetMenuItemState(layout == Layout.VerticalToolbar));
            }
        }

        internal void SetHighlightEnabled(bool highlight)
        {
            rootVisualElement.EnableInClassList(k_Highlight, highlight);
        }

        internal void Initialize(OverlayAttribute attrib) => Initialize(attrib.id, attrib.ussName, attrib.displayName, attrib.defaultSize, attrib.minSize, attrib.maxSize);

        internal void Initialize(string _id, string _uss, string _display, Vector2 defaultSize, Vector2 minSize, Vector2 maxSize)
        {
            m_RootVisualElementName = _uss;
            string name = string.IsNullOrEmpty(_display) ? m_RootVisualElementName : _display;
            m_Id = string.IsNullOrEmpty(_id) ? name : _id;
            displayName = L10n.Tr(name);
            rootVisualElement.style.display = DisplayStyle.None;

            // Taking into account the case where the user modifies the sizes in their overlay constructor.
            if (!float.IsNegativeInfinity(defaultSize.x) && !float.IsNegativeInfinity(defaultSize.y))
                m_DefaultSize = defaultSize;

            if (!float.IsNegativeInfinity(minSize.x) && !float.IsNegativeInfinity(minSize.y))
                m_MinSize = minSize;

            if (!float.IsNegativeInfinity(maxSize.x) && !float.IsNegativeInfinity(maxSize.y))
                m_MaxSize = maxSize;
        }

        // marked obsolete by @karlh 2023/03/13
        [Obsolete("No longer necessary, Overlay data is serialized by the OverlayCanvas.", false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal void ApplySaveData(SaveData data)
        {
            floatingSnapCorner = data.snapCorner;
            m_Floating = data.floating;
            m_Collapsed = data.collapsed;
            m_Layout = data.layout;
            m_FloatingSnapOffset = data.snapOffset;
            m_SnapOffsetDelta = data.snapOffsetDelta;
            m_SizeOverridden = data.sizeOverridden;
            m_Size = data.size;
        }

        internal void Reset(OverlayAttribute attrib)
        {
            if (attrib != null)
            {
                if (!float.IsNegativeInfinity(attrib.defaultSize.x) && !float.IsNegativeInfinity(attrib.defaultSize.y))
                    m_DefaultSize = attrib.defaultSize;

                if (!float.IsNegativeInfinity(attrib.minSize.x) && !float.IsNegativeInfinity(attrib.minSize.y))
                    m_MinSize = attrib.minSize;

                if (!float.IsNegativeInfinity(attrib.maxSize.x) && !float.IsNegativeInfinity(attrib.maxSize.y))
                    m_MaxSize = attrib.maxSize;

                m_Floating = attrib.defaultDockZone == DockZone.Floating;
                m_Collapsed = false;
                m_Layout = attrib.defaultLayout;
            }
        }
    }
}
