// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// Draws the orientation gizmo and handles its input. Reads and drives the view through an
// OrientationGizmoTarget, so it is not tied to the Scene View.
class OrientationGizmoElement : VisualElement
{
    const string k_AxisLabelClass = "orientation-gizmo-label";
    const string k_RotationLockedClass = "orientation-gizmo--rotation-locked";

    // Mirrors #orientation-gizmo-area's width in OrientationGizmo.uss - Overlay_LockIconSitsWhereHeaderClampEnds
    // fails if the two ever drift.
    internal const float k_ElementSize = 85f;
    internal const float k_AxisDistance = 29f;
    const float k_PositiveHandleRadius = 7f;
    const float k_NegativeHandleRadius = 6f;
    const float k_CenterHitRadius = 7f;
    const float k_AxisHitRadius = 9f;
    const float k_CenterSquareHalfSize = 3f;
    const float k_CenterCircleRadius = 3f;
    const float k_RotationLockedAlpha = 0.4f;
    const float k_HoverAlpha = 0.85f;
    // Mirrors #orientation-gizmo-lock-icon in OrientationGizmo.uss.
    const float k_LockIconSize = 16f;
    const float k_LockIconMargin = 3f;
    // OrientationGizmoOverlay clamps the header's width to stop here, short of the lock icon.
    internal const float k_LockIconLeftEdge = k_ElementSize - k_LockIconMargin - k_LockIconSize;
    // OrientationGizmoOverlay clamps the header's height to stop here, short of a straight-up axis marker.
    internal const float k_TopAxisMarkerEdge =
        k_ElementSize * 0.5f + k_GizmoVerticalOffset - k_AxisDistance - k_PositiveHandleRadius;

    // Nudges the circle down from the exact center of its own square area, to account for the
    // projection row below it.
    const float k_GizmoVerticalOffset = 6f;
    // A marker at k_AxisDistance moves under a quarter of a pixel per half-degree, so a smaller
    // delta is not worth a layout pass and a mesh rebuild.
    const float k_MinRotationDeltaDegrees = 0.5f;

    static readonly string[] k_DirectionMenuNames = { "Right", "Top", "Front", "Left", "Bottom", "Back" };

    readonly OrientationGizmoTarget m_Target;
    readonly OrientationGizmoOverlay m_Overlay;
    readonly VisualElement m_GizmoArea;
    readonly VisualElement m_LockIcon;
    readonly VisualElement m_CameraModeIcon;
    readonly VisualElement m_CameraModeToggle;
    readonly Label[] m_AxisLabels = new Label[OrientationGizmoUtility.PositiveAxisCount];
    readonly Label m_ProjectionLabel;

    readonly float[] m_Depths = new float[OrientationGizmoUtility.AxisDirections.Length];
    readonly int[] m_Order = new int[OrientationGizmoUtility.AxisDirections.Length];
    readonly Vector2[] m_ScreenPositions = new Vector2[OrientationGizmoUtility.AxisDirections.Length];
    readonly Color[] m_AxisColors = new Color[OrientationGizmoUtility.PositiveAxisCount];

    Quaternion m_LastRotation;
    bool m_LastOrthographic;
    bool m_LastRotationLocked;

    int m_HoveredAxis = -1;
    bool m_HoveredCenter;

    public OrientationGizmoElement(OrientationGizmoTarget target, OrientationGizmoOverlay overlay)
    {
        m_Target = target;
        m_Overlay = overlay;
        name = "orientation-gizmo";

        // OrientationGizmoOverlay.PopulateRoot loads this same stylesheet onto the docked root, but
        // OverlayPopup's collapsed-overlay preview never calls PopulateRoot, so it needs its own copy too.
        styleSheets.Add(EditorGUIUtility.Load(OrientationGizmoOverlay.k_StylePath) as StyleSheet);

        this.AddManipulator(new ContextualMenuManipulator(BuildContextMenu));

        m_GizmoArea = new VisualElement { name = "orientation-gizmo-area" };
        m_GizmoArea.generateVisualContent += OnGenerateVisualContent;
        Add(m_GizmoArea);

        for (var i = 0; i < m_AxisLabels.Length; i++)
        {
            var label = new Label(GetAxisLabelText(i)) { name = $"orientation-gizmo-label-{i}" };
            label.AddToClassList(k_AxisLabelClass);
            label.pickingMode = PickingMode.Ignore;

            // Text measurement can resolve a layout pass later than the parent's size does, so a
            // label also re-centers off its own GeometryChangedEvent, not just gizmoArea's below.
            label.RegisterCallback<GeometryChangedEvent>(_ => UpdateAxisLabels(GizmoCenter, m_Target.rotation));

            m_AxisLabels[i] = label;
            m_GizmoArea.Add(label);
        }

        m_GizmoArea.RegisterCallback<GeometryChangedEvent>(_ => UpdateAxisLabels(GizmoCenter, m_Target.rotation));

        m_CameraModeToggle = new VisualElement { name = "orientation-gizmo-camera-mode-toggle" };
        m_CameraModeToggle.tooltip = L10n.Tr("Toggle perspective or orthographic projection.", null);
        Add(m_CameraModeToggle);

        m_CameraModeIcon = new VisualElement { name = "orientation-gizmo-camera-mode-icon" };
        m_CameraModeIcon.pickingMode = PickingMode.Ignore;
        m_CameraModeIcon.generateVisualContent += OnGenerateCameraModeIcon;
        m_CameraModeToggle.Add(m_CameraModeIcon);

        m_ProjectionLabel = new Label { name = "orientation-gizmo-projection-label" };
        m_CameraModeToggle.Add(m_ProjectionLabel);

        if (m_Target.supportsRotationLock)
        {
            m_LockIcon = new VisualElement { name = "orientation-gizmo-lock-icon" };
            m_LockIcon.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != (int)MouseButton.LeftMouse)
                    return;

                m_Target.SetRotationLocked(!m_Target.isRotationLocked);
                UpdateRotationLockedState();
                m_GizmoArea.MarkDirtyRepaint();
                evt.StopPropagation();
            });
            m_GizmoArea.Add(m_LockIcon);
        }

        m_GizmoArea.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != (int)MouseButton.LeftMouse && evt.button != (int)MouseButton.MiddleMouse)
                return;

            var niceAngleGesture = evt.shiftKey || evt.button == (int)MouseButton.MiddleMouse;
            if (TryHandlePointerDown(evt.localPosition, niceAngleGesture))
                evt.StopPropagation();
        });

        m_GizmoArea.RegisterCallback<PointerMoveEvent>(evt => UpdateHoverState(evt.localPosition));
        m_GizmoArea.RegisterCallback<PointerLeaveEvent>(_ => ClearHoverState());

        m_CameraModeToggle.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            TogglePerspective();
            evt.StopPropagation();
        });

        m_LastRotation = m_Target.rotation;
        m_LastOrthographic = m_Target.orthographic;
        m_LastRotationLocked = m_Target.isRotationLocked;
        UpdateCurrentViewLabel();
        UpdateRotationLockedState();

        RegisterCallback<AttachToPanelEvent>(_ => EditorApplication.update += OnEditorUpdate);
        RegisterCallback<DetachFromPanelEvent>(_ => EditorApplication.update -= OnEditorUpdate);
    }

    void BuildContextMenu(ContextualMenuPopulateEvent evt)
    {
        var menu = evt.menu;
        var alignedAxisIndex = OrientationGizmoUtility.AlignedAxisIndex(m_Target.rotation);

        menu.AppendAction(L10n.Tr("Free", null),
            _ => AlignToNiceAngle(false),
            _ => alignedAxisIndex < 0 ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

        for (var i = 0; i < k_DirectionMenuNames.Length; i++)
        {
            var axisIndex = i;
            menu.AppendAction(L10n.Tr(k_DirectionMenuNames[i], null),
                _ => AlignToAxis(axisIndex),
                _ => axisIndex == alignedAxisIndex
                    ? DropdownMenuAction.Status.Checked
                    : DropdownMenuAction.Status.Normal);
        }

        menu.AppendSeparator();

        menu.AppendAction(L10n.Tr("Perspective", null),
            _ => TogglePerspective(),
            _ => !m_Target.orthographic ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

        menu.AppendSeparator();

        menu.AppendAction(L10n.Tr("Show background", null),
            _ => m_Overlay.showBackground = !m_Overlay.showBackground,
            _ => m_Overlay.showBackground ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
    }

    static Color GetAxisColor(int colorIndex)
    {
        // Read fresh every call, not cached: these are PrefColors and can change at any time.
        switch (colorIndex)
        {
            case 0: return Handles.xAxisColor;
            case 1: return Handles.yAxisColor;
            case 2: return Handles.zAxisColor;
            default: throw new ArgumentOutOfRangeException(nameof(colorIndex));
        }
    }

    static string GetAxisLabelText(int positiveAxisIndex)
    {
        switch (positiveAxisIndex)
        {
            case 0: return "X";
            case 1: return "Y";
            case 2: return "Z";
            default: throw new ArgumentOutOfRangeException(nameof(positiveAxisIndex));
        }
    }

    void UpdateCurrentViewLabel()
    {
        m_ProjectionLabel.text = GetCurrentViewLabel(m_Target.rotation, m_Target.orthographic);
    }

    static string GetCurrentViewLabel(Quaternion cameraRotation, bool orthographic)
    {
        var alignedAxisIndex = OrientationGizmoUtility.AlignedAxisIndex(cameraRotation);
        if (alignedAxisIndex >= 0)
            return L10n.Tr(k_DirectionMenuNames[alignedAxisIndex], null);

        return orthographic ? L10n.Tr("Iso", null) : L10n.Tr("Persp", null);
    }

    void UpdateRotationLockedState()
    {
        var locked = m_Target.isRotationLocked;

        EnableInClassList(k_RotationLockedClass, locked);

        if (m_LockIcon == null)
            return;

        m_LockIcon.tooltip = locked
            ? L10n.Tr("Unlock view rotation.", null)
            : L10n.Tr("Lock view rotation in the current direction.", null);
    }

    void OnEditorUpdate()
    {
        var rotation = m_Target.rotation;
        var orthographicChanged = m_Target.orthographic != m_LastOrthographic;
        var rotationLockedChanged = m_Target.isRotationLocked != m_LastRotationLocked;
        // Called unconditionally, before the early return: the cache must update every tick.
        var axisColorsChanged = TryConsumeAxisColorChange();
        // m_LastRotation is left untouched when a delta is skipped, so sub-threshold deltas
        // accumulate instead of being dropped.
        var rotationChanged = Quaternion.Angle(rotation, m_LastRotation) >= k_MinRotationDeltaDegrees;

        if (!rotationChanged && !orthographicChanged && !rotationLockedChanged && !axisColorsChanged)
            return;

        m_LastRotation = rotation;
        m_LastOrthographic = m_Target.orthographic;
        m_LastRotationLocked = m_Target.isRotationLocked;

        UpdateCurrentViewLabel();

        if (rotationLockedChanged)
            UpdateRotationLockedState();

        if (orthographicChanged)
            m_CameraModeIcon.MarkDirtyRepaint();

        UpdateAxisLabels(GizmoCenter, m_LastRotation);

        m_GizmoArea.MarkDirtyRepaint();
    }

    internal Vector2 GizmoCenter
    {
        get
        {
            var rect = m_GizmoArea.contentRect;
            return new Vector2(rect.width * 0.5f, rect.height * 0.5f + k_GizmoVerticalOffset);
        }
    }

    void UpdateAxisLabels(Vector2 center, Quaternion cameraRotation)
    {
        var lockedAlpha = m_Target.isRotationLocked ? k_RotationLockedAlpha : 1f;

        for (var i = 0; i < m_AxisLabels.Length; i++)
        {
            var label = m_AxisLabels[i];
            var axis = OrientationGizmoUtility.AxisDirections[i];
            var position = OrientationGizmoUtility.AxisScreenPosition(axis, cameraRotation, center, k_AxisDistance);

            // translate, not left/top: the latter would dirty layout and re-run style resolution
            // on every camera change instead of only the transform.
            label.style.translate = new Translate(
                position.x - label.resolvedStyle.width * 0.5f,
                position.y - label.resolvedStyle.height * 0.5f);

            // Opacity, not the color's alpha: only dirties UIR's opacity phase, not a style/color pass.
            var depth = OrientationGizmoUtility.AxisDepth(axis, cameraRotation);
            label.style.opacity = OrientationGizmoUtility.BehindCenterFadeAlpha(depth) * lockedAlpha;
        }
    }

    internal Vector2[] ComputeAxisScreenPositions(Vector2 center)
    {
        var cameraRotation = m_Target.rotation;
        for (var i = 0; i < m_ScreenPositions.Length; i++)
            m_ScreenPositions[i] = OrientationGizmoUtility.AxisScreenPosition(
                OrientationGizmoUtility.AxisDirections[i], cameraRotation, center, k_AxisDistance);
        return m_ScreenPositions;
    }

    bool TryHitTest(Vector2 localPosition, out bool hitCenter, out int hitAxis)
    {
        var center = GizmoCenter;
        hitCenter = (localPosition - center).sqrMagnitude <= k_CenterHitRadius * k_CenterHitRadius;
        hitAxis = hitCenter
            ? -1
            : OrientationGizmoUtility.PickNearestAxis(localPosition, ComputeAxisScreenPositions(center), k_AxisHitRadius);
        return hitCenter || hitAxis >= 0;
    }

    internal bool TryHandlePointerDown(Vector2 localPosition, bool niceAngleGesture = false)
    {
        if (m_Target.isRotationLocked)
            return false;

        if (!TryHitTest(localPosition, out var hitCenter, out var hitAxis))
            return false;

        if (hitCenter)
        {
            if (niceAngleGesture)
                AlignToNiceAngle(true);
            else
                TogglePerspective();
            return true;
        }

        AlignToAxis(hitAxis);
        return true;
    }

    internal void TogglePerspective()
    {
        if (m_Target.isRotationLocked)
            return;

        m_Target.TogglePerspective();
    }

    void AlignToAxis(int axisIndex)
    {
        if (m_Target.isRotationLocked)
            return;

        var targetRotation = OrientationGizmoUtility.RotationForAxis(OrientationGizmoUtility.AxisDirections[axisIndex]);
        m_Target.AlignTo(targetRotation, m_Target.orthographic);
    }

    internal void HandleSwipeGesture(Vector2 delta)
    {
        AlignToAxis(OrientationGizmoUtility.AxisForSwipe(delta, m_Target.rotation));
    }

    void AlignToNiceAngle(bool forcePerspective)
    {
        if (m_Target.isRotationLocked)
            return;

        var ortho = forcePerspective ? false : m_Target.orthographic;
        m_Target.AlignTo(OrientationGizmoUtility.NiceAngleRotation(m_Target.rotation), ortho);
    }

    void UpdateHoverState(Vector2 localPosition)
    {
        if (m_Target.isRotationLocked)
        {
            ClearHoverState();
            return;
        }

        TryHitTest(localPosition, out var hoveredCenter, out var hoveredAxis);

        if (hoveredAxis == m_HoveredAxis && hoveredCenter == m_HoveredCenter)
            return;

        m_HoveredAxis = hoveredAxis;
        m_HoveredCenter = hoveredCenter;
        m_GizmoArea.MarkDirtyRepaint();
    }

    void ClearHoverState()
    {
        if (m_HoveredAxis == -1 && !m_HoveredCenter)
            return;

        m_HoveredAxis = -1;
        m_HoveredCenter = false;
        m_GizmoArea.MarkDirtyRepaint();
    }

    // generateVisualContent's mesh is cached, so a PrefColor change needs this to reach the screen
    // instead of waiting for some unrelated hover or camera move to dirty the element.
    bool TryConsumeAxisColorChange()
    {
        var changed = false;

        for (var i = 0; i < m_AxisColors.Length; i++)
        {
            var color = GetAxisColor(i);
            if (m_AxisColors[i] == color)
                continue;

            m_AxisColors[i] = color;
            changed = true;
        }

        return changed;
    }

    void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        var center = GizmoCenter;
        var cameraRotation = m_Target.rotation;
        var lockedAlpha = m_Target.isRotationLocked ? k_RotationLockedAlpha : 1f;

        for (var i = 0; i < m_AxisColors.Length; i++)
            m_AxisColors[i] = GetAxisColor(i);

        var screenPositions = ComputeAxisScreenPositions(center);
        for (var i = 0; i < OrientationGizmoUtility.AxisDirections.Length; i++)
        {
            m_Depths[i] = OrientationGizmoUtility.AxisDepth(OrientationGizmoUtility.AxisDirections[i], cameraRotation);
            m_Order[i] = i;
        }
        SortOrderByDepthDescending();

        var painter = mgc.painter2D;

        for (var i = 0; i < OrientationGizmoUtility.AxisDirections.Length; i++)
        {
            var positive = i < OrientationGizmoUtility.PositiveAxisCount;
            var lineColor = m_AxisColors[i % OrientationGizmoUtility.PositiveAxisCount];
            lineColor.a = positive ? 0.7f : 0.5f;
            lineColor.a *= OrientationGizmoUtility.BehindCenterFadeAlpha(m_Depths[i]) * lockedAlpha;
            painter.strokeColor = lineColor;
            painter.lineWidth = 1.5f;

            var handleRadius = positive ? k_PositiveHandleRadius : k_NegativeHandleRadius;
            var toMarker = screenPositions[i] - center;
            var lineEnd = toMarker.sqrMagnitude > 0.0001f
                ? screenPositions[i] - toMarker.normalized * handleRadius
                : screenPositions[i];

            painter.BeginPath();
            painter.MoveTo(center);
            painter.LineTo(lineEnd);
            painter.Stroke();
        }

        var centerDrawn = false;
        foreach (var i in m_Order)
        {
            if (!centerDrawn && m_Depths[i] <= 0f)
            {
                DrawCenterMarker(painter, center, m_Target.orthographic, lockedAlpha, m_HoveredCenter);
                centerDrawn = true;
            }

            var positive = i < OrientationGizmoUtility.PositiveAxisCount;
            var colorIndex = i % OrientationGizmoUtility.PositiveAxisCount;
            var hovered = i == m_HoveredAxis;
            var color = hovered ? Handles.selectedColor : m_AxisColors[colorIndex];
            var aligned = OrientationGizmoUtility.IsAxisAligned(OrientationGizmoUtility.AxisDirections[i], cameraRotation);
            var fade = OrientationGizmoUtility.BehindCenterFadeAlpha(m_Depths[i]) * lockedAlpha;

            painter.BeginPath();
            if (positive)
            {
                var fill = color;
                fill.a *= fade * (hovered ? k_HoverAlpha : 1f);
                painter.fillColor = fill;
                painter.Arc(screenPositions[i], k_PositiveHandleRadius, 0f, 360f);
                painter.Fill();

                if (aligned)
                {
                    var ring = Color.white;
                    ring.a *= fade;
                    painter.BeginPath();
                    painter.strokeColor = ring;
                    painter.lineWidth = 1.5f;
                    painter.Arc(screenPositions[i], k_PositiveHandleRadius, 0f, 360f);
                    painter.Stroke();
                }
            }
            else
            {
                var hollow = color;
                hollow.a = (hovered ? 1f : 0.85f) * fade;
                painter.strokeColor = hollow;
                painter.lineWidth = 2f;
                painter.Arc(screenPositions[i], k_NegativeHandleRadius, 0f, 360f);
                painter.Stroke();
            }
        }

        if (!centerDrawn)
            DrawCenterMarker(painter, center, m_Target.orthographic, lockedAlpha, m_HoveredCenter);
    }

    // Hand-rolled: Array.Sort(T[], Comparison<T>) would allocate a comparer wrapper on every paint.
    void SortOrderByDepthDescending()
    {
        for (var i = 1; i < m_Order.Length; i++)
        {
            var index = m_Order[i];
            var depth = m_Depths[index];

            var j = i - 1;
            while (j >= 0 && m_Depths[m_Order[j]] < depth)
            {
                m_Order[j + 1] = m_Order[j];
                j--;
            }

            m_Order[j + 1] = index;
        }
    }

    static void DrawCenterMarker(Painter2D painter, Vector2 center, bool orthographic, float alpha, bool hovered)
    {
        var fill = hovered ? Handles.selectedColor : Handles.centerColor;
        fill.a = alpha * (hovered ? k_HoverAlpha : 1f);
        painter.fillColor = fill;
        painter.BeginPath();

        if (orthographic)
        {
            painter.MoveTo(center + new Vector2(-k_CenterSquareHalfSize, -k_CenterSquareHalfSize));
            painter.LineTo(center + new Vector2(k_CenterSquareHalfSize, -k_CenterSquareHalfSize));
            painter.LineTo(center + new Vector2(k_CenterSquareHalfSize, k_CenterSquareHalfSize));
            painter.LineTo(center + new Vector2(-k_CenterSquareHalfSize, k_CenterSquareHalfSize));
            painter.ClosePath();
        }
        else
        {
            painter.Arc(center, k_CenterCircleRadius, 0f, 360f);
        }

        painter.Fill();
    }

    void OnGenerateCameraModeIcon(MeshGenerationContext mgc)
    {
        var rect = m_CameraModeIcon.contentRect;
        var pos = new Vector2(0f, rect.height * 0.5f);
        var right = new Vector2(rect.width, 0f);
        var up = new Vector2(0f, -rect.height * 0.5f);
        var persp = m_Target.orthographic ? 0f : 1f;

        var lineColor = m_ProjectionLabel.resolvedStyle.color;
        lineColor.a *= 0.6f;

        var painter = mgc.painter2D;
        painter.strokeColor = lineColor;
        painter.lineWidth = 1f;

        painter.BeginPath();
        painter.MoveTo(pos + up * (1f - persp));
        painter.LineTo(pos + right + up * (1f + persp * 0.5f));
        painter.Stroke();

        painter.BeginPath();
        painter.MoveTo(pos);
        painter.LineTo(pos + right);
        painter.Stroke();

        painter.BeginPath();
        painter.MoveTo(pos - up * (1f - persp));
        painter.LineTo(pos + right - up * (1f + persp * 0.5f));
        painter.Stroke();
    }
}
