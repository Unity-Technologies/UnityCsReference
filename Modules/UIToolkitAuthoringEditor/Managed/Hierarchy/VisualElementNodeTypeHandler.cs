// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using Unity.Properties;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal abstract class VisualElementNodeTypeHandler :
    HierarchyNodeTypeHandler,
    IVisualElementChangeProcessor,
    IHierarchySearchPropositionProvider,
    IHierarchyEntityIdConverter
{
    // Used for tests
    internal MappingsAccess GetMappings() => new(this);

    internal readonly struct MappingsAccess
    {
        private readonly VisualElementNodeTypeHandler m_Handler;

        internal MappingsAccess(VisualElementNodeTypeHandler handler)
        {
            m_Handler = handler;
        }

        public bool TryGetElement(in HierarchyNode node, out VisualElement element)
        {
            return m_Handler.TryGetElementFromNode(in node, out element);
        }

        public bool TryGetNode(VisualElement element, out HierarchyNode node)
        {
            return m_Handler.TryGetNodeFromElement(element, out node);
        }
    }

    public const string HierarchyItemClassName = "ui-hierarchy-item";
    public const string HierarchyItemElementNameClassName = HierarchyItemClassName + "__element-name";
    public const string HierarchyItemElementTypeNameClassName = HierarchyItemClassName + "__element-type-name";
    public const string HierarchyItemUssClassName = HierarchyItemClassName + "__element-uss-class";
    public const string HierarchyItemDisabledClassName = HierarchyItemClassName + "__disabled";

    private const string k_StyleSheetPath = "UIToolkitAuthoring/Hierarchy/VisualElementNodeTypeHandler.uss";

    // [TODO] MP: Convenience struct until APIs are added to retrieve the selection from the HierarchyView
    protected readonly ref struct SelectionContext
    {
        public enum SelectionType
        {
            None,
            Mixed,
            All
        }

        public SelectionContext(
            Span<HierarchyNode> selection,
            int selectionCount,
            SelectionContext.SelectionType handlerType)
        {
            Selection = selection;
            SelectionCount = selectionCount;
            Type = handlerType;
        }

        public readonly Span<HierarchyNode> Selection;
        public readonly int SelectionCount;
        public readonly SelectionType Type;
    }

    private class Mappings
    {
        private readonly Dictionary<HierarchyNode, VisualElement> m_Map = new();
        private readonly Dictionary<VisualElement, HierarchyNode> m_ReversedMap = new();
        private readonly Dictionary<HierarchyNode, EntityId> m_SelectionHandles = new();
        private readonly Dictionary<EntityId, HierarchyNode> m_ReversedSelectionHandles = new();

        public int Count => m_Map.Count;

        public bool TryAdd(in HierarchyNode node, VisualElement element, in EntityId selectionHandle)
        {
            if (node == HierarchyNode.Null || element == null)
                return false;

            return m_Map.TryAdd(node, element) &&
                   m_ReversedMap.TryAdd(element, node) &&
                   m_SelectionHandles.TryAdd(node, selectionHandle) &&
                   m_ReversedSelectionHandles.TryAdd(selectionHandle, node);
        }

        public bool TryGetValue(in HierarchyNode node, out VisualElement element)
        {
            if (node != HierarchyNode.Null)
                return m_Map.TryGetValue(node, out element);

            element = null;
            return false;
        }

        public bool TryGetSelectionHandle(in HierarchyNode node, out EntityId selectionHandle)
        {
            if (node != HierarchyNode.Null)
                return m_SelectionHandles.TryGetValue(node, out selectionHandle);

            selectionHandle = EntityId.None;
            return false;
        }

        public bool TryGetNodeFromSelectionHandle(in EntityId selectionHandle, out HierarchyNode node)
        {
            if (selectionHandle != EntityId.None)
                return m_ReversedSelectionHandles.TryGetValue(selectionHandle, out node);

            node = HierarchyNode.Null;
            return false;
        }

        public bool TryGetValue(VisualElement element, out HierarchyNode node)
        {
            if (element != null)
                return m_ReversedMap.TryGetValue(element, out node);

            node = HierarchyNode.Null;
            return false;
        }

        public bool TryRemove(in HierarchyNode node)
        {
            if (node == HierarchyNode.Null)
                return false;

            return m_Map.Remove(node, out var element) &&
                   m_ReversedMap.Remove(element) &&
                   m_SelectionHandles.Remove(node, out var selectionHandle) &&
                   m_ReversedSelectionHandles.Remove(selectionHandle);
        }

        public bool TryRemove(VisualElement element)
        {
            if (element != null)
                return m_ReversedMap.Remove(element, out var node)
                       && m_Map.Remove(node) &&
                       m_SelectionHandles.Remove(node, out var selectionHandle) &&
                       m_ReversedSelectionHandles.Remove(selectionHandle);

            return false;
        }

        public bool RemoveSelection(VisualElement element)
        {
            if (m_ReversedMap.TryGetValue(element, out var node))
            {
                return m_SelectionHandles.Remove(node, out var selectionHandle) &&
                       m_ReversedSelectionHandles.Remove(selectionHandle);
            }

            return false;
        }
    }

    /// <summary>
    /// Indicates if and how a <see cref="HierarchyNode"/> should be created from a  <see cref="VisualElement"/>é
    /// </summary>
    protected enum NodeCreationType
    {
        /// <summary>
        /// Do not create a node for the current VisualElement or its children.
        /// </summary>
        DontCreate,

        /// <summary>
        /// Create a node for the current VisualElement and its children.
        /// </summary>
        Create,

        /// <summary>
        /// do not create a node for the current VisualElement, but create nodes for its children.
        /// </summary>
        CreateChildren,
    }

    private readonly Mappings m_Mappings = new();
    private readonly List<Panel> m_RegisteredPanels = new();
    private readonly QueryEngine<VisualElement> m_QueryEngine;
    private readonly IVisualElementSelectionHandler m_SelectionHandler;

    private StyleSheet m_StyleSheet;
    private StyleSheet m_ThemeStyleSheet;
    private ParsedQuery<VisualElement> m_ParsedQuery;

    private UIHierarchyDisplayOptions m_DisplayOptions;

    /// <summary>
    /// Flags indicating is mutating operations are permitted in the hierarchy.
    /// </summary>
    // TODO [MP]: Switch visibility to protected once we support mutating operations.
    private bool isReadonly { get; set; } = true;

    private StyleSheet StyleSheet
    {
        get
        {
            if (!m_StyleSheet)
                m_StyleSheet = EditorGUIUtility.Load(k_StyleSheetPath) as StyleSheet;
            return m_StyleSheet;
        }
    }

    private StyleSheet ThemeStyleSheet
    {
        get
        {
            if (!m_ThemeStyleSheet)
            {
                var path = k_StyleSheetPath;
                var index = path.LastIndexOf(".uss", StringComparison.OrdinalIgnoreCase);
                if (EditorGUIUtility.isProSkin)
                    path = path.Insert(index, "Dark");
                else
                    path = path.Insert(index, "Light");
                m_ThemeStyleSheet = EditorGUIUtility.Load(path) as StyleSheet;
            }

            return m_ThemeStyleSheet;
        }
    }

    protected VisualElementNodeTypeHandler(IVisualElementSelectionHandler selectionHandler)
    {
        m_QueryEngine = CreateQueryEngine();
        m_SelectionHandler = selectionHandler;
        UIToolkitAuthoringSettings.DisplayOptionsChanged += OnDisplayOptionsChanged;
        m_DisplayOptions = UIToolkitAuthoringSettings.DisplayOptions;
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.Dispose"/>>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        UnregisterAllPanels();
        UIToolkitAuthoringSettings.DisplayOptionsChanged -= OnDisplayOptionsChanged;
    }

    #region HierarchyNodeTypeHandler

    /// <inheritdoc cref="HierarchyNodeTypeHandler.GetNodeTypeName"/>>
    public sealed override string GetNodeTypeName()
    {
        return "VisualElement";
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.SearchBegin"/>>
    protected sealed override void SearchBegin(HierarchySearchQueryDescriptor query)
    {
        m_ParsedQuery = m_QueryEngine.ParseQuery(query.ToString());
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.SearchMatch"/>>
    protected sealed override bool SearchMatch(in HierarchyNode node)
    {
        return m_Mappings.TryGetValue(node, out var element) && m_ParsedQuery.Test(element);
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.SearchEnd"/>>
    protected sealed override void SearchEnd()
    {
        m_ParsedQuery = null;
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.AcceptParent"/>>
    public sealed override bool AcceptParent(HierarchyView view, in HierarchyNode parent)
    {
        if (isReadonly)
            return false;

        var handler = Hierarchy.GetNodeTypeHandlerBase(in parent);
        return handler is VisualElementNodeTypeHandler;
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.AcceptChild"/>>
    public sealed override bool AcceptChild(HierarchyView view, in HierarchyNode child)
    {
        if (isReadonly)
            return false;

        var handler = Hierarchy.GetNodeTypeHandlerBase(in child);
        return handler is VisualElementNodeTypeHandler;
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.CanSetName"/>>
    public sealed override bool CanSetName(HierarchyView view, in HierarchyNode node)
    {
        if (isReadonly)
            return false;

        return m_Mappings.TryGetValue(in node, out var element) && CanSetName(view, in node, element);
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.OnSetName"/>>
    protected sealed override bool OnSetName(HierarchyView view, in HierarchyNode node, string name)
    {
        if (!m_Mappings.TryGetValue(node, out var element))
            return false;

        if (!ValidateName(name))
            return false;

        element.name = name;
        return OnSetName(view, in node, element, name);
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.GetDisplayName"/>>
    public sealed override string GetDisplayName(HierarchyView view, in HierarchyNode node)
    {
        if (!m_Mappings.TryGetValue(node, out var element) || element == null)
            return "<null>";

        return GetDisplayName(view, node, element);
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.CanCopy"/>>
    public sealed override bool CanCopy(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return CanCopy(view, in selection);
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.OnCopy"/>>
    protected sealed override bool OnCopy(HierarchyView view) => !isReadonly;

    /// <inheritdoc cref="HierarchyNodeTypeHandler.CanCut"/>>
    public sealed override bool CanCut(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return CanCut(view, in selection);
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.OnCut"/>>
    protected sealed override bool OnCut(HierarchyView view) => !isReadonly;

    /// <inheritdoc cref="HierarchyNodeTypeHandler.CanDelete"/>>
    public sealed override bool CanDelete(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return CanDelete(view, in selection);
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.OnDelete"/>>
    protected sealed override bool OnDelete(HierarchyView view) => !isReadonly;

    /// <inheritdoc cref="HierarchyNodeTypeHandler.CanDuplicate"/>>
    public sealed override bool CanDuplicate(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return CanDuplicate(view, in selection);
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.OnDuplicate"/>>
    protected sealed override bool OnDuplicate(HierarchyView view) => !isReadonly;

    /// <inheritdoc cref="HierarchyNodeTypeHandler.CanFindReferences"/>>
    public sealed override bool CanFindReferences(HierarchyView view)
    {
        // We currently don't support visual element references.
        return false;
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.OnFindReferences"/>>
    protected sealed override bool OnFindReferences(HierarchyView view)
    {
        // We currently don't support visual element references.
        return false;
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.CanPaste"/>>
    public sealed override bool CanPaste(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return CanPaste(view, in selection);
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.OnPaste"/>>
    protected sealed override bool OnPaste(HierarchyView view) => !isReadonly;

    /// <inheritdoc cref="HierarchyNodeTypeHandler.CanPasteAsChild"/>>
    public sealed override bool CanPasteAsChild(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return CanPasteAsChild(view, in selection);
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.OnPasteAsChild"/>>
    protected sealed override bool OnPasteAsChild(HierarchyView view) => !isReadonly;

    /// <inheritdoc cref="HierarchyNodeTypeHandler.CanDoubleClick"/>>
    public sealed override bool CanDoubleClick(HierarchyView view, in HierarchyNode node)
    {
        return m_Mappings.TryGetValue(in node, out var element) && CanDoubleClick(view, in node, element);
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.OnDoubleClick"/>>
    protected sealed override bool OnDoubleClick(HierarchyView view, in HierarchyNode node)
    {
        return m_Mappings.TryGetValue(node, out var element) && OnDoubleClick(view, in node, element);
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.CanStartDrag"/>>
    protected sealed override bool CanStartDrag(HierarchyView view, ReadOnlySpan<HierarchyNode> nodes)
    {
        // This requires custom handling compared to the other end points because returning false here
        // will prevent drag and drop of other handlers too. This is an issue that will be fixed on the hierarchy's side.
        using var memoryOwner = GetSelection(view, out var selection);
        return selection.Type switch
        {
            // No elements selected, revert to default handling.
            SelectionContext.SelectionType.None => base.CanStartDrag(view, nodes),
            // Mixed selection, disallow dragging.
            SelectionContext.SelectionType.Mixed => false,
            // We're only dragging visual element here.
            SelectionContext.SelectionType.All => CanStartDrag(view, in selection),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.CanDrop"/>>
    protected sealed override DragVisualMode CanDrop(in HierarchyViewDragAndDropHandlingData data) => DragVisualMode.None;

    protected override void OnBindItem(HierarchyViewItem item)
    {
        if (m_Mappings.TryGetValue(item.Node, out var element))
        {
            item.Icon.style.backgroundImage = GetIcon(element);
            Bind(item, element);
        }
        else
        {
            item.Icon.style.backgroundImage = null;
            item.EnableInClassList(HierarchyItemDisabledClassName, true);
        }
    }

    /// <inheritdoc cref="HierarchyView.UnbindViewItem"/>>
    protected override void OnUnbindItem(HierarchyViewItem item)
    {
        if (m_Mappings.TryGetValue(item.Node, out var element))
        {
            Unbind(item, element);
        }
        else
        {
            item.Icon.style.backgroundImage = null;
            item.RemoveFromClassList(HierarchyItemElementNameClassName);
            item.RemoveFromClassList(HierarchyItemElementTypeNameClassName);
            item.RemoveFromClassList(HierarchyItemUssClassName);
            item.EnableInClassList(HierarchyItemDisabledClassName, false);
            item.LeftCustomContainer.Clear();
            item.parent.Q(className: HierarchyItemElementTypeNameClassName)?.RemoveFromHierarchy();
        }
    }

    /// <inheritdoc cref="HierarchyView.GetTooltip"/>>
    protected override void GetTooltip(HierarchyViewItem item, bool isFiltering, StringBuilder tooltip)
    {
        if (item == null)
            return;

        if (m_Mappings.TryGetValue(item.Node, out var element))
        {
            tooltip.Clear();
            tooltip.Append(GetTooltip(in item.Node, element, isFiltering));
        }
    }

    /// <inheritdoc cref="HierarchyView.PopulateContextMenu"/>>
    protected override void PopulateContextMenu(HierarchyView view, HierarchyViewItem item, DropdownMenu menu)
    {
        if (item == null)
            return;

        if (m_Mappings.TryGetValue(item.Node, out var element))
            PopulateContextMenu(in item.Node, element, menu);
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.OnInitializingView"/>>
    protected override void OnBindView(HierarchyView view)
    {
        view.StyleContainer.styleSheets.Add(StyleSheet);
        view.StyleContainer.styleSheets.Add(ThemeStyleSheet);
    }

    #endregion // HierarchyNodeTypeHandler

    #region IHierarchySearchPropositionProvider

    IEnumerable<SearchProposition> IHierarchySearchPropositionProvider.FetchPropositions(HierarchyViewModel viewModel,
        SearchContext context, SearchPropositionOptions options)
    {
        yield return new SearchProposition(
            category: null,
            label: "UI",
            priority: -1,
            icon: UIResources.GetIconForType(typeof(VisualElement), UIResources.RequestSize.Px16).texture);

        foreach (var proposition in m_QueryEngine.GetPropositions())
            yield return proposition;

        foreach (var t in QueryListBlockAttribute.GetPropositions(typeof(UIQueryTypeListBlock)))
            yield return t;
    }

    #endregion // IHierarchySearchPropositionProvider

    /// <summary>
    /// Determine if the node type handler accept the naming action.
    /// </summary>
    /// <param name="view">The parent <see cref="HierarchyView"/>.</param>
    /// <param name="node">The <see cref="HierarchyNode"/>.</param>
    /// <param name="element">The <see cref="VisualElement"/>.</param>
    /// <returns><see langword="true"/> if the node can be renamed, <see langword="false"/> otherwise.</returns>
    protected virtual bool CanSetName(HierarchyView view, in HierarchyNode node, VisualElement element) => !isReadonly;

    /// <summary>
    /// Called when setting a new name for a <see cref="VisualElement"/> to determine if the name is valid or not.
    /// </summary>
    /// <param name="name">The new name.</param>
    /// <returns><see langword="true"/> if the name is valid; <see langword="false"/> otherwise.</returns>
    protected virtual bool ValidateName(string name) => true;

    /// <summary>
    /// Action to execute when renaming a node.
    /// </summary>
    /// <param name="view">The parent <see cref="HierarchyView"/>.</param>
    /// <param name="node">The <see cref="HierarchyNode"/>.</param>
    /// <param name="element">The <see cref="VisualElement"/> to rename.</param>
    /// <param name="name">The given name.</param>
    /// <returns><see langword="true"/> if the node is renamed successfully, <see langword="false"/> otherwise.</returns>
    protected virtual bool OnSetName(HierarchyView view, in HierarchyNode node, VisualElement element, string name) =>
        false;

    /// <summary>
    /// Get a node display name. Default is the node name property.
    /// </summary>
    /// <param name="view">The parent <see cref="HierarchyView"/>.</param>
    /// <param name="node">The <see cref="HierarchyNode"/>.</param>
    /// <param name="element">The <see cref="VisualElement"/>.</param>
    /// <returns>Display name</returns>
    protected virtual string GetDisplayName(HierarchyView view, in HierarchyNode node, VisualElement element)
    {
        if (string.IsNullOrEmpty(element.name))
            return string.Empty;

        return $"#{element.name}";
    }

    /// <summary>
    /// Determines if selected nodes can be copied.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
    protected virtual bool CanCopy(HierarchyView view, in SelectionContext selection) => false;

    /// <summary>
    /// Determines if selected nodes can be cut.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
    protected virtual bool CanCut(HierarchyView view, in SelectionContext selection) => false;

    /// <summary>
    /// Determines if selected nodes can be deleted.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
    protected virtual bool CanDelete(HierarchyView view, in SelectionContext selection) => false;

    /// <summary>
    /// Determines if selected nodes can be duplicated.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
    protected virtual bool CanDuplicate(HierarchyView view, in SelectionContext selection) => false;

    /// <summary>
    /// Determines if copied nodes can be pasted.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
    protected virtual bool CanPaste(HierarchyView view, in SelectionContext selection) => false;

    /// <summary>
    /// Determines if copied nodes can be pasted as child.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
    protected virtual bool CanPasteAsChild(HierarchyView view, in SelectionContext selection) => false;

    /// <summary>
    /// Determines if a double click operation can be performed on the <see cref="HierarchyNode"/>.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="node">The <see cref="HierarchyNode"/> to perform double click on.</param>
    /// <param name="element">The <see cref="VisualElement"/>.</param>
    /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
    protected virtual bool CanDoubleClick(HierarchyView view, in HierarchyNode node, VisualElement element) => false;

    /// <summary>
    /// Action to execute when double clicking on the <see cref="HierarchyNode"/>.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="node">The <see cref="HierarchyNode"/> to perform double click on.</param>
    /// <param name="element">The <see cref="VisualElement"/>.</param>
    /// <returns><see langword="true"/> if the action was successful, <see langword="false"/> otherwise.</returns>
    protected virtual bool OnDoubleClick(HierarchyView view, in HierarchyNode node, VisualElement element) => false;

    /// <summary>
    /// Determines if a drag operation can be started with the specified nodes.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if the dragging operation can be started, <see langword="false"/> otherwise.</returns>
    protected virtual bool CanStartDrag(HierarchyView view, in SelectionContext selection) => false;

    protected virtual bool CanSetEnabled(HierarchyView view, in HierarchyNode node, VisualElement element) => false;

    protected virtual bool OnSetEnabled(HierarchyView view, in HierarchyNode node, VisualElement element) => false;

    /// <summary>
    /// Called when a hierarchy view item is bound to a hierarchy view, allowing customization of the view item.
    /// </summary>
    /// <param name="item">The hierarchy view item.</param>
    /// <param name="element">The <see cref="VisualElement"/> to bind.</param>
    protected virtual void Bind(HierarchyViewItem item, VisualElement element)
    {
        var nameElement = item.Q(className: "hierarchy-item__name");
        nameElement.AddToClassList(HierarchyItemElementNameClassName);
        var index = nameElement.parent.IndexOf(nameElement);

        if (string.IsNullOrEmpty(element.name) ||
            (m_DisplayOptions & UIHierarchyDisplayOptions.Typename) != 0)
        {
            var typeNameLabel = new Label(element.GetType().Name);
            typeNameLabel.AddToClassList(HierarchyItemElementTypeNameClassName);
            nameElement.parent.Insert(index, typeNameLabel);
            typeNameLabel.EnableInClassList(HierarchyItemDisabledClassName, !element.enabledSelf);
        }

        nameElement.EnableInClassList(HierarchyItemDisabledClassName, !element.enabledSelf);

        if ((m_DisplayOptions & UIHierarchyDisplayOptions.UssClasses) != 0)
        {
            foreach (var ussClass in element.GetClasses())
            {
                var ussClassLabel = new Label($".{ussClass}");
                ussClassLabel.AddToClassList(HierarchyItemUssClassName);
                item.LeftCustomContainer.Add(ussClassLabel);
            }
        }
    }

    /// <summary>
    /// Called when a hierarchy view item is unbound from a hierarchy view, allowing cleanup of the view item.
    /// </summary>
    /// <param name="item">The hierarchy view item.</param>
    /// <param name="element">The <see cref="VisualElement"/> to unbind.</param>
    protected virtual void Unbind(HierarchyViewItem item, VisualElement element)
    {
        var nameElement = item.Q(className: "hierarchy-item__name");
        nameElement.RemoveFromClassList(HierarchyItemElementNameClassName);
        nameElement.EnableInClassList("unity-disabled", false);
        item.EnableInClassList("unity-disabled", false);
        item.LeftCustomContainer.Clear();
        var typenameElement = item.parent.Q(className: HierarchyItemElementTypeNameClassName);
        typenameElement.EnableInClassList("unity-disabled", false);
        typenameElement?.RemoveFromHierarchy();
    }

    /// <summary>
    /// Customize the tooltip displayed when the mouse hovers the node name label.
    /// </summary>
    /// <param name="node">HierarchyNode that is hovered.</param>
    /// <param name="element">The <see cref="VisualElement"/>.</param>
    /// <param name="isFiltering">Is the view filtering results according to a search query? Note: When filtering the view displays its results as a flat list.</param>
    /// <returns>Returns the computed tooltip.</returns>
    protected virtual string GetTooltip(in HierarchyNode node, VisualElement element, bool isFiltering)
    {
        return isFiltering ? Hierarchy.GetPath(in node) : element.tooltip;
    }

    /// <summary>
    /// Append context menu for a given hierarchy node.
    /// </summary>
    /// <param name="view">The selected <see cref="HierarchyView"/>.</param>
    /// <param name="node">The hierarchy node.</param>
    /// <param name="element">The <see cref="VisualElement"/>.</param>
    /// <param name="menu">The <see cref="DropdownMenu"/> to populate with.</param>
    protected virtual void PopulateContextMenu(in HierarchyNode node, VisualElement element, DropdownMenu menu)
    {
    }

    /// <summary>
    /// Returns the icon to use for a given <see cref="VisualElement"/> instance.
    /// </summary>
    /// <param name="element">The <see cref="VisualElement"/>.</param>
    /// <returns>The icon.</returns>
    protected virtual Background GetIcon(VisualElement element)
        => UIResources.GetIconForElement(element, UIResources.RequestSize.Px16);

    /// <summary>
    /// Queries the handler to figure out if a node should be created for the request <see cref="VisualElement"/>.
    /// </summary>
    /// <param name="element">The requested <see cref="VisualElement"/></param>
    /// <returns>The <see cref="NodeCreationType"/> indicating if a node should be created or not.</returns>
    protected virtual NodeCreationType ShouldCreateNode(VisualElement element)
        => NodeCreationType.Create;

    /// <summary>
    /// Attempts to find the parent <see cref="HierarchyNode"/> for a given <see cref="VisualElement"/>.
    /// </summary>
    /// <param name="element">The requested <see cref="VisualElement"/>.</param>
    /// <param name="parentNode">The parent <see cref="HierarchyNode"/> of the <see cref="VisualElement"/>.</param>
    /// <returns>The <see cref="HierarchyNode"/> of the parent of the <see cref="element"/> or the root node.</returns>
    /// <exception cref="ArgumentNullException">If the <see cref="element"/> is null.</exception>
    protected virtual bool TryGetParentNode(VisualElement element, out HierarchyNode parentNode)
    {
        if (null == element)
            throw new ArgumentNullException(nameof(element));

        var parent = element.parent;
        if (null == parent)
        {
            parentNode = Hierarchy.Root;
            return true;
        }

        if (m_Mappings.TryGetValue(parent, out parentNode))
            return true;

        parentNode = HierarchyNode.Null;
        return false;
    }

    /// <summary>
    /// Register a panel to start getting changes.
    /// </summary>
    /// <param name="panel">The requested panel.</param>
    protected void RegisterPanel(Panel panel)
    {
        panel.RegisterChangeProcessor(this);
        m_RegisteredPanels.Add(panel);
    }

    /// <summary>
    /// Unregister a panel to stop getting changes.
    /// </summary>
    /// <param name="panel">The requested panel.</param>
    protected void UnregisterPanel(Panel panel)
    {
        panel.UnregisterChangeProcessor(this);
        m_RegisteredPanels.Remove(panel);
    }

    /// <summary>
    /// Queries the associated <see cref="VisualElement"/> of a given <see cref="HierarchyNode"/>.
    /// </summary>
    /// <param name="node">The <see cref="HierarchyNode"/>.</param>
    /// <param name="element">The associated <see cref="VisualElement"/>.</param>
    /// <returns><see langword="true"/> if the element was found; <see langword="false"/> otherwise.</returns>
    protected bool TryGetElementFromNode(in HierarchyNode node, out VisualElement element)
    {
        return m_Mappings.TryGetValue(node, out element);
    }

    /// <summary>
    /// Queries the associated <see cref="HierarchyNode"/> of a given <see cref="VisualElement"/>.
    /// </summary>
    /// <param name="element">The <see cref="VisualElement"/>.</param>
    /// <param name="node">The associated <see cref="HierarchyNode"/>.</param>
    /// <returns><see langword="true"/> if the element was found; <see langword="false"/> otherwise.</returns>
    protected bool TryGetNodeFromElement(VisualElement element, out HierarchyNode node)
    {
        return m_Mappings.TryGetValue(element, out node);
    }


    public bool GetEnabled(HierarchyView view, in HierarchyNode node)
    {
        return TryGetElementFromNode(node, out var element) && element.enabledSelf;
    }

    public bool SetEnabled(HierarchyView view, in HierarchyNode node, bool value)
    {
        try
        {
            if (TryGetElementFromNode(node, out var element) &&
                CanSetEnabled(view, in node, element))
                return OnSetEnabled(view, in node, element);
            return false;
        }
        finally
        {
            Hierarchy.SetDirty();
        }
    }

    #region IVisualElementChangeProcessor

    void IVisualElementChangeProcessor.BeginProcessing(BaseVisualElementPanel panel)
    {
        if (!Hierarchy.IsCreated)
            return;

        Rebuild(panel);
    }

    void IVisualElementChangeProcessor.ProcessChanges(BaseVisualElementPanel panel, AuthoringChanges changes)
    {
        if (!Hierarchy.IsCreated)
            return;

        using var handle = ListPool<VisualElementRemap>.Get(out var remappings);
        if (changes.addedOrMovedElements.Count > 0 && changes.removedFromPanel.Count > 0)
            VisualElementRemapper.Remap(changes.addedOrMovedElements, changes.removedFromPanel, remappings);

        if (remappings.Count > 0)
        {
            m_SelectionHandler.Remap(remappings);

            foreach (var remap in remappings)
            {
                m_Mappings.RemoveSelection(remap.Previous);
            }
        }

        // We process the elements that were added or moved before the elements that were removed
        // because when the Hierarchy will remove a node, it will also remove its children, which
        // would invalidate the node of children moved to a different parent.
        if (changes.addedOrMovedElements.Count > 0)
        {
            using var parentHashHandler = HashSetPool<HierarchyNode>.Get(out var parentsToSort);
            using var processedElementsHandler = HashSetPool<VisualElement>.Get(out var processedElements);

            foreach (var element in changes.addedOrMovedElements)
            {
                if (processedElements.Contains(element))
                    continue;

                var elementParent = element.hierarchy.parent;

                // Already tracked, the element must have been moved.
                if (m_Mappings.TryGetValue(element, out var elementNode))
                {
                    // Parent is already mapped
                    if (TryGetParentNode(element, out var parentNode))
                    {
                        CommandList.SetParent(elementNode, parentNode);
                        parentsToSort.Add(parentNode);
                    }
                    else
                    {
                        // Wait until the parent is processed.
                    }
                }
                // Element was not known previously
                else
                {
                    // If the element does not have a parent, it is the single root of the panel.
                    var index = elementParent?.IndexOf(element) ?? 0;
                    Rebuild(element, index);

                    if (m_Mappings.TryGetValue(elementParent, out var elementParentNode))
                        parentsToSort.Add(elementParentNode);
                }
            }

            // Recompute sorting index
            foreach (var parentNode in parentsToSort)
            {
                RefreshChildrenSortingIndices(parentNode);
            }
        }

        foreach (var removed in changes.removedFromPanel)
        {
            ClearSingle(removed);
        }

        foreach (var element in changes.stylingContextChanged)
        {
            if (m_Mappings.TryGetValue(element, out var elementNode))
                CommandList.SetName(elementNode, element.name);
        }
    }

    void IVisualElementChangeProcessor.EndProcessing(BaseVisualElementPanel panel)
    {
        if (!Hierarchy.IsCreated)
            return;

        Clear(panel);
    }

    #endregion IVisualElementChangeProcessor

    protected void UnregisterAllPanels()
    {
        while (m_RegisteredPanels.Count > 0)
        {
            var panel = m_RegisteredPanels[^1];
            Clear(panel);
            UnregisterPanel(panel);
        }
    }

    private void Rebuild(BaseVisualElementPanel panel)
    {
        var root = panel.visualTree;
        Rebuild(root, 0);
    }

    private void Rebuild(VisualElement element, int siblingIndex)
    {
        var kind = ShouldCreateNode(element);
        switch (kind)
        {
            case NodeCreationType.DontCreate:
                return;
            case NodeCreationType.Create:
                RebuildSingle(element, siblingIndex);
                break;
            case NodeCreationType.CreateChildren:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        PartialRebuild(element.hierarchy);
    }

    private void RebuildSingle(VisualElement element, int siblingIndex)
    {
        if (!TryGetParentNode(element, out var parentNode))
            return;

        if (!m_Mappings.TryGetValue(element, out var elementNode))
        {
            CommandList.Add(in parentNode, out elementNode);
            var instanceID = m_SelectionHandler?.AcquireInstanceId(element) ?? EntityId.None;
            m_Mappings.TryAdd(in elementNode, element, instanceID);
        }
        else
        {
            CommandList.SetParent(in elementNode, in parentNode);
        }

        CommandList.SetName(in elementNode, element.name);
        CommandList.SetSortIndex(in elementNode, siblingIndex);
    }

    private void PartialRebuild(VisualElement.Hierarchy hierarchy)
    {
        for (var i = 0; i < hierarchy.childCount; ++i)
        {
            var child = hierarchy[i];
            Rebuild(child, i);
        }
    }

    private void Clear(BaseVisualElementPanel panel)
    {
        var root = panel.visualTree;
        Clear(root);
    }

    private void Clear(VisualElement element)
    {
        ClearSingle(element);

        for (var i = 0; i < element.hierarchy.childCount; ++i)
        {
            var child = element.hierarchy[i];
            Clear(child);
        }
    }

    private void ClearSingle(VisualElement element)
    {
        if (!m_Mappings.TryGetValue(element, out var removedNode))
            return;

        m_SelectionHandler?.ReleaseInstanceId(element);
        CommandList.Remove(removedNode);
        m_Mappings.TryRemove(removedNode);
    }

    private void RefreshChildrenSortingIndices(HierarchyNode node)
    {
        if (!m_Mappings.TryGetValue(node, out var parent))
            return;

        for (var i = 0; i < parent.hierarchy.childCount; ++i)
        {
            var child = parent.hierarchy[i];
            if (m_Mappings.TryGetValue(child, out var childNode))
                CommandList.SetSortIndex(childNode, i);
            else
                Debug.Log($"Trying to set sort index for element `{GetDebugName(child)}`");
        }

        CommandList.SortChildren(node);
    }

    private static string GetDebugName(VisualElement element)
    {
        if (element == null)
            return "<null>";
        var name = string.IsNullOrEmpty(element.name) ? "" : $"#{element.name}";
        var typeName = TypeUtility.GetTypeDisplayName(element.GetType());
        return $"{typeName}{name}, id={element.controlid}";
    }

    private IMemoryOwner<HierarchyNode> GetSelection(HierarchyView view, out SelectionContext selectionContext)
    {
        var selectionCount = view.ViewModel.HasAllFlagsCount(HierarchyNodeFlags.Selected);
        var owner = MemoryPool<HierarchyNode>.Shared.Rent(selectionCount);
        var selection = owner.Memory.Span[..selectionCount];
        view.ViewModel.GetNodesWithAllFlags(HierarchyNodeFlags.Selected, selection);

        var containsElements = false;
        var onlyContainsElements = true;
        foreach (var node in selection)
        {
            if (view.Source.GetNodeTypeHandlerBase(node) == this)
                containsElements = true;
            else
                onlyContainsElements = false;
        }

        SelectionContext.SelectionType type;
        if (onlyContainsElements)
            type = SelectionContext.SelectionType.All;
        else if (containsElements)
            type = SelectionContext.SelectionType.Mixed;
        else
            type = SelectionContext.SelectionType.None;

        selectionContext = new SelectionContext(selection, selectionCount, type);
        return owner;
    }

    private static QueryEngine<VisualElement> CreateQueryEngine()
    {
        var engine = new QueryEngine<VisualElement>();
        engine.AddFilter<List<string>>(UISearchTokens.UssClassesSearchToken, CompareClasses);
        if (engine.TryGetFilter(UISearchTokens.UssClassesSearchToken, out var classesFilter))
        {
            classesFilter.AddTypeParser(ExtractUssClassesFromInputString);
            classesFilter.AddOrUpdatePropositionData(
                label: "classes",
                category: "UI",
                help: "Search by USS classes",
                replacement: UISearchTokens.UssClassesSearchToken + ":",
                type: typeof(string),
                priority: -2,
                icon: UIResources.GetIconForType(typeof(StyleSheet), UIResources.RequestSize.Px16).texture
            );
        }

        engine.AddFilter(UISearchTokens.TypeSearchToken, ExtractTypeName);
        if (engine.TryGetFilter(UISearchTokens.TypeSearchToken, out var typeFilter))
        {
            typeFilter.AddOrUpdatePropositionData(
                label: "type",
                category: "UI",
                help: "Search by UI Toolkit types",
                replacement: UISearchTokens.TypeSearchToken + ":",
                type: typeof(string),
                priority: -1,
                icon: UIResources.GetIconForType(typeof(VisualTreeAsset), UIResources.RequestSize.Px16).texture
            );
        }

        return engine;
    }

    private static ParseResult<List<string>> ExtractUssClassesFromInputString(string classesString)
    {
        if (string.IsNullOrEmpty(classesString))
            return new ParseResult<List<string>>(false, null);

        var classes = new List<string>();
        // [TODO] MP: Change to add TrimEntries when we get access to it.
        var classesArray = classesString.Split(", ", StringSplitOptions.RemoveEmptyEntries);
        foreach (var classString in classesArray)
        {
            classes.Add(classString.Trim());
        }

        return new ParseResult<List<string>>(true, classes);
    }

    private static string ExtractTypeName(VisualElement element)
    {
        return element.typeName;
    }

    private static bool CompareClasses(VisualElement element, string _, List<string> classes)
    {
        if (classes == null || classes.Count == 0)
            return false;

        for (var i = 0; i < classes.Count; ++i)
        {
            var found = false;
            // While this comparison is not particularly efficient, it allows users
            // to search for partial uss classes.
            foreach (var ussClass in element.GetClasses())
            {
                if (!ussClass.Contains(classes[i], StringComparison.InvariantCultureIgnoreCase))
                    continue;

                found = true;
                break;
            }

            if (!found)
                return false;
        }

        return true;
    }

    HierarchyNode IHierarchyEntityIdConverter.GetNode(EntityId entityId)
    {
        return m_Mappings.TryGetNodeFromSelectionHandle(entityId, out var node)
            ? node
            : HierarchyNode.Null;
    }

    void IHierarchyEntityIdConverter.GetNodes(ReadOnlySpan<EntityId> entityIds, Span<HierarchyNode> outNodes)
    {
        for (var i = 0; i < entityIds.Length; ++i)
        {
            ref var outNode = ref outNodes[i];
            if (outNode != HierarchyNode.Null)
                continue;

            if (m_Mappings.TryGetNodeFromSelectionHandle(entityIds[i], out var node))
                outNode = node;
        }
    }

    EntityId IHierarchyEntityIdConverter.GetEntityId(in HierarchyNode node)
    {
        return m_Mappings.TryGetSelectionHandle(node, out var selectionHandle)
            ? selectionHandle
            : EntityId.None;
    }

    void IHierarchyEntityIdConverter.GetEntityIds(ReadOnlySpan<HierarchyNode> nodes,
        Span<EntityId> outEntityIds)
    {
        for (var i = 0; i < nodes.Length; ++i)
        {
            ref var outEntityId = ref outEntityIds[i];
            if (outEntityId != EntityId.None)
                continue;

            if (m_Mappings.TryGetSelectionHandle(nodes[i], out var selectionHandle))
                outEntityId = selectionHandle;
        }
    }

    private void OnDisplayOptionsChanged(UIHierarchyDisplayOptions options)
    {
        m_DisplayOptions = options;
        if (Hierarchy.IsCreated)
            CommandList.SetDirty();
    }
}
