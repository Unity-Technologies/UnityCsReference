// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class PlacementIndicator : VisualElement
{
    const float k_Thickness = 4f;
    const float k_HalfThickness = k_Thickness / 2f;

    public PlacementIndicator()
    {
        style.position = Position.Absolute;
        style.display = DisplayStyle.None;
        pickingMode = PickingMode.Ignore;
        AddToClassList("unity-collection-view__drag-hover-bar");
    }

    public void Show(VisualElement element, bool nearLeft, bool nearRight, bool nearTop, bool nearBottom)
    {
        var bounds = element.worldBound;
        var ppp = ((Panel)element.panel)?.pixelsPerPoint ?? 1f;

        // Convert from sub-panel world space to document-root local CSS space (same as SelectionHandle).
        var x = bounds.x / ppp;
        var y = bounds.y / ppp;
        var w = bounds.width / ppp;
        var h = bounds.height / ppp;

        if (nearLeft)
        {
            style.left = x - k_HalfThickness;
            style.top = y;
            style.width = k_Thickness;
            style.height = h;
        }
        else if (nearRight)
        {
            style.left = x + w - k_HalfThickness;
            style.top = y;
            style.width = k_Thickness;
            style.height = h;
        }
        else if (nearTop)
        {
            style.left = x;
            style.top = y - k_HalfThickness;
            style.width = w;
            style.height = k_Thickness;
        }
        else if (nearBottom)
        {
            style.left = x;
            style.top = y + h - k_HalfThickness;
            style.width = w;
            style.height = k_Thickness;
        }
        else
        {
            return;
        }

        style.display = DisplayStyle.Flex;
    }

    public void Hide() => style.display = DisplayStyle.None;
}
