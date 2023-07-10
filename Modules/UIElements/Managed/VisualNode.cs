// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements.Layout;

namespace UnityEngine.UIElements;

/// <summary>
/// A <see cref="VisualNode"/> represents a low level node in the visual hierarchy.
/// </summary>
readonly struct VisualNode : IEnumerable<VisualNode>, IEquatable<VisualNode>, IEquatable<VisualNodeHandle>
{
    public static VisualNode Null => new(default, VisualNodeHandle.Null);

    /// <summary>
    /// An enumerator over all children of a node.
    /// </summary>
    public struct Enumerator : IEnumerator<VisualNode>
    {
        readonly VisualNode m_Node;

        int m_Position;

        public Enumerator(in VisualNode node)
        {
            m_Node = node;
            m_Position = -1;
        }

        public bool MoveNext() => ++m_Position < m_Node.ChildCount;

        public void Reset() => m_Position = -1;

        public VisualNode Current => m_Node[m_Position];

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// The manager storing the actual node data.
    /// </summary>
    readonly VisualManager m_Manager;

    /// <summary>
    /// The handle to the underlying data.
    /// </summary>
    readonly VisualNodeHandle m_Handle;

    /// <summary>
    /// Returns <see langword="true"/> if the underlying memory is allocated for this panel.
    /// </summary>
    public bool IsCreated => !m_Handle.Equals(VisualNodeHandle.Null) && m_Manager.ContainsNode(m_Handle);

    /// <summary>
    /// Gets the handle for this node.
    /// </summary>
    public VisualNodeHandle Handle => m_Handle;

    /// <summary>
    /// Returns <see langword="true"/> if this node is the root of the hierarchy.
    /// </summary>
    public bool IsRoot => m_Handle.Id == 1;

    /// <summary>
    /// Gets the storage id for this node. This is number is unique at any given time but id's are re-cycled.
    /// </summary>
    public int Id => m_Handle.Id;

    /// <summary>
    /// Gets the unique control id for this node. This is an ever incrementing number and guaranteed to be unique.
    /// </summary>
    public uint ControlId => m_Manager.GetProperty<VisualNodeData>(m_Handle).ControlId;

    /// <summary>
    /// Gets the parent for this node.
    /// </summary>
    public VisualNode Parent => new(m_Manager, m_Manager.GetParent(in m_Handle));

    /// <summary>
    /// Gets the number of children this node has.
    /// </summary>
    /// <returns>The number of children.</returns>
    public int ChildCount => m_Manager.GetChildrenCount(in m_Handle);

    /// <summary>
    /// Gets the child at the specified index.
    /// </summary>
    /// <param name="index">The index of the child to get.</param>
    public VisualNode this[int index] => GetChildren()[index];

    /// <summary>
    /// Returns true if the <see cref="VisualNode"/> is enabled in the hierarchy.
    /// </summary>
    public ref bool Enabled => ref m_Manager.GetProperty<VisualNodeData>(m_Handle).Enabled;

    /// <summary>
    /// Gets the <see cref="VisualElementFlags"/> for this node.
    /// </summary>
    public ref VisualElementFlags Flags => ref m_Manager.GetProperty<VisualNodeData>(m_Handle).Flags;

    /// <summary>
    /// Gets or sets the <see cref="PseudoStates"/> for this node.
    /// </summary>
    public PseudoStates PseudoStates
    {
        get => m_Manager.GetPseudoStates(m_Handle);
        set => m_Manager.SetPseudoStates(m_Handle, value);
    }

    /// <summary>
    /// Returns true if the <see cref="VisualNode"/> is enabled in the hierarchy.
    /// </summary>
    public bool EnabledInHierarchy => (PseudoStates & PseudoStates.Disabled) != PseudoStates.Disabled;

    /// <summary>
    /// Gets or sets the <see cref="RenderHints"/> for this node.
    /// </summary>
    public RenderHints RenderHints
    {
        get => m_Manager.GetRenderHints(m_Handle);
        set => m_Manager.SetRenderHints(m_Handle, value);
    }

    /// <summary>
    /// Gets or sets the <see cref="LanguageDirection"/> for this node.
    /// </summary>
    public LanguageDirection LanguageDirection
    {
        get => m_Manager.GetLanguageDirection(m_Handle);
        set => m_Manager.SetLanguageDirection(m_Handle, value);
    }

    /// <summary>
    /// Gets or sets the local <see cref="LanguageDirection"/> for this node.
    /// </summary>
    public LanguageDirection LocalLanguageDirection
    {
        get => m_Manager.GetLocalLanguageDirection(m_Handle);
        set => m_Manager.SetLocalLanguageDirection(m_Handle, value);
    }

    /// <summary>
    /// Gets the HierarchyDisplayed flag for this node.
    /// </summary>
    internal bool areAncestorsAndSelfDisplayed => (Flags & VisualElementFlags.HierarchyDisplayed) == VisualElementFlags.HierarchyDisplayed;

    /// <summary>
    /// Gets or sets the callback interest for this node.
    /// </summary>
    public ref VisualNodeCallbackInterest CallbackInterest => ref m_Manager.GetProperty<VisualNodeData>(m_Handle).CallbackInterest;

    /// <summary>
    /// Initializes a new <see cref="VisualNode"/> object.
    /// </summary>
    /// <param name="manager">The manager storing the node data.</param>
    /// <param name="handle">The handle to the node data.</param>
    internal VisualNode(VisualManager manager, VisualNodeHandle handle)
    {
        m_Manager = manager;
        m_Handle = handle;
    }

    /// <summary>
    /// Destroys the <see cref="VisualNode"/> instance.
    /// </summary>
    internal void Destroy()
    {
        m_Manager.RemoveNode(in m_Handle);
    }

    /// <summary>
    /// Gets the panel for this node.
    /// </summary>
    public VisualPanel GetPanel() => new(m_Manager, m_Manager.GetProperty<VisualNodeData>(m_Handle).Panel);

    /// <summary>
    /// Sets the panel for this node.
    /// </summary>
    /// <param name="panel">The panel to set.</param>
    public void SetPanel(VisualPanel panel) => m_Manager.GetProperty<VisualNodeData>(m_Handle).Panel = panel.Handle;

    /// <summary>
    /// Gets the managed owner for this node.
    /// </summary>
    public VisualElement GetOwner() => m_Manager.GetOwner(in m_Handle);

    /// <summary>
    /// Sets the managed owner for this node.
    /// </summary>
    /// <param name="owner">The managed owner to set.</param>
    public void SetOwner(VisualElement owner) => m_Manager.SetOwner(in m_Handle, owner);

    /// <summary>
    /// Gets layout for this node.
    /// </summary>
    public LayoutNode GetLayout() => m_Manager.GetProperty<VisualNodeData>(m_Handle).LayoutNode;

    /// <summary>
    /// Sets the layout for this node.
    /// </summary>
    public void SetLayout(LayoutNode value) => m_Manager.GetProperty<VisualNodeData>(m_Handle).LayoutNode = value;

    /// <summary>
    /// Gets the children for this node.
    /// </summary>
    public VisualNodeChildren GetChildren() => new(m_Manager, m_Handle);


    /// <summary>
    /// Inserts the child at the specified index.
    /// </summary>
    /// <param name="index">The index to insert the child at.</param>
    /// <param name="child">The child to insert.</param>
    public void InsertChildAtIndex(int index, in VisualNode child)
    {
        m_Manager.InsertChildAtIndex(in m_Handle, index, in child.m_Handle);
    }

    /// <summary>
    /// Adds the specified child to this node.
    /// </summary>
    /// <param name="child">The child to add.</param>
    public void AddChild(in VisualNode child)
    {
        m_Manager.AddChild(in m_Handle, in child.m_Handle);
    }

    /// <summary>
    /// Adds the specified child to this node.
    /// </summary>
    /// <param name="child">The child to add.</param>
    public void RemoveChild(in VisualNode child)
    {
        m_Manager.RemoveChild(in m_Handle, in child.m_Handle);
    }

    /// <summary>
    /// Gets the index for the specified child.
    /// </summary>
    /// <param name="child">The child to get the index for.</param>
    public int IndexOfChild(in VisualNode child)
    {
        return m_Manager.IndexOfChild(in m_Handle, in child.m_Handle);
    }

    /// <summary>
    /// Removes the child at the specified index.
    /// </summary>
    /// <param name="index">The index to remove the child at.</param>
    public void RemoveChildAtIndex(int index)
    {
        m_Manager.RemoveChildAtIndex(in m_Handle, index);
    }

    /// <summary>
    /// Removes all children from the node.
    /// </summary>
    public void ClearChildren()
    {
        m_Manager.ClearChildren(in m_Handle);
    }

    /// <summary>
    /// Removes this node from it's parent.
    /// </summary>
    public void RemoveFromParent()
    {
        m_Manager.RemoveFromParent(in m_Handle);
    }

    /// <summary>
    /// Gets the class list for this node.
    /// </summary>
    public VisualNodeClassList GetClassList() => new(m_Manager, m_Handle);

    /// <summary>
    /// Adds a class to this <see cref="VisualNode"/>.
    /// </summary>
    /// <param name="className">The name of the class to add.</param>
    public void AddToClassList(string className)
    {
        if (string.IsNullOrEmpty(className))
            return;

        m_Manager.AddToClassList(in m_Handle, className);
    }

    /// <summary>
    /// Removes a class from this <see cref="VisualNode"/>.
    /// </summary>
    /// <param name="className">The name of the class to remove.</param>
    public bool RemoveFromClassList(string className)
    {
        if (string.IsNullOrEmpty(className))
            return false;

        return m_Manager.RemoveFromClassList(in m_Handle, className);
    }

    /// <summary>
    /// Checks if the given class has been added to this <see cref="VisualNode"/>.
    /// </summary>
    /// <param name="className">The name of the class to check.</param>
    public bool ClassListContains(string className)
    {
        if (string.IsNullOrEmpty(className))
            return false;

        return m_Manager.ClassListContains(in m_Handle, className);
    }

    /// <summary>
    /// Clears all classes from the <see cref="VisualNode"/>.
    /// </summary>
    public bool ClearClassList()
    {
        return m_Manager.ClearClassList(in m_Handle);
    }

    /// <summary>
    /// Sets the enabled state of this <see cref="VisualNode"/>.
    /// </summary>
    /// <param name="value">The state to set.</param>
    public void SetEnabled(bool value)
    {
        m_Manager.SetEnabled(in m_Handle, value);
    }

    /// <summary>
    /// Gets the child enumerator for this <see cref="VisualNode"/>.
    /// </summary>
    /// <returns>The child enumerator.</returns>
    public Enumerator GetEnumerator() => new(in this);

    IEnumerator<VisualNode> IEnumerable<VisualNode>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Equals(VisualNode other) => m_Handle.Equals(other.m_Handle);
    public bool Equals(VisualNodeHandle other) => m_Handle.Equals(other);
    public override bool Equals(object obj) => obj is VisualNode other ? Equals(other) : obj is VisualNodeHandle handle && Equals(handle);
    public override int GetHashCode() => HashCode.Combine(m_Manager, m_Handle);
}
