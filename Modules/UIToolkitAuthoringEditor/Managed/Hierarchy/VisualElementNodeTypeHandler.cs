// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using Unity.Properties;
using Unity.UIToolkit.Editor.Utilities;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal abstract class VisualElementNodeTypeHandler :
    HierarchyNodeTypeHandler,
    IVisualElementChangeProcessor,
    IHierarchySearchPropositionProvider,
    IHierarchyEntityIdConverter,
    IHierarchyEditorNodeTypeHandler
{
    public const string NodeTypeName = "VisualElement";
    protected const string VisualElementDisabledUssClass = "unity-disabled";

    public static Regex elementNameRegex { get; } = new (@"^[a-zA-Z0-9\-_]+$", RegexOptions.Compiled);
    public const string DraggedVisualElementKey = "VisualElementHandler__DraggedVisualElements";

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
    public const string HierarchyItemTemplatePath = HierarchyItemClassName + "__template-path";
    public const string HierarchyItemDisabledClassName = HierarchyItemClassName + "__disabled";

    private const string k_StyleSheetPath = "UIToolkitAuthoring/Hierarchy/VisualElementNodeTypeHandler.uss";

    static readonly ManipulatorActivationFilter k_StageAltActivationFilter = new() { button = MouseButton.LeftMouse, modifiers = EventModifiers.Alt };

    private class Mappings
    {
        private readonly Dictionary<HierarchyNode, VisualElement> m_Map = new();
        private readonly Dictionary<VisualElement, HierarchyNode> m_ReversedMap = new();
        private readonly Dictionary<HierarchyNode, EntityId> m_SelectionHandles = new();
        private readonly Dictionary<EntityId, HierarchyNode> m_ReversedSelectionHandles = new();

        public int Count => m_Map.Count;

        public Dictionary<VisualElement, HierarchyNode>.KeyCollection MappedElements => m_ReversedMap.Keys;

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

        public void Remap(List<VisualElementRemap> remappings)
        {
            foreach (var remap in remappings)
            {
                if (TryGetValue(remap.Previous, out var node))
                {
                    m_Map[node] = remap.Remapped;
                    m_ReversedMap[remap.Remapped] = node;
                    m_ReversedMap.Remove(remap.Previous);
                    // Intentionally not remapping selection, because it's based on the node.
                }
            }
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
    readonly HashSet<HierarchyNode> m_HighlightedNodes = new();
    VisualElement m_HoveredElement;

    private StyleSheet m_StyleSheet;
    private StyleSheet m_ThemeStyleSheet;
    private ParsedQuery<VisualElement> m_ParsedQuery;

    private UIHierarchyDisplayOptions m_DisplayOptions;
    private bool m_EnableUIStages;

    internal List<VisualElementAsset> m_NodesToSelect;

    protected IVisualElementSelectionHandler SelectionHandler => m_SelectionHandler;

    /// <summary>
    /// Flags indicating if mutating operations are permitted in the hierarchy.
    /// </summary>
    protected bool isReadonly { get; set; } = true;

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

    public VisualElement HoveredElement
    {
        get => m_HoveredElement;
        set
        {
            if (m_HoveredElement == value)
                return;
            m_HoveredElement = value;
            if (m_HoveredElement != null)
                HighlightUtility.RequestHighlights(m_HoveredElement, CommandSources.Hierarchy);
            else
                HighlightUtility.ClearHighlights();
        }
    }

    protected VisualElementNodeTypeHandler(IVisualElementSelectionHandler selectionHandler)
    {
        m_QueryEngine = CreateQueryEngine();
        m_SelectionHandler = selectionHandler;
        UIToolkitAuthoringSettings.DisplayOptionsChanged += OnDisplayOptionsChanged;
        m_DisplayOptions = UIToolkitAuthoringSettings.DisplayOptions;
        UIToolkitAuthoringSettings.UIStagesChanged += EnableUIStages;
        m_EnableUIStages = UIToolkitAuthoringSettings.EnableUIStages;
    }

    protected override void Initialize()
    {
        UICommandQueue.RegisterHandler<HighlightCommand>(ProcessHighlightElementsCommand);
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.Dispose"/>>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        HoveredElement = null;
        UnregisterAllPanels();
        UIToolkitAuthoringSettings.DisplayOptionsChanged -= OnDisplayOptionsChanged;
        UICommandQueue.UnregisterHandler<HighlightCommand>(ProcessHighlightElementsCommand);
    }

    #region HierarchyNodeTypeHandler


    /// <inheritdoc cref="HierarchyNodeTypeHandler.GetNodeTypeName"/>>
    public sealed override string GetNodeTypeName()
    {
        return NodeTypeName;
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

    protected override void OnBindItem(HierarchyViewItem item)
    {
        if (m_Mappings.TryGetValue(item.Node, out var element))
        {
            if (m_HighlightedNodes.Contains(item.Node))
            {
                var highlightColor = EditorGUIUtility.isProSkin ? 0.1888f : 0.6980f;
                item.RowContainer.style.backgroundColor = new Color(highlightColor, highlightColor, highlightColor, 1.0f);
            }

            item.Icon.style.backgroundImage = GetIcon(element);
            Bind(item, element);
            BindNavigation(item, element);
            item.RowContainer.RegisterCallback<PointerEnterEvent, VisualElement>(OnStartHover, element);
            item.RowContainer.RegisterCallback<PointerLeaveEvent>(OnEndHover);
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
        item.RowContainer.style.backgroundColor = StyleKeyword.Null;

        if (m_Mappings.TryGetValue(item.Node, out var element))
        {
            Unbind(item, element);
            UnbindNavigation(item, element);
            item.RowContainer.UnregisterCallback<PointerEnterEvent, VisualElement>(OnStartHover);
            item.RowContainer.UnregisterCallback<PointerLeaveEvent>(OnEndHover);
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
            item.Q(className: HierarchyItemTemplatePath)?.RemoveFromHierarchy();
            UnsetStageNodeNavigation(item);
        }
    }

    /// <inheritdoc cref="HierarchyNodeTypeHandler.OnInitializingView"/>>
    protected override void OnBindView(HierarchyView view)
    {
        view.StyleContainer.styleSheets.Add(StyleSheet);
        view.StyleContainer.styleSheets.Add(ThemeStyleSheet);
    }

    #endregion // HierarchyNodeTypeHandler

    #region IHierarchyEditorNodeTypeHandler
    bool IHierarchyEditorNodeTypeHandler.CanCut(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return CanCut(view, in selection);
    }

    bool IHierarchyEditorNodeTypeHandler.OnCut(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return OnCut(view, in selection);
    }

    bool IHierarchyEditorNodeTypeHandler.CanCopy(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return CanCopy(view, in selection);
    }

    bool IHierarchyEditorNodeTypeHandler.OnCopy(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return OnCopy(view, in selection);
    }

    bool IHierarchyEditorNodeTypeHandler.CanPaste(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return CanPaste(view, in selection);
    }

    bool IHierarchyEditorNodeTypeHandler.OnPaste(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return OnPaste(view, in selection);
    }

    bool IHierarchyEditorNodeTypeHandler.CanPasteAsChild(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return CanPasteAsChild(view, in selection);
    }

    bool IHierarchyEditorNodeTypeHandler.OnPasteAsChild(HierarchyView view, bool keepWorldPos)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return OnPasteAsChild(view, in selection);
    }

    bool IHierarchyEditorNodeTypeHandler.CanSetName(HierarchyView view, in HierarchyNode node)
    {
        if (isReadonly)
            return false;

        return m_Mappings.TryGetValue(in node, out var element) && CanSetName(view, in node, element);
    }

    bool IHierarchyEditorNodeTypeHandler.OnSetName(HierarchyView view, in HierarchyNode node, string name)
    {
        if (!m_Mappings.TryGetValue(node, out var element))
        {
            CommandList.SetDirty();
            return false;
        }

        if (!ValidateName(name))
        {
            CommandList.SetDirty();
            return false;
        }

        var elementVea = element.visualElementAsset;
        if (elementVea == null)
        {
            CommandList.SetDirty();
            return false;
        }
        new SetElementNameCommand(elementVea, name).Execute();
        element.name = name;
        return true;
    }

    string IHierarchyEditorNodeTypeHandler.GetDisplayName(HierarchyView view, in HierarchyNode node)
    {
        if (!m_Mappings.TryGetValue(node, out var element) || element == null)
            return "<null>";

        return GetDisplayName(view, node, element);
    }

    bool IHierarchyEditorNodeTypeHandler.CanDuplicate(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return CanDuplicate(view, in selection);
    }

    bool IHierarchyEditorNodeTypeHandler.OnDuplicate(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return OnDuplicate(view, in selection);
    }

    bool IHierarchyEditorNodeTypeHandler.CanDelete(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return CanDelete(view, in selection);
    }

    bool IHierarchyEditorNodeTypeHandler.OnDelete(HierarchyView view)
    {
        if (isReadonly)
            return false;

        using var memoryOwner = GetSelection(view, out var selection);
        return OnDelete(view, in selection);
    }

    bool IHierarchyEditorNodeTypeHandler.CanFindReferences(HierarchyView view)
    {
        // We currently don't support visual element references.
        return false;
    }

    bool IHierarchyEditorNodeTypeHandler.OnFindReferences(HierarchyView view)
    {
        // We currently don't support visual element references.
        return false;
    }

    bool IHierarchyEditorNodeTypeHandler.CanDoubleClick(HierarchyView view, in HierarchyNode node)
    {
        return m_Mappings.TryGetValue(in node, out var element) && CanDoubleClick(view, in node, element);
    }

    bool IHierarchyEditorNodeTypeHandler.OnDoubleClick(HierarchyView view, in HierarchyNode node)
    {
        return m_Mappings.TryGetValue(node, out var element) && OnDoubleClick(view, in node, element);
    }

    void IHierarchyEditorNodeTypeHandler.GetTooltip(HierarchyViewItem item, bool isFiltering, StringBuilder tooltip)
    {
        if (item == null)
            return;

        if (m_Mappings.TryGetValue(item.Node, out var element))
        {
            tooltip.Clear();
            tooltip.Append(GetTooltip(in item.Node, element, isFiltering));
        }
    }

    void IHierarchyEditorNodeTypeHandler.PopulateContextMenu(HierarchyView view, HierarchyViewItem item, DropdownMenu menu)
    {
        if (item == null)
            return;

        if (m_Mappings.TryGetValue(item.Node, out var element))
            PopulateContextMenu(view, in item.Node, element, menu);
    }

    bool IHierarchyEditorNodeTypeHandler.AcceptParent(HierarchyView view, in HierarchyNode parent)
    {
        if (isReadonly)
            return false;

        if (parent == Hierarchy.Root)
            return AcceptRootAsParent();

        var handler = view.ViewModel.GetNodeTypeHandlerBase(in parent);
        if (handler != this)
            return false;

        if (TryGetElementFromNode(parent, out var physicalParentElement))
        {
            var logicalParent = GetLogicalParentFromPhysicalParent(physicalParentElement);
            if (TryGetNodeFromElement(logicalParent, out var logicalParentNode))
            {
                return AcceptParent(view, in logicalParentNode, logicalParent);
            }
        }

        return false;
    }

    protected static VisualElement GetLogicalParentFromPhysicalParent(VisualElement physicalParent)
    {
        return physicalParent.GetFirstAncestorWhere(ve => ve.contentContainer == physicalParent) ?? physicalParent;
    }

    protected virtual bool AcceptRootAsParent() => false;

    protected virtual bool AcceptParent(HierarchyView view, in HierarchyNode parentNode, VisualElement parent) => false;

    bool IHierarchyEditorNodeTypeHandler.AcceptChild(HierarchyView view, in HierarchyNode child)
    {
        if (isReadonly)
            return false;

        var handler = view.ViewModel.GetNodeTypeHandlerBase(in child);
        if (handler != this)
            return false;

        return TryGetElementFromNode(child, out var childElement) && AcceptChild(view, in child, childElement);
    }

    protected virtual bool AcceptChild(HierarchyView view, in HierarchyNode childNode, VisualElement child) => false;

    bool IHierarchyEditorNodeTypeHandler.CanStartDrag(HierarchyView view, ReadOnlySpan<HierarchyNode> nodes)
    {
        // This requires custom handling compared to the other end points because returning false here
        // will prevent drag and drop of other handlers too. This is an issue that will be fixed on the hierarchy's side.
        using var memoryOwner = GetSelection(view, out var selection);
        return selection.Type switch
        {
            // No elements selected, revert to default handling.
            SelectionContext.SelectionType.None => true,
            // Mixed selection, disallow dragging.
            SelectionContext.SelectionType.Mixed => false,
            // We're only dragging visual element here.
            SelectionContext.SelectionType.All => CanStartDrag(view, in selection),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    void IHierarchyEditorNodeTypeHandler.OnStartDrag(in HierarchyViewDragAndDropSetupData data) =>
        InitializeDrag(in data);

    protected virtual void InitializeDrag(in HierarchyViewDragAndDropSetupData data)
    {
        var list = new List<VisualElement>();
        foreach (var node in data.Nodes)
        {
            if (TryGetElementFromNode(in node, out var element))
            {
                list.Add(element);
            }
        }
        data.SetGenericData(DraggedVisualElementKey, list);
    }

    DragVisualMode IHierarchyEditorNodeTypeHandler.CanDrop(in HierarchyViewDragAndDropHandlingData data) => HandleDrop(in data, false);

    DragVisualMode IHierarchyEditorNodeTypeHandler.OnDrop(in HierarchyViewDragAndDropHandlingData data) => HandleDrop(in data, true);

    protected virtual DragVisualMode HandleDrop(in HierarchyViewDragAndDropHandlingData data, bool performDrop) => DragVisualMode.None;
    #endregion

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
    protected virtual bool ValidateName(string name)
    {
        return elementNameRegex.IsMatch(name);
    }

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
    /// Executes the copy operation on the selected nodes.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if action is executed, <see langword="false"/> otherwise.</returns>
    protected virtual bool OnCopy(HierarchyView view, in SelectionContext selection) => false;

    /// <summary>
    /// Determines if selected nodes can be cut.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
    protected virtual bool CanCut(HierarchyView view, in SelectionContext selection) => false;

    /// <summary>
    /// Executes the cut operation on the selected nodes.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if action is executed, <see langword="false"/> otherwise.</returns>
    protected virtual bool OnCut(HierarchyView view, in SelectionContext selection) => false;

    /// <summary>
    /// Determines if selected nodes can be deleted.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
    protected virtual bool CanDelete(HierarchyView view, in SelectionContext selection) => false;

    /// <summary>
    /// Executes the delete operation on the selected nodes.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if action is executed, <see langword="false"/> otherwise.</returns>
    protected virtual bool OnDelete(HierarchyView view, in SelectionContext selection) => false;

    /// <summary>
    /// Determines if selected nodes can be duplicated.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
    protected virtual bool CanDuplicate(HierarchyView view, in SelectionContext selection) => false;

    /// <summary>
    /// Executes the duplicate operation on the selected nodes.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if action is executed, <see langword="false"/> otherwise.</returns>
    protected virtual bool OnDuplicate(HierarchyView view, in SelectionContext selection) => false;

    /// <summary>
    /// Determines if copied nodes can be pasted.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
    protected virtual bool CanPaste(HierarchyView view, in SelectionContext selection) => false;

    /// <summary>
    /// Executes the paste operation on the selected nodes.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if action is executed, <see langword="false"/> otherwise.</returns>
    protected virtual bool OnPaste(HierarchyView view, in SelectionContext selection) => false;

    /// <summary>
    /// Determines if copied nodes can be pasted as child.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if action is supported, <see langword="false"/> otherwise.</returns>
    protected virtual bool CanPasteAsChild(HierarchyView view, in SelectionContext selection) => false;

    /// <summary>
    /// Executes the paste on child operation on the selected nodes.
    /// </summary>
    /// <param name="view">The <see cref="HierarchyView"/>.</param>
    /// <param name="selection">The selection.</param>
    /// <returns><see langword="true"/> if action is executed, <see langword="false"/> otherwise.</returns>
    protected virtual bool OnPasteAsChild(HierarchyView view, in SelectionContext selection) => false;

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
    protected virtual bool CanStartDrag(HierarchyView view, in SelectionContext selection) => true;

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
            typeNameLabel.EnableInClassList(HierarchyItemDisabledClassName, isReadonly || !element.enabledSelf);
        }

        nameElement.EnableInClassList(HierarchyItemDisabledClassName, isReadonly || !element.enabledSelf);

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

    protected virtual void BindNavigation(HierarchyViewItem item, VisualElement container)
    {
        if (container.visualElementAsset is TemplateAsset subDocument)
        {
            var nameElement = item.Q(className: "hierarchy-item__name");
            var index = nameElement.parent.IndexOf(nameElement);
            var path = AssetDatabase.GetAssetPath(subDocument.ResolveTemplate());
            var filename = Path.GetFileName(path);
            var label = new Label(filename){ tooltip = path };
            label.AddToClassList(HierarchyItemTemplatePath);
            nameElement.parent.Insert(index + 1, label);
        }
    }

    protected virtual void UnbindNavigation(HierarchyViewItem item, VisualElement container)
    {
        item.Q(className:HierarchyItemTemplatePath)?.RemoveFromHierarchy();
    }

    protected void SetStageNodeNavigation(HierarchyViewItem item, VisualElement container)
    {
        switch (container)
        {
            case IPanelComponentRootElement panelComponentRoot when panelComponentRoot.panelComponent != null && panelComponentRoot.panelComponent.visualTreeAsset != null:
            {
                var panelComponent = panelComponentRoot.panelComponent;
                var context = new VisualTreeAssetEditingContext(panelComponent.visualTreeAsset, panelComponent.panelSettings);
                SetStageNodeNavigation(item, context);
                break;
            }
            case { visualElementAsset: TemplateAsset subDocument }:
            {
                using var _ = ListPool<TemplateAsset>.Get(out var subDocumentPath);
                container.GenerateSubDocumentPath(subDocumentPath);

                var rootVisualTreeAsset = GetRootVisualTreeAsset(container);
                if (!VisualTreeAssetEditingContext.ValidateSubDocumentIsPartOrMainAssetHierarchy(rootVisualTreeAsset, NoAllocHelpers.CreateSpan(subDocumentPath)))
                {
                    UnsetStageNodeNavigation(item);
                    break;
                }

                var panelSettings = GetPanelSettings(container);
                var context = new VisualTreeAssetEditingContext(
                    rootVisualTreeAsset,
                    subDocumentPath.ToArray(),
                    SubDocumentOptions.InContext,
                    panelSettings
                );
                SetStageNodeNavigation(item, context);
                break;
            }
            default:
                UnsetStageNodeNavigation(item);
                break;
        }
    }

    private VisualTreeAsset GetRootVisualTreeAsset(VisualElement element)
    {
        var vta = default(VisualTreeAsset);
        while (element != null)
        {
            if (element.visualTreeAssetSource != null)
                vta = element.visualTreeAssetSource;
            element = element.parent;
        }

        return vta;
    }

    protected PanelSettings GetPanelSettings(VisualElement element)
    {
        return element.GetPanelSettings();
    }

    protected void SetStageNodeNavigation(HierarchyViewItem item, VisualTreeAssetEditingContext context)
    {
        var modifierKey = Application.platform == RuntimePlatform.OSXEditor ? "Option" : "Alt";
        var navigationTooltip = $"Open Visual Tree Asset in context.\nPress the {modifierKey} modifier key to open in isolation.";
        var navigateButton = item.NavigateIntoButton;

        if (!m_EnableUIStages || navigateButton == null)
            return;

        navigateButton.style.display = DisplayStyle.Flex;
        navigateButton.tooltip = navigationTooltip;
        if (navigateButton.userData == null)
        {
            navigateButton.clickable.activators.Add(k_StageAltActivationFilter);
            navigateButton.clickable.clickedWithEventInfo += OpenStageMode;
        }

        navigateButton.userData = context;
    }

    protected void UnsetStageNodeNavigation(HierarchyViewItem item)
    {
        var navigateButton = item.NavigateIntoButton;
        if (navigateButton == null)
            return;

        navigateButton.style.display = DisplayStyle.None;
        navigateButton.tooltip = null;
        navigateButton.clickable.activators.Remove(k_StageAltActivationFilter);
        navigateButton.clickable.clickedWithEventInfo -= OpenStageMode;
        navigateButton.userData = null;
    }

    void OpenStageMode(EventBase obj)
    {
        if (obj.target is not Button button)
            return;

        if (button.userData is not VisualTreeAssetEditingContext context)
            return;

        var isolationRequested = obj is PointerUpEvent { altKey: true };
        if (isolationRequested && context.SubDocumentPath != null)
        {
            GoToStage(new VisualTreeAssetEditingContext(
                context.RootVisualTreeAsset,
                context.SubDocumentPath,
                SubDocumentOptions.Isolation,
                context.PanelSettings
            ), BreadcrumbBar.SeparatorStyle.Line);
        }
        else
        {
            GoToStage(context, isolationRequested ? BreadcrumbBar.SeparatorStyle.Line : BreadcrumbBar.SeparatorStyle.Arrow);
        }
    }

    internal void GoToStage(VisualTreeAssetEditingContext context, BreadcrumbBar.SeparatorStyle separatorStyle)
    {
        var stage = ScriptableObject.CreateInstance<VisualElementEditingStage>();
        stage.SeparatorStyle = separatorStyle;
        stage.SetContext(context);
        StageUtility.GoToStage(stage, false);
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
    protected virtual void PopulateContextMenu(HierarchyView view, in HierarchyNode node, VisualElement element, DropdownMenu menu)
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
    /// <returns>The <see cref="HierarchyNode"/> of the parent of the <paramref name="element"/> or the root node.</returns>
    /// <exception cref="ArgumentNullException">If the <paramref name="element"/> is null.</exception>
    protected virtual bool TryGetParentNode(VisualElement element, out HierarchyNode parentNode)
    {
        if (null == element)
            throw new ArgumentNullException(nameof(element));

        var parent = element.hierarchy.parent;
        if (null == parent)
        {
            parentNode = Hierarchy.Root;
            return true;
        }

        while (true)
        {
            switch (ShouldCreateNode(parent))
            {
                case NodeCreationType.DontCreate:
                    parentNode = HierarchyNode.Null;
                    break;
                case NodeCreationType.Create:
                    if (m_Mappings.TryGetValue(parent, out parentNode))
                        return true;
                    parentNode = HierarchyNode.Null;
                    return false;
                case NodeCreationType.CreateChildren:
                    parent = parent.hierarchy.parent;
                    if (parent == null)
                    {
                        parentNode = Hierarchy.Root;
                        return true;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    /// <summary>
    /// Register a panel to start getting changes.
    /// </summary>
    /// <param name="panel">The requested panel.</param>
    protected void RegisterPanel(Panel panel)
    {
        panel.RegisterChangeProcessor(this);
        m_RegisteredPanels.Add(panel);
        if (Hierarchy.IsCreated)
            Rebuild(panel);
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

    protected bool TryGetSelectionObject(HierarchyNode node, out EntityId entityId)
    {
        return m_Mappings.TryGetSelectionHandle(node, out entityId);
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

    internal void RequestSelectionOnNextUpdate(IList<VisualElementAsset> assets)
    {
        m_NodesToSelect ??= new List<VisualElementAsset>();
        m_NodesToSelect.AddRange(assets);
    }

    protected List<VisualElementAsset> GetDelayedSelectionRequests()
    {
        return m_NodesToSelect;
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
            m_Mappings.Remap(remappings);
            m_SelectionHandler.Remap(remappings);

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

        UnregisterOrphanedElements();
    }

    void UnregisterOrphanedElements()
    {
        using var _ = ListPool<VisualElement>.Get(out var mappedChildren);
        mappedChildren.AddRange(m_Mappings.MappedElements);
        foreach(var child in mappedChildren)
            Clear(child);
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

        CommandList.Remove(removedNode);
        m_Mappings.TryRemove(removedNode);
        m_SelectionHandler?.ReleaseInstanceId(element);
    }

    private void RefreshChildrenSortingIndices(HierarchyNode node)
    {
        if (node == Hierarchy.Root)
        {
            var children = Hierarchy.GetChildren(node);
            VisualElement rootParent = null;
            for (var i = 0; i < children.Length; ++i)
            {
                if (!TryGetElementFromNode(children[i], out var element) || element.panel == null)
                {
                    continue;
                }

                if (rootParent == null)
                    rootParent = element.hierarchy.parent;
            }

            if (rootParent == null)
                return;
            for (var i = 0; i < rootParent.hierarchy.childCount; ++i)
            {
                if (!TryGetNodeFromElement(rootParent.hierarchy[i], out var elementNode))
                    continue;
                CommandList.SetSortIndex(elementNode, i);
            }
            CommandList.SortChildren(node);
            return;
        }

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
        var selectionCount = view.ViewModel.HasFlagsCount(HierarchyNodeFlags.Selected);
        var owner = MemoryPool<HierarchyNode>.Shared.Rent(selectionCount);
        var selection = owner.Memory.Span[..selectionCount];
        view.ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Selected, selection);

        var containsElements = false;
        var onlyContainsElements = true;
        foreach (var node in selection)
        {
            if (view.ViewModel.GetNodeTypeHandlerBase(node) == this)
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

    private void EnableUIStages(bool value)
    {
        m_EnableUIStages = value;
        if (Hierarchy.IsCreated)
            CommandList.SetDirty();
    }

    void OnStartHover(PointerEnterEvent evt, VisualElement element)
    {
        HoveredElement = element;
    }

    void OnEndHover(PointerLeaveEvent evt)
    {
        HoveredElement = null;
    }

    void ProcessHighlightElementsCommand(in CommandContext context)
    {
        m_HighlightedNodes.Clear();
        if (Hierarchy.IsCreated)
            CommandList.SetDirty();
        if (context.Status != CommandExecutionStatus.Success)
            return;

        if (context.Source == CommandSources.Hierarchy)
            return;

        if (context.Command is not HighlightCommand command)
            return;

        if (command.Elements == null)
            return;

        foreach (var element in command.Elements)
        {
            if (m_Mappings.TryGetValue(element, out var node))
            {
                m_HighlightedNodes.Add(node);
            }
        }
    }
}
