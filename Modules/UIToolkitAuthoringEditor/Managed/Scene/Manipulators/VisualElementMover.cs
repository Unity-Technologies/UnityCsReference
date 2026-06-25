// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class VisualElementMover : VisualElementTransformer
{
    const string k_UssClass = "unity-ve-canvas-mover";
    const string k_StyleSheet = "UIToolkitAuthoring/UIViewportWindow/UIElementMover.uss";
    const string k_CoverLayerName = "drag-hover-cover-layer";
    const string k_CoverLayerUssClass = "unity-ve-canvas-transformer__drag-hover-cover-layer";

    DragManipulator m_MoveManipulator;

    public VisualElementMover()
    {
        AddToClassList(k_UssClass);

        if (EditorGUIUtility.Load(k_StyleSheet) is StyleSheet styleSheet)
            styleSheets.Add(styleSheet);

        m_MoveManipulator = new DragManipulator(OnStartDrag, OnEndDrag, OnMove);

        var coverLayer = new VisualElement();
        coverLayer.name = k_CoverLayerName;
        coverLayer.AddToClassList(k_CoverLayerUssClass);
        Add(coverLayer);

        InitializeDragHoverCoverLayer();
    }

    protected override void SetStylesFromTargetStyles()
    {
        if (Target == null) return;

        var isRelative = Target.resolvedStyle.position == Position.Relative;
        var isAttached = m_MoveManipulator.target != null;

        if (isRelative && isAttached)
        {
            style.cursor = StyleKeyword.None;
            this.RemoveManipulator(m_MoveManipulator);
        }
        else if (!isRelative && !isAttached)
        {
            style.cursor = StyleKeyword.Null;
            this.AddManipulator(m_MoveManipulator);
        }
    }

    protected override void OnDeactivated()
    {
        this.RemoveManipulator(m_MoveManipulator);
    }

    void OnMove(Vector2 diff)
    {
        if (IsTargetScaledOrRotated)
            return;

        var forceTop = IsNoneOrAuto(TrackedStyles.Top) && IsNoneOrAuto(TrackedStyles.Bottom);
        var forceLeft = IsNoneOrAuto(TrackedStyles.Left) && IsNoneOrAuto(TrackedStyles.Right);

        using var group = UICommandQueue.BeginGroup("Move Element");

        if (!AreStylesBound(TrackedStyles.Top | TrackedStyles.Bottom))
        {
            MoveAxis(TrackedStyles.Top, forceTop, TargetRectOnStartDrag.y, diff.y);
            MoveAxis(TrackedStyles.Bottom, false, TargetCorrectedBottomOnStartDrag, -diff.y);
        }

        if (!AreStylesBound(TrackedStyles.Left | TrackedStyles.Right))
        {
            MoveAxis(TrackedStyles.Left, forceLeft, TargetRectOnStartDrag.x, diff.x);
            MoveAxis(TrackedStyles.Right, false, TargetCorrectedRightOnStartDrag, -diff.x);
        }
    }

    void MoveAxis(TrackedStyles trackedStyle, bool force, float onStartDragPrimary, float rawDelta)
    {
        if (IsNoneOrAuto(trackedStyle) && !force) return;
        var delta = Mathf.Ceil(rawDelta * HostToSubPanelScale);
        SetInlineStylePixelValue(trackedStyle, onStartDragPrimary + delta);
    }
}
