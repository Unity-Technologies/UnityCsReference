// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Draws a handle per selected element, positioned from the element the canvas actually renders.
/// </summary>
/// <remarks>
/// A selection does not always name that element: outside the UI Stage it names the live scene element while
/// the canvas shows a preview clone of it, so <paramref name="resolveTarget"/> maps one to the other. It is
/// re-asked rather than cached because a re-clone replaces the element behind an unchanged selection.
/// </remarks>
sealed class SelectionHandleManager(
    VisualElement handleContainer,
    Func<VisualElementSelection, VisualElement> resolveTarget)
{
    const string k_HideHeadersUssClass = "unity-selection-handle-container--no-header";

    [NoAutoStaticsCleanup] // handle pool, safe to persist
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

        handle.Target = resolveTarget(selection);
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
        handle.Target = resolveTarget(selection);
        handle.SetLayoutFromTarget();
    }

    public void UpdateAllHandles()
    {
        foreach (var kvp in m_SelectionObjectToHandle)
        {
            kvp.Value.Target = resolveTarget(kvp.Key);
            kvp.Value.SetLayoutFromTarget();
        }
    }
}
