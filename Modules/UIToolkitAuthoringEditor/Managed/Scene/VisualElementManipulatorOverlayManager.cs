// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class VisualElementManipulatorOverlayManager
{
    static readonly UnityEngine.Pool.ObjectPool<VisualElementManipulatorOverlay> s_Pool =
        new(() => new VisualElementManipulatorOverlay(),
            null,
            overlay => overlay.Deactivate());

    readonly VisualElement m_Container;

    readonly Dictionary<VisualElementSelection, VisualElementManipulatorOverlay> m_SelectionToOverlay = new();

    public float ZoomScale { get; set; } = 1f;

    public VisualElementManipulatorOverlayManager(VisualElement container)
    {
        m_Container = container;
    }

    public void AcquireOverlay(VisualElementSelection selection)
    {
        if (m_SelectionToOverlay.ContainsKey(selection)) return;

        var overlay = s_Pool.Get();
        overlay.ZoomScale = ZoomScale;
        m_Container.Add(overlay);
        overlay.Target = selection.Element;
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
        overlay.Target = selection.Element;
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
            overlay.Target = selection.Element;
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
