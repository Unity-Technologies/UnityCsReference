// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Hierarchy;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// 1. When a StyleSheet is added to hierarchy:
/// - AddStyleSheet() calls m_StyleSheetSelectionHandler.AcquireInstanceId(styleSheet)
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
internal class StyleSheetNodeTypeHandler : HierarchyNodeTypeHandler, IHierarchyEntityIdConverter
{
    const string DraggedSelectorsKey = "StyleSheetNodeTypeHandler.DraggedSelectors";

    internal class StyleSheetEditorExporter : StyleSheetExporter
    {
        public string ToUssString(StyleSheet styleSheet, StyleComplexSelector[] selectors, UssExportOptions options = null)
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

        public Node(StyleSheet styleSheet)
        {
            StyleSheet = styleSheet;
            Rule = null;
        }

        public Node(StyleSheet styleSheet, StyleRule rule)
        {
            StyleSheet = styleSheet;
            Rule = rule;
        }
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

            var success = m_HierarchyNodeToNode.TryAdd(hierarchyNode, node) &&
                          m_SelectionHandles.TryAdd(hierarchyNode, selectionHandle) &&
                          m_ReversedSelectionHandles.TryAdd(selectionHandle, hierarchyNode);

            // Also maintain StyleSheet mappings for the root node
            if (success && node.Rule == null && node.StyleSheet != null)
            {
                m_Map.TryAdd(hierarchyNode, node.StyleSheet);
                m_ReversedMap.TryAdd(node.StyleSheet, hierarchyNode);
            }

            return success;
        }

        public bool TryGetValue(StyleSheet styleSheet, out HierarchyNode node)
        {
            if (styleSheet != null)
                return m_ReversedMap.TryGetValue(styleSheet, out node);

            node = HierarchyNode.Null;
            return false;
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
                m_ReversedMap.Remove(stylesheet);
            }

            return m_HierarchyNodeToNode.Remove(hierarchyNode) &&
                   m_SelectionHandles.Remove(hierarchyNode, out var selectionHandle) &&
                   m_ReversedSelectionHandles.Remove(selectionHandle);
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
    readonly StyleSheetEditorExporter m_Exporter = new();
    readonly StyleSheetExporter.UssExportOptions m_ExportOptions = new()
    {
        ignoreSelectorPrefixList = new[] { "__unity" }
    };

    readonly IStyleSheetSelectionHandler m_StyleSheetSelectionHandler;
    readonly IStyleRuleSelectionHandler m_StyleRuleSelectionHandler;

    internal NodeMappings Mappings => m_Mappings;

    public StyleSheetNodeTypeHandler()
        : this(new StyleSheetSelectionHandler(), new StyleRuleSelectionHandler())
    {
    }

    public StyleSheetNodeTypeHandler(IStyleSheetSelectionHandler styleSheetSelectionHandler, IStyleRuleSelectionHandler styleRuleSelectionHandler)
    {
        m_StyleSheetSelectionHandler = styleSheetSelectionHandler;
        m_StyleRuleSelectionHandler = styleRuleSelectionHandler;
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
        return m_Mappings.TryGetSelectionHandle(node, out var entityId)
            ? entityId
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

    public HierarchyNode AddStyleSheet(StyleSheet styleSheet)
    {
        if (!Hierarchy.IsCreated)
            return HierarchyNode.Null;

        CommandList.Add(Hierarchy.Root, 1, out var root);

        CommandList.SetName(root[0], styleSheet.name);

        var styleSheetEntityId = m_StyleSheetSelectionHandler.AcquireInstanceId(styleSheet);
        m_Mappings.TryAdd(root[0], new Node(styleSheet), styleSheetEntityId);

        for (var i = 0; i < styleSheet.rules.Length; i++)
        {
            var rule = styleSheet.rules[i];
            var displayString = m_Exporter.ToUssString(styleSheet, rule.complexSelectors, m_ExportOptions);

            // Only add the rule if it has at least one non-internal selector
            if (!string.IsNullOrWhiteSpace(displayString))
            {
                var ruleEntityId = m_StyleRuleSelectionHandler.AcquireInstanceId(rule);

                CommandList.Add(root[0], 1, out var ruleNode);
                m_Mappings.TryAdd(ruleNode[0], new Node(styleSheet, rule), ruleEntityId);
                CommandList.SetName(ruleNode[0], displayString);
            }
        }

        return root[0];
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
                    m_StyleRuleSelectionHandler.ReleaseInstanceId(node.Rule);
                }

                m_Mappings.TryRemove(childNode);
            }
        }

        // Release stylesheet selection instance
        if (m_Mappings.TryGetValue(rootNode, out var rootNodeData) && rootNodeData.StyleSheet != null)
        {
            m_StyleSheetSelectionHandler.ReleaseInstanceId(rootNodeData.StyleSheet);
        }

        m_Mappings.TryRemove(rootNode);
        if (Hierarchy.IsCreated && Hierarchy.Exists(rootNode))
            CommandList.Remove(rootNode);
    }

    public void Sort(HierarchyNode rootNode, List<StyleSheet> styleSheets)
    {
        // Update sort order for all stylesheets to match UXML order
        for (var i = 0; i < styleSheets.Count; i++)
        {
            if (m_Mappings.TryGetValue(styleSheets[i], out var node))
            {
                CommandList.SetSortIndex(node, i);
            }
        }
        CommandList.SortChildren(rootNode);
    }

    public void RefreshStyleSheetRules(HierarchyNode rootNode, StyleSheet styleSheet)
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
                    m_StyleRuleSelectionHandler.ReleaseInstanceId(remap.Previous);
                    var ruleEntityId = m_StyleRuleSelectionHandler.AcquireInstanceId(remap.Remapped);

                    // Update the mapping to point to new rule
                    m_Mappings.TryRemove(node);
                    m_Mappings.TryAdd(node, new Node(styleSheet, remap.Remapped), ruleEntityId);

                    // Update display name
                    var displayString = m_Exporter.ToUssString(styleSheet, remap.Remapped.complexSelectors, m_ExportOptions);
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
            var displayString = m_Exporter.ToUssString(styleSheet, rule.complexSelectors, m_ExportOptions);
            var ruleEntityId = m_StyleRuleSelectionHandler.AcquireInstanceId(rule);

            CommandList.Add(rootNode, 1, out var ruleNode);
            m_Mappings.TryAdd(ruleNode[0], new Node(styleSheet, rule), ruleEntityId);
            CommandList.SetName(ruleNode[0], displayString);

            // Add to existingRuleNodes so it gets sorted properly
            existingRuleNodes[rule] = ruleNode[0];
        }

        // Remove truly removed rules
        foreach (var rule in removedRules)
        {
            if (existingRuleNodes.TryGetValue(rule, out var node))
            {
                m_StyleRuleSelectionHandler.ReleaseInstanceId(rule);
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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // Clear selection handlers directly since hierarchy is already emptied at this point
        m_StyleSheetSelectionHandler.Clear();
        m_StyleRuleSelectionHandler.Clear();
    }
}
