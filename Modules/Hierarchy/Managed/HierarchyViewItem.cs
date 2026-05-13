// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using UnityEngine.UIElements;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Represents a UI element that displays a hierarchy item in a <see cref="HierarchyView"/>.
    /// </summary>
    public sealed class HierarchyViewItem : VisualElement
    {
        static readonly UniqueStyleString k_UnityListViewItem = new("unity-list-view__item");
        static readonly UniqueStyleString k_UnityTreeViewItem = new("unity-tree-view__item");
        static readonly UniqueStyleString k_UnityTreeViewItemToggle = new("unity-tree-view__item-toggle");
        static readonly UniqueStyleString k_UnityToggleCheckmark = new("unity-toggle__checkmark");
        static readonly UniqueStyleString k_HierarchyItemContainer = new("hierarchy-item__container");
        static readonly UniqueStyleString k_HierarchyItemOverrideBarContainer = new("hierarchy-item__override-bar-container");
        static readonly UniqueStyleString k_HierarchyItemIcon = new("hierarchy-item__icon");
        static readonly UniqueStyleString k_HierarchyItemIconCut = new("hierarchy-item__icon--cut");
        static readonly UniqueStyleString k_HierarchyItemOverlayIcon = new("hierarchy-item__overlay-icon");
        static readonly UniqueStyleString k_HierarchyItemName = HierarchyViewItemName.k_StyleName;
        static readonly UniqueStyleString k_HierarchyItemLeftContainer = new("hierarchy-item__left-container");
        static readonly UniqueStyleString k_HierarchyItemLeftCustomSection = new("hierarchy-item__left-custom-section");
        static readonly UniqueStyleString k_HierarchyItemRightContainer = new("hierarchy-item__right-container");
        static readonly UniqueStyleString k_HierarchyItemRightArrowButton = new("hierarchy-item__right-arrow-button");
        static readonly UniqueStyleString k_HierarchyItemToggleHidden = new("hierarchy-item__toggle--hidden");
        internal const int k_IndentWidth = 14; // internal for tests

        // These members are set in Bind, and reset in Unbind
        HierarchyNode m_Node;
        HierarchyNodeTypeHandler m_Handler;
        HierarchyView m_View;

        // These members are set in the constructor and never reset
        readonly Toggle m_Toggle;
        readonly VisualElement m_OverrideBarContainer;
        readonly VisualElement m_Icon;
        readonly VisualElement m_OverlayIcon;
        readonly HierarchyViewItemName m_Name;

        // Users can add their VE to this container, they will appear on the right beside the name,
        // with style left aligned.
        readonly VisualElement m_LeftCustomContainer;
        // Users can add their VE to this container, they will appear on the right side of the main column,
        // with style right aligned.
        readonly VisualElement m_RightCustomContainer;

        readonly VisualElement m_LeftContainer;
        internal VisualElement LeftContainer => m_LeftContainer;

        internal delegate void ExpandedStateChangedEventHandler(in HierarchyNode node, bool isExpanded, bool recursive);
        internal event ExpandedStateChangedEventHandler ExpandedStateChanged;

        /// <summary>
        /// Gets the <see cref="HierarchyNodeType"/> of the <see cref="HierarchyNode"/> bound to this <see cref="HierarchyViewItem"/>.
        /// </summary>
        public HierarchyNodeType NodeType => m_Handler?.GetNodeType() ?? HierarchyNodeType.Null;

        /// <summary>
        /// Gets the <see cref="HierarchyNode"/> bound to this <see cref="HierarchyViewItem"/>.
        /// This value is <see cref="HierarchyNode.Null"/> when <see cref="HierarchyNodeTypeHandler.OnUnbindItem(HierarchyViewItem)"/> executes.
        /// </summary>
        public ref readonly HierarchyNode Node => ref m_Node;

        /// <summary>
        /// Gets the <see cref="Label"/> that displays the name of the item.
        /// </summary>
        public Label Name => m_Name.Label;

        /// <summary>
        /// Gets the <see cref="VisualElement"/> that displays the icon of the item.
        /// Add a USS class to this element to display a custom icon for the item type.
        /// </summary>
        public VisualElement Icon => m_Icon;

        /// <summary>
        /// Gets the <see cref="VisualElement"/> that represents the overlay icon of the item. This element is hidden by default.
        /// </summary>
        public VisualElement OverlayIcon => m_OverlayIcon;

        /// <summary>
        /// Gets the left-aligned <see cref="VisualElement"/> container to the right of the <see cref="Name"/>.
        /// </summary>
        public VisualElement LeftCustomContainer => m_LeftCustomContainer;

        /// <summary>
        /// Gets the right-aligned <see cref="VisualElement"/> container on the right side of this <see cref="HierarchyViewItem"/>.
        /// </summary>
        public VisualElement RightCustomContainer => m_RightCustomContainer;

        /// <summary>
        /// Gets the <see cref="Button"/> used to navigate into a node. This button is typically displayed as an arrow button.
        /// </summary>
        public Button NavigateIntoButton
        {
            get
            {
                var row = RowContainer;
                if (row == null)
                    return null;

                // Query for the button in the navigate column
                return row.Q<Button>(className: "hierarchy-item__right-arrow-button");
            }
        }

        /// <summary>
        /// Gets the <see cref="VisualElement"/> that represents the override bar at the left of the item.
        /// </summary>
        public VisualElement OverrideBarContainer => m_OverrideBarContainer;

        /// <summary>
        /// Gets the <see cref="UnityEngine.UIElements.Toggle"/> used to expand or collapse the item.
        /// </summary>
        public Toggle Toggle => m_Toggle;

        /// <summary>
        /// Gets the <see cref="VisualElement"/> that represents the entire row container of this <see cref="HierarchyViewItem"/>.
        /// </summary>
        public VisualElement RowContainer
        {
            get
            {
                var p = parent;
                while (p != null && !p.ClassListContains(MultiColumnController.rowContainerUssClassNameUnique))
                    p = p.parent;
                return p;
            }
        }

        /// <summary>
        /// Gets the <see cref="HierarchyNodeTypeHandler"/> currently associated with this <see cref="HierarchyViewItem"/>.
        /// </summary>
        public HierarchyNodeTypeHandler Handler => m_Handler;

        /// <summary>
        /// Gets the <see cref="HierarchyView"/> currently associated with this <see cref="HierarchyViewItem"/>.
        /// </summary>
        public HierarchyView View => m_View;

        internal bool Bound => m_Node != HierarchyNode.Null || m_View != null;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeReloadSafety", "UAL0015:Auto cleaned up symbol assigned by constructor", Justification = "This is a visual element that is recreated on code reload")]
        internal HierarchyViewItem()
        {
            // Setup the root. This is taken from 'TreeView.MakeTreeItem'
            name = (string)k_UnityTreeViewItem;
            style.flexDirection = FlexDirection.Row;

            var root = new VisualElement();
            root.AddToClassList(k_HierarchyItemContainer);
            hierarchy.Add(root);

            // Left container
            m_LeftContainer = new VisualElement();
            m_LeftContainer.AddToClassList(k_HierarchyItemLeftContainer);

            m_OverrideBarContainer = new VisualElement();
            m_OverrideBarContainer.AddToClassList(k_HierarchyItemOverrideBarContainer);

            m_Toggle = new Toggle();
            m_Toggle.AddToClassList(k_UnityTreeViewItemToggle);
            m_Toggle.AddToClassList(Foldout.toggleUssClassNameUnique);
            m_Toggle.Q(className: (string)k_UnityToggleCheckmark).style.marginTop = 0;
            m_Toggle.focusable = false;

            m_Icon = new VisualElement();
            m_Icon.AddToClassList(k_HierarchyItemIcon);
            m_OverlayIcon = new VisualElement();
            m_OverlayIcon.AddToClassList(k_HierarchyItemOverlayIcon);

            m_Name = new HierarchyViewItemName();
            m_Name.AddToClassList(k_HierarchyItemName);

            m_LeftCustomContainer = new VisualElement();
            m_LeftCustomContainer.AddToClassList(k_HierarchyItemLeftCustomSection);

            m_LeftContainer.Add(m_OverrideBarContainer);
            m_LeftContainer.Add(m_Toggle);
            m_LeftContainer.Add(m_Icon);
            m_LeftContainer.Add(m_OverlayIcon);
            m_LeftContainer.Add(m_Name);
            m_LeftContainer.Add(m_LeftCustomContainer);

            // Right container
            m_RightCustomContainer = new VisualElement();
            m_RightCustomContainer.AddToClassList(k_HierarchyItemRightContainer);

            root.Add(m_OverrideBarContainer);
            root.Add(m_LeftContainer);
            root.Add(m_RightCustomContainer);

            AddToClassList(k_UnityTreeViewItem);
            AddToClassList(k_UnityListViewItem);
        }

        internal void Bind(in HierarchyNode node, HierarchyView view)
        {
            if (Bound)
                throw new InvalidOperationException("Cannot bind a hierarchy view item that is already bound.");

            // Setup object
            m_Node = node;
            m_Handler = view.ViewModel.GetNodeTypeHandler(in node);
            m_View = view;

            // Setup styling
            var viewModel = m_View.ViewModel;
            var root = viewModel.GetRoot();
            var depth = viewModel.GetDepth(in m_Node);
            var relativeDepth = root == m_View.Source.Root ? depth : depth - viewModel.GetDepth(root) - 1;
            var noFilter = !m_View.Filtering;
            var indentWidth = noFilter ? relativeDepth * k_IndentWidth : 0;
            var oldValue = m_LeftContainer.style.translate.value;
            m_LeftContainer.style.translate = new Translate(m_LeftContainer.CeilToPanelPixelSize(indentWidth), oldValue.y, oldValue.z);

            var showToggle = noFilter && viewModel.HasVisibleChildren(in m_Node);
            m_Toggle.EnableInClassList(k_HierarchyItemToggleHidden, !showToggle);

            var isExpanded = viewModel.HasFlags(in m_Node, HierarchyNodeFlags.Expanded);
            m_Toggle.SetValueWithoutNotify(showToggle && isExpanded);
            Icon.EnableInClassList(k_HierarchyItemIconCut, viewModel.HasFlags(in m_Node, HierarchyNodeFlags.Cut));
            if (m_Handler is IHierarchyEditorNodeTypeHandler editorHandler)
                m_Name.Text = editorHandler.GetDisplayName(m_View, in m_Node);
            else
                m_Name.Text = m_View.Source.Exists(in m_Node) ? m_View.Source.GetName(in m_Node) : string.Empty;

            // Setup handler-specific or user-defined styling
            m_View.InvokeBindViewItem(this);

            // Register events
            m_Name.OnBeginRename += OnBeginRename;
            m_Name.OnEndRename += OnEndRename;
        }

        internal void Unbind()
        {
            if (!Bound)
                return;

            if (RowContainer is not null && RowContainer.ClassListContains(HierarchyView.k_HierarchyPingBase))
            {
                // Two TransitionEndEvents need to be sent because the fade in and fade out of the ping effect are two different
                // transitions. Second event is ignored if these events are fired during the fade out.
                RowContainer.SendEvent(new TransitionEndEvent() { target = RowContainer }, DispatchMode.Immediate);
                RowContainer.SendEvent(new TransitionEndEvent() { target = RowContainer }, DispatchMode.Immediate);
            }

            // Reset handler-specific or user-defined styling
            m_Node = HierarchyNode.Null;
            m_View.InvokeUnbindViewItem(this);

            // Unregister events
            m_Name.OnBeginRename -= OnBeginRename;
            m_Name.OnEndRename -= OnEndRename;

            // Reset object
            m_Handler = null;
            m_View = null;
        }

        [EventInterest(typeof(TooltipEvent))]
        [EventInterest(typeof(ClickEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            if (evt is TooltipEvent tooltipEvent)
            {
                var isFiltering = m_View.Filtering;
                var tooltipBuilder = new StringBuilder();

                m_View.InvokeGetTooltip(this, isFiltering, tooltipBuilder);
                if (tooltipBuilder.Length == 0)
                    return;

                tooltipEvent.rect = m_Name.worldBound;
                tooltipEvent.tooltip = tooltipBuilder.ToString();
            }
            else if (evt is ClickEvent clickEvent && m_Toggle.visible && m_Toggle.worldBound.Contains(clickEvent.position))
            {
                var isExpanded = !m_View.ViewModel.HasFlags(in m_Node, HierarchyNodeFlags.Expanded);
                ExpandedStateChanged?.Invoke(in m_Node, isExpanded, clickEvent.altKey);
                evt.StopPropagation();
            }
        }

        [EventInterest(typeof(PointerDownEvent))]
        protected override void HandleEventTrickleDown(EventBase evt)
        {
            // If the item is in renaming state, clicking on the toggle should cancel it.
            if (evt is not PointerDownEvent pde || (!(m_View?.m_IsRenamingItem ?? false)) ||
                !m_Toggle.worldBound.Contains(pde.position))
                return;

            pde.StopImmediatePropagation();
        }

        /// <summary>
        /// Starts the rename operation on the <see cref="Name"/> of this <see cref="HierarchyViewItem"/>, if the node supports renaming.
        /// </summary>
        public void BeginRename()
        {
            if (m_Node == HierarchyNode.Null)
                return;

            if (m_Handler is IHierarchyEditorNodeTypeHandler editorHandler && !editorHandler.CanSetName(m_View, in m_Node))
                return;

            m_Name.BeginRename();
        }

        void OnBeginRename()
        {
            m_View.SetRenamingItem(this);
        }

        void OnEndRename(string text, bool canceled)
        {
            m_View.SetRenamingItem(null);

            if (canceled)
                return;

            if (m_Node == HierarchyNode.Null || string.IsNullOrEmpty(text))
                return;

            if (m_Handler is IHierarchyEditorNodeTypeHandler editorHandler)
                editorHandler.OnSetName(m_View, in m_Node, text);
            else
                m_View.Source.SetName(in m_Node, text);
        }
    }
}
