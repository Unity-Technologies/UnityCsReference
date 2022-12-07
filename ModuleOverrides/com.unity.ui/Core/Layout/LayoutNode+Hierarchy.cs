// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Assertions;

namespace UnityEngine.UIElements.Layout;

partial struct LayoutNode
{
    const int k_DefaultChildCapacity = 4;

    /// <summary>
    /// Gets or sets the parent for this node.
    /// </summary>
    public LayoutNode Parent
    {
        get => new (m_Access, m_Access.GetNodeData(m_Handle).Parent);
        set => m_Access.GetNodeData(m_Handle).Parent = value.m_Handle;
    }

    /// <summary>
    /// Gets or sets the next child for this node.
    /// </summary>
    public LayoutNode NextChild
    {
        get => new(m_Access, m_Access.GetNodeData(m_Handle).NextChild);
        set => m_Access.GetNodeData(m_Handle).NextChild = value.m_Handle;
    }

    /// <summary>
    /// Returns the underlying layout list of children.
    /// </summary>
    LayoutList<LayoutHandle> Children => m_Access.GetNodeData(m_Handle).Children;

    /// <summary>
    /// Return the child count for this node.
    /// </summary>
    public int Count => Children.IsCreated ? Children.Count : 0;

    /// <summary>
    /// Gets or sets the child at the given index.
    /// </summary>
    /// <remarks>
    /// WARNING: This has no safety checks, use with caution.
    /// </remarks>
    /// <param name="index"></param>
    public LayoutNode this[int index]
    {
        get => new (m_Access, Children[index]);
        set => Children[index] = value.Handle;
    }

    /// <summary>
    /// Adds the specified node as a child.
    /// </summary>
    /// <param name="child">The child to add.</param>
    public void AddChild(LayoutNode child)
    {
        Insert(Count, child);
    }

    /// <summary>
    /// Removes the specified child.
    /// </summary>
    /// <param name="child">The child to remove.</param>
    public void RemoveChild(LayoutNode child)
    {
        ref var data = ref m_Access.GetNodeData(m_Handle);

        Assert.IsTrue(data.Children.IsCreated);

        var index = data.Children.IndexOf(child.m_Handle);

        if (index >= 0)
            RemoveAt(index);
    }

    /// <summary>
    /// Returns the index of the specified child.
    /// </summary>
    /// <param name="child">The child to ge the index of</param>
    /// <returns>The index of the specified child; -1 if it's not a child.</returns>
    public int IndexOf(LayoutNode child)
    {
        ref var data = ref m_Access.GetNodeData(m_Handle);

        if (data.Children.IsCreated)
            return data.Children.IndexOf(child.m_Handle);

        return -1;
    }

    /// <summary>
    /// Inserts a new child to this node.
    /// </summary>
    /// <param name="index">The index to insert the child at.</param>
    /// <param name="child">The child node to insert.</param>
    public void Insert(int index, LayoutNode child)
    {
        ref var data = ref m_Access.GetNodeData(m_Handle);

        if (!data.Children.IsCreated)
            data.Children = new LayoutList<LayoutHandle>(k_DefaultChildCapacity, Allocator.Persistent);

        data.Children.Insert(index, child.Handle);
        child.Parent = this;

        MarkDirty();
    }

    /// <summary>
    /// Removes the child at the specified index.
    /// </summary>
    /// <param name="index">The index to remove the child at.</param>
    public void RemoveAt(int index)
    {
        ref var data = ref m_Access.GetNodeData(m_Handle);

        Assert.IsTrue(data.Children.IsCreated);

        if ((uint) index >= data.Children.Count)
            throw new ArgumentOutOfRangeException();

        var childHandle = data.Children[index];

        ref var childData = ref m_Access.GetNodeData(childHandle);

        var isOwned = childData.Parent.Equals(m_Handle);
        childData.Parent = LayoutHandle.Undefined;
        data.Children.RemoveAt(index);

        if (isOwned)
            MarkDirty();
    }

    /// <summary>
    /// Clears all children from this node.
    /// </summary>
    public void Clear()
    {
        ref var data = ref m_Access.GetNodeData(m_Handle);

        if (!data.Children.IsCreated)
            return;

        while (data.Children.Count > 0)
            RemoveAt(data.Children.Count-1);
    }

    /// <summary>
    /// Gets an enumerator to iterate over all children.
    /// </summary>
    /// <remarks>
    /// This uses duck typing and does explicitly implement IEnumerable{YogaNode}.
    /// </remarks>
    public Enumerator GetEnumerator()
    {
        return new Enumerator(m_Access, Children);
    }

    public struct Enumerator : IEnumerator<LayoutNode>
    {
        readonly LayoutDataAccess m_Access;
        LayoutList<LayoutHandle>.Enumerator m_Enumerator;

        public Enumerator(LayoutDataAccess access, LayoutList<LayoutHandle> children)
        {
            m_Access = access;
            m_Enumerator = children.GetEnumerator();
        }

        public LayoutNode Current => new (m_Access, m_Enumerator.Current);
        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }

        public void Reset()
        {
            m_Enumerator.Reset();
        }

        public bool MoveNext()
        {
            return m_Enumerator.MoveNext();
        }
    }
}
