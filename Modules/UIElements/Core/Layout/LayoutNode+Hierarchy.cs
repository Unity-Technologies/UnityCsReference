// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UIElements.Unmanaged;
using UnityEngine.Assertions;

namespace UnityEngine.UIElements.Layout;

partial struct LayoutNode
{
    /// <summary>
    /// Gets or sets the parent for this node.
    /// </summary>
    public LayoutNode Parent
    {
        get => new (m_Access, m_Access.GetNodeData(m_Handle).Parent);
        set => m_Access.GetNodeData(m_Handle).Parent = value.m_Handle;
    }

    /// <summary>
    /// Gets or sets the first child for this node.
    /// </summary>
    public LayoutNode FirstChild
    {
        get => new (m_Access, m_Access.GetNodeData(m_Handle).FirstChild);
        set => m_Access.GetNodeData(m_Handle).FirstChild = value.m_Handle;
    }

    /// <summary>
    /// Gets or sets the next sibling for this node. Undefined after last child.
    /// </summary>
    public LayoutNode NextSibling
    {
        get => new (m_Access, m_Access.GetNodeData(m_Handle).NextSibling);
        set => m_Access.GetNodeData(m_Handle).NextSibling = value.m_Handle;
    }

    /// <summary>
    /// Gets or sets the previous sibling for this node. Loops around from first child to last child.
    /// </summary>
    public LayoutNode PrevSiblingRing
    {
        get => new (m_Access, m_Access.GetNodeData(m_Handle).PrevSiblingRing);
        set => m_Access.GetNodeData(m_Handle).PrevSiblingRing = value.m_Handle;
    }

    /// <summary>
    /// Returns true if this node has no children.
    /// </summary>
    public bool IsEmpty => FirstChild.IsUndefined;

    /// <summary>
    /// Returns whether the provided node is a child of this node.
    /// </summary>
    /// <param name="child">The node to verify as being one of our children</param>
    /// <returns>True if @@child@@ is a child of this node</returns>
    public bool Contains(LayoutNode child) => child.Parent == this;

    /// <summary>
    /// Adds the specified node as a child.
    /// </summary>
    /// <param name="child">The child to add.</param>
    public void AddChild(LayoutNode child)
    {
        Assert.IsFalse(child.IsUndefined);
        Assert.IsTrue(child.Parent.IsUndefined);

        var firstChild = FirstChild;
        if (firstChild.IsUndefined)
        {
            child.PrevSiblingRing = child;
            child.NextSibling = Undefined;
            FirstChild = child;
        }
        else
        {
            var oldLastChild = firstChild.PrevSiblingRing;
            firstChild.PrevSiblingRing = child;
            child.PrevSiblingRing = oldLastChild;
            oldLastChild.NextSibling = child;
            child.NextSibling = Undefined;
        }

        child.Parent = this;
        MarkDirty();
    }

    /// <summary>
    /// Inserts a new child to this node before the other provided child.
    /// </summary>
    /// <param name="nextChild">The child node before the child to insert.</param>
    /// <param name="child">The child node to insert.</param>
    public void InsertBefore(LayoutNode nextChild, LayoutNode child)
    {
        Assert.IsFalse(child.IsUndefined);
        Assert.IsTrue(child.Parent.IsUndefined);
        Assert.IsFalse(nextChild.IsUndefined);
        if (nextChild.Parent != this)
            throw new ArgumentException("Argument nextChild is not a child of this node.");

        var oldNextPrevSibling = nextChild.PrevSiblingRing;
        nextChild.PrevSiblingRing = child;
        child.PrevSiblingRing = oldNextPrevSibling;
        child.NextSibling = nextChild;
        if (nextChild == FirstChild)
            FirstChild = child;
        else
            oldNextPrevSibling.NextSibling = child;

        child.Parent = this;
        MarkDirty();
    }

    /// <summary>
    /// Removes the specified child.
    /// </summary>
    /// <param name="child">The child to remove.</param>
    public void RemoveChild(LayoutNode child)
    {
        Assert.IsFalse(child.IsUndefined);
        if (child.Parent != this)
            throw new ArgumentException("Argument child is not a child of this node.");

        var firstChild = FirstChild;
        if (firstChild == child)
        {
            var secondChild = firstChild.NextSibling;
            if (!secondChild.IsUndefined)
                secondChild.PrevSiblingRing = firstChild.PrevSiblingRing;
            FirstChild = secondChild;
        }
        else
        {
            var prevChild = child.PrevSiblingRing;
            var nextChild = child.NextSibling;
            prevChild.NextSibling = nextChild;
            if (!nextChild.IsUndefined)
                nextChild.PrevSiblingRing = prevChild;
            else
                firstChild.PrevSiblingRing = prevChild;
        }

        child.PrevSiblingRing = child.NextSibling = child.Parent = Undefined;
        MarkDirty();
    }

    /// <summary>
    /// Clears all children from this node.
    /// </summary>
    public void Clear()
    {
        var child = FirstChild;

        // Empty list
        if (child.IsUndefined)
            return;

        do
        {
            var oldNextSibling = child.NextSibling;
            child.PrevSiblingRing = child.NextSibling = child.Parent = Undefined;
            child = oldNextSibling;
        } while (!child.IsUndefined);

        FirstChild = Undefined;
        MarkDirty();
    }

    /// <summary>
    /// Gets an enumerator to iterate over all children.
    /// </summary>
    /// <remarks>
    /// This uses duck typing and does explicitly implement IEnumerable{YogaNode}.
    /// </remarks>
    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    public struct Enumerator : IEnumerator<LayoutNode>
    {
        private LayoutNode m_Current;
        private LayoutNode m_Next;

        public Enumerator(LayoutNode parent)
        {
            m_Current = Undefined;
            m_Next = parent.FirstChild;
        }

        public LayoutNode Current => m_Current;
        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }

        public void Reset()
        {
            throw new InvalidOperationException();
        }

        public bool MoveNext()
        {
            if (m_Next.IsUndefined) return false;
            m_Current = m_Next;
            m_Next = m_Next.NextSibling;
            return true;
        }
    }
}
