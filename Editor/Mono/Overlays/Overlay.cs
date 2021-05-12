// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
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

    public abstract partial class Overlay
    {
        const string k_UxmlPath = "UXML/Overlays/overlay.uxml";
        public static readonly string ussClassName = "unity-overlay";
        const string k_Highlight = "overlay-box-highlight";
        const string k_Floating = "overlay--floating";
        internal const string headerTitle = "overlay-header__title";
        const string k_Collapsed = "unity-overlay--collapsed";
        internal const string k_Header = "overlay-header";
        internal const string k_Expanded = "unity-overlay--expanded";
        internal const string k_CollapsedContent = "overlay-collapsed-content";
        internal const string k_CollapsedIconButton = "unity-overlay-collapsed-dropdown__icon";
        internal const string k_ToolbarHorizontalLayout = "overlay-layout--toolbar-horizontal";
        internal const string k_ToolbarVerticalLayout = "overlay-layout--toolbar-vertical";
        internal const string k_PanelLayout = "overlay-layout--freesize";
        internal const string k_Background = "unity-overlay";

        public EditorWindow containerWindow => canvas.containerWindow;

        VisualElement m_CurrentContent;
        internal VisualElement m_CollapsedContent;
        OverlayDropZone m_BeforeDropZone;
        OverlayDropZone m_AfterDropZone;
        // When collapsed, this is the VisualElement root of the modal popup overlay
        OverlayPopup m_ModalPopup;

        public string id => m_Id;
        OverlayContainer m_Container;
        internal event Action<Overlay, OverlayContainer> containerChanged;

        Layout m_Layout = Layout.Panel;
        public event Action<Layout> layoutChanged;
        public event Action<bool> collapsedChanged;
        bool m_Collapsed;

        static VisualTreeAsset s_TreeAsset;
        event Action displayNameChanged;
        string m_DisplayName;
        string m_Id;
        VisualElement m_ContentRoot;

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
                if (this is ICreateHorizontalToolbar)
                    supported |= Layout.HorizontalToolbar;
                if (this is ICreateVerticalToolbar)
                    supported |= Layout.VerticalToolbar;
                return supported;
            }
        }

        VisualElement m_RootVisualElement;
        internal VisualElement rootVisualElement
        {
            get
            {
                var root = m_RootVisualElement;
                if (root != null)
                    return root;
                CreateRoot();
                return m_RootVisualElement;
            }
        }

        void UpdateDropZones()
        {
            var dropZonesDisplay = !floating ? DisplayStyle.Flex : DisplayStyle.None;
            m_BeforeDropZone.style.display = dropZonesDisplay;
            m_AfterDropZone.style.display = dropZonesDisplay;
        }

        public Layout layout
        {
            get => m_Layout;

            internal set
            {
                if (m_Layout == value)
                    return;
                m_Layout = value;
                RebuildContent(value);
                layoutChanged?.Invoke(layout);
            }
        }

        internal void RebuildContent(Layout layout)
        {
            // If the layout is changed in a child class c'tor this can happen. It doesn't hurt anything, so just
            // handle it gracefully and let the constructor finish.
            if (m_ContentRoot == null)
                return;

            if (!collapsed)
            {
                m_CurrentContent?.RemoveFromHierarchy();
                m_CurrentContent = CreateContent(layout);
                m_ContentRoot.Add(m_CurrentContent);
            }

            UpdateStyling();
        }

        internal bool userControlledVisibility
        {
            get => !(this is IControlVisibility || this is ITransientOverlay);
        }

        public bool collapsed
        {
            get => m_Collapsed;
            set
            {
                if (m_Collapsed == value) return;

                // cannot expand in toolbar
                if (!value && !floating && !container.IsOverlayLayoutSupported(supportedLayouts)) return;

                m_Collapsed = value;
                OnCollapsedChanged(value);
            }
        }

        void OnCollapsedChanged(bool collapsed)
        {
            if (collapsed)
            {
                m_CurrentContent?.RemoveFromHierarchy();
                m_CurrentContent = null;
                m_ContentRoot.Add(m_CollapsedContent);
            }
            else
            {
                m_CollapsedContent.RemoveFromHierarchy();
                m_ContentRoot.Add(m_CurrentContent ?? (m_CurrentContent = CreateContent(layout)));
            }

            UpdateStyling();
            collapsedChanged?.Invoke(collapsed);
        }

        public string displayName
        {
            get
            {
                if (String.IsNullOrEmpty(m_DisplayName))
                    return m_RootVisualElement.name;
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

        //current container
        internal OverlayContainer container
        {
            get => m_Container;
            set
            {
                if (m_Container != value)
                {
                    m_Container = value;
                    UpdateLayoutBasedOnContainer();
                    containerChanged?.Invoke(this, value);
                }
            }
        }

        public event Action<bool> displayedChanged;

        // whether visible or not
        public bool displayed
        {
            get => rootVisualElement.style.display == DisplayStyle.Flex;
            set
            {
                if (rootVisualElement.style.display != (value ? DisplayStyle.Flex : DisplayStyle.None))
                {
                    rootVisualElement.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                    UpdateLayoutBasedOnContainer();
                    container?.UpdateIsVisibleInContainer(this);
                    displayedChanged?.Invoke(value);
                }
            }
        }

        internal OverlayCanvas canvas { get; set; }

        VisualElement CreateContent(Layout layout)
        {
            VisualElement content;

            switch (layout)
            {
                case Layout.Panel:
                    return CreatePanelContentSafe();

                case Layout.HorizontalToolbar:
                    if (this is ICreateHorizontalToolbar horizontal &&
                        (content = horizontal.CreateHorizontalToolbarContent()) != null)
                        return content;
                    goto default;

                case Layout.VerticalToolbar:
                    if (this is ICreateVerticalToolbar vertical &&
                        (content = vertical.CreateVerticalToolbarContent()) != null)
                        return content;
                    goto default;

                default:
                    Debug.LogError($"Overlay {GetType()} attempting to set unsupported layout: {layout}");
                    goto case Layout.Panel;
            }
        }

        // Wraps CreatePanelContent with try/catch and always returns a valid VisualElement. This prevents overlays
        // from bringing down the entire EditorWindow when something goes wrong in the constructor.
        internal VisualElement CreatePanelContentSafe()
        {
            var previousContent = m_ContentRoot;

            try
            {
                var content = CreatePanelContent();

                if (content != null)
                {
                    // When this happens styling isn't applied correctly when transitioning between popup window and
                    // floating/docked.
                    if (content == previousContent)
                        Debug.LogError($"Overlay named \"{displayName}\" returned a copy the existing VisualElement" +
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

        public virtual void OnCreated() {}

        public virtual void OnWillBeDestroyed() {}

        internal static VisualTreeAsset treeAsset
        {
            get
            {
                if (s_TreeAsset != null)
                    return s_TreeAsset;
                return s_TreeAsset = (VisualTreeAsset)EditorGUIUtility.Load(k_UxmlPath);
            }
        }

        void CreateRoot()
        {
            m_RootVisualElement = new VisualElement();
            treeAsset.CloneTree(m_RootVisualElement);

            m_RootVisualElement.AddToClassList(ussClassName);
            m_ContentRoot = m_RootVisualElement.Q("overlay-content");
            m_ContentRoot.renderHints = RenderHints.ClipWithScissors;

            var dragger = new OverlayDragger(this);
            var contextClick = new ContextualMenuManipulator(BuildContextMenu);

            var header = m_RootVisualElement.Q(null, k_Header);
            header.AddManipulator(contextClick);
            header.AddManipulator(dragger);
            m_CollapsedContent = m_RootVisualElement.Q(k_CollapsedContent);
            rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            var iconElement = m_RootVisualElement.Q<Label>(classes: k_CollapsedIconButton);
            var iconTexture = EditorGUIUtility.LoadIcon(EditorGUIUtility.GetIconPathFromAttribute(GetType()));
            var text = OverlayUtilities.GetSignificantLettersForIcon(displayName);

            if (iconTexture != null)
                iconElement.style.backgroundImage = iconTexture;
            else
                iconElement.text = text;

            m_CollapsedContent.Q<Button>().clicked += ToggleCollapsedPopup;

            var title = m_RootVisualElement.Q<Label>(headerTitle);
            title.text = displayName;
            displayNameChanged += () => title.text = displayName;

            m_RootVisualElement.Insert(0, m_BeforeDropZone = new OverlayDropZone(this, OverlayDropZone.Placement.Before));
            m_RootVisualElement.Add(m_AfterDropZone = new OverlayDropZone(this, OverlayDropZone.Placement.After));
            m_RootVisualElement.tooltip = L10n.Tr(displayName);

            layout = Layout.Panel;
            OnCollapsedChanged(collapsed);
            OnFloatingChanged(floating);
        }

        void ToggleCollapsedPopup()
        {
            if (m_ModalPopup != null)
            {
                ClosePopup();
                return;
            }

            m_ModalPopup = new OverlayPopup(this);

            m_ModalPopup.RegisterCallback<FocusOutEvent>(evt =>
            {
                if (evt.relatedTarget is VisualElement target && m_ModalPopup.Contains(target))
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
                if (floating || container.IsOverlayLayoutSupported(supportedLayouts))
                    menu.AppendAction(L10n.Tr("Expand"), (action) => collapsed = false);
            }
            else
                menu.AppendAction(L10n.Tr("Collapse"), (action) => collapsed = true);

            if (!isInToolbar)
            {
                menu.AppendSeparator();
                var layouts = supportedLayouts;

                if ((layouts & Layout.Panel) != 0)
                    menu.AppendAction(L10n.Tr("Panel"), action => layout = Layout.Panel, GetMenuItemState(layout == Layout.Panel));
                if ((layouts & Layout.HorizontalToolbar) != 0)
                    menu.AppendAction(L10n.Tr("Horizontal"), action => layout = Layout.HorizontalToolbar, GetMenuItemState(layout == Layout.HorizontalToolbar));
                if ((layouts & Layout.VerticalToolbar) != 0)
                    menu.AppendAction(L10n.Tr("Vertical"), action => layout = Layout.VerticalToolbar, GetMenuItemState(layout == Layout.VerticalToolbar));
            }
        }

        public bool isInToolbar => !floating && canvas.IsInToolbar(this);

        // update styling according to floating mode
        internal void UpdateStyling()
        {
            if (floating)
            {
                rootVisualElement.style.position = Position.Absolute;
                rootVisualElement.AddToClassList(k_Floating);
            }
            else
            {
                rootVisualElement.style.position = Position.Relative;
                rootVisualElement.style.left = 0;
                rootVisualElement.style.top = 0;
                rootVisualElement.RemoveFromClassList(k_Floating);
            }

            rootVisualElement.EnableInClassList(k_ToolbarVerticalLayout, layout == Layout.VerticalToolbar);
            rootVisualElement.EnableInClassList(k_ToolbarHorizontalLayout, layout == Layout.HorizontalToolbar);
            rootVisualElement.EnableInClassList(k_PanelLayout, layout == Layout.Panel);
            rootVisualElement.EnableInClassList(k_Collapsed, collapsed);
            rootVisualElement.EnableInClassList(k_Expanded, !collapsed);
        }

        void UpdateLayoutBasedOnContainer()
        {
            if (container == null)
                return;

            if (floating)
            {
                //if was forced into collapsed mode because container didn't support layout, force expand
                if (!container.IsOverlayLayoutSupported(supportedLayouts))
                {
                    // change the layout if it was collapsed into an unsupported layout
                    if ((layout & supportedLayouts) == 0)
                        layout = Layout.Panel;

                    collapsed = false;
                }
            }
            else if (container is ToolbarOverlayContainer)
            {
                if (!container.IsOverlayLayoutSupported(supportedLayouts))
                    collapsed = true;

                layout = container.isHorizontal
                    ? Layout.HorizontalToolbar
                    : Layout.VerticalToolbar;
            }
        }

        internal void Highlight(bool highlight)
        {
            rootVisualElement.EnableInClassList(k_Highlight, highlight);
        }

        internal void InitializeFromAttribute(OverlayAttribute overlayAttribute)
        {
            string ussName = overlayAttribute.ussName;
            string name = string.IsNullOrEmpty(overlayAttribute.displayName) ? ussName : overlayAttribute.displayName;
            m_Id = string.IsNullOrEmpty(overlayAttribute.id) ? name : overlayAttribute.id;
            displayName = L10n.Tr(name);
            // must be invoked before accessing rootVisualElement
            OnCreated();
            rootVisualElement.name = ussName;
        }

        // Do not use this function to initialize an Overlay. It omits callbacks that are necessary for proper initialization.
        // Overlays should be created an initialized via attribute, not directly created in code.
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal void LoadFromSerializedData(bool displayed,
            bool collapsed,
            bool floating,
            Vector2 floatingSnapOffset,
            Layout layout,
            SnapCorner snapCorner,
            OverlayContainer container)
        {
            this.container = container;
            this.floatingSnapCorner = snapCorner;
            // Changing layout, floating, or collapsed will all rebuild the content. During initialization, set these
            // values directly and invoke their callbacks once so that content isn't created and discarded multiple times.
            this.m_Layout = layout;
            this.m_Floating = floating;
            this.floatingSnapOffset = floatingSnapOffset;
            this.collapsed = collapsed;
            UpdateStyling();
            UpdateDropZones();
            floatingChanged?.Invoke(floating);
            layoutChanged?.Invoke(m_Layout);
            this.displayed = displayed;
        }
    }
}
