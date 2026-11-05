// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

// How the UI Viewport's canvas and the Scene view's UI Preview overlay pick elements (UUM-157821). Like UI Builder's
// canvas (BuilderViewport.PickElement), they pick the elements that ignore picking at runtime too: decorations, and
// labels or cards over a scrim or a dimmer, would otherwise only be selectable from the Hierarchy. Hidden ones
// (visibility: hidden, display: none) still aren't. An element that ignores picking is only picked where it shows: the
// renderer drew something visible for it (VisualElement.rendersVisibleGeometry), or it contains a picked element that
// shows. A clear layer over the whole screen (an overlay's root, a template's container) lets the click through, as at
// runtime, or it would take every click.
// Turning off Project Settings > UI Toolkit > Select Elements with Picking Mode Ignore picks as the runtime does.
static class UIAuthoringPicking
{
    // The elements under the position, topmost first.
    internal static void PickAll(Panel panel, Vector2 position, List<VisualElement> picked)
    {
        if (!UIToolkitAuthoringSettings.SelectPickingModeIgnore)
        {
            panel.PickAll(position, picked);
            return;
        }

        panel.ValidateLayout();
        picked.Clear();
        Panel.PickAll(panel.visualTree, position, picked, includeIgnoredElement: true);

        // Set aside the elements that ignore picking and show nothing themselves: they only show through what they
        // contain.
        using var _ = ListPool<VisualElement>.Get(out var clear);
        for (var i = picked.Count - 1; i >= 0; i--)
        {
            var element = picked[i];
            if (element.pickingMode != PickingMode.Ignore || element.rendersVisibleGeometry)
                continue;
            clear.Add(element);
            picked.RemoveAt(i);
        }

        // Each one comes back if it contains something that shows, right after the last of those. Usually that's where
        // it was, but a negative z-index puts a child behind its parent, after it in the picks.
        foreach (var element in clear)
        {
            var last = -1;
            for (var i = 0; i < picked.Count; i++)
            {
                if (element.Contains(picked[i]))
                    last = i;
            }
            if (last >= 0)
                picked.Insert(last + 1, element);
        }
    }

    // Whether rectangle selection can take the element, if the rectangle covers it: like a click, elements that ignore
    // picking at runtime are selectable where they show, themselves or through their built-in parts; hidden ones
    // aren't.
    internal static bool IsRectangleSelectable(VisualElement element)
    {
        if (!element.visible)
            return false;
        if (element.pickingMode != PickingMode.Ignore)
            return true;
        return UIToolkitAuthoringSettings.SelectPickingModeIgnore && ShowsItselfOrThroughItsParts(element);
    }

    // A control shows through its built-in parts too: a Toggle's box and label, a Slider's tracker, made in code with no
    // UXML of their own. Its authored children are selectable on their own.
    static bool ShowsItselfOrThroughItsParts(VisualElement element)
    {
        if (element.rendersVisibleGeometry)
            return true;
        var children = element.hierarchy;
        for (var i = 0; i < children.childCount; i++)
        {
            var child = children[i];
            if (child.visualElementAsset != null || !child.visible || child.resolvedStyle.display == DisplayStyle.None)
                continue;
            if (ShowsItselfOrThroughItsParts(child))
                return true;
        }
        return false;
    }
}
