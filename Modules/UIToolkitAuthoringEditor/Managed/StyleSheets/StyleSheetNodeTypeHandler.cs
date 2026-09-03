// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Hierarchy;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// 1. When a StyleSheet is added to hierarchy:
/// - AddStyleSheet() calls m_StyleSheetSelectionHandler.AcquireInstanceId(styleSheet, isReadOnly)
/// - Selection handler creates a StyleSheetSelection ScriptableObject and returns its EntityId
/// - EntityId is stored in mappings alongside the HierarchyNode
/// 2. When a node is selected in hierarchy:
/// - IHierarchyEntityIdConverter.GetEntityId(node) returns the stored EntityId
/// - Unity's selection system receives the EntityId (which points to the ScriptableObject)
/// - Selection.activeObject becomes the StyleSheetSelection object
/// - StyleSheetSelectionEditor displays the inspector UI
/// 3. When a StyleSheet is removed:
/// - RemoveStyleSheet() calls ReleaseInstanceId() on both handlers
/// - Reference count decreases
/// - If count reaches 0, the ScriptableObject is destroyed
/// </summary>
internal class StyleSheetNodeTypeHandler : HierarchyNodeTypeHandler
{
    internal class StyleSheetEditorExporter : StyleSheetExporter
    {
        public string ToUssString(StyleSheet styleSheet, StyleComplexSelector[] selectors, UssExportOptions options)
        {
            using var _ = StringBuilderPool.Get(out var stringBuilder);
            var context = new ExportContext(styleSheet, stringBuilder, options);

            for (var i = 0; i < selectors.Length; ++i)
            {
                if (context.options.IsSelectorIgnored(selectors[i]))
                    continue;

                if (stringBuilder.Length > 0)
                    stringBuilder.Append(", ");

                WriteSelector(ref context, selectors[i]);
            }

            return stringBuilder.ToString();
        }
    }

    internal readonly struct Node
    {
        public readonly StyleSheet StyleSheet;
        public readonly StyleRule Rule;
        public readonly bool IsReadOnly;
        public readonly VisualTreeAsset OwningDocument;
        public readonly bool IsGroup;

        public Node(StyleSheet styleSheet, bool isReadOnly = false, VisualTreeAsset owningDocument = null)
        {
            StyleSheet = styleSheet;
            Rule = null;
            IsReadOnly = isReadOnly;
            OwningDocument = owningDocument;
            IsGroup = false;
        }

        public Node(StyleSheet styleSheet, StyleRule rule, bool isReadOnly = false)
        {
            StyleSheet = styleSheet;
            Rule = rule;
            IsReadOnly = isReadOnly;
            OwningDocument = null;
            IsGroup = false;
        }

        Node(bool isGroup)
        {
            StyleSheet = null;
            Rule = null;
            IsReadOnly = true;
            OwningDocument = null;
            IsGroup = isGroup;
        }

        public static Node CreateGroup() => new(isGroup: true);
    }

    internal class NodeMappings
    {
        private readonly Dictionary<HierarchyNode, StyleSheet> m_Map = new();
        private readonly Dictionary<StyleSheet, HierarchyNode> m_ReversedMap = new();
        readonly Dictionary<HierarchyNode, Node> m_HierarchyNodeToNode = new();
        readonly Dictionary<HierarchyNode, EntityId> m_SelectionHandles = new();
        readonly Dictionary<EntityId, HierarchyNode> m_ReversedSelectionHandles = new();

        public bool TryAdd(HierarchyNode hierarchyNode, Node node, EntityId selectionHandle)
        {
            if (hierarchyNode == HierarchyNode.Null)
                return false;

            if (!m_HierarchyNodeToNode.TryAdd(hierarchyNode, node))
                return false;

            if (selectionHandle != EntityId.None)
            {
                if (!m_SelectionHandles.TryAdd(hierarchyNode, selectionHandle))
                {
                    m_HierarchyNodeToNode.Remove(hierarchyNode);
                    return false;
                }

                m_ReversedSelectionHandles.TryAdd(selectionHandle, hierarchyNode);
            }

            // Also maintain StyleSheet mappings for the root node
            if (node.Rule == null && node.StyleSheet != null)
            {
                m_Map.TryAdd(hierarchyNode, node.StyleSheet);

                if (!node.IsReadOnly || !m_ReversedMap.ContainsKey(node.StyleSheet))
                    m_ReversedMap[node.StyleSheet] = hierarchyNode;
            }

            return true;
        }

        public bool TryGetValue(StyleSheet styleSheet, out HierarchyNode node)
        {
            if (styleSheet != null)
                return m_ReversedMap.TryGetValue(styleSheet, out node);

            node = HierarchyNode.Null;
            return false;
        }

        public bool TryGetValue(StyleRule styleRule, out HierarchyNode node)
        {
            if (styleRule == null)
            {
                node = HierarchyNode.Null;
                return false;
            }

            node = HierarchyNode.Null;
            var found = false;
            foreach (var kvp in m_HierarchyNodeToNode)
            {
                if (kvp.Value.Rule != styleRule)
                    continue;
                node = kvp.Key;
                found = true;
                if (!kvp.Value.IsReadOnly)
                    return true;
            }

            return found;
        }

        public bool TryGetValue(HierarchyNode hierarchyNode, out Node node)
        {
            if (hierarchyNode != HierarchyNode.Null)
                return m_HierarchyNodeToNode.TryGetValue(hierarchyNode, out node);

            node = default;
            return false;
        }

        public bool TryGetSelectionHandle(HierarchyNode hierarchyNode, out EntityId entityId)
        {
            if (hierarchyNode != HierarchyNode.Null)
                return m_SelectionHandles.TryGetValue(hierarchyNode, out entityId);

            entityId = EntityId.None;
            return false;
        }

        public bool TryGetNodeFromSelectionHandle(EntityId entityId, out HierarchyNode node)
        {
            if (entityId != EntityId.None)
                return m_ReversedSelectionHandles.TryGetValue(entityId, out node);

            node = HierarchyNode.Null;
            return false;
        }

        public bool TryRemove(HierarchyNode hierarchyNode)
        {
            if (hierarchyNode == HierarchyNode.Null)
                return false;

            // Remove StyleSheet mappings if this is a root node
            if (m_Map.Remove(hierarchyNode, out var stylesheet))
            {
                if (m_ReversedMap.TryGetValue(stylesheet, out var mapped) && mapped == hierarchyNode)
                {
                    if (TryFindNodeForStyleSheet(stylesheet, out var replacement))
                        m_ReversedMap[stylesheet] = replacement;
                    else
                        m_ReversedMap.Remove(stylesheet);
                }
            }

            var removed = m_HierarchyNodeToNode.Remove(hierarchyNode);

            // Group nodes have no selection handle; only clean the selection maps when one exists.
            if (m_SelectionHandles.Remove(hierarchyNode, out var selectionHandle))
            {
                if (m_ReversedSelectionHandles.TryGetValue(selectionHandle, out var mappedNode) && mappedNode == hierarchyNode)
                {
                    if (TryFindNodeForSelectionHandle(selectionHandle, out var replacement))
                        m_ReversedSelectionHandles[selectionHandle] = replacement;
                    else
                        m_ReversedSelectionHandles.Remove(selectionHandle);
                }
            }

            return removed;
        }

        bool TryFindNodeForStyleSheet(StyleSheet stylesheet, out HierarchyNode result)
        {
            result = HierarchyNode.Null;
            var found = false;
            foreach (var (candidate, sheet) in m_Map)
            {
                if (sheet != stylesheet)
                    continue;
                result = candidate;
                found = true;
                if (!m_HierarchyNodeToNode.TryGetValue(candidate, out var node) || !node.IsReadOnly)
                    return true;
            }
            return found;
        }

        bool TryFindNodeForSelectionHandle(EntityId selectionHandle, out HierarchyNode result)
        {
            foreach (var (candidate, handle) in m_SelectionHandles)
            {
                if (handle == selectionHandle)
                {
                    result = candidate;
                    return true;
                }
            }
            result = HierarchyNode.Null;
            return false;
        }

        public void RemoveAll(IEnumerable<HierarchyNode> nodes)
        {
            foreach (var node in nodes)
                TryRemove(node);
        }

        public bool TryGetEntityId(HierarchyNode node, out EntityId entityId)
        {
            return m_SelectionHandles.TryGetValue(node, out entityId);
        }

        public bool TryGetNodeFromEntityId(EntityId entityId, out HierarchyNode node)
        {
            return m_ReversedSelectionHandles.TryGetValue(entityId, out node);
        }

        public void Remap(List<StyleSheetRemap> remappings)
        {
            foreach (var remap in remappings)
            {
                if (TryGetValue(remap.Previous, out var hierarchyNode))
                {
                    m_Map[hierarchyNode] = remap.Remapped;
                    m_ReversedMap[remap.Remapped] = hierarchyNode;
                    m_ReversedMap.Remove(remap.Previous);
                    if (TryGetValue(hierarchyNode, out var node)) {
                        m_HierarchyNodeToNode[hierarchyNode] = node;
                    }
                    // Intentionally not remapping selection, because it's based on the node.
                }
            }
        }
    }

    readonly NodeMappings m_Mappings = new();
    protected readonly StyleSheetEditorExporter m_Exporter = new();
    [NoAutoStaticsCleanup] // immutable export-options, safe to persist
    internal static readonly StyleSheetExporter.UssExportOptions s_ExportOptions = new()
    {
        ignoreSelectorPrefixList = new[] { "__unity" }
    };

    readonly IStyleSheetSelectionHandler m_StyleSheetSelectionHandler;
    readonly IStyleRuleSelectionHandler m_StyleRuleSelectionHandler;
    readonly Dictionary<HierarchyViewItem, (Action, Action<string, bool>)> m_RenameCallbacks = new();
    readonly HashSet<StyleSheet> m_NewlyAddedStyleSheets = new();
    readonly HashSet<HierarchyNode> m_HighlightedNodes = new();
    StyleRule m_HoveredRule;

    internal StyleRule HoveredRule
    {
        get => m_HoveredRule;
        set
        {
            if (m_HoveredRule == value)
                return;
            m_HoveredRule = value;
            if (m_HoveredRule != null)
                HighlightUtility.RequestHighlights(m_HoveredRule, CommandSources.StyleSheets);
            else
                HighlightUtility.ClearHighlights();
        }
    }

    protected HashSet<StyleSheet> NewlyAddedStyleSheets => m_NewlyAddedStyleSheets;

    DragPreviewWindow DragPreviewWindow;

    internal static readonly string DraggedSelectorKey = "StyleSheetNodeTypeHandler.DraggedSelector";
    static readonly Regex s_SelectorTokenRegex = new(@"(\s+[>+~]\s+|\s+)", RegexOptions.Compiled);

    internal NodeMappings Mappings => m_Mappings;
    const long k_MinDragDurationMs = 150;

    internal static readonly string StyleSheetsWindowUssClassName = "unity-style-sheets-window";
    internal static readonly string StyleSheetsWindowHierarchyViewUssClassName = StyleSheetsWindowUssClassName + "__hierarchy-view";
    internal static readonly string StyleRuleHeaderUssClassName = StyleSheetsWindowUssClassName + "__style-rule-header";
    internal static readonly string StyleRuleHeaderActiveUssClassName = StyleRuleHeaderUssClassName + "--active-stylesheet";
    internal static readonly string StyleSheetReadOnlyRowUssClassName = StyleSheetsWindowUssClassName + "__row--readonly";
    internal static readonly string StyleSheetGroupRowUssClassName = StyleSheetsWindowUssClassName + "__group-row";
    internal static readonly string StyleRuleSelectorNameUssClass = "unity-builder-code-label--element-name";
    internal static readonly string StyleRuleSelectorTypeUssClass = "unity-builder-code-label--element-type";
    internal static readonly string StyleRuleSelectorPseudoStateUssClass = "unity-builder-code-label--element-pseudo-state";
    internal static readonly string StyleRuleSelectorChainedClassUssClass = StyleSheetsWindowUssClassName + "__class-pill--chained";
    internal const string GroupNodeName = "Inherited Style Sheets";

    protected IStyleRuleSelectionHandler SelectionHandler => m_StyleRuleSelectionHandler;

    public StyleSheetsWindow Window { get; set; }

    // Tracks drag start time to filter spurious IMGUI events during editor initialization
    long m_DragStartTime;

    /// <summary>
    /// Flags indicating if mutating operations are permitted in the hierarchy.
    /// </summary>
    protected bool isReadonly { get; set; } = true;

    /// <summary>
    /// Toggles whether the hierarchy is read-only. The window drives this from its current data source:
    /// editable when inside a UI Stage, read-only when displaying a selected panel component's document.
    /// </summary>
    internal void SetReadOnly(bool value) => isReadonly = value;

    public StyleSheetNodeTypeHandler()
        : this(new StyleSheetSelectionHandler(), new StyleRuleSelectionHandler())
    {
    }

    public StyleSheetNodeTypeHandler(IStyleSheetSelectionHandler styleSheetSelectionHandler, IStyleRuleSelectionHandler styleRuleSelectionHandler)
    {
        m_StyleSheetSelectionHandler = styleSheetSelectionHandler;
        m_StyleRuleSelectionHandler = styleRuleSelectionHandler;
    }

    public override HierarchyNode GetNodeFromEntityId(EntityId entityId)
    {
        return m_Mappings.TryGetNodeFromSelectionHandle(entityId, out var node)
            ? node
            : HierarchyNode.Null;
    }

    public override int GetNodesFromEntityIds(ReadOnlySpan<EntityId> entityIds, Span<HierarchyNode> outNodes)
    {
        var resolved = 0;
        for (var i = 0; i < entityIds.Length; ++i)
        {
            ref var outNode = ref outNodes[i];
            if (outNode != HierarchyNode.Null)
            {
                ++resolved;
                continue;
            }

            if (m_Mappings.TryGetNodeFromSelectionHandle(entityIds[i], out var node))
            {
                outNode = node;
                ++resolved;
            }
        }
        return entityIds.Length - resolved;
    }

    public override EntityId GetEntityIdFromNode(in HierarchyNode node)
    {
        return m_Mappings.TryGetSelectionHandle(node, out var entityId)
            ? entityId
            : EntityId.None;
    }

    public override int GetEntityIdsFromNodes(ReadOnlySpan<HierarchyNode> nodes,
        Span<EntityId> outEntityIds)
    {
        var resolved = 0;
        for (var i = 0; i < nodes.Length; ++i)
        {
            ref var outEntityId = ref outEntityIds[i];
            if (outEntityId != EntityId.None)
            {
                ++resolved;
                continue;
            }

            if (m_Mappings.TryGetSelectionHandle(nodes[i], out var selectionHandle))
            {
                outEntityId = selectionHandle;
                ++resolved;
            }
        }
        return nodes.Length - resolved;
    }

    public HierarchyNode AddStyleSheet(StyleSheet styleSheet, bool isReadOnly = false, VisualTreeAsset owningDocument = null, HierarchyNode parent = default)
    {
        if (!Hierarchy.IsCreated)
            return HierarchyNode.Null;

        var parentNode = parent != HierarchyNode.Null ? parent : Hierarchy.Root;
        CommandList.Add(parentNode, 1, out var root);

        CommandList.SetName(root[0], styleSheet.name + OwnerSuffix(owningDocument));

        var styleSheetEntityId = m_StyleSheetSelectionHandler.AcquireInstanceId(styleSheet, isReadOnly);
        m_Mappings.TryAdd(root[0], new Node(styleSheet, isReadOnly, owningDocument), styleSheetEntityId);

        for (var i = 0; i < styleSheet.rules.Length; i++)
        {
            var rule = styleSheet.rules[i];
            var displayString = m_Exporter.ToUssString(styleSheet, rule.complexSelectors, s_ExportOptions);

            // Only add the rule if it has at least one non-internal selector
            if (!string.IsNullOrWhiteSpace(displayString))
            {
                var ruleEntityId = m_StyleRuleSelectionHandler.AcquireInstanceId(rule, isReadOnly);

                CommandList.Add(root[0], 1, out var ruleNode);
                m_Mappings.TryAdd(ruleNode[0], new Node(styleSheet, rule, isReadOnly), ruleEntityId);
                CommandList.SetName(ruleNode[0], displayString);
            }
        }

        m_NewlyAddedStyleSheets.Add(styleSheet);

        return root[0];
    }

    public HierarchyNode AddStyleSheetGroup()
    {
        if (!Hierarchy.IsCreated)
            return HierarchyNode.Null;

        CommandList.Add(Hierarchy.Root, 1, out var group);
        CommandList.SetName(group[0], GroupNodeName);
        m_Mappings.TryAdd(group[0], Node.CreateGroup(), EntityId.None);
        return group[0];
    }

    protected static string OwnerSuffix(VisualTreeAsset owningDocument) => owningDocument != null ? $" ({StyleSheetAssetUtilities.GetDocumentDisplayName(owningDocument)})" : string.Empty;

    /// <summary>
    /// The label of a style sheet row: its file name, the same unsaved-changes marker the document rows carry
    /// in the hierarchy, and — for an inherited sheet — the document it comes from.
    /// </summary>
    /// <remarks>
    /// The marker is deliberately not gated on the row being editable: a sheet displayed read-only here can
    /// still have been dirtied by the UI Builder or the UI Stage, and that is exactly when saying so matters.
    /// It is part of the display name only, so filtering keeps matching the bare file name.
    /// </remarks>
    protected static string GetStyleSheetDisplayName(in Node node)
    {
        var marker = UIAssetRegistry.LiveInstance?.IsDirty(node.StyleSheet) == true ? "*" : string.Empty;
        return $"{node.StyleSheet.name}.uss{marker}{OwnerSuffix(node.OwningDocument)}";
    }

    public void RefreshStyleSheetNodeName(HierarchyNode node)
    {
        if (Hierarchy.IsCreated && m_Mappings.TryGetValue(node, out var styleNode) && styleNode.Rule == null && styleNode.StyleSheet != null)
            CommandList.SetName(node, styleNode.StyleSheet.name + OwnerSuffix(styleNode.OwningDocument));
    }

    public void RemoveStyleSheetGroup(HierarchyNode groupNode)
    {
        if (groupNode == HierarchyNode.Null)
            return;

        m_Mappings.TryRemove(groupNode);
        if (Hierarchy.IsCreated && Hierarchy.Exists(groupNode))
            CommandList.Remove(groupNode);
    }

    public void RemoveStyleSheet(HierarchyNode rootNode)
    {
        if (rootNode == HierarchyNode.Null)
            return;

        if (Hierarchy.IsCreated && Hierarchy.Exists(rootNode))
        {
            // Remove all child rule node mappings and release selection instances
            var childCount = Hierarchy.GetChildrenCount(rootNode);
            for (var i = 0; i < childCount; i++)
            {
                var childNode = Hierarchy.GetChild(rootNode, i);
                if (m_Mappings.TryGetValue(childNode, out var node) && node.Rule != null)
                {
                    if (node.Rule == m_HoveredRule)
                        HoveredRule = null;

                    m_StyleRuleSelectionHandler.ReleaseInstanceId(node.Rule, node.IsReadOnly);
                }

                m_Mappings.TryRemove(childNode);
            }
        }

        // Release stylesheet selection instance
        if (m_Mappings.TryGetValue(rootNode, out var rootNodeData) && rootNodeData.StyleSheet != null)
        {
            m_StyleSheetSelectionHandler.ReleaseInstanceId(rootNodeData.StyleSheet, rootNodeData.IsReadOnly);
        }

        m_Mappings.TryRemove(rootNode);
        if (Hierarchy.IsCreated && Hierarchy.Exists(rootNode))
            CommandList.Remove(rootNode);
    }

    public void SortTopLevel(HierarchyNode groupNode, IReadOnlyList<HierarchyNode> editableNodes)
    {
        var sortIndex = 0;

        if (groupNode != HierarchyNode.Null)
            CommandList.SetSortIndex(groupNode, sortIndex++);

        foreach (var node in editableNodes)
            CommandList.SetSortIndex(node, sortIndex++);

        CommandList.SortChildren(Hierarchy.Root);
    }

    public void SortGroupChildren(HierarchyNode groupNode, IReadOnlyList<HierarchyNode> parentNodes)
    {
        if (groupNode == HierarchyNode.Null)
            return;

        for (var i = 0; i < parentNodes.Count; i++)
            CommandList.SetSortIndex(parentNodes[i], i);

        CommandList.SortChildren(groupNode);
    }

    public void RefreshStyleSheetRules(HierarchyNode rootNode, StyleSheet styleSheet, bool isReadOnly = false)
    {
        if (rootNode == HierarchyNode.Null || styleSheet == null)
            return;

        if (!Hierarchy.IsCreated || !Hierarchy.Exists(rootNode))
            return;

        using var ruleRemappingsHandle = ListPool<StyleRuleRemap>.Get(out var ruleRemappings);
        using var addedRulesHandle = HashSetPool<StyleRule>.Get(out var addedRules);
        using var removedRulesHandle = HashSetPool<StyleRule>.Get(out var removedRules);

        // Build new rules from the stylesheet
        using var _ = HashSetPool<StyleRule>.Get(out var newRules);
        for (var i = 0; i < styleSheet.rules.Length; i++)
        {
            var rule = styleSheet.rules[i];
            if (rule.complexSelectors.Length > 0)
            {
                newRules.Add(rule);
            }
        }

        // Get existing child nodes and extract their rules
        var childCount = Hierarchy.GetChildrenCount(rootNode);
        var existingRuleNodes = new Dictionary<StyleRule, HierarchyNode>();

        for (var i = 0; i < childCount; i++)
        {
            var childNode = Hierarchy.GetChild(rootNode, i);
            if (m_Mappings.TryGetValue(childNode, out var node) && node.Rule != null)
            {
                existingRuleNodes[node.Rule] = childNode;

                if (!newRules.Contains(node.Rule))
                {
                    removedRules.Add(node.Rule);
                }
            }
        }

        // Find newly added rules
        foreach (var rule in newRules)
        {
            if (!existingRuleNodes.ContainsKey(rule))
            {
                addedRules.Add(rule);
            }
        }

        // Check for remappings (rules that moved positions due to reload)
        if (addedRules.Count > 0 && removedRules.Count > 0)
        {
            StyleRuleRemapper.Remap(addedRules, removedRules, ruleRemappings);
        }

        // Handle remapped rules
        if (ruleRemappings.Count > 0)
        {
            foreach (var remap in ruleRemappings)
            {
                if (existingRuleNodes.TryGetValue(remap.Previous, out var node))
                {
                    // Release the old rule first, then acquire the new one
                    // This keeps ref count correct if called multiple times
                    m_StyleRuleSelectionHandler.ReleaseInstanceId(remap.Previous, isReadOnly);
                    var ruleEntityId = m_StyleRuleSelectionHandler.AcquireInstanceId(remap.Remapped, isReadOnly);

                    // Update the mapping to point to new rule
                    m_Mappings.TryRemove(node);
                    m_Mappings.TryAdd(node, new Node(styleSheet, remap.Remapped, isReadOnly), ruleEntityId);

                    if (remap.Previous == m_HoveredRule)
                        HoveredRule = remap.Remapped;

                    // Update display name
                    var displayString = m_Exporter.ToUssString(styleSheet, remap.Remapped.complexSelectors, s_ExportOptions);
                    CommandList.SetName(node, displayString);

                    // Remove from sets since they're handled
                    addedRules.Remove(remap.Remapped);
                    removedRules.Remove(remap.Previous);
                }
            }
        }

        // Add truly new rules
        foreach (var rule in addedRules)
        {
            var displayString = m_Exporter.ToUssString(styleSheet, rule.complexSelectors, s_ExportOptions);
            var ruleEntityId = m_StyleRuleSelectionHandler.AcquireInstanceId(rule, isReadOnly);

            CommandList.Add(rootNode, 1, out var ruleNode);
            m_Mappings.TryAdd(ruleNode[0], new Node(styleSheet, rule, isReadOnly), ruleEntityId);
            CommandList.SetName(ruleNode[0], displayString);

            // Add to existingRuleNodes so it gets sorted properly
            existingRuleNodes[rule] = ruleNode[0];
        }

        // Remove truly removed rules
        foreach (var rule in removedRules)
        {
            if (rule == m_HoveredRule)
                HoveredRule = null;

            if (existingRuleNodes.TryGetValue(rule, out var node))
            {
                m_StyleRuleSelectionHandler.ReleaseInstanceId(rule, isReadOnly);
                m_Mappings.TryRemove(node);
                CommandList.Remove(node);
            }
        }

        // Update sort order for all rules to match stylesheet order
        for (var i = 0; i < styleSheet.rules.Length; i++)
        {
            var rule = styleSheet.rules[i];
            if (existingRuleNodes.TryGetValue(rule, out var node))
            {
                CommandList.SetSortIndex(node, i);
            }
        }
        CommandList.SortChildren(rootNode);
    }

    public void Remap(List<StyleSheetRemap> remappings)
    {
        m_StyleSheetSelectionHandler.Remap(remappings);
    }

    public bool IsStyleSheet(in HierarchyNode node)
    {
        return Mappings.TryGetValue(node, out var styleNode) && styleNode.Rule == null && !styleNode.IsGroup;
    }

    public bool IsReadOnly(in HierarchyNode node)
    {
        return Mappings.TryGetValue(node, out var styleNode) && styleNode.IsReadOnly;
    }

    public bool IsGroup(in HierarchyNode node)
    {
        return Mappings.TryGetValue(node, out var styleNode) && styleNode.IsGroup;
    }

    protected override void Initialize()
    {
        base.Initialize();
        UICommandQueue.RegisterHandlerForCategory(CommandCategory.Highlight, ProcessHighlightElementsCommand);
        UIAssetRegistry.instance.AssetDirtyStateChanged += OnAssetDirtyStateChanged;
    }

    // The `.uss` rows carry the unsaved-changes marker (see GetStyleSheetDisplayName), so they have to be
    // redrawn when a style sheet is dirtied or saved — by this window or by any other tool sharing it. Only
    // the sheets that have a row here are worth a redraw; the registry reports every tracked asset.
    void OnAssetDirtyStateChanged(UnityEngine.Object asset)
    {
        if (asset is StyleSheet styleSheet && Hierarchy.IsCreated && m_Mappings.TryGetValue(styleSheet, out _))
            CommandList.SetDirty();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        HoveredRule = null;
        UICommandQueue.UnregisterHandlerForCategory(CommandCategory.Highlight, ProcessHighlightElementsCommand);

        var registry = UIAssetRegistry.LiveInstance;
        if (registry != null)
            registry.AssetDirtyStateChanged -= OnAssetDirtyStateChanged;

        // Clear selection handlers directly since hierarchy is already emptied at this point
        m_StyleSheetSelectionHandler.Clear();
        m_StyleRuleSelectionHandler.Clear();

        ClosePreviewWindow();
    }

    protected override void OnBindItem(HierarchyViewItem item)
    {
        base.OnBindItem(item);

        if (!Mappings.TryGetValue(item.Node, out var node))
            return;

        ApplyReadOnlyState(item, node.IsReadOnly && !node.IsGroup);

        if (node.IsGroup)
        {
            item.RowContainer.AddToClassList(StyleSheetGroupRowUssClassName);
            item.RowContainer.AddToClassList(StyleRuleHeaderUssClassName);
            return;
        }

        // Add style classes to the StyleSheet nodes
        if (node.Rule == null)
        {
            item.RowContainer.AddToClassList(StyleRuleHeaderUssClassName);
            item.RowContainer.EnableInClassList(StyleRuleHeaderActiveUssClassName, !node.IsReadOnly && node.StyleSheet == Window?.ActiveStyleSheet);

            return;
        }

        if (m_HighlightedNodes.Contains(item.Node))
        {
            var highlightColor = EditorGUIUtility.isProSkin ? 0.1888f : 0.6980f;
            item.RowContainer.style.backgroundColor = new Color(highlightColor, highlightColor, highlightColor, 1.0f);
        }

        item.RegisterCallback<PointerEnterEvent, StyleRule>(OnStartHover, node.Rule);
        item.RegisterCallback<PointerLeaveEvent>(OnEndHover);
        item.Name.style.display = DisplayStyle.None;
        var itemName = item.Q<HierarchyViewItemName>();
        if (itemName != null)
        {
            Action onBegin = () => item.LeftCustomContainer.style.display = DisplayStyle.None;
            Action<string, bool> onEnd = (_, _) =>
            {
                item.LeftCustomContainer.style.display = DisplayStyle.Flex;
                item.Name.style.display = DisplayStyle.None;
            };

            itemName.OnBeginRename += onBegin;
            itemName.OnEndRename += onEnd;
            m_RenameCallbacks[item] = (onBegin, onEnd);
        }

        var selectorStr = m_Exporter.ToUssString(node.StyleSheet, node.Rule.complexSelectors, s_ExportOptions);
        var parts = selectorStr.Split(',');

        for (var i = 0; i < parts.Length; i++)
        {
            var trimmedPart = parts[i].Trim();
            var tokens = s_SelectorTokenRegex.Split(trimmedPart);

            foreach (var token in tokens)
            {
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                var trimmedToken = token.Trim();

                // Check for chained class selectors (e.g., ".red.blue.green")
                var hasMultipleClasses = trimmedToken.IndexOf('.', 1) != -1;
                if (trimmedToken.StartsWith(".") && hasMultipleClasses)
                {
                    // Create a container for chained selectors
                    var chainedContainer = new VisualElement();
                    chainedContainer.AddToClassList(StyleRuleSelectorChainedClassUssClass);

                    var classSelectors = trimmedToken.Split('.', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var selector in classSelectors)
                    {
                        AddSelectorToken(chainedContainer, "." + selector, true, allowDrag: !node.IsReadOnly);
                    }

                    item.LeftCustomContainer.Add(chainedContainer);
                }
                else
                {
                    AddSelectorToken(item.LeftCustomContainer, trimmedToken, false, allowDrag: !node.IsReadOnly);
                }
            }

            if (i < parts.Length - 1)
                item.LeftCustomContainer.Add(new Label(", "));
        }
    }

    protected override void OnUnbindItem(HierarchyViewItem item)
    {
        base.OnUnbindItem(item);

        item.RowContainer.style.backgroundColor = StyleKeyword.Null;

        if (m_RenameCallbacks.TryGetValue(item, out var callbacks))
        {
            var itemName = item.Q<HierarchyViewItemName>();
            if (itemName != null)
            {
                itemName.OnBeginRename -= callbacks.Item1;
                itemName.OnEndRename -= callbacks.Item2;
            }
            m_RenameCallbacks.Remove(item);
        }

        item.RowContainer.RemoveFromClassList(StyleRuleHeaderUssClassName);
        item.RowContainer.RemoveFromClassList(StyleRuleHeaderActiveUssClassName);
        item.RowContainer.RemoveFromClassList(StyleSheetReadOnlyRowUssClassName);
        item.RowContainer.RemoveFromClassList(StyleSheetGroupRowUssClassName);
        item.Name.style.display = DisplayStyle.Flex;
        item.LeftCustomContainer.Clear();
        item.UnregisterCallback<PointerEnterEvent, StyleRule>(OnStartHover);
        item.UnregisterCallback<PointerLeaveEvent>(OnEndHover);
    }

    static void ApplyReadOnlyState(HierarchyViewItem item, bool isReadOnly)
    {
        item.RowContainer.EnableInClassList(StyleSheetReadOnlyRowUssClassName, isReadOnly);
    }

    void OnStartHover(PointerEnterEvent evt, StyleRule rule)
    {
        HoveredRule = rule;
    }

    void OnEndHover(PointerLeaveEvent evt)
    {
        HoveredRule = null;
    }

    void AddSelectorToken(VisualElement container, string token, bool isChained = false, bool allowDrag = true)
    {
        var pseudoIndex = token.IndexOf(':');

        // Rule with pseudo-class
        if (pseudoIndex > 0)
        {
            AddSelectorPart(container, token.Substring(0, pseudoIndex), isChained, allowDrag);
            AddLabel(container, token.Substring(pseudoIndex), StyleRuleSelectorPseudoStateUssClass);
            return;
        }

        // Pure pseudo-class (e.g., :root)
        if (token.StartsWith(":"))
        {
            AddLabel(container, token, StyleRuleSelectorPseudoStateUssClass);
            return;
        }

        AddSelectorPart(container, token, isChained, allowDrag);
    }

    void AddSelectorPart(VisualElement container, string selector, bool isChained = false, bool allowDrag = true)
    {
        // Class selector
        if (selector.StartsWith(".") && !selector.Contains("["))
        {
            var pill = new ClassPill { text = selector, canBeRemoved = false };

            if (isChained)
                pill.AddToClassList(StyleRuleSelectorChainedClassUssClass);

            if (allowDrag)
                AddDragSupport(pill, selector);
            container.Add(pill);
            return;
        }

        // ID selector
        if (selector.StartsWith("#"))
        {
            AddLabel(container, selector, StyleRuleSelectorNameUssClass);
            return;
        }

        // Type selector
        if (!string.IsNullOrEmpty(selector) && char.IsLetter(selector[0]))
        {
            AddLabel(container, selector, StyleRuleSelectorTypeUssClass);
            return;
        }

        // Other (combinators, etc)
        container.Add(new Label(selector));
    }

    void AddLabel(VisualElement container, string text, string ussClass)
    {
        var label = new Label(text);
        label.AddToClassList(ussClass);
        container.Add(label);
    }

    void AddDragSupport(ClassPill pill, string selectorString)
    {
        pill.AddManipulator(new ClassPillDragManipulator(selectorString, pill, this));
    }

    internal void StartDragPreview(ClassPill sourcePill)
    {
        if (DragPreviewWindow != null)
            ClosePreviewWindow();

        DragPreviewWindow = DragPreviewWindow.Show(new ClassPill { text = sourcePill.text, canBeRemoved = sourcePill.canBeRemoved }, sourcePill.layout);
        m_DragStartTime = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

        // The globalEventHandler doesn't dispatch the expected IMGUI events, therefore we take advantage of the update loop to update the position.
        EditorApplication.update += UpdateDrag;
        EditorApplication.globalEventHandler += OnDragEnd;
    }

    void UpdateDrag()
    {
        if (DragPreviewWindow == null)
        {
            EditorApplication.update -= UpdateDrag;
            return;
        }

        DragPreviewWindow.UpdatePosition(UnityEditor.Editor.GetCurrentMousePosition());
    }

    void OnDragEnd()
    {
        if (Event.current?.type != EventType.MouseDrag)
            return;

        // Don't close if drag just started (rogue MouseDrag event on fresh load)
        var currentTime = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        var dragDuration = currentTime - m_DragStartTime;
        if (dragDuration < k_MinDragDurationMs)
            return;

        ClosePreviewWindow();
    }

    protected void ClosePreviewWindow()
    {
        EditorApplication.update -= UpdateDrag;
        EditorApplication.globalEventHandler -= OnDragEnd;
        DragPreviewWindow?.Close();
        DragPreviewWindow = null;
    }

    void ProcessHighlightElementsCommand(in CommandContext context)
    {
        m_HighlightedNodes.Clear();
        if (Hierarchy.IsCreated)
            CommandList.SetDirty();
        if (context.Status != CommandExecutionStatus.Success)
            return;

        if (context.Source == CommandSources.StyleSheets)
            return;

        if (context.Command is not HighlightCommand command)
            return;

        if (command.Rules == null)
            return;

        foreach (var rule in command.Rules)
        {
            if (m_Mappings.TryGetValue(rule.styleSheet, out var node))
            {
                var index = Array.FindIndex(rule.styleSheet.rules, r => r == rule);
                if (index < 0)
                    continue;

                if (!Hierarchy.Exists(node))
                    continue;
                var ruleNode = Hierarchy.GetChild(node, index);
                m_HighlightedNodes.Add(ruleNode);
            }
        }
    }
}
