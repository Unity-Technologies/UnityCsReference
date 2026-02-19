// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
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
        readonly Dictionary<HierarchyNode, Node> m_HierarchyNodeToNode = new();
        readonly Dictionary<HierarchyNode, EntityId> m_SelectionHandles = new();
        readonly Dictionary<EntityId, HierarchyNode> m_ReversedSelectionHandles = new();

        public bool TryAdd(HierarchyNode hierarchyNode, Node node, EntityId selectionHandle)
        {
            if (hierarchyNode == HierarchyNode.Null)
                return false;

            return m_HierarchyNodeToNode.TryAdd(hierarchyNode, node) &&
                   m_SelectionHandles.TryAdd(hierarchyNode, selectionHandle) &&
                   m_ReversedSelectionHandles.TryAdd(selectionHandle, hierarchyNode);
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
    }

    readonly NodeMappings m_Mappings = new();
    readonly StyleSheetEditorExporter m_Exporter = new();
    readonly StyleSheetExporter.UssExportOptions m_ExportOptions = new()
    {
        ignoreSelectorPrefixList = new[] { "__unity-selector" }
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
}
