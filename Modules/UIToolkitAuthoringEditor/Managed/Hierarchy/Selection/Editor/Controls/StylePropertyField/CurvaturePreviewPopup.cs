// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.UIElements.Cursor;

namespace Unity.UIToolkit.Editor
{
    // Curved UI — auto-closing popup (color-picker style) that previews and edits -unity-curvature.
    // Opened from CurvatureStyleField via PopupWindow.Show(worldBound, ...); it closes when the user clicks
    // elsewhere. Renders the bend as a tilted amber surface (translucent fill + mesh grid): drag to orbit,
    // scroll to zoom 100%->50%. Editing θx/θy writes back through the onChanged callback (angle units preserved).
    //
    // The bend math replicates the runtime UIRCurvatureMeshModifier (BendInSpace + CombineSag) — keep the two in
    // sync. The surface fill is a single closed Painter2D path (boundary trace), so it has no interior seams.
    partial class CurvaturePreviewContent : PopupWindowContent
    {
        readonly Action<Curvature> m_OnChanged;
        readonly AngleUnit m_UnitX, m_UnitY;
        float m_ThetaXDeg, m_ThetaYDeg;

        // View state persists across opens for the session.
        const float k_DefYaw = 26f, k_DefPitch = 20f, k_DefZoom = 1f;
        [NoAutoStaticsCleanup] // Session view state (primitives, no user-code references); intentionally persists across code reload.
        static float s_YawDeg = k_DefYaw, s_PitchDeg = k_DefPitch, s_Depth = 1f, s_Zoom = k_DefZoom;
        [NoAutoStaticsCleanup] // Session view state (primitive); intentionally persists across code reload.
        static int s_Interior = 3;

        bool m_Dragging;
        Vector2 m_DragStart;
        float m_DragYaw, m_DragPitch;
        int m_UndoGroup = -1;

        VisualElement m_Preview;
        Label m_ShapeLabel, m_HintLabel;
        Slider m_SliderX, m_SliderY, m_SliderYaw, m_SliderPitch;

        static readonly string[] s_PresetNames = { "Flat", "Cyl X", "Cyl Y", "Dome", "Saddle", "Bowl" };
        static readonly Vector2[] s_Presets =
            { new Vector2(0, 0), new Vector2(45, 0), new Vector2(0, 45), new Vector2(45, 45), new Vector2(45, -45), new Vector2(-45, -45) };

        static readonly Color k_Bg = new Color(0.14f, 0.14f, 0.14f);
        static readonly Color k_Amber = new Color(1f, 0.72f, 0.12f, 0.95f);
        static readonly Color k_AmberGrid = new Color(1f, 0.72f, 0.12f, 0.60f);
        static readonly Color k_Fill = new Color(1f, 0.72f, 0.12f, 0.16f);
        const float k_EdgeWidth = 3f, k_GridWidth = 2f;
        const int k_Seg = 22;

        public CurvaturePreviewContent(Curvature curvature, Action<Curvature> onChanged)
        {
            m_OnChanged = onChanged;
            m_UnitX = curvature.x.unit;
            m_UnitY = curvature.y.unit;
            m_ThetaXDeg = ToDegrees(curvature.x);
            m_ThetaYDeg = ToDegrees(curvature.y);
        }

        public override VisualElement CreateGUI()
        {
            var root = new VisualElement
            {
                style =
                {
                    width = 360, height = 440,
                    paddingLeft = 4, paddingRight = 4, paddingTop = 4, paddingBottom = 4
                }
            };

            var header = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            header.Add(new Label("Shape") { style = { width = 42 } });
            m_ShapeLabel = new Label(CurvatureLabel()) { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            header.Add(m_ShapeLabel);
            root.Add(header);

            for (int row = 0; row < 2; row++)
            {
                var presetRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                for (int col = 0; col < 3; col++)
                {
                    int preset = row * 3 + col;
                    presetRow.Add(new Button(() => ApplyPreset(preset))
                    {
                        text = s_PresetNames[preset],
                        style = { flexGrow = 1, flexBasis = 0 }
                    });
                }
                root.Add(presetRow);
            }

            m_SliderX = MakeSlider("X", -90f, 90f, m_ThetaXDeg, evt =>
            {
                m_ThetaXDeg = evt.newValue;
                OnCurvatureEdited();
            });
            m_SliderY = MakeSlider("Y", -90f, 90f, m_ThetaYDeg, evt =>
            {
                m_ThetaYDeg = evt.newValue;
                OnCurvatureEdited();
            });
            RegisterDragUndoBatching(m_SliderX);
            RegisterDragUndoBatching(m_SliderY);
            root.Add(Row(m_SliderX, m_SliderY));

            m_SliderYaw = MakeSlider("Yaw", -60f, 60f, s_YawDeg, evt => { s_YawDeg = Round2(evt.newValue); m_Preview.MarkDirtyRepaint(); });
            m_SliderPitch = MakeSlider("Pitch", -60f, 60f, s_PitchDeg, evt => { s_PitchDeg = Round2(evt.newValue); m_Preview.MarkDirtyRepaint(); });
            root.Add(Row(m_SliderYaw, m_SliderPitch));

            var depth = MakeSlider("Depth", 0f, 3f, s_Depth, evt => { s_Depth = Round2(evt.newValue); m_Preview.MarkDirtyRepaint(); });
            var grid = new SliderInt("Grid", 0, 6) { value = s_Interior, showInputField = true, style = { flexGrow = 1, flexBasis = 0 } };
            grid.labelElement.style.minWidth = 40;
            grid.RegisterValueChangedCallback(evt => { s_Interior = evt.newValue; m_Preview.MarkDirtyRepaint(); });
            root.Add(Row(depth, grid));

            m_HintLabel = new Label
            {
                style = { alignSelf = Align.Center, fontSize = 10, color = Color.grey }
            };
            UpdateHint();
            root.Add(m_HintLabel);

            m_Preview = new VisualElement
            {
                style =
                {
                    flexGrow = 1, marginTop = 4,
                    backgroundColor = k_Bg,
                    overflow = Overflow.Hidden,
                    cursor = new Cursor { defaultCursorId = (int)MouseCursor.Pan }
                }
            };
            m_Preview.generateVisualContent += DrawPreview;
            m_Preview.RegisterCallback<PointerDownEvent>(OnPointerDown);
            m_Preview.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            m_Preview.RegisterCallback<PointerUpEvent>(OnPointerUp);
            m_Preview.RegisterCallback<WheelEvent>(OnWheel);
            root.Add(m_Preview);

            var reset = new Button(ResetView)
            {
                tooltip = "Reset orientation",
                style = { position = Position.Absolute, right = 6, bottom = 6, width = 26, height = 22 }
            };
            reset.Add(new Image { image = EditorGUIUtility.IconContent("Refresh").image, style = { flexGrow = 1 } });
            m_Preview.Add(reset);

            return root;
        }

        // Each drag tick commits through onChanged, which registers an undo step per change; keeping the whole
        // drag in one undo group collapses them to a single undoable edit without losing the live preview.
        void RegisterDragUndoBatching(Slider slider)
        {
            slider.RegisterCallback<PointerCaptureEvent>(_ =>
            {
                if (m_UndoGroup < 0)
                    m_UndoGroup = Undo.GetCurrentGroup();
            });
            slider.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                if (m_UndoGroup < 0)
                    return;
                Undo.CollapseUndoOperations(m_UndoGroup);
                m_UndoGroup = -1;
            });
        }

        static Slider MakeSlider(string label, float lo, float hi, float value, EventCallback<ChangeEvent<float>> onChanged)
        {
            var slider = new Slider(label, lo, hi)
            {
                value = value,
                showInputField = true,
                style = { flexGrow = 1, flexBasis = 0 }
            };
            slider.labelElement.style.minWidth = 40;
            slider.RegisterValueChangedCallback(onChanged);
            return slider;
        }

        static VisualElement Row(VisualElement a, VisualElement b)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            row.Add(a);
            row.Add(b);
            return row;
        }

        void ApplyPreset(int preset)
        {
            m_ThetaXDeg = s_Presets[preset].x;
            m_ThetaYDeg = s_Presets[preset].y;
            m_SliderX.SetValueWithoutNotify(m_ThetaXDeg);
            m_SliderY.SetValueWithoutNotify(m_ThetaYDeg);
            OnCurvatureEdited();
        }

        void OnCurvatureEdited()
        {
            m_ShapeLabel.text = CurvatureLabel();
            Commit();
            m_Preview.MarkDirtyRepaint();
        }

        void ResetView()
        {
            s_YawDeg = k_DefYaw;
            s_PitchDeg = k_DefPitch;
            s_Zoom = k_DefZoom;
            m_SliderYaw.SetValueWithoutNotify(s_YawDeg);
            m_SliderPitch.SetValueWithoutNotify(s_PitchDeg);
            UpdateHint();
            m_Preview.MarkDirtyRepaint();
        }

        void UpdateHint() => m_HintLabel.text = $"drag to orbit  ·  scroll to zoom ({Mathf.RoundToInt(s_Zoom * 100)}%)";

        void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.target != m_Preview)
                return; // the reset button handles its own input
            m_Dragging = true;
            m_DragStart = (Vector2)evt.localPosition;
            m_DragYaw = s_YawDeg;
            m_DragPitch = s_PitchDeg;
            m_Preview.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (!m_Dragging || !m_Preview.HasPointerCapture(evt.pointerId))
                return;
            Vector2 delta = (Vector2)evt.localPosition - m_DragStart;
            s_YawDeg = Round2(Mathf.Clamp(m_DragYaw - delta.x * 0.3f, -60f, 60f));
            s_PitchDeg = Round2(Mathf.Clamp(m_DragPitch + delta.y * 0.3f, -60f, 60f));
            m_SliderYaw.SetValueWithoutNotify(s_YawDeg);
            m_SliderPitch.SetValueWithoutNotify(s_PitchDeg);
            m_Preview.MarkDirtyRepaint();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (!m_Dragging)
                return;
            m_Dragging = false;
            m_Preview.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }

        void OnWheel(WheelEvent evt)
        {
            s_Zoom = Round2(Mathf.Clamp(s_Zoom - evt.delta.y * 0.03f, 0.5f, 1f)); // scroll down = zoom out to 50%
            UpdateHint();
            m_Preview.MarkDirtyRepaint();
            evt.StopPropagation();
        }

        // Zeroed angles commit `none` rather than an explicit 0deg pair (a flat barrier in the composition).
        void Commit() => m_OnChanged?.Invoke(m_ThetaXDeg == 0f && m_ThetaYDeg == 0f
            ? Curvature.None()
            : new Curvature(FromDegrees(m_ThetaXDeg, m_UnitX), FromDegrees(m_ThetaYDeg, m_UnitY)));

        static float Round2(float v) => Mathf.Round(v * 100f) / 100f;

        string CurvatureLabel()
        {
            bool fx = Mathf.Abs(m_ThetaXDeg) < 0.5f, fy = Mathf.Abs(m_ThetaYDeg) < 0.5f;
            if (fx && fy) return "Flat";
            if (fx || fy) return "Cylinder";
            bool sameSign = (m_ThetaXDeg > 0) == (m_ThetaYDeg > 0);
            if (!sameSign) return "Saddle (anticlastic)";
            return m_ThetaXDeg > 0 ? "Dome (convex)" : "Bowl (concave)";
        }

        void DrawPreview(MeshGenerationContext mgc)
        {
            Rect area = m_Preview.contentRect;
            float margin = 26f;
            Rect box = new Rect(margin, margin, area.width - 2 * margin, area.height - 2 * margin);
            if (box.width < 10f || box.height < 10f)
                return;

            // -unity-curvature names the rotation axis (like transform rotation): θx bends the vertical extent
            // (around X), θy the horizontal (around Y). ProjectBent's first angle bends the horizontal extent,
            // so θy feeds it and θx the vertical — matching the runtime BendInSpace mapping.
            float thetaH = m_ThetaYDeg * Mathf.Deg2Rad, thetaV = m_ThetaXDeg * Mathf.Deg2Rad;
            float yaw = -s_YawDeg * Mathf.Deg2Rad, pitch = -s_PitchDeg * Mathf.Deg2Rad; // inverted

            Vector2 S(float u, float v) => ProjectBent(u, v, thetaH, thetaV, box.width, box.height, s_Depth, yaw, pitch);

            // Fixed scale — no fit-to-viewport. 100% = native (box-pixel) scale, scroll zooms out to 50%.
            float scale = s_Zoom;
            Vector2 center = S(0.5f, 0.5f);
            Vector2 c = box.center;
            Vector2 Map(float u, float v) { Vector2 p = S(u, v); return new Vector2(c.x + (p.x - center.x) * scale, c.y + (p.y - center.y) * scale); }

            var painter = mgc.painter2D;
            painter.lineJoin = LineJoin.Round;

            // 1) seamless translucent surface — one closed boundary path, so no interior seams by construction.
            painter.fillColor = k_Fill;
            painter.BeginPath();
            painter.MoveTo(Map(0f, 0f));
            for (int s = 1; s <= k_Seg; s++) painter.LineTo(Map(s / (float)k_Seg, 0f));
            for (int s = 1; s <= k_Seg; s++) painter.LineTo(Map(1f, s / (float)k_Seg));
            for (int s = 1; s <= k_Seg; s++) painter.LineTo(Map(1f - s / (float)k_Seg, 1f));
            for (int s = 1; s <= k_Seg; s++) painter.LineTo(Map(0f, 1f - s / (float)k_Seg));
            painter.ClosePath();
            painter.Fill();

            // 2) interior mesh grid + outline
            int nx = s_Interior + 2, ny = s_Interior + 2;
            for (int gx = 0; gx < nx; gx++)
            {
                float u = gx / (float)(nx - 1);
                bool edge = gx == 0 || gx == nx - 1;
                StrokeGridLine(painter, Map, u, true, edge ? k_Amber : k_AmberGrid, edge ? k_EdgeWidth : k_GridWidth);
            }
            for (int gy = 0; gy < ny; gy++)
            {
                float v = gy / (float)(ny - 1);
                bool edge = gy == 0 || gy == ny - 1;
                StrokeGridLine(painter, Map, v, false, edge ? k_Amber : k_AmberGrid, edge ? k_EdgeWidth : k_GridWidth);
            }
        }

        static void StrokeGridLine(Painter2D painter, Func<float, float, Vector2> map, float t, bool vertical, Color color, float width)
        {
            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.BeginPath();
            painter.MoveTo(vertical ? map(t, 0f) : map(0f, t));
            for (int seg = 1; seg <= k_Seg; seg++)
            {
                float a = seg / (float)k_Seg;
                painter.LineTo(vertical ? map(t, a) : map(a, t));
            }
            painter.Stroke();
        }

        // Runtime bend (BendInSpace: arc position + per-axis sag via CombineSag) + fixed viewing tilt, orthographic.
        static Vector2 ProjectBent(float u, float v, float thetaH, float thetaV, float w, float h, float depth, float yaw, float pitch)
        {
            float x = u * w, y = v * h, sagH = 0f, sagV = 0f;
            if (Mathf.Abs(thetaH) > 1e-4f) { float r = w / thetaH, a = (u - 0.5f) * thetaH; x = w * 0.5f + r * Mathf.Sin(a); sagH = r * (1f - Mathf.Cos(a)); }
            if (Mathf.Abs(thetaV) > 1e-4f) { float r = h / thetaV, a = (v - 0.5f) * thetaV; y = h * 0.5f + r * Mathf.Sin(a); sagV = r * (1f - Mathf.Cos(a)); }
            float z = -CombineSag(sagH, sagV) * depth;

            float cx = x - w * 0.5f, cy = y - h * 0.5f;
            float x1 = cx * Mathf.Cos(yaw) + z * Mathf.Sin(yaw);
            float z1 = -cx * Mathf.Sin(yaw) + z * Mathf.Cos(yaw);
            float y1 = cy * Mathf.Cos(pitch) - z1 * Mathf.Sin(pitch);
            return new Vector2(x1, y1);
        }

        static float CombineSag(float sagX, float sagY)
        {
            return sagX * sagY >= 0f
                ? Mathf.Sign(sagX + sagY) * Mathf.Sqrt(sagX * sagX + sagY * sagY)
                : sagX + sagY;
        }

        static float ToDegrees(Angle a)
        {
            switch (a.unit)
            {
                case AngleUnit.Radian: return a.value * Mathf.Rad2Deg;
                case AngleUnit.Gradian: return a.value * 0.9f;   // 400 grad == 360 deg
                case AngleUnit.Turn: return a.value * 360f;
                default: return a.value;                          // Degree
            }
        }

        static Angle FromDegrees(float deg, AngleUnit unit)
        {
            switch (unit)
            {
                case AngleUnit.Radian: return new Angle(deg * Mathf.Deg2Rad, AngleUnit.Radian);
                case AngleUnit.Gradian: return new Angle(deg / 0.9f, AngleUnit.Gradian);
                case AngleUnit.Turn: return new Angle(deg / 360f, AngleUnit.Turn);
                default: return new Angle(deg, AngleUnit.Degree);
            }
        }
    }
}
