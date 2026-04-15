// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class SelectionHandleManager(VisualElement handleContainer)
{
    const string k_HideHeadersUssClass = "unity-selection-handle-container--no-header";

    static readonly UnityEngine.Pool.ObjectPool<SelectionHandle> s_HandlePool =
        new (() => new SelectionHandle(),
            null,
            handle => handle.Target = null);

    readonly Dictionary<VisualElementSelection, SelectionHandle> m_SelectionObjectToHandle = new();

    int HandleCount => m_SelectionObjectToHandle.Count;

    public void AcquireSelectionHandle(VisualElementSelection selection)
    {
        if (!m_SelectionObjectToHandle.TryGetValue(selection, out var handle))
        {
            m_SelectionObjectToHandle[selection] = handle = s_HandlePool.Get();
            handleContainer.Add(handle);
        }

        handle.Target = selection.Element;
        handleContainer.EnableInClassList(k_HideHeadersUssClass, HandleCount > 1);
    }

    public void ReleaseSelectionHandle(VisualElementSelection selection)
    {
        if (!m_SelectionObjectToHandle.TryGetValue(selection, out var handle))
            return;
        handle.RemoveFromHierarchy();
        handle.Target = null;
        s_HandlePool.Release(handle);
        m_SelectionObjectToHandle.Remove(selection);
        handleContainer.EnableInClassList(k_HideHeadersUssClass, HandleCount > 1);
    }

    public void UpdateSelectionHandle(VisualElementSelection selection)
    {
        if (!m_SelectionObjectToHandle.TryGetValue(selection, out var handle))
            return;
        handle.Target = selection.Element;
        handle.SetLayoutFromTarget();
    }

    public void UpdateAllHandles()
    {
        foreach (var kvp in m_SelectionObjectToHandle)
        {
            kvp.Value.Target = kvp.Key.Element;
            kvp.Value.SetLayoutFromTarget();
        }
    }
}
