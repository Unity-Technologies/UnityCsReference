// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal class OverrideBarManipulator : Manipulator
{
    internal static readonly string ussClassName = "unity-override-row";
    internal static readonly string overrideBarUssClassName = ussClassName + "__override-bar";

    private bool m_IsOverridden;
    VisualElement m_OverrideContainer;

    public Color @Color { get; set; } = Color.white;

    public bool IsOverridden
    {
        get => m_IsOverridden;
        set
        {
            if (m_IsOverridden == value)
                return;

            m_IsOverridden = value;
            target.MarkDirtyRepaint();
        }
    }

    public VisualElement OverrideContainer
    {
        get => m_OverrideContainer ?? target;
        set
        {
            if (m_OverrideContainer == value)
                return;
            m_OverrideContainer = value;
            target.MarkDirtyRepaint();
        }
    }

    public OverrideBarManipulator()
    {
        Color = EditorGUIUtility.isProSkin ? Color.white : Color.black;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.generateVisualContent += DrawOverrideBar;
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.generateVisualContent -= DrawOverrideBar;
    }

    void DrawOverrideBar(MeshGenerationContext mgc)
    {
        if (!IsOverridden)
            return;

        const float lineWidth = 4;
        const float halfLineWidth = 2;

        var targetParent = target.GetFirstAncestorWhere(ve => ve.ClassListContains(overrideBarUssClassName)) ?? target.panel?.visualTree;
        if (targetParent == null)
            return;

        var painter = mgc.painter2D;
        painter.strokeColor = Color;
        painter.lineWidth = lineWidth;
        painter.BeginPath();

        // Get global position of both elements
        var xOffset = targetParent.LocalToWorld(Vector2.zero).x - target.LocalToWorld(Vector2.zero).x + halfLineWidth;
        var yOffset = OverrideContainer.LocalToWorld(Vector2.zero).y - target.LocalToWorld(Vector2.zero).y;
        var yEnd = OverrideContainer.resolvedStyle.height + OverrideContainer.resolvedStyle.marginTop + OverrideContainer.resolvedStyle.marginBottom;

        painter.MoveTo(new Vector2(xOffset, yOffset));
        painter.LineTo(new Vector2(xOffset, yOffset + yEnd));

        painter.Stroke();
    }
}
