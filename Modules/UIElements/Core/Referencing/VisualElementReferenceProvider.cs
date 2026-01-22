// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Pool;

namespace UnityEngine.UIElements;

/// <summary>
/// Provides and manages a list of VisualElement reference handlers.
/// Handles adding, removing, invoking, and disposing reference handlers.
/// Handlers are stored as weak references to avoid memory leaks.
/// </summary>
[VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
class VisualElementReferenceProvider : IDisposable
{
    Dictionary<IVisualElementReferenceHandler, int> m_DeferredOperations;
    internal readonly List<GCHandle> m_VisualElementReferences = new();
    VisualElementAssetReferenceTable m_ReferenceTable;
    internal bool m_Invoking;

    internal VisualElementAssetReferenceTable referenceTable
    {
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        get => m_ReferenceTable;
    }

    ~VisualElementReferenceProvider() => Dispose(false);

    /// <summary>
    /// Adds a reference handler to the list.
    /// Duplicate handlers are ignored.
    /// </summary>
    /// <param name="handler">The handler to add.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public virtual void Add(IVisualElementReferenceHandler handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        if (m_Invoking)
        {
            m_DeferredOperations ??= DictionaryPool<IVisualElementReferenceHandler, int>.Get();
            if (m_DeferredOperations.TryGetValue(handler, out var count))
            {
                // Increment net count for add
                count++;
                m_DeferredOperations[handler] = count;
            }
            else
            {
                // Create new entry with 1 for add
                m_DeferredOperations[handler] = 1;
            }
            return;
        }

        // Only add if not already present (ignore duplicates)
        if (Contains(handler))
            return;

        var handle = GCHandle.Alloc(handler, GCHandleType.Weak);
        m_VisualElementReferences.Add(handle);

        // If we already have a reference table, inform the newly registered reference about it.
        if (m_ReferenceTable != null)
        {
            handler.ResolveReferences(m_ReferenceTable);
        }
    }

    /// <summary>
    /// Removes a reference handler from the list.
    /// </summary>
    /// <param name="handler">The handler to remove.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public virtual void Remove(IVisualElementReferenceHandler handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        if (m_Invoking)
        {
            m_DeferredOperations ??= DictionaryPool<IVisualElementReferenceHandler, int>.Get();
            if (m_DeferredOperations.TryGetValue(handler, out var count))
            {
                count--;
                m_DeferredOperations[handler] = count;
            }
            else
            {
                // Create new entry with -1 for remove
                m_DeferredOperations[handler] = -1;
            }
            return;
        }

        for (int i = 0; i < m_VisualElementReferences.Count; i++)
        {
            var h = m_VisualElementReferences[i];
            if (h.IsAllocated && h.Target == handler)
            {
                h.Free();

                var lastIndex = m_VisualElementReferences.Count - 1;
                if (i != lastIndex)
                    m_VisualElementReferences[i] = m_VisualElementReferences[lastIndex];
                m_VisualElementReferences.RemoveAt(lastIndex);
                return;
            }
        }
    }

    /// <summary>
    /// Checks if the list contains the given handler.
    /// </summary>
    /// <param name="handler">The handler to check.</param>
    /// <returns></returns>
    public bool Contains(IVisualElementReferenceHandler handler)
    {
        for (int i = 0; i < m_VisualElementReferences.Count; i++)
        {
            var h = m_VisualElementReferences[i];
            if (h.IsAllocated && h.Target == handler)
                return true;
        }
        return false;
    }

    void Invoke(Action<IVisualElementReferenceHandler> action)
    {
        try
        {
            if (m_Invoking)
                throw new InvalidOperationException("Cannot invoke while already invoking.");

            // To avoid unpredictable modifications to the list during execution, we defer all add and remove operations
            m_Invoking = true;

            // Clear references in reverse order to allow for safe removal during iteration
            int i = m_VisualElementReferences.Count - 1;
            while (i >= 0)
            {
                var handle = m_VisualElementReferences[i];
                if (handle.IsAllocated && handle.Target is IVisualElementReferenceHandler handler)
                {
                    action(handler);
                    i--;
                }
                else
                {
                    // Prune null references
                    if (handle.IsAllocated)
                        handle.Free();

                    int lastIndex = m_VisualElementReferences.Count - 1;
                    if (i != lastIndex)
                    {
                        // Swap current with last
                        m_VisualElementReferences[i] = m_VisualElementReferences[lastIndex];
                    }

                    // Remove last element
                    m_VisualElementReferences.RemoveAt(lastIndex);
                    i--; // Move to previous index
                }
            }
        }
        finally
        {
            m_Invoking = false;

            if (m_DeferredOperations != null)
            {
                foreach (var kvp in m_DeferredOperations)
                {
                    var handler = kvp.Key;
                    var count = kvp.Value;

                    if (count > 0)
                    {
                        Add(handler);
                    }
                    else if (count < 0)
                    {
                        Remove(handler);
                    }
                    // If count == 0, no operation needed (adds and removes cancelled out)
                }

                DictionaryPool<IVisualElementReferenceHandler, int>.Release(m_DeferredOperations);
                m_DeferredOperations = null;
            }
        }
    }

    static void ClearReferences(IVisualElementReferenceHandler handler) => handler.ClearReferences();

    VisualElementAssetReferenceTable m_CurrentTable;
    void ResolveReferences(IVisualElementReferenceHandler handler) => handler.ResolveReferences(m_CurrentTable);

    /// <summary>
    /// Invokes ClearReferences on all reference handlers in the list and releases the reference table.
    /// </summary>
    public void UnloadReferences()
    {
        if (m_ReferenceTable != null)
        {
            Invoke(ClearReferences);
            m_ReferenceTable.ReleaseToPool();
            m_ReferenceTable = null;
        }
    }

    /// <summary>
    /// Invokes ResolveReferences on all reference handlers in the list.
    /// </summary>
    /// <param name="table">The table to use to resolve the references.</param>
    public void ResolveReferences(VisualElementAssetReferenceTable table)
    {
        m_ReferenceTable = table;
        InvokeResolveReferences(table);
    }

    void InvokeResolveReferences(VisualElementAssetReferenceTable table)
    {
        try
        {
            m_CurrentTable = table;
            Invoke(ResolveReferences);
        }
        finally
        {
            m_CurrentTable = null;
        }
    }

    /// <summary>
    /// Clears and frees all handles in the list.
    /// </summary>
    void ClearHandles()
    {
        foreach (var handle in m_VisualElementReferences)
        {
            if (handle.IsAllocated)
                handle.Free();
        }
        m_VisualElementReferences.Clear();
    }

    /// <summary>
    /// Disposes the reference handler list and frees all handles.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    void Dispose(bool disposing)
    {
        ClearHandles();
    }
}
