// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using UnityEngine;
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

        internal const float k_IndentWidth = 14f;
        internal const float k_OverrideBarWidth = 4f;

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

        // Cached to avoid a per-bind delegate allocation.
        readonly Action m_OnBeginRename;
        readonly Action<string, bool> m_OnEndRename;

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
        /// <example>
        /// The following example changes the icon a GameObject uses in the Hierarchy window if it has a specified tag. It uses `Icon` to add a custom USS class to the icon element of items whose GameObject has the `Favorite` tag. 
        ///
        /// The example requires a USS file called `ChangeNodeIcon.uss` and a tag called `Favorite`.
        ///
        /// To use this example:
        ///
        ///1. Save the script in a folder called `Assets/Editor/ChangeNodeIcon`. Scripts in an `Editor` folder can use the Hierarchy module API without additional setup. If you save the script outside of an `Editor` folder, you must enable the Hierarchy built-in module in the **Package Manager** window, which also adds the module to your Player builds.
        ///2. Copy the styles from the USS example on this page. Save them in a USS file called `ChangeNodeIcon.uss` in the same `Assets/Editor/ChangeNodeIcon` folder. 
        ///3. Create a tag called `Favorite`: select a GameObject, open the **Tag** dropdown in the **Inspector** window, and select **Add Tag**.
        ///4. Assign the `Favorite` tag to a GameObject to change the icon it displays.
        ///
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/ChangeNodeIcon/ChangeNodeIcon.cs"/>
        /// </example>
        /// <example>
        /// The following example shows how to style `ChangeNodeIcon.uss`.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/ChangeNodeIcon/ChangeNodeIcon.uss"/>
        /// </example>
        public VisualElement Icon => m_Icon;

        /// <summary>
        /// Gets the <see cref="VisualElement"/> that represents an icon that overlays over the icon of the item. This element is hidden by default.
        /// </summary>
        /// <remarks>
        ///  Typically used to add an icon that indicates the state of the item. For example, you can use this to display an icon over a GameObject's icon that indicates a prefab has a broken reference. 
        /// </remarks>
        /// <example>
        /// The following example adds a custom tooltip that displays in the Hierarchy window when you hover over any GameObject that has a custom component named `Notes` attached to it. It uses `OverlayIcon` to add the custom overlay indicator to the icon of the GameObject. 
        ///
        /// The example requires a USS file called `CustomTooltip.uss` and a custom MonoBehaviour script called `Notes.cs`.
        ///
        /// To use this example:
        ///
        ///1. Save the script in a folder called `Assets/Editor/CustomTooltip`. Scripts in an `Editor` folder can use the Hierarchy module API without additional setup. If you save the script outside of an `Editor` folder, you must enable the Hierarchy built-in module in the **Package Manager** window, which also adds the module to your Player builds.
        ///2. Copy the styles from the USS example on this page. Save them in a USS file called `CustomTooltip.uss` in the same `Assets/Editor/CustomTooltip` folder. 
        ///3. Save the `Notes.cs` script outside of an `Editor` folder, because MonoBehaviour scripts in an `Editor` folder can't be attached to GameObjects.
        ///4. Add the `Notes` component to a GameObject.
        ///5. In the **Inspector** window, enter text in the **Note** field of the `Notes` component.
        ///6. In the Hierarchy window, hover over the name of the GameObject to display the text as a tooltip. A small overlay indicator also displays in the corner of the icon of any GameObject that has a note.
        ///
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/CustomTooltip/CustomTooltip.cs"/>
        /// </example>
        /// <example>
        /// The following example shows how to style `CustomTooltip.uss`.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/CustomTooltip/CustomTooltip.uss"/>
        /// </example>
        /// <example>
        /// The following example shows the `Notes` component that the CustomTooltip example uses.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Runtime/Notes.cs"/>
        /// </example>
        public VisualElement OverlayIcon => m_OverlayIcon;

        /// <summary>
        /// Gets the left-aligned <see cref="VisualElement"/> container to the right of the <see cref="Name"/>.
        /// </summary>
        /// <example>
        /// The following example displays how many children each collapsed item has in the Hierarchy window. It uses `LeftCustomContainer` to host a `Label` that shows the number of child nodes in parentheses next to any collapsed item.
        /// To use this example, save the script in a folder called `Assets/Editor/CountWhenCollapsed`. Scripts in an `Editor` folder can use the Hierarchy module API without additional setup. If you save the script outside of an `Editor` folder, you must enable the Hierarchy built-in module in the **Package Manager** window, which also adds the module to your Player builds.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/CountWhenCollapsed/CountWhenCollapsed.cs"/>
        /// </example>
        public VisualElement LeftCustomContainer => m_LeftCustomContainer;

        /// <summary>
        /// Gets the right-aligned <see cref="VisualElement"/> container on the right side of this <see cref="HierarchyViewItem"/>.
        /// </summary>
        /// <example>
        /// The following example adds a button next to a GameObject in the Hierarchy window if that GameObject is a prefab instance. You can select the button to locate and highlight the prefab asset in the **Project** window. It uses `RightCustomContainer` to host the button. 
        ///
        /// The example requires a USS file called `PrefabActionButtons.uss`.
        /// To use this example, save the script and USS file in a folder called `Assets/Editor/PrefabActionButtons`. Scripts in an `Editor` folder can use the Hierarchy module API without additional setup. If you save the script outside of an `Editor` folder, you must enable the Hierarchy built-in module in the **Package Manager** window, which also adds the module to your Player builds.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/PrefabActionButtons/PrefabActionButtons.cs"/>
        /// </example>
        /// <example>
        /// The following example shows how to style `PrefabActionButtons.uss`.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/PrefabActionButtons/PrefabActionButtons.uss"/>
        /// </example>
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
        /// <example>
        /// The following example changes the row background color of the Hierarchy window for GameObjects that have an `Enemy` component attached to them. It uses `RowContainer` to apply a custom USS class to the entire row. 
        ///
        /// The example requires two USS files for theme-based styling: `ChangeRowColor_dark.uss` for the Dark theme, and `ChangeRowColor_light.uss` for the Light theme. 
        ///
        /// It also requires a custom MonoBehaviour script called `Enemy.cs`.
        ///
        /// To use this example:
        ///
        ///1. Save the script in a folder called `Assets/Editor/ChangeRowColor`. Scripts in an `Editor` folder can use the Hierarchy module API without additional setup. If you save the script outside of an `Editor` folder, you must enable the Hierarchy built-in module in the **Package Manager** window, which also adds the module to your Player builds.
        ///2. Copy the styles from the USS examples on this page. Save them in USS files called `ChangeRowColor_dark.uss` and `ChangeRowColor_light.uss` in the same `Assets/Editor/ChangeRowColor` folder. 
        ///3. Save the `Enemy.cs` script outside of an `Editor` folder, because MonoBehaviour scripts in an `Editor` folder can't be attached to GameObjects.
        ///4. Add the `Enemy` component to a GameObject. The background color of the row of that GameObject changes in the Hierarchy window.
        ///
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/ChangeRowColor/ChangeRowColor.cs"/>
        /// </example>
        /// <example>
        /// The following example shows how to style `ChangeRowColor_dark.uss`.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/ChangeRowColor/ChangeRowColor_dark.uss"/>
        /// </example>
        /// <example>
        /// The following example shows how to style `ChangeRowColor_light.uss`.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Editor/ChangeRowColor/ChangeRowColor_light.uss"/>
        /// </example>
        /// <example>
        /// The following example shows the `Enemy` component that the ChangeRowColor example uses.
        /// <code source="../../../Tests/EditModeAndPlayModeTests/HierarchySamples/Assets/Runtime/Enemy.cs"/>
        /// </example>
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
        internal float IndentWidth => m_LeftContainer?.style.translate.value.x.value ?? 0f;
        internal float IndentOffset => ToggleWidth + k_OverrideBarWidth;
        internal float ToggleWidth => m_Toggle?.layout.width ?? 0f;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeReloadSafety", "UAL0015:Auto cleaned up symbol assigned by constructor", Justification = "This is a visual element that is recreated on code reload")]
        internal HierarchyViewItem()
        {
            // Setup the root. This is taken from 'TreeView.MakeTreeItem'
            SetName(k_UnityTreeViewItem);
            style.flexDirection = FlexDirection.Row;

            m_OnBeginRename = OnBeginRename;
            m_OnEndRename = OnEndRename;

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
            m_Toggle.Q(className: k_UnityToggleCheckmark.value).style.marginTop = 0;
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

            //Only refresh styling and bind elements when they are attached to a panel, otherwise some styling properties are not defined
            if (panel == null)
                return;

            // Setup object
            m_Node = node;
            m_Handler = view.ViewModel.GetNodeTypeHandler(in node);
            m_View = view;

            // Setup styling
            var viewModel = m_View.ViewModel;
            var noFilter = !m_View.Filtering;
            var indentWidth = CalculateIndentWidth();
            var oldValue = m_LeftContainer.style.translate.value;
            m_LeftContainer.style.translate = new Translate(m_LeftContainer.CeilToPanelPixelSize(indentWidth), oldValue.y, oldValue.z);

            var showToggle = noFilter && viewModel.HasVisibleChildren(in m_Node);
            m_Toggle.EnableInClassList(k_HierarchyItemToggleHidden, !showToggle);

            var isExpanded = viewModel.HasFlags(in m_Node, HierarchyNodeFlags.Expanded);
            m_Toggle.SetValueWithoutNotify(showToggle && isExpanded);

            Icon.EnableInClassList(k_HierarchyItemIconCut, viewModel.HasFlags(in m_Node, HierarchyNodeFlags.Cut));

            // A null override means "use the node's raw name": stream the node's native UTF-8 name straight
            // into the text element without materializing a managed string. Handlers that need to decorate
            // the name return a non-null override instead.
            var overrideName = (m_Handler as IHierarchyEditorNodeTypeHandler)?.GetDisplayNameOverride(m_View, in m_Node);
            if (overrideName != null)
                m_Name.Text = overrideName;
            else if (m_View.Source.Exists(in m_Node))
                m_Name.Label.SetTextUtf8(m_View.Source.GetNameRaw(in m_Node));
            else
                m_Name.Text = string.Empty;

            // Setup handler-specific or user-defined styling
            m_View.InvokeBindViewItem(this);

            // Register events
            m_Name.OnBeginRename += m_OnBeginRename;
            m_Name.OnEndRename += m_OnEndRename;
        }

        internal void Unbind()
        {
            if (!Bound)
                return;

            // RowContainer is an expensive getter
            var rowContainer = RowContainer;
            if (rowContainer is not null && rowContainer.ClassListContains(HierarchyView.k_HierarchyPingBase))
            {
                // Two TransitionEndEvents need to be sent because the fade in and fade out of the ping effect are two different
                // transitions. Second event is ignored if these events are fired during the fade out.
                using (var firstTransition = TransitionEndEvent.GetPooled())
                {
                    firstTransition.target = rowContainer;
                    rowContainer.SendEvent(firstTransition, DispatchMode.Immediate);
                }

                using (var secondTransition = TransitionEndEvent.GetPooled())
                {
                    secondTransition.target = rowContainer;
                    rowContainer.SendEvent(secondTransition, DispatchMode.Immediate);
                }
            }

            // Reset handler-specific or user-defined styling
            m_Node = HierarchyNode.Null;
            m_View.InvokeUnbindViewItem(this);

            // Unregister events
            m_Name.OnBeginRename -= m_OnBeginRename;
            m_Name.OnEndRename -= m_OnEndRename;

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

            if (m_Handler is IHierarchyEditorNodeTypeHandler editorHandler)
            {
                if (!editorHandler.CanSetName(m_View, in m_Node))
                    return;

                var renameText = editorHandler.GetRenameTextOverride(m_View, in m_Node);
                if (renameText != null)
                {
                    m_Name.BeginRename(renameText);
                    return;
                }
            }

            m_Name.BeginRename();
        }

        internal float CalculateIndentWidth()
        {
            if (m_View.Filtering)
                return 0f;

            var viewModel = m_View.ViewModel;
            var depth = viewModel.GetDepth(in m_Node);

            // Apply relative depth if view model uses a custom root
            var viewModelRoot = viewModel.GetRoot();
            if (viewModelRoot != m_View.Source.Root)
                depth -= viewModel.GetDepth(viewModelRoot) + 1;

            return k_IndentWidth * depth;
        }

        void OnBeginRename()
        {
            m_View.SetRenamingItem(this);
        }

        void OnEndRename(string text, bool canceled)
        {
            m_View.SetRenamingItem(null, canceled);

            if (canceled)
                return;

            if (m_Node == HierarchyNode.Null || string.IsNullOrEmpty(text))
                return;

            if (m_Handler is IHierarchyEditorNodeTypeHandler editorHandler)
            {
                editorHandler.OnSetName(m_View, in m_Node, text);

                // The label was just set to the committed text, which carries none of the decorations the
                // handler adds, and a rejected or no-op rename asks for no re-bind that would restore them.
                var overrideName = editorHandler.GetDisplayNameOverride(m_View, in m_Node);
                if (overrideName != null)
                    m_Name.Text = overrideName;
            }
            else
            {
                m_View.Source.SetName(in m_Node, text);
            }
        }

        internal Rect GetRenameRect()
        {
            var renameRect = worldBound;
            renameRect.xMin = m_Name.worldBound.xMin;
            return renameRect;
        }
    }
}
