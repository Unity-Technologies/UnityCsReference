// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitAuthoringFramework not yet converted
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class DragPreviewWindow : EditorWindow
{
    Vector2 m_LastPosition;
    const float k_WindowOffset = 10f;

    public static DragPreviewWindow Show(VisualElement element, Rect layout)
    {
        var window = CreateInstance<DragPreviewWindow>();
        window.rootVisualElement.Add(element);

        var screenPos = GUIUtility.GUIToScreenPoint(layout.position);
        var rect = new Rect(screenPos, layout.size);
        window.ShowAsDropDown(rect, layout.size);

        return window;
    }

    public void UpdatePosition(Vector2 screenPos)
    {
        position = new Rect(screenPos + new Vector2(k_WindowOffset, k_WindowOffset), position.size);
    }

    void Update()
    {
        if (position.position == m_LastPosition)
            return;

        m_LastPosition = position.position;
        Repaint();
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
