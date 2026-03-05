// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.HierarchyV2;

namespace Unity.Hierarchy
{
    /// <summary>
    /// UI element control that displays a <see cref="Hierarchy"/>.
    /// </summary>
    public sealed class HierarchyView : VisualElement, IDisposable
    {
        internal const int k_ItemHeight = 20;
        const string k_ListViewName = "unity-tree-view__list-view";
        const string k_HierarchyViewRootStyleName = "hierarchy";
        const string k_HierarchyViewStyleContainerStyleName = "hierarchy__container";
        const int k_RenamingDelayMs = 500;

        internal const string k_HierarchyPingBase = "hierarchy - item__ping-base";
        const string k_HierarchyPingRampIn_Style = "hierarchy-item__ping-ramp-in-style";
        const string k_HierarchyPingRampIn_Start = "hierarchy-item__ping-ramp-in-start";
        const string k_HierarchyPingRampOut_Style = "hierarchy-item__ping-ramp-out-style";
        const string k_HierarchyPingRampOut_Start = "hierarchy-item__ping-ramp-out-start";

        enum UpdateStage
        {
            // Data update stages
            UpdatingHierarchy,
            UpdatingHierarchyFlattened,
            UpdatingHierarchyViewModel,

            // Display update stages
            UpdatingListView,

            // Post update stages
            ExecutePostUpdateActions,

            // Utilities
            Count,
            First = UpdatingHierarchy,
            Last = ExecutePostUpdateActions
        }

        enum UpdateMode
        {
            Update,
            UpdateIncremental,
            UpdateIncrementalTimed
        }

        internal class TestHelper
        {
            public static int FirstUpdateStage => (int)UpdateStage.First;
            public static int LastUpdateStage => (int)UpdateStage.Last;
            public static int HierarchyUpdateStage => (int)UpdateStage.UpdatingHierarchy;
            public static int CurrentUpdateStage(HierarchyView view) => (int)view.m_UpdateStage;
            public static bool ViewUpdateNeeded(HierarchyView view) => view.UpdateNeeded;
            public static bool HierarchyUpdateNeeded(HierarchyView view) => view.m_Hierarchy.UpdateNeeded;
        }

        // Data
        Unity.Hierarchy.Hierarchy m_Hierarchy;
        HierarchyFlattened m_HierarchyFlattened;
        HierarchyViewModel m_HierarchyViewModel;
        int m_Version;

        // Data update state
        UpdateStage m_UpdateStage = UpdateStage.First;
        readonly Stopwatch m_UpdateTimer = new();
        readonly CircularBuffer<Action> m_PostUpdateActionQueue = new(16);

        // UX elements
        readonly CollectionView m_CollectionView;
        readonly MultiColumnLayoutConfiguration m_MultiColumnLayoutConfiguration;
        readonly HierarchyViewColumnName m_NameColumn;
        readonly HierarchyViewDragHandler m_DragHandler;
        readonly VisualElement m_ListViewScrollView;
        readonly HierarchyViewSelection m_Selection;

        // UX update state
        VisualElement m_StyleContainer;
        IVisualElementScheduledItem m_ScheduledItem;
        int m_LastMouseUpSelectionIndex;
        internal bool m_IsRenamingItem => m_RenamingItem != null;
        HierarchyViewItem m_RenamingItem;
        internal int m_RenameDelayMs;

        // Profiler markers
        readonly ProfilerMarker m_RefreshItemsProfilerMarker = new ProfilerMarker("HierarchyView.RefreshItems");
        readonly ProfilerMarker m_SetSelectionMarker = new ProfilerMarker("HierarchyView.SetSelection");

        /// <summary>
        /// Delegate type used to handle <see cref="SourceHierarchyChanging"/> event.
        /// </summary>
        /// <param name="oldHierarchy">The previous source <see cref="Hierarchy"/>.</param>
        /// <param name="newHierarchy">The current source <see cref="Hierarchy"/>.</param>
        /// <param name="defaultFlags">The default flags used to initialize new nodes.</param>
        public delegate void SourceHierarchyChangingEventHandler(Unity.Hierarchy.Hierarchy oldHierarchy, Unity.Hierarchy.Hierarchy newHierarchy, HierarchyNodeFlags defaultFlags);

        /// <summary>
        /// Raised when the source hierarchy is about to change.
        /// </summary>
        public event SourceHierarchyChangingEventHandler SourceHierarchyChanging;

        /// <summary>
        /// Delegate type used to handle <see cref="SourceHierarchyChanged"/> event.
        /// </summary>
        /// <param name="hierarchy">The current source <see cref="Hierarchy"/>.</param>
        /// <param name="defaultFlags">The default flags used to initialize new nodes.</param>
        public delegate void SourceHierarchyChangedEventHandler(Unity.Hierarchy.Hierarchy hierarchy, HierarchyNodeFlags defaultFlags);

        /// <summary>
        /// Raised when the source hierarchy has been changed.
        /// </summary>
        public event SourceHierarchyChangedEventHandler SourceHierarchyChanged;

        /// <summary>
        /// Raised when a <see cref="HierarchyViewItem"/> is bound to a <see cref="HierarchyView"/>, allowing customization of the view item.
        /// </summary>
        public event Action<HierarchyViewItem> BindViewItem;

        /// <summary>
        /// Raised when a <see cref="HierarchyViewItem"/> is unbound from a <see cref="HierarchyView"/>, allowing cleanup of the view item.
        /// </summary>
        public event Action<HierarchyViewItem> UnbindViewItem;

        /// <summary>
        /// Raised when flags on <see cref="HierarchyNode"/> instances are changed.
        /// </summary>
        public event HierarchyViewModel.FlagsChangedEventHandler FlagsChanged;

        /// <summary>
        /// Delegate type used to handle <see cref="PopulateContextMenu"/> event.
        /// </summary>
        /// <param name="item">The <see cref="HierarchyViewItem"/> the context is being created for. Can be null if the context menu is created from the background of the view.</param>
        /// <param name="menu">The <see cref="DropdownMenu"/> being populated.</param>
        public delegate void PopulateContextMenuEventHandler(HierarchyViewItem item, DropdownMenu menu);

        /// <summary>
        /// Raised when a right click is handled on a <see cref="HierarchyNode"/> or on the background of the view.
        /// </summary>
        /// <remarks>
        /// This callback receives the <see cref="HierarchyViewItem"/> to create the context menu for and the <see cref="DropdownMenu"/> to populate.
        /// If the user right clicks in empty space, the callback receives null for the view item.
        /// </remarks>
        public event PopulateContextMenuEventHandler PopulateContextMenu;

        /// <summary>
        /// Delegate type used to handle the event of getting a tooltip for a <see cref="HierarchyViewItem" />.
        /// </summary>
        /// <param name="item">The <see cref="HierarchyViewItem"/> for which the tooltip is being requested.</param>
        /// <param name="filtering">A boolean value indicating whether the hierarchy is currently being filtered.</param>
        /// <param name="tooltip">A <see cref="StringBuilder"/> object to which the tooltip text should be appended.</param>
        public delegate void GetTooltipEventHandler(HierarchyViewItem item, bool filtering, StringBuilder tooltip);

        /// <summary>
        /// Customize the tooltip displayed when the mouse hovers the node name label.
        /// </summary>
        /// <remarks>
        /// This callback receives the <see cref="HierarchyViewItem"/> to get the tooltip for the StringBuilder to build the tooltip, and to determine whether the <see cref="HierarchyView"/> is being filtered.
        /// </remarks>
        public event GetTooltipEventHandler GetTooltip;

        /// <summary>
        /// Raised when the <see cref="HierarchyView"/> is initializing, typically allowing to load additional stylesheets and add styles to <see cref="StyleContainer"/>.
        /// Internal because it is only used by HierarchyWindow to allow to statically customize the <see cref="HierarchyView"/>.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.HierarchyModule")]
        internal event Action Bind; // Internal because it is only used by HierarchyWindow to allow to statically customize the HierarchyView.

        /// <summary>
        /// Gets the source hierarchy used to populate the <see cref="HierarchyView"/>.
        /// Use <see cref="SetSourceHierarchy"/> to change it.
        /// </summary>
        /// <remarks>
        /// The user is responsible for disposing the <see cref="Hierarchy"/>.
        /// </remarks>
        public Unity.Hierarchy.Hierarchy Source => m_Hierarchy;

        /// <summary>
        /// Gets the underlying <see cref="HierarchyFlattened"/> of this <see cref="HierarchyView"/>.
        /// </summary>
        public HierarchyFlattened Flattened => m_HierarchyFlattened;

        /// <summary>
        /// Gets the underlying <see cref="HierarchyViewModel"/> of this <see cref="HierarchyView"/>.
        /// </summary>
        public HierarchyViewModel ViewModel => m_HierarchyViewModel;

        /// <summary>
        /// The <see cref="CollectionView"/> used to display the hierarchy.
        /// </summary>
        internal CollectionView ListView
        {
            [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
            get => m_CollectionView;
        }

        /// <summary>
        /// Gets the <see cref="VisualElement"/> used as the container for the styles and stylesheets of the <see cref="HierarchyView"/>.
        /// </summary>
        public VisualElement StyleContainer => m_StyleContainer;

        /// <summary>
        /// Gets or sets the filter used to display the <see cref="Hierarchy"/>.
        /// </summary>
        public string Filter
        {
            get => m_HierarchyViewModel.Query.ToString();
            set => m_HierarchyViewModel.SetQuery(value);
        }

        /// <summary>
        /// Whether the <see cref="HierarchyView"/> is currently filtering nodes.
        /// </summary>
        public bool Filtering => m_HierarchyViewModel.Filtering;

        /// <summary>
        /// Whether the <see cref="HierarchyView"/> is currently updating.
        /// </summary>
        public bool Updating
        {
            get
            {
                if (m_Hierarchy == null || !m_Hierarchy.IsCreated)
                    return false;

                return m_UpdateStage != UpdateStage.First || m_Hierarchy.Updating || m_HierarchyFlattened.Updating || m_HierarchyViewModel.Updating;
            }
        }

        /// <summary>
        /// Whether the <see cref="HierarchyView"/> requires an update.
        /// </summary>
        public bool UpdateNeeded
        {
            get
            {
                if (m_Hierarchy == null || !m_Hierarchy.IsCreated)
                    return false;

                return Updating || DataUpdateNeeded || DisplayUpdateNeeded || ExecutePostUpdateActionsNeeded;
            }
        }

        /// <summary>
        /// The current progress of the hierarchy view update.
        /// </summary>
        public float UpdateProgress
        {
            get
            {
                if (!Updating)
                    return 100f;
                if (m_UpdateStage == UpdateStage.UpdatingHierarchyViewModel)
                    return m_HierarchyViewModel.UpdateProgress;
                return 0f;
            }
        }

        /// <summary>
        /// Gets the <see cref="MultiColumnLayoutConfiguration"/> used to configure the columns and layout of the hierarchy view.
        /// </summary>
        internal MultiColumnLayoutConfiguration ListViewLayoutConfiguration => m_MultiColumnLayoutConfiguration;

        internal bool DataUpdateNeeded => m_Hierarchy.UpdateNeeded || m_HierarchyFlattened.UpdateNeeded || m_HierarchyViewModel.UpdateNeeded;
        internal bool DisplayUpdateNeeded => m_Version != m_HierarchyViewModel.Version;
        internal bool ExecutePostUpdateActionsNeeded => m_PostUpdateActionQueue.Count > 0;
        internal HierarchyViewDragHandler DragHandler => m_DragHandler;

        internal HierarchyViewColumnName NameColumn
        {
            [VisibleToOtherModules]
            get => m_NameColumn;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="HierarchyView"/>.
        /// </summary>
        public HierarchyView()
        {
            HierarchyLogging.Log($"HierarchyView({GetHashCode():X}).New()");

            // UX elements
            AddToClassList(k_HierarchyViewRootStyleName);
            this.AddManipulator(new ContextualMenuManipulator(InvokePopulateContextMenu));

            m_MultiColumnLayoutConfiguration = new()
            {
                columns = { stretchMode = Columns.StretchMode.Grow }
            };
            m_Selection = new HierarchyViewSelection();
            m_CollectionView = new CollectionView(m_Selection)
            {
                name = k_ListViewName,
                fixedItemHeight = k_ItemHeight,
                selectionType = SelectionType.Multiple,
                reorderMode = ListViewReorderMode.Simple,
                reorderable = true,
                itemsSource = null
            };

            m_NameColumn = new HierarchyViewColumnName(this);
            m_DragHandler = new HierarchyViewDragHandler(this);

            m_CollectionView.AddToClassList(k_ListViewName);
            m_CollectionView.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            m_CollectionView.RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
            m_CollectionView.scrollView.RegisterCallback<PointerDownEvent>(OnListViewPointerDown);
            m_MultiColumnLayoutConfiguration.columns.Add(m_NameColumn);
            m_NameColumn.stretchable = true;
            m_NameColumn.OnBindItem += OnBindItem;
            m_NameColumn.OnUnbindItem += OnUnbindItem;

            m_CollectionView.layoutConfiguration = m_MultiColumnLayoutConfiguration;

            m_ListViewScrollView = m_CollectionView.scrollView;
            m_ListViewScrollView.RegisterCallback<ClickEvent>(OnClickEvent);
            m_ListViewScrollView.RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);

            // UX update state
            m_StyleContainer = new();
            m_StyleContainer.AddToClassList(k_HierarchyViewStyleContainerStyleName);
            m_StyleContainer.Add(m_CollectionView);
            this.Add(m_StyleContainer);

            m_LastMouseUpSelectionIndex = -1;
            SetRenamingItem(null);
            m_RenameDelayMs = k_RenamingDelayMs;
        }

        /// <summary>
        /// Releases the resources used by the <see cref="HierarchyView"/>.
        /// </summary>
        public void Dispose()
        {
            SetSourceHierarchy(null);

            BindViewItem = null;
            UnbindViewItem = null;
            PopulateContextMenu = null;
            GetTooltip = null;
        }

        /// <summary>
        /// Sets the source hierarchy used to populate the <see cref="HierarchyView"/>.
        /// </summary>
        /// <param name="hierarchy">The <see cref="Hierarchy"/> to set as the source.</param>
        /// <param name="defaultFlags">The default flags used to initialize new nodes.</param>
        public void SetSourceHierarchy(Unity.Hierarchy.Hierarchy hierarchy, HierarchyNodeFlags defaultFlags = HierarchyNodeFlags.None)
        {
            if (m_Hierarchy == hierarchy)
                return;

            HierarchyLogging.Log($"HierarchyView({GetHashCode():X}).SetSourceHierarchy(hierarchy={hierarchy?.GetHashCode():X}, flags={defaultFlags})");

            // Unregister events
            if (m_Hierarchy != null)
                m_Hierarchy.HandlerCreated -= OnHandlerCreated;
            if (m_HierarchyViewModel != null)
                m_HierarchyViewModel.FlagsChanged -= OnViewModelFlagsChanged;
            if (m_CollectionView != null)
                m_CollectionView.BeforeRefreshingItems -= UpdateData;

            // Invoke source hierarchy changing
            SourceHierarchyChanging?.Invoke(m_Hierarchy, hierarchy, defaultFlags);

            // Clear columns before releasing UX
            ClearColumns();

            // Reset styling
            Reset();

            // Reset UX update state
            SetRenamingItem(null);
            m_LastMouseUpSelectionIndex = -1;
            m_ScheduledItem = null;

            // Reset UX elements
            m_CollectionView.itemsSource = null;

            // Reset Data update state
            m_PostUpdateActionQueue.Clear();
            m_UpdateStage = UpdateStage.First;

            // Reset Data
            m_Version = 0;
            if (m_HierarchyViewModel != null)
            {
                if (m_HierarchyViewModel.IsCreated)
                    m_HierarchyViewModel.Dispose();
                m_HierarchyViewModel = null;
            }
            if (m_HierarchyFlattened != null)
            {
                if (m_HierarchyFlattened.IsCreated)
                    m_HierarchyFlattened.Dispose();
                m_HierarchyFlattened = null;
            }
            m_Hierarchy = null; // User is responsible for disposing the hierarchy

            // If setting to null, we're done
            if (hierarchy == null)
                return;

            // Set the new hierarchy source
            m_Hierarchy = hierarchy;
            m_HierarchyFlattened = new HierarchyFlattened(m_Hierarchy);
            m_HierarchyViewModel = new HierarchyViewModel(m_HierarchyFlattened, defaultFlags);

            // Force update data to ensure list view reads valid data when we set the items source
            UpdateData();

            // Update the list view items source
            m_CollectionView.itemsSource = m_HierarchyViewModel.AsReadOnlyList();

            // Update view model content for selection container.
            m_Selection.SetSourceViewModel(m_HierarchyViewModel);

            // Update other UX elements
            BindColumns();
            Initialize();

            // Invoke source hierarchy changed
            SourceHierarchyChanged?.Invoke(hierarchy, defaultFlags);

            // Register events
            m_Hierarchy.HandlerCreated += OnHandlerCreated;
            m_HierarchyViewModel.FlagsChanged += OnViewModelFlagsChanged;

            // Subscribe to BeforeRefreshingItems and calling UpdateData to make sure the data
            // of Hierarchy, Flattened and ViewModel, alongside all handlers state are up to date for the collection view
            // to iterate over it.
            // We had a case where a node was removed from the GameObjectHandler mapping but the node was
            // still in the hierarchy because the command list was not done being processed.
            m_CollectionView.BeforeRefreshingItems += UpdateData;
        }

        /// <summary>
        /// Updates the <see cref="HierarchyView"/> displayed content.
        /// </summary>
        public void Update()
        {
            HierarchyLogging.Log($"HierarchyView({GetHashCode():X}).Update()");
            while (DoUpdate(UpdateMode.Update)) { }
        }

        /// <summary>
        /// Incrementally updates the <see cref="HierarchyView"/> displayed content.
        /// </summary>
        public bool UpdateIncremental()
        {
            HierarchyLogging.Log($"HierarchyView({GetHashCode():X}).UpdateIncremental()");
            return DoUpdate(UpdateMode.UpdateIncremental);
        }

        /// <summary>
        /// Incrementally updates the <see cref="HierarchyView"/> displayed content until the time limit is reached.
        /// </summary>
        /// <param name="milliseconds">The maximum time in milliseconds to spend updating the <see cref="HierarchyView"/>.</param>
        /// <returns><see langword="true"/> if additional invocations are needed to complete the update, <see langword="false"/> otherwise.</returns>
        public bool UpdateIncrementalTimed(double milliseconds)
        {
            HierarchyLogging.Log($"HierarchyView({GetHashCode():X}).UpdateIncrementalTimed(milliseconds={milliseconds})");
            while (true)
            {
                m_UpdateTimer.Restart();
                if (!DoUpdate(UpdateMode.UpdateIncrementalTimed, milliseconds))
                    return false; // Update completed

                milliseconds -= m_UpdateTimer.ElapsedMillisecondsPrecise();
                if (milliseconds <= 0.0)
                    return true; // Timed out
            }
        }

        /// <summary>
        /// Selects the specified <see cref="HierarchyNode"/>.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to select.</param>
        public void Select(in HierarchyNode node)
        {
            m_HierarchyViewModel.SetFlags(in node, HierarchyNodeFlags.Selected);
            Update();
        }

        /// <summary>
        /// Selects the specified <see cref="HierarchyNode"/>s.
        /// </summary>
        /// <param name="nodes">The <see cref="HierarchyNode"/>s to select.</param>
        public void Select(ReadOnlySpan<HierarchyNode> nodes)
        {
            m_HierarchyViewModel.SetFlags(nodes, HierarchyNodeFlags.Selected);
            Update();
        }

        /// <summary>
        /// Selects the specified node and all ancestors or descendants recursively.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to select.</param>
        /// <param name="direction">The direction of traversal.</param>
        public void SelectRecursive(in HierarchyNode node, HierarchyTraversalDirection direction = HierarchyTraversalDirection.Children)
        {
            m_HierarchyViewModel.SetFlagsRecursive(in node, HierarchyNodeFlags.Selected, direction);
            Update();
        }

        /// <summary>
        /// Selects the specified nodes and all their ancestors or descendants recursively.
        /// </summary>
        /// <param name="nodes">The <see cref="HierarchyNode"/>s to select.</param>
        /// <param name="direction">The direction of traversal.</param>
        public void SelectRecursive(ReadOnlySpan<HierarchyNode> nodes, HierarchyTraversalDirection direction = HierarchyTraversalDirection.Children)
        {
            m_HierarchyViewModel.SetFlagsRecursive(nodes, HierarchyNodeFlags.Selected, direction);
            Update();
        }

        /// <summary>
        /// Selects all nodes in the hierarchy.
        /// </summary>
        /// <param name="exposedOnly">
        /// When <see langword="true"/>, selects only exposed nodes (excludes hidden or unreachable nodes).
        /// When <see langword="false"/>, selects all nodes regardless of exposure state.
        /// <b>Note:</b> Nodes outside the viewport are still selected if they are exposed.
        /// </param>
        public void SelectAll(bool exposedOnly)
        {
            if (exposedOnly)
                m_HierarchyViewModel.SetFlags(m_HierarchyViewModel.AsReadOnlySpan(), HierarchyNodeFlags.Selected);
            else
                m_HierarchyViewModel.SetFlags(HierarchyNodeFlags.Selected);
            Update();
        }

        /// <summary>
        /// Sets the current selection to a single node, making all other nodes unselected.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to set as the selection.</param>
        public void SetSelection(in HierarchyNode node)
        {
            using (var _ = new HierarchyViewModelFlagsChangeScope(m_HierarchyViewModel))
            {
                m_HierarchyViewModel.ClearFlags(HierarchyNodeFlags.Selected);
                m_HierarchyViewModel.SetFlags(in node, HierarchyNodeFlags.Selected);
            }
            Update();
        }

        /// <summary>
        /// Sets the current selection to the specified nodes, making all other nodes unselected.
        /// </summary>
        /// <param name="nodes">The <see cref="HierarchyNode"/>s to set as the selection.</param>
        public void SetSelection(ReadOnlySpan<HierarchyNode> nodes)
        {
            using (var _ = new HierarchyViewModelFlagsChangeScope(m_HierarchyViewModel))
            {
                m_HierarchyViewModel.ClearFlags(HierarchyNodeFlags.Selected);
                m_HierarchyViewModel.SetFlags(nodes, HierarchyNodeFlags.Selected);
            }
            Update();
        }

        /// <summary>
        /// Determines if the specified node is selected.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to check if it is selected.</param>
        /// <returns><see langword="true"/> if the specified node is selected; <see langword="false"/> otherwise.</returns>
        public bool IsSelected(in HierarchyNode node)
        {
            return m_HierarchyViewModel.HasFlags(in node, HierarchyNodeFlags.Selected);
        }

        /// <summary>
        /// Determines if the specified node, or any of its ancestors, is selected.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to check if it is selected or any of its ancestors is selected.</param>
        public bool IsSelectedOrAnyAncestorSelected(in HierarchyNode node)
        {
            // todo: reimplement in native
            var currentNode = node;
            while (true)
            {
                if (currentNode == m_Hierarchy.Root)
                    return false;

                if (IsSelected(in currentNode))
                    return true;

                currentNode = m_HierarchyViewModel.GetParent(in currentNode);
            }
        }

        /// <summary>
        /// Toggles the selection state of the specified node.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to toggle the selection state of.</param>
        public void ToggleSelected(in HierarchyNode node)
        {
            m_HierarchyViewModel.ToggleFlags(in node, HierarchyNodeFlags.Selected);
            Update();
        }

        /// <summary>
        /// Toggles the selection state of the specified nodes.
        /// </summary>
        /// <param name="nodes">The <see cref="HierarchyNode"/>s to toggle the selection state of.</param>
        public void ToggleSelected(ReadOnlySpan<HierarchyNode> nodes)
        {
            m_HierarchyViewModel.ToggleFlags(nodes, HierarchyNodeFlags.Selected);
            Update();
        }

        /// <summary>
        /// Toggles the selection state of the specified node and all its ancestors or descendants recursively.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to toggle the selection state of.</param>
        /// <param name="direction">The direction of traversal.</param>
        public void ToggleSelectedRecursive(in HierarchyNode node, HierarchyTraversalDirection direction = HierarchyTraversalDirection.Children)
        {
            m_HierarchyViewModel.ToggleFlagsRecursive(in node, HierarchyNodeFlags.Selected, direction);
            Update();
        }

        /// <summary>
        /// Toggles the selection state of the specified nodes and all their ancestors or descendants recursively.
        /// </summary>
        /// <param name="nodes">The <see cref="HierarchyNode"/>s to toggle the selection state of.</param>
        /// <param name="direction">The direction of traversal.</param>
        public void ToggleSelectedRecursive(ReadOnlySpan<HierarchyNode> nodes, HierarchyTraversalDirection direction = HierarchyTraversalDirection.Children)
        {
            m_HierarchyViewModel.ToggleFlagsRecursive(nodes, HierarchyNodeFlags.Selected, direction);
            Update();
        }

        /// <summary>
        /// Toggles the selection state of the current selection.
        /// </summary>
        public void ToggleSelection()
        {
            m_HierarchyViewModel.ToggleFlags(HierarchyNodeFlags.Selected);
            Update();
        }

        /// <summary>
        /// Clears the selection state of the specified node.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to clear the selection state of.</param>
        public void Deselect(in HierarchyNode node)
        {
            m_HierarchyViewModel.ClearFlags(in node, HierarchyNodeFlags.Selected);
            Update();
        }

        /// <summary>
        /// Clears the selection state of the specified nodes.
        /// </summary>
        /// <param name="nodes">The <see cref="HierarchyNode"/>s to clear the selection state of.</param>
        public void Deselect(ReadOnlySpan<HierarchyNode> nodes)
        {
            m_HierarchyViewModel.ClearFlags(nodes, HierarchyNodeFlags.Selected);
            Update();
        }

        /// <summary>
        /// Clears the selection state of the specified node and all its ancestors or descendants recursively.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to clear the selection state of.</param>
        /// <param name="direction">The direction of traversal.</param>
        public void DeselectRecursive(in HierarchyNode node, HierarchyTraversalDirection direction = HierarchyTraversalDirection.Children)
        {
            m_HierarchyViewModel.ClearFlagsRecursive(in node, HierarchyNodeFlags.Selected, direction);
            Update();
        }

        /// <summary>
        /// Clears the selection state of the specified nodes and all their ancestors or descendants recursively.
        /// </summary>
        /// <param name="nodes">The <see cref="HierarchyNode"/>s to clear the selection state of.</param>
        /// <param name="direction">The direction of traversal.</param>
        public void DeselectRecursive(ReadOnlySpan<HierarchyNode> nodes, HierarchyTraversalDirection direction = HierarchyTraversalDirection.Children)
        {
            m_HierarchyViewModel.ClearFlagsRecursive(nodes, HierarchyNodeFlags.Selected, direction);
            Update();
        }

        /// <summary>
        /// Clears the current selection, making all nodes unselected.
        /// </summary>
        public void DeselectAll()
        {
            m_HierarchyViewModel.ClearFlags(HierarchyNodeFlags.Selected);
            Update();
        }

        /// <summary>
        /// Expands the specified node.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to expand.</param>
        public void Expand(in HierarchyNode node)
        {
            m_HierarchyViewModel.SetFlags(in node, HierarchyNodeFlags.Expanded);
            Update();
        }

        /// <summary>
        /// Expands the specified nodes.
        /// </summary>
        /// <param name="nodes">The <see cref="HierarchyNode"/>s to expand.</param>
        public void Expand(ReadOnlySpan<HierarchyNode> nodes)
        {
            m_HierarchyViewModel.SetFlags(nodes, HierarchyNodeFlags.Expanded);
            Update();
        }

        /// <summary>
        /// Expands the specified node and all its ancestors or descendants recursively.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to expand.</param>
        /// <param name="direction">The direction of traversal.</param>
        public void ExpandRecursive(in HierarchyNode node, HierarchyTraversalDirection direction = HierarchyTraversalDirection.Children)
        {
            m_HierarchyViewModel.SetFlagsRecursive(in node, HierarchyNodeFlags.Expanded, direction);
            Update();
        }

        /// <summary>
        /// Expands the specified nodes and all their ancestors or descendants recursively.
        /// </summary>
        /// <param name="nodes">The <see cref="HierarchyNode"/>s to expand.</param>
        /// <param name="direction">The direction of traversal.</param>
        public void ExpandRecursive(ReadOnlySpan<HierarchyNode> nodes, HierarchyTraversalDirection direction = HierarchyTraversalDirection.Children)
        {
            m_HierarchyViewModel.SetFlagsRecursive(nodes, HierarchyNodeFlags.Expanded, direction);
            Update();
        }

        /// <summary>
        /// Expands all <see cref="HierarchyNode"/>s in the <see cref="Hierarchy"/>.
        /// </summary>
        public void ExpandAll()
        {
            m_HierarchyViewModel.SetFlags(HierarchyNodeFlags.Expanded);
            Update();
        }

        /// <summary>
        /// Determines if the specified node is expanded.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to check if it is expanded.</param>
        /// <returns><see langword="true"/> if the specified node is expanded, <see langword="false"/> otherwise.</returns>
        public bool IsExpanded(in HierarchyNode node)
        {
            return m_HierarchyViewModel.HasFlags(in node, HierarchyNodeFlags.Expanded);
        }

        /// <summary>
        /// Collapses the specified node.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to collapse.</param>
        public void Collapse(in HierarchyNode node)
        {
            m_HierarchyViewModel.ClearFlags(in node, HierarchyNodeFlags.Expanded);
            Update();
        }

        /// <summary>
        /// Collapses the specified nodes.
        /// </summary>
        /// <param name="nodes">The <see cref="HierarchyNode"/>s to collapse.</param>
        public void Collapse(ReadOnlySpan<HierarchyNode> nodes)
        {
            m_HierarchyViewModel.ClearFlags(nodes, HierarchyNodeFlags.Expanded);
            Update();
        }

        /// <summary>
        /// Collapses the specified node and all its ancestors or descendants recursively.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to collapse.</param>
        /// <param name="direction">The direction of traversal.</param>
        public void CollapseRecursive(in HierarchyNode node, HierarchyTraversalDirection direction = HierarchyTraversalDirection.Children)
        {
            m_HierarchyViewModel.ClearFlagsRecursive(in node, HierarchyNodeFlags.Expanded, direction);
            Update();
        }

        /// <summary>
        /// Collapses the specified nodes and all their ancestors or descendants recursively.
        /// </summary>
        /// <param name="nodes">The <see cref="HierarchyNode"/>s to collapse.</param>
        /// <param name="direction">The direction of traversal.</param>
        public void CollapseRecursive(ReadOnlySpan<HierarchyNode> nodes, HierarchyTraversalDirection direction = HierarchyTraversalDirection.Children)
        {
            m_HierarchyViewModel.ClearFlagsRecursive(nodes, HierarchyNodeFlags.Expanded, direction);
            Update();
        }

        /// <summary>
        /// Collapses all <see cref="HierarchyNode"/>s in the <see cref="Hierarchy"/>.
        /// </summary>
        public void CollapseAll()
        {
            m_HierarchyViewModel.ClearFlags(HierarchyNodeFlags.Expanded);
            Update();
        }

        /// <summary>
        /// Determines if the specified node is collapsed.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to check if it is collapsed.</param>
        /// <returns><see langword="true"/> if the specified node is collapsed, <see langword="false"/> otherwise.</returns>
        public bool IsCollapsed(in HierarchyNode node)
        {
            return m_HierarchyViewModel.DoesNotHaveFlags(in node, HierarchyNodeFlags.Expanded);
        }

        /// <summary>
        /// Shows the specified node.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to show.</param>
        public void Show(in HierarchyNode node)
        {
            m_HierarchyViewModel.ClearFlags(in node, HierarchyNodeFlags.Hidden);
            Update();
        }

        /// <summary>
        /// Shows the specified nodes.
        /// </summary>
        /// <param name="nodes">The <see cref="HierarchyNode"/>s to show.</param>
        public void Show(ReadOnlySpan<HierarchyNode> nodes)
        {
            m_HierarchyViewModel.ClearFlags(nodes, HierarchyNodeFlags.Hidden);
            Update();
        }

        /// <summary>
        /// Shows the specified node and all its ancestors or descendants recursively.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to show.</param>
        /// <param name="direction">The direction of traversal.</param>
        public void ShowRecursive(in HierarchyNode node, HierarchyTraversalDirection direction = HierarchyTraversalDirection.Children)
        {
            m_HierarchyViewModel.ClearFlagsRecursive(in node, HierarchyNodeFlags.Hidden, direction);
            Update();
        }

        /// <summary>
        /// Shows the specified nodes and all their ancestors or descendants recursively.
        /// </summary>
        /// <param name="nodes">The <see cref="HierarchyNode"/>s to show.</param>
        /// <param name="direction">The direction of traversal.</param>
        public void ShowRecursive(ReadOnlySpan<HierarchyNode> nodes, HierarchyTraversalDirection direction = HierarchyTraversalDirection.Children)
        {
            m_HierarchyViewModel.ClearFlagsRecursive(nodes, HierarchyNodeFlags.Hidden, direction);
            Update();
        }

        /// <summary>
        /// Shows all <see cref="HierarchyNode"/>s in the <see cref="Hierarchy"/>.
        /// </summary>
        public void ShowAll()
        {
            m_HierarchyViewModel.ClearFlags(HierarchyNodeFlags.Hidden);
            Update();
        }

        /// <summary>
        /// Determines if the specified node is shown.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to check if it is shown.</param>
        /// <returns><see langword="true"/> if the specified node is shown, <see langword="false"/> otherwise.</returns>
        public bool IsShown(in HierarchyNode node)
        {
            return m_HierarchyViewModel.DoesNotHaveFlags(in node, HierarchyNodeFlags.Hidden);
        }

        /// <summary>
        /// Hides the specified node.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to hide.</param>
        public void Hide(in HierarchyNode node)
        {
            m_HierarchyViewModel.SetFlags(in node, HierarchyNodeFlags.Hidden);
            Update();
        }

        /// <summary>
        /// Hides the specified nodes.
        /// </summary>
        /// <param name="nodes">The <see cref="HierarchyNode"/>s to hide.</param>
        public void Hide(ReadOnlySpan<HierarchyNode> nodes)
        {
            m_HierarchyViewModel.SetFlags(nodes, HierarchyNodeFlags.Hidden);
            Update();
        }

        /// <summary>
        /// Hides the specified node and all its ancestors or descendants recursively.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to hide.</param>
        /// <param name="direction">The direction of traversal.</param>
        public void HideRecursive(in HierarchyNode node, HierarchyTraversalDirection direction = HierarchyTraversalDirection.Children)
        {
            m_HierarchyViewModel.SetFlagsRecursive(in node, HierarchyNodeFlags.Hidden, direction);
            Update();
        }

        /// <summary>
        /// Hides the specified nodes and all their ancestors or descendants recursively.
        /// </summary>
        /// <param name="nodes">The <see cref="HierarchyNode"/>s to hide.</param>
        /// <param name="direction">The direction of traversal.</param>
        public void HideRecursive(ReadOnlySpan<HierarchyNode> nodes, HierarchyTraversalDirection direction = HierarchyTraversalDirection.Children)
        {
            m_HierarchyViewModel.SetFlagsRecursive(nodes, HierarchyNodeFlags.Hidden, direction);
            Update();
        }

        /// <summary>
        /// Hides all <see cref="HierarchyNode"/>s in the <see cref="Hierarchy"/>.
        /// </summary>
        public void HideAll()
        {
            m_HierarchyViewModel.SetFlags(HierarchyNodeFlags.Hidden);
            Update();
        }

        /// <summary>
        /// Determines if the specified node is hidden.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to check if it is hidden.</param>
        /// <returns><see langword="true"/> if the specified node is hidden, <see langword="false"/> otherwise.</returns>
        public bool IsHidden(in HierarchyNode node)
        {
            return m_HierarchyViewModel.HasFlags(in node, HierarchyNodeFlags.Hidden);
        }

        /// <summary>
        /// Frames the specified node, expanding its ancestors and scrolling to it.
        /// </summary>
        /// <param name="node">The <see cref="HierarchyNode"/> to frame.</param>
        public void Frame(in HierarchyNode node)
        {
            HierarchyLogging.Log($"HierarchyView({GetHashCode():X}).FrameNode({node})");
            if (node == HierarchyNode.Null || node == m_Hierarchy.Root)
                return;

            ExpandParents(in node);
            m_HierarchyViewModel.Update();
            UpdateListView();
            ScrollToNode(in node);
        }

        /// <summary>
        /// Frames the specified nodes, expanding their ancestors and scrolling to the first node.
        /// </summary>
        /// <param name="nodes">The <see cref="HierarchyNode"/>s to frame.</param>
        public void Frame(ReadOnlySpan<HierarchyNode> nodes)
        {
            HierarchyLogging.Log($"HierarchyView({GetHashCode():X}).FrameNodes(nodes={HierarchyLogging.ToString(nodes)})");
            if (nodes.Length == 0)
                return;

            ExpandParents(nodes);
            m_HierarchyViewModel.Update();
            UpdateListView();
            ScrollToNode(in nodes[0]);
        }

        /// <summary>
        /// Creates columns according to a list of columns specification and a potential <see cref="HierarchyViewState"/>.
        /// </summary>
        /// <param name="columns">The list of <see cref="HierarchyViewColumn"/> specifications.</param>
        /// <param name="state">Optional <see cref="HierarchyViewState"/> used to override the default properties of columns such as visibility, width, and order.</param>
        public void SetColumns(List<Column> columns, HierarchyViewState state = null)
        {
            if (state != null && state.Columns != null && state.Columns.Length > 0)
            {
                foreach (var colState in state.Columns)
                {
                    var column = HierarchyViewColumnUtility.GetColumnWithId(columns, colState.ColumnId);
                    if (column == null)
                        continue;
                    column.visible = colState.Visible;
                    HierarchyViewColumn.SetWidth(column, colState.Width);
                }

                columns.Sort((c1, c2) =>
                {
                    var p1 = HierarchyViewColumnUtility.GetVisibleIndex(state, c1);
                    var p2 = HierarchyViewColumnUtility.GetVisibleIndex(state, c2);
                    return p1 - p2;
                });
            }
            else
            {
                foreach (var col in columns)
                {
                    if (col is HierarchyViewColumn hc)
                    {
                        hc.ApplyDefaultColumnProperties();
                    }
                    else if (col is HierarchyViewColumnName viewItemCol)
                    {
                        viewItemCol.ApplyDefaultColumnProperties();
                    }
                }
            }

            // Note: There is no way to set all columns at the same time in the ListView.
            m_MultiColumnLayoutConfiguration.columns.Clear();
            foreach (var col in columns)
            {
                m_MultiColumnLayoutConfiguration.columns.Add(col);
            }

            // Bind columns
            BindColumns();
        }

        /// <summary>
        /// Creates a set of columns from a list of columns and cell descriptors. If a <see cref="HierarchyViewState"/> is passed, all the
        /// columns default order, width and visibility will be overridden by the viewState.
        /// </summary>
        /// <param name="columnDescriptors">The list of <see cref="HierarchyViewColumnDescriptor"/>s used to create the <see cref="HierarchyViewColumn"/>s.</param>
        /// <param name="cellDescriptors">The list of <see cref="HierarchyViewCellDescriptor"/>s used to create the <see cref="HierarchyViewCell"/>s within the <see cref="HierarchyViewColumn"/>s.</param>
        /// <param name="state">Optional <see cref="HierarchyViewState"/> used to override the default properties of columns, such as visibility, width, and order.</param>
        public void SetColumnDescriptors(IEnumerable<HierarchyViewColumnDescriptor> columnDescriptors, IEnumerable<HierarchyViewCellDescriptor> cellDescriptors, HierarchyViewState state = null)
        {
            var columns = new List<Column>
            {
                NameColumn
            };

            foreach (var colDesc in columnDescriptors)
            {
                var column = new HierarchyViewColumn(this, colDesc);
                foreach (var cellDesc in cellDescriptors)
                {
                    if (cellDesc.ValidForColumn(colDesc))
                    {
                        column.AddCell(cellDesc);
                    }
                }
                columns.Add(column);
            }

            columns.Sort((c1, c2) =>
            {
                var p1 = HierarchyViewColumnUtility.GetColumnDefaultPriority(c1);
                var p2 = HierarchyViewColumnUtility.GetColumnDefaultPriority(c2);
                return p1 - p2;
            });

            SetColumns(columns, state);
        }

        /// <summary>
        /// Updates the <see cref="HierarchyView"/> with a new ViewState.
        /// </summary>
        /// <param name="viewState">The <see cref="HierarchyViewState"/> to apply.</param>
        public void SetState(HierarchyViewState viewState)
        {
            HierarchyLogging.Log($"HierarchyView({GetHashCode():X}).SetState(state=...)");
            if ((viewState.ValidContent & (HierarchyViewState.Content.SearchText | HierarchyViewState.Content.Columns | HierarchyViewState.Content.ViewModelState)) != 0)
            {
                EnqueuePostUpdateAction(() =>
                {
                    if (viewState.ValidContent.HasFlag(HierarchyViewState.Content.Columns))
                        SetColumnState(viewState);

                    if (viewState.ValidContent.HasFlag(HierarchyViewState.Content.SearchText))
                        Filter = viewState.SearchText;

                    if (viewState.ValidContent.HasFlag(HierarchyViewState.Content.ViewModelState))
                        m_HierarchyViewModel.SetState(viewState.ViewModelState);
                });
            }

            if (viewState.ValidContent.HasFlag(HierarchyViewState.Content.ScrollPosition))
            {
                m_CollectionView.scrollView.verticalScroller.value = viewState.ScrollPositionY;
                m_CollectionView.scrollView.horizontalScroller.value = viewState.ScrollPositionX;
            }
        }

        /// <summary>
        /// Gets the current viewstate of the <see cref="HierarchyView"/>.
        /// </summary>
        /// <param name="content">Flags indicating what <see cref="HierarchyViewState"/> members you want to extract.</param>
        /// <returns>Returns the current view state (columns configurations and such) of the <see cref="HierarchyView"/>.</returns>
        public HierarchyViewState GetState(HierarchyViewState.Content content = HierarchyViewState.Content.All)
        {
            var windowState = new HierarchyViewState(content);
            if (windowState.ValidContent.HasFlag(HierarchyViewState.Content.ViewModelState))
            {
                windowState.ViewModelState = m_HierarchyViewModel.GetState();
            }

            if (windowState.ValidContent.HasFlag(HierarchyViewState.Content.SearchText))
            {
                windowState.SearchText = Filter;
            }

            if (windowState.ValidContent.HasFlag(HierarchyViewState.Content.ScrollPosition))
            {
                windowState.ScrollPositionX = m_CollectionView.scrollView.horizontalScroller.value;
                windowState.ScrollPositionY = m_CollectionView.scrollView.verticalScroller.value;
            }

            if (windowState.ValidContent.HasFlag(HierarchyViewState.Content.Columns))
            {
                windowState.Columns = new HierarchyViewColumnState[m_MultiColumnLayoutConfiguration.columns.Count];
                // Note: There is currently no way of knowing the column ordering (visibleIndex is internal): gather all columnHeader and use their name (which corresponds to ColumnId)
                // to know their index.
                var columnHeaders = m_CollectionView.Query<VisualElement>(null, "unity-multi-column-header__column").ToList();
                var i = 0;
                foreach (var column in m_MultiColumnLayoutConfiguration.columns)
                {
                    var columnId = HierarchyViewColumnUtility.GetColumnId(column);
                    var visibleIndex = columnHeaders.FindIndex(header => header.name == columnId);
                    windowState.Columns[i] = new HierarchyViewColumnState()
                    {
                        ColumnId = columnId,
                        Width = column.width.value,
                        Visible = column.visible,
                        Index = visibleIndex != -1 ? visibleIndex : i
                    };
                    ++i;
                }

                Array.Sort(windowState.Columns, (c1, c2) => c1.Index - c2.Index);
            }

            return windowState;
        }

        /// <summary>
        /// Initialize the view with styles and stylesheets provided by <see cref="HierarchyNodeTypeHandler.OnBindView(HierarchyView)"/>.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.HierarchyModule")]
        internal void Initialize()
        {
            HierarchyLogging.Log($"HierarchyView({GetHashCode():X}).{nameof(Initialize)}()");

            BindHandlers();
            try
            {
                Bind?.Invoke();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }

        /// <summary>
        /// Reset the view to its initial state.
        /// </summary>
        internal void Reset()
        {
            HierarchyLogging.Log($"HierarchyView({GetHashCode():X}).{nameof(Reset)}()");

            // Unbind handlers
            UnbindHandlers();

            // Remove then style container
            m_StyleContainer.Remove(m_CollectionView);
            m_StyleContainer.RemoveFromHierarchy();

            // Make a new style container
            m_StyleContainer = new VisualElement();
            m_StyleContainer.AddToClassList(k_HierarchyViewStyleContainerStyleName);
            m_StyleContainer.Add(m_CollectionView);
            Add(m_StyleContainer);
        }

        [VisibleToOtherModules("UnityEditor.HierarchyModule")]
        internal void EnqueuePostUpdateAction(Action action)
        {
            HierarchyLogging.Log($"HierarchyView({GetHashCode():X}).EnqueuePostUpdateAction(action=...)");
            if (m_PostUpdateActionQueue.Locked)
                throw new InvalidOperationException("Cannot enqueue post update action while processing post update actions.");

            m_PostUpdateActionQueue.PushBack(action);
        }

        [VisibleToOtherModules("UnityEditor.HierarchyModule")]
        internal void BeginRename(in HierarchyNode node)
        {
            HierarchyLogging.Log($"HierarchyView({GetHashCode():X}).BeginRename({node})");
            var index = m_HierarchyViewModel.IndexOf(in node);
            if (index < 0)
                return;

            var item = GetHierarchyViewItemFromIndex(index);
            if (item == null)
                return;

            item.BeginRename();
        }

        [VisibleToOtherModules("UnityEditor.HierarchyModule")]
        internal void OnLostFocus()
        {
            m_LastMouseUpSelectionIndex = -1;
        }

        internal int GetIndexFromLocalPosition(Vector2 pos)
        {
            return m_CollectionView.GetIndexFromPosition(pos);
        }

        internal int GetIndexFromWorldPosition(Vector2 worldPos, float offset = 0)
        {
            var offsetWorldPosition = new Vector3(worldPos.x, worldPos.y - offset, 0f);
            var localPosition = m_ListViewScrollView.WorldToLocal(offsetWorldPosition);
            return GetIndexFromLocalPosition(localPosition);
        }

        internal void InvokeBindViewItem(HierarchyViewItem item)
        {
            item.Handler?.Internal_BindItem(item);
            BindViewItem?.Invoke(item);
        }

        internal void InvokeUnbindViewItem(HierarchyViewItem item)
        {
            item.Handler?.Internal_UnbindItem(item);
            UnbindViewItem?.Invoke(item);
        }

        internal void InvokePopulateContextMenu(ContextualMenuPopulateEvent evt)
        {
            var hierarchyView = evt.target as HierarchyView;
            if (hierarchyView == null)
                return;

            if (m_IsRenamingItem)
            {
                var itemName = m_RenamingItem.Q<HierarchyViewItemName>();
                itemName?.CancelRename();
                SetRenamingItem(null);
            }

            evt.StopImmediatePropagation();

            var localposition = hierarchyView.ChangeCoordinatesTo(m_ListViewScrollView, evt.localMousePosition);
            var itemIndex = GetIndexFromLocalPosition(localposition);
            var item = GetHierarchyViewItemFromIndex(itemIndex);
            // item == null if user right-clicks in empty space of HierarchyView.
            // PopulateContextMenu callbacks may populate the menu with default actions
            // not specific to any one view item if the view item == null.
            if (item == null)
            {
                m_Selection.Clear();
                foreach (var handler in m_Hierarchy.EnumerateNodeTypeHandlers())
                {
                    if (handler is IHierarchyEditorNodeTypeHandler editorHandler)
                        editorHandler.PopulateContextMenu(this, null, evt.menu);
                }
            }
            else
            {
                if (item.Handler is IHierarchyEditorNodeTypeHandler editorHandler)
                    editorHandler.PopulateContextMenu(this, item, evt.menu);
            }

            PopulateContextMenu?.Invoke(item, evt.menu);
        }

        internal void InvokeGetTooltip(HierarchyViewItem item, bool filtering, StringBuilder tooltip)
        {
            if (item.Handler is IHierarchyEditorNodeTypeHandler editorHandler)
                editorHandler.GetTooltip(item, filtering, tooltip);
            GetTooltip?.Invoke(item, filtering, tooltip);
        }

        [VisibleToOtherModules("UnityEditor.HierarchyModule")]
        internal void PingNode(HierarchyNode node)
        {
            HierarchyLogging.Log($"HierarchyView({GetHashCode():X}).PingNode({node})");
            if (node == HierarchyNode.Null || node == m_Hierarchy.Root)
                return;

            // If the node is invalid, we cannot ping it.
            if (!m_Hierarchy.Exists(in node))
                return;

            ExpandParents(in node);

            // After expanding parents, need full update again since ExpandParents marks hierarchy as dirty
            Update();

            var index = m_HierarchyViewModel.IndexOf(in node);
            if (index < 0)
                return;

            m_CollectionView.ScrollToItem(index);

            EnqueuePostUpdateAction(() =>
            {
                schedule.Execute(() => DoPingAnimation(node));
            });
        }

        void DoPingAnimation(HierarchyNode node)
        {
            var index = m_HierarchyViewModel.IndexOf(in node);
            if (index < 0)
                return;

            var item = GetHierarchyViewItemFromIndex(index);
            if (item == null)
                return;

            var rowContainer = item.RowContainer;
            if (rowContainer == null)
                return;

            if (rowContainer.ClassListContains(k_HierarchyPingBase))
                return;

            // Begin ping animation
            // Note: Trigger start of anim next frame so the previous AnimatedValue resolved style is properly setup and Transition will occur.
            rowContainer.AddToClassList(k_HierarchyPingBase);
            rowContainer.schedule.Execute(() =>
            {
                // Note: this MIGHT seem messy but this is how USS transition works:

                // 1- Add Transition class specifying how background color will transition.
                rowContainer.AddToClassList(k_HierarchyPingRampIn_Style);

                // 2-  Change background-color value. Background-color will transition over time until it reached k_HierarchyStartPingStyleName
                rowContainer.AddToClassList(k_HierarchyPingRampIn_Start);

                rowContainer.RegisterCallbackOnce<TransitionEndEvent>(_ =>
                {
                    // 3- Remove k_HierarchyStartPingStyleName.
                    rowContainer.RemoveFromClassList(k_HierarchyPingRampIn_Start);
                    rowContainer.RemoveFromClassList(k_HierarchyPingRampIn_Style);

                    // 4- Background-color will transition until they go back to their original value
                    rowContainer.AddToClassList(k_HierarchyPingRampOut_Start);
                    rowContainer.AddToClassList(k_HierarchyPingRampOut_Style);

                    rowContainer.RegisterCallbackOnce<TransitionEndEvent>(_ =>
                    {
                        // 5- Remove Transition styling.

                        rowContainer.RemoveFromClassList(k_HierarchyPingBase);
                        rowContainer.RemoveFromClassList(k_HierarchyPingRampOut_Start);
                        rowContainer.RemoveFromClassList(k_HierarchyPingRampOut_Style);
                    });
                });
            });
        }

        internal void ScrollToNode(in HierarchyNode node)
        {
            if (node == HierarchyNode.Null || node == m_Hierarchy.Root)
                return;

            var index = m_HierarchyViewModel.IndexOf(in node);
            if (index >= 0)
            {
                m_CollectionView.ScrollToItem(index);
            }
        }

        [VisibleToOtherModules("UnityEditor.HierarchyModule")]
        internal void ExpandParents(in HierarchyNode node)
        {
            if (node == HierarchyNode.Null || node == m_Hierarchy.Root)
                return;

            // We do not want to expand the node itself, only its parents recursively.
            var parentNode = m_Hierarchy.GetParent(in node);
            if (parentNode == HierarchyNode.Null || parentNode == m_Hierarchy.Root)
                return;

            m_HierarchyViewModel.SetFlagsRecursive(in parentNode, HierarchyNodeFlags.Expanded, HierarchyTraversalDirection.Parents);
        }

        internal void ExpandParents(ReadOnlySpan<HierarchyNode> nodes)
        {
            // We do not want to expand the nodes themselves, only their parents recursively.
            // No need to create a hash set, since the native implementation already takes care of that.
            using var parents = new RentSpanUnmanaged<HierarchyNode>(nodes.Length, clear: true);
            for (int i = 0, c = nodes.Length; i < c; ++i)
            {
                ref readonly var node = ref nodes[i];
                if (node == HierarchyNode.Null || node == m_Hierarchy.Root)
                    continue;

                parents.Span[i] = m_Hierarchy.GetParent(in node);
            }

            // Note: Its fine to pass null, root or invalid nodes to SetFlagsRecursive, no need to check for that.
            m_HierarchyViewModel.SetFlagsRecursive(parents.Span, HierarchyNodeFlags.Expanded, HierarchyTraversalDirection.Parents);
        }

        [VisibleToOtherModules]
        internal void SelectChildrenAndExpandRecursive()
        {
            var count = m_HierarchyViewModel.HasFlagsCount(HierarchyNodeFlags.Selected);
            if (count == 0)
                return;

            using var nodes = new RentSpanUnmanaged<HierarchyNode>(count);
            m_HierarchyViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected, nodes);
            m_HierarchyViewModel.SetFlagsRecursive(nodes, HierarchyNodeFlags.Selected | HierarchyNodeFlags.Expanded, HierarchyTraversalDirection.Children);
            Update();
        }

        internal void SetRenamingItem(HierarchyViewItem item)
        {
            m_RenamingItem = item;
        }

        void BindHandlers()
        {
            HierarchyLogging.Log($"HierarchyView({GetHashCode():X}).BindHandlers()");
            if (m_Hierarchy == null || !m_Hierarchy.IsCreated)
                return;

            var handlers = m_Hierarchy.EnumerateNodeTypeHandlers();
            foreach (var handler in handlers)
                handler.Internal_BindView(this);
        }

        void UnbindHandlers()
        {
            HierarchyLogging.Log($"HierarchyView({GetHashCode():X}).UnbindHandlers()");
            if (m_Hierarchy == null || !m_Hierarchy.IsCreated)
                return;

            var handlers = m_Hierarchy.EnumerateNodeTypeHandlers();
            foreach (var handler in handlers)
                handler.Internal_UnbindView(this);
        }

        void OnClickEvent(ClickEvent evt)
        {
            m_ScheduledItem?.Pause();

            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            var itemIndex = GetIndexFromLocalPosition(evt.localPosition);
            var item = GetHierarchyViewItemFromIndex(itemIndex);
            if (item == null)
                return;

            var position = evt.position;

            // Rename only works for the name element, not the whole item.
            var itemName = item.Q<HierarchyViewItemName>();
            if (itemIndex == m_LastMouseUpSelectionIndex && evt.clickCount == 1
                                                         && itemName != null && itemName.worldBound.Contains(position))
            {
                if (m_RenameDelayMs == 0)
                {
                    // Execute synchronously if there is no delay.
                    // Schedule will be asynchronous even with a 0ms delay.
                    item.BeginRename();
                }
                else
                {
                    m_ScheduledItem = schedule.Execute(() =>
                    {
                        item.BeginRename();
                        m_ScheduledItem = null;
                    }).StartingIn(m_RenameDelayMs);
                }
            }

            // Double click.
            else if (evt.clickCount == 2)
            {
                ref readonly var node = ref m_HierarchyViewModel[itemIndex];
                var handler = m_Hierarchy.GetNodeTypeHandler(in node);
                if (handler is IHierarchyEditorNodeTypeHandler editorHandler)
                    editorHandler.OnDoubleClick(this, in node);
            }

            m_LastMouseUpSelectionIndex = itemIndex;
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            HierarchyLogging.Log($"HierarchyView({GetHashCode():X}).OnKeyDown(evt.keyCode={evt.keyCode})");

            if (m_IsRenamingItem)
                return;

            var shouldStopPropagation = true;
            switch (evt.keyCode)
            {
                case KeyCode.PageUp:
                case KeyCode.Home:
                    // Select first item
                    m_CollectionView.SetSelection(0);
                    break;
                case KeyCode.PageDown:
                case KeyCode.End:
                    // Select last item
                    m_CollectionView.SetSelection(m_CollectionView.itemsSource.Count - 1);
                    break;
                case KeyCode.Escape:
                    // Reset selected indices state
                    m_CollectionView.ClearSelection();
                    break;
                default:
                    shouldStopPropagation = false;
                    break;
            }

            m_ListViewScrollView.Focus();

            if (shouldStopPropagation)
                evt.StopPropagation();
        }

        void OnNavigationMove(NavigationMoveEvent evt)
        {
            if (m_IsRenamingItem || m_Selection.indexCount == 0)
                return;

            var shouldStopPropagation = true;
            using var outIndices = new RentSpanUnmanaged<int>(m_Selection.indexCount);
            m_HierarchyViewModel.GetIndicesWithFlags(HierarchyNodeFlags.Selected, outIndices);
            var selectedIndex = outIndices.Span[0];

            if (selectedIndex == -1)
            {
                switch (evt.direction)
                {
                    case NavigationMoveEvent.Direction.Up:
                    case NavigationMoveEvent.Direction.Down:
                        m_CollectionView.SetSelection(0);
                        break;
                    default:
                        shouldStopPropagation = false;
                        break;
                }

                m_ListViewScrollView.Focus();
            }
            else
            {
                switch (evt.direction)
                {
                    case NavigationMoveEvent.Direction.Right:
                    case NavigationMoveEvent.Direction.Left:
                        using (var selectedNodes = new RentSpanUnmanaged<HierarchyNode>(m_Selection.indexCount))
                        {
                            m_HierarchyViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected, selectedNodes);
                            SetExpandedState(selectedNodes, evt.direction == NavigationMoveEvent.Direction.Right, evt.altKey);
                        }
                        break;

                    default:
                        shouldStopPropagation = false;
                        break;
                }
            }

            if (shouldStopPropagation)
                evt.StopPropagation();
        }

        void OnNavigationCancel(NavigationCancelEvent evt)
        {
            m_HierarchyViewModel.ClearFlags(HierarchyNodeFlags.Cut);
            Update();

            evt.StopImmediatePropagation();
        }

        // Clear selection when left clicking on the empty space.
        void OnListViewPointerDown(PointerDownEvent evt)
        {
            var target = evt.target as VisualElement;
            if (target != m_CollectionView.scrollView.contentContainer)
                return;

            m_HierarchyViewModel.ClearFlags(HierarchyNodeFlags.Selected);
            Update();

            m_LastMouseUpSelectionIndex = -1;
            evt.StopImmediatePropagation();
        }

        void OnUnbindItem(HierarchyViewItem element)
        {
            element.ExpandedStateChanged -= SetExpandedState;
        }

        void OnHandlerCreated(HierarchyNodeTypeHandlerBase handler)
        {
            Reset();
            Initialize();
        }

        void OnViewModelFlagsChanged(HierarchyNodeFlags flags) => FlagsChanged?.Invoke(flags);

        HierarchyViewItem GetHierarchyViewItemFromIndex(int index)
        {
            if (index == -1)
                return null;

            var root = m_CollectionView.GetRootElementForIndex(index);
            var item = root?.Q<HierarchyViewItem>();
            return item;
        }

        void OnBindItem(HierarchyViewItem item)
        {
            item.ExpandedStateChanged += SetExpandedState;
        }

        void SetExpandedState(in HierarchyNode node, bool isExpanded, bool recurse)
        {
            if (isExpanded)
            {
                if (recurse)
                    ExpandRecursive(in node, HierarchyTraversalDirection.Children);
                else
                    Expand(in node);
            }
            else
            {
                if (recurse)
                    CollapseRecursive(in node, HierarchyTraversalDirection.Children);
                else
                    Collapse(in node);
            }
        }

        void SetExpandedState(ReadOnlySpan<HierarchyNode> nodes, bool isExpanded, bool recurse)
        {
            if (isExpanded)
            {
                if (recurse)
                    ExpandRecursive(nodes, HierarchyTraversalDirection.Children);
                else
                    Expand(nodes);
            }
            else
            {
                if (recurse)
                    CollapseRecursive(nodes, HierarchyTraversalDirection.Children);
                else
                    Collapse(nodes);
            }
        }

        [VisibleToOtherModules("UnityEditor.HierarchyModule")]
        internal bool UpdateListView()
        {
            // If this is the same hierarchy view model version, no need to refresh the list view
            if (m_Version == m_HierarchyViewModel.Version)
                return false;

            HierarchyLogging.Log($"HierarchyView({GetHashCode():X}).UpdateListView()");

            // Refresh the list view
            using (m_RefreshItemsProfilerMarker.Auto())
            {
                m_CollectionView.RefreshItems();
            }

            // Store last version
            m_Version = m_HierarchyViewModel.Version;
            return false;
        }

        void SetColumnState(HierarchyViewState state)
        {
            var columns = ListPool<Column>.Get();
            foreach (var column in m_MultiColumnLayoutConfiguration.columns)
            {
                columns.Add(column);
            }

            SetColumns(columns, state);
        }

        void ClearColumns()
        {
            // Note: current MultiColumnListView doesn't handle Unbinding and Destroying cells when the list is disposed.
            // Implement this workflow directly for the hierarchy view cells:
            var rows = m_CollectionView.Query<VisualElement>("unity-multi-column-view__row-container").ToList();

            // Note: If there are no rows, we still need to unbind the columns.
            foreach (var row in rows)
            {
                var cells = row.Query<HierarchyViewCell>("HierarchyViewCell").ToList();
                foreach (var cell in cells)
                {
                    if (cell.Descriptor == null)
                        continue;
                    cell.UnbindCell();
                }
            }

            foreach (var column in m_MultiColumnLayoutConfiguration.columns)
            {
                if (column is HierarchyViewColumn hc)
                {
                    hc.UnbindColumn(this);
                }
            }
        }

        void BindColumns()
        {
            foreach (var column in m_MultiColumnLayoutConfiguration.columns)
            {
                if (column is HierarchyViewColumn hc)
                {
                    hc.BindColumn(this);
                }
            }
        }

        bool DoUpdate(UpdateMode mode, double milliseconds = 0.0)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool UpdateHierarchy(UpdateMode mode, double milliseconds)
            {
                if (m_Hierarchy == null || !m_Hierarchy.IsCreated || !m_Hierarchy.UpdateNeeded)
                    return false;

                switch (mode)
                {
                    case UpdateMode.Update:
                        m_Hierarchy.Update();
                        return false;

                    case UpdateMode.UpdateIncremental:
                        return m_Hierarchy.UpdateIncremental();

                    case UpdateMode.UpdateIncrementalTimed:
                        return m_Hierarchy.UpdateIncrementalTimed(milliseconds);

                    default:
                        throw new NotImplementedException(mode.ToString());
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool UpdateHierarchyFlattened(UpdateMode mode, double milliseconds)
            {
                if (m_HierarchyFlattened == null || !m_HierarchyFlattened.IsCreated || !m_HierarchyFlattened.UpdateNeeded)
                    return false;

                switch (mode)
                {
                    case UpdateMode.Update:
                        m_HierarchyFlattened.Update();
                        return false;

                    case UpdateMode.UpdateIncremental:
                        return m_HierarchyFlattened.UpdateIncremental();

                    case UpdateMode.UpdateIncrementalTimed:
                        return m_HierarchyFlattened.UpdateIncrementalTimed(milliseconds);

                    default:
                        throw new NotImplementedException(mode.ToString());
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool UpdateHierarchyViewModel(UpdateMode mode, double milliseconds)
            {
                if (m_HierarchyViewModel == null || !m_HierarchyViewModel.IsCreated || !m_HierarchyViewModel.UpdateNeeded)
                    return false;

                switch (mode)
                {
                    case UpdateMode.Update:
                        m_HierarchyViewModel.Update();
                        return false;

                    case UpdateMode.UpdateIncremental:
                        return m_HierarchyViewModel.UpdateIncremental();

                    case UpdateMode.UpdateIncrementalTimed:
                        return m_HierarchyViewModel.UpdateIncrementalTimed(milliseconds);

                    default:
                        throw new NotImplementedException(mode.ToString());
                }
            }

            void ExecuteActions(CircularBuffer<Action> actions)
            {
                if (!actions.IsEmpty)
                    HierarchyLogging.Log($"HierarchyView({GetHashCode():X}).ExecuteActions()");

                while (!actions.IsEmpty)
                {
                    // Execute action at front of the queue
                    var action = actions.Front();

                    // Invoke action in a try-catch block to prevent exceptions from locking the buffer indefinitely
                    try
                    {
                        // Lock the buffer to prevent modification while executing
                        actions.Locked = true;
                        action?.Invoke();
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                    finally
                    {
                        // Unlock the buffer and pop the front action
                        actions.Locked = false;
                        actions.PopFront();
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            bool DoUpdateStage(UpdateMode mode, double milliseconds)
            {
                switch (m_UpdateStage)
                {
                    case UpdateStage.UpdatingHierarchy:
                        return UpdateHierarchy(mode, milliseconds);

                    case UpdateStage.UpdatingHierarchyFlattened:
                        return UpdateHierarchyFlattened(mode, milliseconds);

                    case UpdateStage.UpdatingHierarchyViewModel:
                        return UpdateHierarchyViewModel(mode, milliseconds);

                    case UpdateStage.UpdatingListView:
                        return UpdateListView();

                    case UpdateStage.ExecutePostUpdateActions:
                        ExecuteActions(m_PostUpdateActionQueue);
                        return false;

                    default:
                        throw new NotImplementedException(m_UpdateStage.ToString());
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IncrementUpdateStage()
            {
                m_UpdateStage = (UpdateStage)(((int)m_UpdateStage + 1) % ((int)UpdateStage.Count));
            }

            // Execute the current stage
            var callAgain = DoUpdateStage(mode, milliseconds);

            // If we are done with the current stage, increment to the next stage
            if (!callAgain)
                IncrementUpdateStage();

            // Return whether or not we need to call Update again
            return callAgain || UpdateNeeded;
        }

        void UpdateData()
        {
            if (m_Hierarchy is { IsCreated: true })
                m_Hierarchy.Update();

            if (m_HierarchyFlattened is { IsCreated: true })
                m_HierarchyFlattened.Update();

            if (m_HierarchyViewModel is { IsCreated: true })
                m_HierarchyViewModel.Update();
        }
    }

    static class StopwatchExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ElapsedMillisecondsPrecise(this Stopwatch stopwatch)
        {
            return (double)stopwatch.ElapsedTicks / Stopwatch.Frequency * 1000.0;
        }
    }
}
