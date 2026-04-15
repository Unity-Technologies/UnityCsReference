// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor.Utilities;

static class VisualElementUtility
{
    const string k_SelectionObjectPropertyKey = "unity-selection-object";

    public static void SetSelectionObject(this VisualElement element, VisualElementSelection selectionObject)
    {
        element.SetProperty(k_SelectionObjectPropertyKey, selectionObject);
    }

    public static VisualElementSelection GetSelectionObject(this VisualElement element)
    {
        return element.GetProperty(k_SelectionObjectPropertyKey) as VisualElementSelection;
    }

    public static void ClearSelectionObject(this VisualElement element)
    {
        element.ClearProperty(k_SelectionObjectPropertyKey);
    }

    public static void SetInlineBorderColor(this VisualElement element, StyleColor color)
    {
        element.style.borderTopColor = color;
        element.style.borderRightColor = color;
        element.style.borderBottomColor = color;
        element.style.borderLeftColor = color;
    }
}
