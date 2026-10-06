// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Hosts the move/resize overlay for the selected element, over the element the canvas actually renders.
/// </summary>
/// <remarks>
/// Same indirection as <see cref="SelectionHandleManager"/>: the selection can name a live scene element while
/// the canvas shows a preview clone of it, and the overlay has to sit on the clone to land on screen. The
/// writes it makes go to the backing asset either way, so both faces of the element see them.
/// </remarks>
sealed class VisualElementManipulatorOverlayManager
{
    [NoAutoStaticsCleanup] // overlay pool, safe to persist
    static readonly UnityEngine.Pool.ObjectPool<VisualElementManipulatorOverlay> s_Pool =
        new(() => new VisualElementManipulatorOverlay(),
            null,
            overlay => overlay.Deactivate());

    readonly VisualElement m_Container;
    readonly Func<VisualElementSelection, VisualElement> m_ResolveTarget;

    readonly Dictionary<VisualElementSelection, VisualElementManipulatorOverlay> m_SelectionToOverlay = new();

    public float ZoomScale { get; set; } = 1f;

    public VisualElementManipulatorOverlayManager(
        VisualElement container,
        Func<VisualElementSelection, VisualElement> resolveTarget)
    {
        m_Container = container;
        m_ResolveTarget = resolveTarget;
    }

    public void AcquireOverlay(VisualElementSelection selection)
    {
        if (m_SelectionToOverlay.ContainsKey(selection)) return;

        var overlay = s_Pool.Get();
        overlay.ZoomScale = ZoomScale;
        m_Container.Add(overlay);
        overlay.Target = m_ResolveTarget(selection);
        overlay.IsReadOnly = IsReadOnly(selection);
        overlay.Activate();

        m_SelectionToOverlay[selection] = overlay;
    }

    public void ReleaseOverlay(VisualElementSelection selection)
    {
        if (!m_SelectionToOverlay.TryGetValue(selection, out var overlay)) return;

        overlay.RemoveFromHierarchy();
        s_Pool.Release(overlay);

        m_SelectionToOverlay.Remove(selection);
    }

    public void UpdateOverlay(VisualElementSelection selection)
    {
        if (!m_SelectionToOverlay.TryGetValue(selection, out var overlay)) return;
        overlay.ZoomScale = ZoomScale;
        overlay.Target = m_ResolveTarget(selection);
        overlay.IsReadOnly = IsReadOnly(selection);
        overlay.Activate();
        overlay.UpdateLayout();
    }

    public void OnProcessChangeOnTarget(VisualElementSelection selection)
    {
        if (!m_SelectionToOverlay.TryGetValue(selection, out var overlay)) return;
        overlay.OnProcessChangeOnTarget();
    }

    public void UpdateAllOverlays()
    {
        var zoomScale = ZoomScale;
        foreach (var kvp in m_SelectionToOverlay)
        {
            var overlay = kvp.Value;
            var selection = kvp.Key;
            overlay.ZoomScale = zoomScale;
            overlay.Target = m_ResolveTarget(selection);
            overlay.IsReadOnly = IsReadOnly(selection);
            overlay.Activate();
        }
    }

    public void ReleaseAll()
    {
        foreach (var kvp in m_SelectionToOverlay)
        {
            kvp.Value.RemoveFromHierarchy();
            s_Pool.Release(kvp.Value);
        }

        m_SelectionToOverlay.Clear();
    }

    static bool IsReadOnly(VisualElementSelection selection) =>
        selection == null || (selection.EditFlags & VisualElementEditFlags.Styles) == 0;
}
