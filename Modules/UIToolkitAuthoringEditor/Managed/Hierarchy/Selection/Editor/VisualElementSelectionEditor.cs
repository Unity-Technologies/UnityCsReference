// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace Unity.UIToolkit.Editor;

[CustomEditor(typeof(VisualElementSelection))]
class VisualElementSelectionEditor : UISelectionEditor
{
    private VisualElementSelection Target => (VisualElementSelection)target;

    protected override UISelectionInspectorContent CreateContent() => new VisualElementSelectionInspectorContent();

    public bool HasFrameBounds()
    {
        return GetBoundsInternal(out _);
    }

    public Bounds OnGetFrameBounds()
    {
        if (GetBoundsInternal(out var bounds))
            return bounds;

        return default;
    }

    bool GetBoundsInternal(out Bounds bounds)
    {
        // Resolves the world-space scene instance (incl. from a staging clone), falls back to the host
        // GameObject, and floors the flat-quad bounds — so Edit > Frame Selected / F frame the element
        // properly in the SceneView instead of diving in or doing nothing.
        return VisualElementSceneViewOverlay.TryGetFramableWorldBounds(Target.Element, out bounds, out _);
    }
}
