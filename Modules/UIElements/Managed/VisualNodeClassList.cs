// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.UIElements;

/// <summary>
/// The <see cref="VisualNodeClassList"/> represents a low level view over the class names of a <see cref="VisualNode"/>.
/// </summary>
readonly struct VisualNodeClassList : IList<string>
{
    public struct Enumerator : IEnumerator<string>
    {
        readonly VisualManager m_Manager;
        readonly VisualNodeClassData m_Data;

        int m_Position;

        internal Enumerator(VisualManager manager, in VisualNodeClassData data)
        {
            m_Manager = manager;
            m_Data = data;
            m_Position = -1;
        }

        public bool MoveNext() => ++m_Position < m_Data.Count;

        public void Reset() => m_Position = -1;

        public string Current => m_Manager.ClassNameStore.GetClassNameManaged(m_Data[m_Position]);

        object IEnumerator.Current => Current;

        public void Dispose() { }
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
    /// Gets the number of classes that have been added to this list.
    /// </summary>
    public int Count => m_Manager.GetProperty<VisualNodeClassData>(m_Handle).Count;

    public string this[int index]
    {
        get
        {
            ref var classes = ref m_Manager.GetProperty<VisualNodeClassData>(m_Handle);
            return m_Manager.ClassNameStore.GetClassNameManaged(classes[index]);
        }
        set => throw new System.NotImplementedException();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="VisualNodeClassList"/> for the specified <see cref="VisualNodeHandle"/>.
    /// </summary>
    /// <param name="store">The manager containing the data.</param>
    /// <param name="handle">The handle to the node.</param>
    public VisualNodeClassList(VisualManager store, VisualNodeHandle handle)
    {
        m_Manager = store;
        m_Handle = handle;
    }

    /// <summary>
    /// Adds the given class to the <see cref="VisualNodeClassData"/>.
    /// </summary>
    /// <param name="className">The class name to add.</param>
    public void Add(string className)
        => m_Manager.AddToClassList(in m_Handle, className);

    /// <summary>
    /// Removes the given class from the <see cref="VisualNodeClassData"/>.
    /// </summary>
    /// <param name="className">The class name to remove.</param>
    public bool Remove(string className)
        => m_Manager.RemoveFromClassList(in m_Handle, className);

    /// <summary>
    /// Checks if the given class has been added to the list.
    /// </summary>
    /// <param name="className">The class name to check.</param>
    public bool Contains(string className)
        => m_Manager.ClassListContains(in m_Handle, className);

    /// <summary>
    /// Clears all classes from the list.
    /// </summary>
    public void Clear()
        => m_Manager.ClearClassList(in m_Handle);

    bool ICollection<string>.IsReadOnly => false;

    void ICollection<string>.CopyTo(string[] array, int arrayIndex)
    {
        for (int srcIndex = 0, dstIndex = arrayIndex; srcIndex < Count; srcIndex++, dstIndex++)
            array[dstIndex] = this[srcIndex];
    }

    public Enumerator GetEnumerator()
        => new (m_Manager, m_Manager.GetProperty<VisualNodeClassData>(m_Handle));

    IEnumerator<string> IEnumerable<string>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    int IList<string>.IndexOf(string item)
    {
        throw new System.NotImplementedException();
    }

    void IList<string>.Insert(int index, string item)
    {
        throw new System.NotImplementedException();
    }

    void IList<string>.RemoveAt(int index)
    {
        throw new System.NotImplementedException();
    }
}


