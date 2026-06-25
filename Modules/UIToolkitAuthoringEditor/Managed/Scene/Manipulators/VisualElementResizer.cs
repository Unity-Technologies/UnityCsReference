// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

sealed class VisualElementResizer : VisualElementTransformer
{
    const string k_VisualTreeAsset = "UIToolkitAuthoring/UIViewportWindow/UIElementResizer.uxml";
    const string k_FlexDrivenMessage = "Cannot resize an element whose size is driven by flex layout.";
    const string k_BoundPropertyMessageFormat = "Cannot resize because of bound property(ies): {0}";

    static readonly Color k_DisabledColor = new(0.77f, 0.77f, 0.77f); // #C4C4C4

    const string k_TopHandleName = "top-handle";
    const string k_LeftHandleName = "left-handle";
    const string k_BottomHandleName = "bottom-handle";
    const string k_RightHandleName = "right-handle";
    const string k_TopLeftHandleName = "top-left-handle";
    const string k_TopRightHandleName = "top-right-handle";
    const string k_BottomLeftHandleName = "bottom-left-handle";
    const string k_BottomRightHandleName = "bottom-right-handle";

    const string k_SideInnerClass = "unity-ui-element-resizer__side__inner";
    const string k_CornerInnerClass = "unity-ui-element-resizer__corner__inner";

    const string k_TrackedStylesProperty = "TrackedStyles";

    readonly List<VisualElement> m_AbsoluteOnlyHandleElements = new();
    readonly List<VisualElement> m_AllHandleElements = new();

    VisualElement m_ActiveDragHandle;
    Vector2 m_LastDragDiff;
    bool m_ConstraintCorrectionPending;
    bool m_IsApplyingConstraint;

    public VisualElementResizer()
    {
        pickingMode = PickingMode.Ignore;

        var vta = EditorGUIUtility.Load(k_VisualTreeAsset) as VisualTreeAsset;
        vta?.CloneTree(this);

        InitializeDragHoverCoverLayer();

        AddHandle(k_TopHandleName, absoluteOnly: true, TrackedStyles.Top | TrackedStyles.Height, OnStartResizeDrag,
            OnEndResizeDrag, OnDragTop);
        AddHandle(k_LeftHandleName, absoluteOnly: true, TrackedStyles.Left | TrackedStyles.Width, OnStartResizeDrag,
            OnEndResizeDrag, OnDragLeft);
        AddHandle(k_BottomHandleName, absoluteOnly: false, TrackedStyles.Bottom | TrackedStyles.Height,
            OnStartResizeDrag, OnEndResizeDrag, OnDragBottom);
        AddHandle(k_RightHandleName, absoluteOnly: false, TrackedStyles.Right | TrackedStyles.Width, OnStartResizeDrag,
            OnEndResizeDrag, OnDragRight);
        AddHandle(k_TopLeftHandleName, absoluteOnly: true,
            TrackedStyles.Left | TrackedStyles.Top | TrackedStyles.Width | TrackedStyles.Height, OnStartResizeDrag,
            OnEndResizeDrag, OnDragTopLeft);
        AddHandle(k_TopRightHandleName, absoluteOnly: true,
            TrackedStyles.Right | TrackedStyles.Top | TrackedStyles.Height | TrackedStyles.Width, OnStartResizeDrag,
            OnEndResizeDrag, OnDragTopRight);
        AddHandle(k_BottomLeftHandleName, absoluteOnly: true,
            TrackedStyles.Left | TrackedStyles.Bottom | TrackedStyles.Width | TrackedStyles.Height, OnStartResizeDrag,
            OnEndResizeDrag, OnDragBottomLeft);
        AddHandle(k_BottomRightHandleName, absoluteOnly: false,
            TrackedStyles.Right | TrackedStyles.Bottom | TrackedStyles.Width | TrackedStyles.Height, OnStartResizeDrag,
            OnEndResizeDrag, OnDragBottomRight);
    }

    void AddHandle(
        string handleName,
        bool absoluteOnly,
        TrackedStyles trackedStyles,
        Action<VisualElement> startDrag,
        Action endDrag,
        Action<Vector2> dragAction)
    {
        var handle = this.Q(handleName);

        m_AllHandleElements.Add(handle);
        if (absoluteOnly)
            m_AbsoluteOnlyHandleElements.Add(handle);

        var inner = handle.Q(className: k_SideInnerClass)
                    ?? handle.Q(className: k_CornerInnerClass);
        if (inner != null)
        {
            handle.RegisterCallback<PointerEnterEvent>(_ =>
            {
                if (!handle.ClassListContains(k_DisabledHandleUssClass))
                    inner.style.backgroundColor = GetHoverColor();
            });
            handle.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                inner.style.backgroundColor = handle.ClassListContains(k_DisabledHandleUssClass)
                    ? k_DisabledColor
                    : ColorPreferences.SelectionOutline;
            });
        }

        handle.SetProperty(k_TrackedStylesProperty, trackedStyles);

        // Registered before DragManipulator so it fires even when the handle is disabled.
        // DragManipulator.OnPointerDown calls StopImmediatePropagation for disabled handles,
        // but this callback has already run by then.
        handle.RegisterCallback<PointerDownEvent>(e =>
        {
            if (e.button != (int)MouseButton.LeftMouse || e.clickCount == 2)
                return;
            if (handle.ClassListContains(k_DisabledHandleUssClass) &&
                Target != null && !IsTargetScaledOrRotated &&
                Target.resolvedStyle.flexGrow != 0 &&
                Target.resolvedStyle.position == Position.Relative)
            {
                NotifyMessage(k_FlexDrivenMessage);
            }
        });

        handle.AddManipulator(new DragManipulator(
            startDrag,
            endDrag,
            diff =>
            {
                if (!IsTargetScaledOrRotated)
                    dragAction(diff);
            }));
    }

    protected override void HandleEventBubbleUp(EventBase evt)
    {
        switch (evt)
        {
            case AttachToPanelEvent:
                PrefSettings.settingChanged += OnPrefsChanged;
                RefreshHandleColors();
                break;
            case DetachFromPanelEvent:
                PrefSettings.settingChanged -= OnPrefsChanged;
                break;
        }

        base.HandleEventBubbleUp(evt);
    }

    void OnPrefsChanged(string prefName, Type prefType)
    {
        if (string.CompareOrdinal(ColorPreferences.SelectionOutlineColor, prefName) == 0)
            RefreshHandleColors();
    }

    void RefreshHandleColors()
    {
        var normalColor = ColorPreferences.SelectionOutline;
        foreach (var handle in m_AllHandleElements)
        {
            var inner = handle.Q(className: k_SideInnerClass)
                        ?? handle.Q(className: k_CornerInnerClass);
            if (inner != null)
                inner.style.backgroundColor = handle.ClassListContains(k_DisabledHandleUssClass)
                    ? k_DisabledColor
                    : normalColor;
        }
    }

    static Color GetHoverColor() => Color.Lerp(ColorPreferences.SelectionOutline, Color.white, 0.4f);

    // --- Lifecycle ---

    protected override void UpdateBoundStyles()
    {
        base.UpdateBoundStyles();

        if (Target?.parent != null &&
            Target.resolvedStyle.position == Position.Relative &&
            !Mathf.Approximately(Target.resolvedStyle.flexGrow, 0))
        {
            BoundStyles |= Target.parent.resolvedStyle.flexDirection == FlexDirection.Column
                ? TrackedStyles.Height
                : TrackedStyles.Width;
        }
    }

    protected override void SetStylesFromTargetStyles()
    {
        if (Target == null) return;

        var isRelative = Target.resolvedStyle.position == Position.Relative;
        foreach (var handle in m_AbsoluteOnlyHandleElements)
            handle.style.display = isRelative ? DisplayStyle.None : DisplayStyle.Flex;

        UpdateHandleDisabledStates();
    }

    protected override void OnDeactivated()
    {
        m_ActiveDragHandle = null;
    }

    void UpdateHandleDisabledStates()
    {
        foreach (var handle in m_AllHandleElements)
        {
            var trackedStyles = (TrackedStyles)handle.GetProperty(k_TrackedStylesProperty);
            var boundStyles = BoundStyles & trackedStyles;
            var isBound = boundStyles != TrackedStyles.None;

            handle.EnableInClassList(k_DisabledHandleUssClass, isBound);
            handle.tooltip = isBound
                ? GetDisabledTooltip(boundStyles)
                : string.Empty;

            var inner = handle.Q(className: k_SideInnerClass)
                        ?? handle.Q(className: k_CornerInnerClass);
            if (inner != null)
                inner.style.backgroundColor = isBound ? k_DisabledColor : ColorPreferences.SelectionOutline;
        }
    }

    string GetDisabledTooltip(TrackedStyles boundStyles)
    {
        if (Target != null && !IsTargetScaledOrRotated &&
            Target.resolvedStyle.flexGrow != 0 &&
            Target.resolvedStyle.position == Position.Relative)
            return k_FlexDrivenMessage;
        return string.Format(k_BoundPropertyMessageFormat, FormatBoundStyles(boundStyles));
    }

    static string FormatBoundStyles(TrackedStyles styles)
    {
        var names = new System.Text.StringBuilder();

        void Append(string name)
        {
            if (names.Length > 0) names.Append(", ");
            names.Append(name);
        }

        if ((styles & TrackedStyles.Width) != TrackedStyles.None) Append("width");
        if ((styles & TrackedStyles.Height) != TrackedStyles.None) Append("height");
        if ((styles & TrackedStyles.Left) != TrackedStyles.None) Append("left");
        if ((styles & TrackedStyles.Top) != TrackedStyles.None) Append("top");
        if ((styles & TrackedStyles.Right) != TrackedStyles.None) Append("right");
        if ((styles & TrackedStyles.Bottom) != TrackedStyles.None) Append("bottom");
        return names.ToString();
    }

    // --- Drag start / end ---

    void OnStartResizeDrag(VisualElement handle)
    {
        OnStartDrag(handle);

        if (IsTargetScaledOrRotated) return;
        if (Target.resolvedStyle.flexGrow != 0 &&
            Target.resolvedStyle.position == Position.Relative)
        {
            NotifyMessage(k_FlexDrivenMessage);
            return;
        }

        m_ActiveDragHandle = handle;
    }

    void OnEndResizeDrag()
    {
        OnEndDrag();
        m_ActiveDragHandle = null;
    }

    void OnDrag(
        TrackedStyles primaryStyle,
        float onStartDragLength,
        float onStartDragPrimary,
        float delta)
    {
        m_ConstraintCorrectionPending = true;

        var oppositeStyle = GetOppositeStyle(primaryStyle);
        var lengthStyle = GetLengthStyle(primaryStyle);

        var scaledDelta = delta * HostToSubPanelScale;
        delta = scaledDelta >= 0f ? Mathf.Ceil(scaledDelta) : -Mathf.Ceil(-scaledDelta);

        using var group = UICommandQueue.BeginGroup("Resize Element");

        if (!IsNoneOrAuto(oppositeStyle) && !IsNoneOrAuto(primaryStyle))
        {
            SetInlineStylePixelValue(primaryStyle, onStartDragPrimary - delta);
        }
        else if (IsNoneOrAuto(oppositeStyle) && !IsNoneOrAuto(primaryStyle))
        {
            SetInlineStylePixelValue(lengthStyle, onStartDragLength + delta);
            SetInlineStylePixelValue(primaryStyle, onStartDragPrimary - delta);
        }
        else if (!IsNoneOrAuto(oppositeStyle) && IsNoneOrAuto(primaryStyle))
        {
            SetInlineStylePixelValue(lengthStyle, onStartDragLength + delta);
        }
        else
        {
            if (primaryStyle == TrackedStyles.Top || primaryStyle == TrackedStyles.Left)
            {
                SetInlineStylePixelValue(lengthStyle, onStartDragLength + delta);
                SetInlineStylePixelValue(primaryStyle, onStartDragPrimary - delta);
            }
            else
            {
                SetInlineStylePixelValue(lengthStyle, onStartDragLength + delta);
            }
        }
    }

    void OnDragTop(Vector2 diff)
    {
        var newHeight = TargetRectOnStartDrag.height - diff.y * HostToSubPanelScale;
        if (newHeight < 0)
            diff.y = TargetRectOnStartDrag.height / HostToSubPanelScale;

        OnDrag(TrackedStyles.Top, TargetRectOnStartDrag.height, TargetRectOnStartDrag.y, -diff.y);
    }

    void OnDragLeft(Vector2 diff)
    {
        var newWidth = TargetRectOnStartDrag.width - diff.x * HostToSubPanelScale;
        if (newWidth < 0)
            diff.x = TargetRectOnStartDrag.width / HostToSubPanelScale;

        OnDrag(TrackedStyles.Left, TargetRectOnStartDrag.width, TargetRectOnStartDrag.x, -diff.x);
    }

    void OnDragBottom(Vector2 diff)
    {
        var newHeight = TargetRectOnStartDrag.height + diff.y * HostToSubPanelScale;
        if (newHeight < 0)
            diff.y = -TargetRectOnStartDrag.height / HostToSubPanelScale;

        m_LastDragDiff.y = diff.y;

        OnDrag(TrackedStyles.Bottom, TargetRectOnStartDrag.height, TargetCorrectedBottomOnStartDrag, diff.y);
    }

    void OnDragRight(Vector2 diff)
    {
        var newWidth = TargetRectOnStartDrag.width + diff.x * HostToSubPanelScale;
        if (newWidth < 0)
            diff.x = -TargetRectOnStartDrag.width / HostToSubPanelScale;

        m_LastDragDiff.x = diff.x;

        OnDrag(TrackedStyles.Right, TargetRectOnStartDrag.width, TargetCorrectedRightOnStartDrag, diff.x);
    }

    void OnDragTopLeft(Vector2 diff)
    {
        OnDragTop(diff);
        OnDragLeft(diff);
    }

    void OnDragTopRight(Vector2 diff)
    {
        OnDragTop(diff);
        OnDragRight(diff);
    }

    void OnDragBottomLeft(Vector2 diff)
    {
        OnDragBottom(diff);
        OnDragLeft(diff);
    }

    void OnDragBottomRight(Vector2 diff)
    {
        OnDragBottom(diff);
        OnDragRight(diff);
    }

    public override void OnProcessChangeOnTarget()
    {
        OnConstrainDuringDrag();
    }

    void OnConstrainDuringDrag()
    {
        if (!m_ConstraintCorrectionPending || m_IsApplyingConstraint) return;
        m_ConstraintCorrectionPending = false;
        m_IsApplyingConstraint = true;
        try
        {
            ApplyConstraintCorrection();
        }
        finally
        {
            m_IsApplyingConstraint = false;
        }
    }

    void ApplyConstraintCorrection()
    {
        switch (m_ActiveDragHandle?.name)
        {
            case k_TopHandleName:
            {
                if (ShouldConstrainDragTopToTargetHeight(out var diffY))
                    OnDragTop(new Vector2(0, diffY));
                break;
            }

            case k_LeftHandleName:
            {
                if (ShouldConstrainDragLeftToTargetWidth(out var diffX))
                    OnDragLeft(new Vector2(diffX, 0));
                break;
            }

            case k_BottomHandleName:
            {
                if (ShouldConstrainDragBottomToTargetHeight(out var diffY))
                    OnDragBottom(new Vector2(0, diffY));
                break;
            }

            case k_RightHandleName:
            {
                if (ShouldConstrainDragRightToTargetWidth(out var diffX))
                    OnDragRight(new Vector2(diffX, 0));
                break;
            }

            case k_TopLeftHandleName:
            {
                var constrainedW = ShouldConstrainDragLeftToTargetWidth(out var diffX);
                var constrainedH = ShouldConstrainDragTopToTargetHeight(out var diffY);
                if (constrainedW) OnDragLeft(new Vector2(diffX, diffY));
                if (constrainedH) OnDragTop(new Vector2(diffX, diffY));
                break;
            }

            case k_TopRightHandleName:
            {
                var constrainedW = ShouldConstrainDragRightToTargetWidth(out var diffX);
                var constrainedH = ShouldConstrainDragTopToTargetHeight(out var diffY);
                if (constrainedW) OnDragRight(new Vector2(diffX, diffY));
                if (constrainedH) OnDragTop(new Vector2(diffX, diffY));
                break;
            }

            case k_BottomLeftHandleName:
            {
                var constrainedW = ShouldConstrainDragLeftToTargetWidth(out var diffX);
                var constrainedH = ShouldConstrainDragBottomToTargetHeight(out var diffY);
                if (constrainedW) OnDragLeft(new Vector2(diffX, diffY));
                if (constrainedH) OnDragBottom(new Vector2(diffX, diffY));
                break;
            }

            case k_BottomRightHandleName:
            {
                var constrainedW = ShouldConstrainDragRightToTargetWidth(out var diffX);
                var constrainedH = ShouldConstrainDragBottomToTargetHeight(out var diffY);
                if (constrainedW) OnDragRight(new Vector2(diffX, diffY));
                if (constrainedH) OnDragBottom(new Vector2(diffX, diffY));
                break;
            }
        }
    }

    // --- Constraint helpers ---

    bool ShouldConstrainDragTopToTargetHeight(out float diffY)
    {
        var parentBorderTop = Target.parent?.resolvedStyle.borderTopWidth ?? 0f;
        var marginTop = Target.resolvedStyle.marginTop;
        if (!Mathf.Approximately(
                TargetRectOnStartDrag.yMax,
                Target.layout.yMax - marginTop - parentBorderTop))
        {
            diffY = GenerateClampedHeightDiff();
            return true;
        }

        diffY = 0;
        return false;
    }

    bool ShouldConstrainDragLeftToTargetWidth(out float diffX)
    {
        var parentBorderLeft = Target.parent?.resolvedStyle.borderLeftWidth ?? 0f;
        var marginLeft = Target.resolvedStyle.marginLeft;
        if (!Mathf.Approximately(
                TargetRectOnStartDrag.xMax,
                Target.layout.xMax - parentBorderLeft - marginLeft))
        {
            diffX = GenerateClampedWidthDiff();
            return true;
        }

        diffX = 0;
        return false;
    }

    bool ShouldConstrainDragBottomToTargetHeight(out float diffY)
    {
        var expected = Mathf.Ceil(TargetRectOnStartDrag.height + m_LastDragDiff.y * HostToSubPanelScale);
        if (!Mathf.Approximately(Target.resolvedStyle.height, expected))
        {
            diffY = -GenerateClampedHeightDiff();
            return true;
        }

        diffY = 0;
        return false;
    }

    bool ShouldConstrainDragRightToTargetWidth(out float diffX)
    {
        var expected = Mathf.Ceil(TargetRectOnStartDrag.width + m_LastDragDiff.x * HostToSubPanelScale);
        if (!Mathf.Approximately(Target.resolvedStyle.width, expected))
        {
            diffX = -GenerateClampedWidthDiff();
            return true;
        }

        diffX = 0;
        return false;
    }

    float GenerateClampedHeightDiff()
    {
        var scaledHeight = Target.resolvedStyle.height / HostToSubPanelScale;
        return OverlayRectOnStartDrag.height - scaledHeight;
    }

    float GenerateClampedWidthDiff()
    {
        var scaledWidth = Target.resolvedStyle.width / HostToSubPanelScale;
        return OverlayRectOnStartDrag.width - scaledWidth;
    }
}
