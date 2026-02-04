// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Hierarchy;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class StyleSheetNodeTypeHandler : HierarchyNodeTypeHandler
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

        public void Add(HierarchyNode hierarchyNode, Node node)
        {
            m_HierarchyNodeToNode.Add(hierarchyNode, node);
        }

        public void Remove(HierarchyNode hierarchyNode)
        {
            m_HierarchyNodeToNode.Remove(hierarchyNode);
        }

        public void RemoveAll(IEnumerable<HierarchyNode> nodes)
        {
            foreach (var node in nodes)
                m_HierarchyNodeToNode.Remove(node);
        }

        public bool TryGetNode(HierarchyNode inNode, out Node outNode)
        {
            return m_HierarchyNodeToNode.TryGetValue(inNode, out outNode);
        }
    }

    readonly NodeMappings m_Mappings = new();
    readonly StyleSheetEditorExporter m_Exporter = new();
    readonly StyleSheetExporter.UssExportOptions m_ExportOptions = new()
    {
        ignoreSelectorPrefixList = new[] { "__unity-selector" }
    };

    internal NodeMappings Mappings => m_Mappings;

    public HierarchyNode AddStyleSheet(StyleSheet styleSheet)
    {
        if (!Hierarchy.IsCreated)
            return HierarchyNode.Null;

        CommandList.Add(Hierarchy.Root, 1, out var root);
        CommandList.SetName(root[0], styleSheet.name);
        m_Mappings.Add(root[0], new Node(styleSheet));

        for (var i = 0; i < styleSheet.rules.Length; i++)
        {
            var rule = styleSheet.rules[i];
            var displayString = m_Exporter.ToUssString(styleSheet, rule.complexSelectors, m_ExportOptions);

            // Only add the rule if it has at least one non-internal selector
            if (!string.IsNullOrWhiteSpace(displayString))
            {
                CommandList.Add(root[0], 1, out var ruleNode);
                m_Mappings.Add(ruleNode[0], new Node(styleSheet, rule));
                CommandList.SetName(ruleNode[0], displayString);
            }
        }

        return root[0];
    }

    public void RemoveStyleSheet(HierarchyNode rootNode)
    {
        if (!Hierarchy.IsCreated || rootNode == HierarchyNode.Null || !Hierarchy.Exists(rootNode))
            return;

        // Remove all child rule node mappings
        var childCount = Hierarchy.GetChildrenCount(rootNode);
        for (var i = 0; i < childCount; i++)
        {
            var childNode = Hierarchy.GetChild(rootNode, i);
            m_Mappings.Remove(childNode);
        }

        m_Mappings.Remove(rootNode);
        CommandList.Remove(rootNode);
    }
}
