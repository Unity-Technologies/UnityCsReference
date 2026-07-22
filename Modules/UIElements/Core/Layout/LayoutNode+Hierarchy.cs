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

        ref var childData = ref m_Access.GetNodeData(child.m_Handle);
        Assert.IsTrue(childData.Parent.IsUndefined);

        ref var data = ref m_Access.GetNodeData(m_Handle);
        var firstChildHandle = data.FirstChild;
        if (firstChildHandle.IsUndefined)
        {
            childData.PrevSiblingRing = child.m_Handle;
            childData.NextSibling = UnmanagedDataHandle.Undefined;
            data.FirstChild = child.m_Handle;
        }
        else
        {
            ref var firstChildData = ref m_Access.GetNodeData(firstChildHandle);
            var oldLastChildHandle = firstChildData.PrevSiblingRing;
            firstChildData.PrevSiblingRing = child.m_Handle;
            childData.PrevSiblingRing = oldLastChildHandle;
            m_Access.GetNodeData(oldLastChildHandle).NextSibling = child.m_Handle;
            childData.NextSibling = UnmanagedDataHandle.Undefined;
        }

        childData.Parent = m_Handle;
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
        Assert.IsFalse(nextChild.IsUndefined);

        ref var childData = ref m_Access.GetNodeData(child.m_Handle);
        Assert.IsTrue(childData.Parent.IsUndefined);

        ref var nextChildData = ref m_Access.GetNodeData(nextChild.m_Handle);
        if (nextChildData.Parent != m_Handle)
            throw new ArgumentException("Argument nextChild is not a child of this node.");

        ref var data = ref m_Access.GetNodeData(m_Handle);
        var oldNextPrevSiblingHandle = nextChildData.PrevSiblingRing;
        nextChildData.PrevSiblingRing = child.m_Handle;
        childData.PrevSiblingRing = oldNextPrevSiblingHandle;
        childData.NextSibling = nextChild.m_Handle;
        if (nextChild.m_Handle == data.FirstChild)
            data.FirstChild = child.m_Handle;
        else
            m_Access.GetNodeData(oldNextPrevSiblingHandle).NextSibling = child.m_Handle;

        childData.Parent = m_Handle;
        MarkDirty();
    }

    /// <summary>
    /// Removes the specified child.
    /// </summary>
    /// <param name="child">The child to remove.</param>
    public void RemoveChild(LayoutNode child)
    {
        Assert.IsFalse(child.IsUndefined);

        ref var childData = ref m_Access.GetNodeData(child.m_Handle);
        if (childData.Parent != m_Handle)
            throw new ArgumentException("Argument child is not a child of this node.");

        ref var data = ref m_Access.GetNodeData(m_Handle);
        var firstChildHandle = data.FirstChild;
        if (firstChildHandle == child.m_Handle)
        {
            var secondChildHandle = childData.NextSibling;
            if (!secondChildHandle.IsUndefined)
                m_Access.GetNodeData(secondChildHandle).PrevSiblingRing = childData.PrevSiblingRing;
            data.FirstChild = secondChildHandle;
        }
        else
        {
            var prevChildHandle = childData.PrevSiblingRing;
            var nextChildHandle = childData.NextSibling;
            m_Access.GetNodeData(prevChildHandle).NextSibling = nextChildHandle;
            if (!nextChildHandle.IsUndefined)
                m_Access.GetNodeData(nextChildHandle).PrevSiblingRing = prevChildHandle;
            else
                m_Access.GetNodeData(firstChildHandle).PrevSiblingRing = prevChildHandle;
        }

        childData.PrevSiblingRing = UnmanagedDataHandle.Undefined;
        childData.NextSibling = UnmanagedDataHandle.Undefined;
        childData.Parent = UnmanagedDataHandle.Undefined;
        MarkDirty();
    }

    /// <summary>
    /// Clears all children from this node.
    /// </summary>
    public void Clear()
    {
        ref var data = ref m_Access.GetNodeData(m_Handle);
        var childHandle = data.FirstChild;

        // Empty list
        if (childHandle.IsUndefined)
            return;

        do
        {
            ref var childData = ref m_Access.GetNodeData(childHandle);
            var nextSiblingHandle = childData.NextSibling;
            childData.PrevSiblingRing = UnmanagedDataHandle.Undefined;
            childData.NextSibling = UnmanagedDataHandle.Undefined;
            childData.Parent = UnmanagedDataHandle.Undefined;
            childHandle = nextSiblingHandle;
        } while (!childHandle.IsUndefined);

        data.FirstChild = UnmanagedDataHandle.Undefined;
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
