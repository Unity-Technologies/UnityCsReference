// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine.UIElements;

/// <summary>
/// A <see cref="VisualNodeChildren"/> represents a low level view over the children of a <see cref="VisualNode"/>.
/// </summary>
readonly unsafe struct VisualNodeChildren : IEnumerable<VisualNode>
{
    public struct Enumerator : IEnumerator<VisualNode>
    {
        readonly VisualManager m_Manager;
        readonly VisualNodeChildrenData m_Children;

        int m_Position;

        internal Enumerator(VisualManager manager, in VisualNodeChildrenData children)
        {
            m_Manager = manager;
            m_Children = children;
            m_Position = -1;
        }

        public bool MoveNext() => ++m_Position < m_Children.Count;

        public void Reset() => m_Position = -1;

        public VisualNode Current => new(m_Manager, m_Children[m_Position]);

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
    /// Gets the internal property data ptr. This is only valid outside of structural changes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    VisualNodeChildrenData* GetDataPtr() => (VisualNodeChildrenData*) m_Manager.GetChildrenPtr(in m_Handle).ToPointer();

    /// <summary>
    /// Gets the number of children.
    /// </summary>
    public int Count => m_Manager.GetChildrenCount(in m_Handle);

    /// <summary>
    /// Gets the child at the specified index.
    /// </summary>
    /// <param name="index">The index of the child to get.</param>
    public VisualNode this[int index]
    {
        get
        {
            var data = GetDataPtr();

            if ((uint)index >= data->Count)
                throw new IndexOutOfRangeException();

            return new VisualNode(m_Manager, data->ElementAt(index));
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="VisualNodeChildren"/> for the specified <see cref="VisualNodeHandle"/>.
    /// </summary>
    /// <param name="manager">The manager containing the data.</param>
    /// <param name="handle">The handle to the node.</param>
    public VisualNodeChildren(VisualManager manager, VisualNodeHandle handle)
    {
        m_Manager = manager;
        m_Handle = handle;
    }

    /// <summary>
    /// Adds the given child to the list.
    /// </summary>
    /// <param name="child">The child to add.</param>
    public void Add(in VisualNode child)
    {
        m_Manager.AddChild(m_Handle, child.Handle);
    }

    /// <summary>
    /// Removes the given child from the list.
    /// </summary>
    /// <param name="child">The child to remove.</param>
    public bool Remove(in VisualNode child)
    {
        return m_Manager.RemoveChild(m_Handle, child.Handle);
    }

    public Enumerator GetEnumerator() => new(m_Manager, *GetDataPtr());
    IEnumerator<VisualNode> IEnumerable<VisualNode>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
