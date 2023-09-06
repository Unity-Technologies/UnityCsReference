// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Accessibility;

namespace UnityEditor.Accessibility
{
    /// <summary>
    /// A view model of an accessibility hierarchy
    /// </summary>
    internal class AccessibilityHierarchyViewModel
    {
        private AccessibilityHierarchy m_Hierarchy;

        /// <summary>
        /// The underlying accessibility hierarchy.
        /// </summary>
        public AccessibilityHierarchy accessibilityHierarchy
        {
            get => m_Hierarchy;
            set
            {
                if (m_Hierarchy == value)
                    return;

                if (m_Hierarchy != null)
                {
                    m_Hierarchy.changed -= OnHierarchyChanged;
                }

                m_Hierarchy = value;

                if (m_Hierarchy != null)
                {
                    m_Hierarchy.changed += OnHierarchyChanged;
                }

                Reset();
            }
        }

        /// <summary>
        /// The root node.
        /// </summary>
        public AccessibilityViewModelNode root { get; }

        /// <summary>
        /// Sent when the model is reset, causing a rebuild of the attached views.
        /// </summary>
        public event Action modelReset;

        /// <summary>
        /// Constructor.
        /// </summary>
        public AccessibilityHierarchyViewModel()
        {
            root = CreateRootNode();
        }

        private void OnHierarchyChanged(AccessibilityHierarchy hierarchy)
        {
            Reset();
        }

        /// <summary>
        /// Resets the model.
        /// </summary>
        public void Reset()
        {
            modelReset?.Invoke();
        }

        /// <summary>
        /// Creates a model node from an accessibility node
        /// </summary>
        /// <param name="node">The accessibility node</param>
        /// <returns>The created model node</returns>
        public AccessibilityViewModelNode CreateNode(AccessibilityNode node)
        {
            return new AccessibilityViewModelNode(node, this);
        }

        /// <summary>
        /// Tries to get the model node with the specified node id.
        /// </summary>
        /// <param name="nodeId">The node id</param>
        /// <param name="modelNode">The node found</param>
        /// <returns>Returns true if a node is found with the specified id and false otherwise.</returns>
        public bool TryGetNode(int nodeId, out AccessibilityViewModelNode modelNode)
        {
            modelNode = default;

            if (m_Hierarchy == null)
                return false;

            if (m_Hierarchy.TryGetNode(nodeId, out var node))
            {
                modelNode = CreateNode(node);
                return true;
            }

            return false;
        }

        private AccessibilityViewModelNode CreateRootNode()
        {
            return new AccessibilityViewModelNode(null, this, true);
        }
    }

    /// <summary>
    /// A view model node of an accessibility node.
    /// </summary>
    internal struct AccessibilityViewModelNode : IEquatable<AccessibilityViewModelNode>
    {
        const int k_RootNodeId = int.MaxValue;

        private AccessibilityHierarchyViewModel m_Model;
        private AccessibilityNode m_Node;
        internal bool m_Root;

        /// <summary>
        /// Indicates whether the model node is the root node of the model.
        /// </summary>
        [CreateProperty]
        public bool isRoot => m_Root;

        /// <summary>
        /// Indicates whether the model node is null.
        /// </summary>
        public bool isNull => m_Node == null && !isRoot;

        /// <summary>
        /// The id of the node.
        /// </summary>
        [CreateProperty]
        public int id => isRoot ? k_RootNodeId : (m_Node?.id ?? 0);

        /// <summary>
        /// The label of the node.
        /// </summary>
        [CreateProperty]
        public string label
        {
            get
            {
                if (isRoot)
                {
                    if (m_Model.accessibilityHierarchy != null)
                        return m_Model.accessibilityHierarchy == AssistiveSupport.activeHierarchy ? L10n.Tr("Active Hierarchy") : L10n.Tr("Inactive Hierarchy");
                    return L10n.Tr("No Active Hierarchy");
                }
                return m_Node?.label;
            }
        }

        /// <summary>
        /// The value of the node.
        /// </summary>
        [CreateProperty]
        public string value => m_Node?.value;

        /// <summary>
        /// The hint of the node.
        /// </summary>
        [CreateProperty]
        public string hint => m_Node?.hint;

        /// <summary>
        /// Indicates whether the node is active.
        /// </summary>
        [CreateProperty]
        public bool isActive => m_Node?.isActive ?? false;

        /// <summary>
        /// The frame of the node.
        /// </summary>
        [CreateProperty]
        public Rect frame => m_Node?.frame ?? Rect.zero;

        /// <summary>
        /// The role of the node.
        /// </summary>
        [CreateProperty]
        public AccessibilityRole role => m_Node?.role ?? default;

        // Indicates whether the node allows direct interaction.
        [CreateProperty]
        public bool allowsDirectInteraction => m_Node?.allowsDirectInteraction ?? default;

        /// <summary>
        ///  The state of the node.
        /// </summary>
        [CreateProperty]
        public AccessibilityState state => m_Node?.state ?? default;

        /// <summary>
        /// The number of child model nodes.
        /// </summary>
        [CreateProperty]
        public int childNodeCount => isRoot ? (m_Model.accessibilityHierarchy?.rootNodes.Count ?? 0) : m_Node?.children.Count ?? 0;

        /// <summary>
        /// The accessibility node.
        /// </summary>
        public AccessibilityNode accessibilityNode => m_Node;

        /// <summary>
        /// The child model node at the specified index.
        /// </summary>
        /// <param name="index">The index</param>
        /// <returns>The node at the index.</returns>
        public AccessibilityViewModelNode GetChildNode(int index)
        {
            return m_Model?.CreateNode(isRoot ? m_Model.accessibilityHierarchy.rootNodes[index] : m_Node.children[index]) ?? default;
        }

        /// <summary>
        /// Constructs a model node from an accessibility node, a view model and a value that indicates whether is the root node of the model.
        /// </summary>
        /// <param name="accessibilityNode">The accessibility node.</param>
        /// <param name="model">The parent view model.</param>
        /// <param name="isRootNode">Indicates whether the node is the root of the view model.</param>
        internal AccessibilityViewModelNode(AccessibilityNode accessibilityNode, AccessibilityHierarchyViewModel model, bool isRootNode = false)
        {
            m_Model = model;
            m_Node = accessibilityNode;
            m_Root = isRootNode;
        }

        public override string ToString()
        {
            return id.ToString();
        }

        public bool Equals(AccessibilityViewModelNode other)
        {
            return Equals(m_Root, other.m_Root) && Equals(m_Model, other.m_Model) && Equals(m_Node, other.m_Node);
        }

        public override bool Equals(object obj)
        {
            return obj is AccessibilityViewModelNode other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(m_Model, m_Node, m_Root);
        }

        public static bool operator ==(AccessibilityViewModelNode obj1, AccessibilityViewModelNode obj2)
        {
            return obj1.Equals(obj2);
        }

        public static bool operator !=(AccessibilityViewModelNode obj1, AccessibilityViewModelNode obj2)
        {
            return !(obj1 == obj2);
        }
    }
}
