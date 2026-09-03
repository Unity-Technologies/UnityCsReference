// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UIToolkit.Editor;

/// <summary>
/// Resolves the panel component the editor selection points at, which is what the UI Viewport previews
/// outside the UI Stage.
/// </summary>
static class MainStageViewportSelection
{
    /// <summary>
    /// The panel component behind the current selection, or <see langword="null"/> when the selection does not
    /// name a live one — which is what keeps the viewport on the document it already shows.
    /// </summary>
    public static IPanelComponent ResolveFromSelection()
    {
        foreach (var selected in Selection.objects)
        {
            var panelComponent = Resolve(selected);
            if (IsAlive(panelComponent))
                return panelComponent;
        }

        return null;
    }

    public static IPanelComponent Resolve(Object selected)
    {
        switch (selected)
        {
            case VisualTreeAssetSelection documentSelection:
                return documentSelection.PanelComponent;

            // The outermost document the element belongs to, so an element inside a nested template still
            // resolves to the component rendering it.
            case VisualElementSelection { Element: { } element }:
                return element.GetFirstOfType<IPanelComponentRootElement>()?.panelComponent;

            // Parent chain and inactive components included, so a selection resolves the same way it does for
            // the Scene view's UI preview overlay.
            case GameObject gameObject:
                return gameObject.GetComponentInParent<IPanelComponent>(true);

            case Component component:
                return component as IPanelComponent;

            default:
                return null;
        }
    }

    // Unity's fake-null does not survive the cast to IPanelComponent, and the selection objects of a deleted
    // document hold exactly such a reference until they are reaped, so aliveness is asked of the Object.
    static bool IsAlive(IPanelComponent panelComponent) => (panelComponent as Object) != null;
}
