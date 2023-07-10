// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.UIElements;

/// <summary>
/// The <see cref="VisualNodeClassNameStore"/> represents the bindings to the native string storage.
/// </summary>
[NativeType(Header = "Modules/UIElements/VisualNodeClassNameStore.h")]
class VisualNodeClassNameStore : IDisposable
{
    [UsedImplicitly]
    internal static class BindingsMarshaller
    {
        public static IntPtr ConvertToNative(VisualNodeClassNameStore store) => store.m_Ptr;
        public static VisualNodeClassNameStore ConvertToManaged(IntPtr ptr) => new(ptr, isWrapper: true);
    }

    /// <summary>
    /// Handle to the native allocated manager.
    /// </summary>
    [RequiredByNativeCode] IntPtr m_Ptr;

    /// <summary>
    /// Flag indicating if this instance owns the managed memory.
    /// </summary>
    [RequiredByNativeCode] bool m_IsWrapper;

    /// <summary>
    /// Managed string allocations.
    /// </summary>
    string[] m_ClassNames = new string[512];

    /// <summary>
    /// Managed map of string hash to id.
    /// </summary>
    Dictionary<string, int> m_Map = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="VisualManager"/> class.
    /// </summary>
    public VisualNodeClassNameStore() : this(Internal_Create(), false)
    {
    }

    VisualNodeClassNameStore(IntPtr ptr, bool isWrapper)
    {
        m_Ptr = ptr;
        m_IsWrapper = isWrapper;
    }

    ~VisualNodeClassNameStore()
    {
        Dispose(false);
    }

    /// <summary>
    /// Dispose this object, releasing its memory.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        if (m_Ptr != IntPtr.Zero)
        {
            if (!m_IsWrapper)
                Internal_Destroy(m_Ptr);
            m_Ptr = IntPtr.Zero;
        }
    }

    /// <summary>
    /// Gets the class name for the given id.
    /// </summary>
    /// <param name="id">The id to get the name for.</param>
    /// <returns>The class name.</returns>
    public string GetClassNameManaged(int id)
    {
        var length = m_ClassNames.Length;

        if ((uint) id < length)
        {
            if (!string.IsNullOrEmpty(m_ClassNames[id]))
                return m_ClassNames[id];
        }
        else
        {
            while (length <= id)
                length *= 2;

            Array.Resize(ref m_ClassNames, length);
        }

        var str = GetClassName(id);

        m_ClassNames[id] = str;
        return str;
    }

    /// <summary>
    /// Gets the id for the given class name.
    /// </summary>
    /// <param name="className">The classname to get the id for.</param>
    /// <returns>The class name id.</returns>
    public int GetClassNameIdManaged(string className)
    {
        if (m_Map.TryGetValue(className, out var id))
            return id;

        id = GetClassNameId(className);
        m_Map.Add(className, id);
        return id;
    }

    [FreeFunction("VisualNodeClassNameStore::Create")]
    static extern IntPtr Internal_Create();

    [FreeFunction("VisualNodeClassNameStore::Destroy")]
    static extern void Internal_Destroy(IntPtr ptr);

    [NativeThrows]
    internal extern int Insert(string className);

    [NativeThrows]
    internal extern int GetClassNameId(string className);

    [NativeThrows]
    internal extern string GetClassName(int id);
}
