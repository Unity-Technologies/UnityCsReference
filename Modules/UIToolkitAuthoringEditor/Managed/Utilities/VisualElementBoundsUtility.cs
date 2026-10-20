// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
static class VisualElementBoundsUtility
{
    // Returns false when any visible descendant has no computed layout yet.
    public static bool TryEncapsulateDescendants(VisualElement element, ref Rect bounds, ref bool hasBounds)
    {
        var complete = true;
        for (var i = 0; i < element.hierarchy.childCount; ++i)
        {
            var child = element.hierarchy[i];
            if (child.resolvedStyle.display == DisplayStyle.None)
                continue;

            var childBounds = child.worldBound;
            if (float.IsNaN(childBounds.xMin) || float.IsNaN(childBounds.yMin) ||
                float.IsNaN(childBounds.xMax) || float.IsNaN(childBounds.yMax))
            {
                complete = false;
            }
            else if (!hasBounds)
            {
                bounds = childBounds;
                hasBounds = true;
            }
            else
            {
                bounds.xMin = Mathf.Min(bounds.xMin, childBounds.xMin);
                bounds.yMin = Mathf.Min(bounds.yMin, childBounds.yMin);
                bounds.xMax = Mathf.Max(bounds.xMax, childBounds.xMax);
                bounds.yMax = Mathf.Max(bounds.yMax, childBounds.yMax);
            }

            complete &= TryEncapsulateDescendants(child, ref bounds, ref hasBounds);
        }
        return complete;
    }
}
