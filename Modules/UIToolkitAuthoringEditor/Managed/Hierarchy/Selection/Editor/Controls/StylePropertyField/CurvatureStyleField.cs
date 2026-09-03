// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// A field for editing <see cref="Curvature"/> values as X and Y bend angles, with a button
    /// opening a 3D preview popup of the bend.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    [UxmlElement]
    internal partial class CurvatureStyleField : BaseField<Curvature>
    {
        static readonly string s_FieldClassName = "unity-curvature-style-field";
        static readonly string s_UssPath = "UIToolkitAuthoring/Inspector/Controls/CurvatureStyleField.uss";
        public static readonly string s_AngleXFieldName = "x-angle-field";
        public static readonly string s_AngleYFieldName = "y-angle-field";

        AngleField m_AngleXField;
        AngleField m_AngleYField;

        /// <summary>
        /// Returns the <see cref="AngleField"/> for the X bend angle.
        /// </summary>
        public AngleField xField => m_AngleXField;

        /// <summary>
        /// Returns the <see cref="AngleField"/> for the Y bend angle.
        /// </summary>
        public AngleField yField => m_AngleYField;

        public CurvatureStyleField() : this(null) { }

        public CurvatureStyleField(string label) : base(label, null)
        {
            AddToClassList(s_FieldClassName);

            styleSheets.Add(EditorGUIUtility.Load(s_UssPath) as StyleSheet);

            m_AngleXField = new AngleField("X") { name = s_AngleXFieldName, tooltip = "-unity-curvature", showUnitAsDropdown = true };
            m_AngleYField = new AngleField("Y") { name = s_AngleYFieldName, tooltip = "-unity-curvature", showUnitAsDropdown = true };

            var angles = new VisualElement();
            angles.AddToClassList("unity-curvature-style-field__angles");
            angles.Add(m_AngleXField);
            angles.Add(m_AngleYField);
            visualInput.Add(angles);

            m_AngleXField.RegisterValueChangedCallback(e =>
            {
                UpdateCurvatureField();
                e.StopPropagation();
            });

            m_AngleYField.RegisterValueChangedCallback(e =>
            {
                UpdateCurvatureField();
                e.StopPropagation();
            });

            var xDragger = new FieldMouseDragger<Angle>(m_AngleXField);
            xDragger.SetDragZone(m_AngleXField.labelElement);
            var yDragger = new FieldMouseDragger<Angle>(m_AngleYField);
            yDragger.SetDragZone(m_AngleYField.labelElement);

            // A small affordance that opens an auto-closing 3D preview popup of the bend (color-picker
            // style). Editing curvature in the popup writes back through this field's value.
            var previewButton = new Button(OpenPreview) { tooltip = "Preview the -unity-curvature bend" };
            previewButton.AddToClassList("unity-curvature-style-field__preview-button");
            previewButton.style.width = 38;
            previewButton.style.flexShrink = 0;
            previewButton.style.paddingLeft = 2;
            previewButton.style.paddingRight = 2;
            previewButton.style.alignItems = Align.Center;
            previewButton.style.justifyContent = Justify.Center;

            var icon = new VisualElement { name = "curvature-preview-icon", pickingMode = PickingMode.Ignore };
            icon.style.width = 32;
            icon.style.height = 16;
            icon.generateVisualContent += GenerateCurveIcon;
            previewButton.Add(icon);
            visualInput.Add(previewButton);

            value = Curvature.None();
        }

        // Opens the preview popup anchored to this field; edits flow back through value.
        void OpenPreview()
        {
            UnityEditor.PopupWindow.Show(worldBound, new CurvaturePreviewContent(value, c => value = c));
        }

        // A tiny curved-mesh glyph for the preview button — a cylinder grid (columns compress
        // toward the edges, rows arc from the tilt), i.e. a miniature of the popup surface, so it reads as a curved
        // surface rather than stacked waves. Vector-drawn (Painter2D) so it stays crisp and theme-coloured.
        static void GenerateCurveIcon(MeshGenerationContext mgc)
        {
            Rect rc = mgc.visualElement.contentRect;
            if (rc.width < 4f || rc.height < 4f)
                return;

            const float pad = 1.5f;
            var box = new Rect(rc.x + pad, rc.y + pad, rc.width - 2f * pad, rc.height - 2f * pad);
            const float theta = 1.15f; // horizontal bend (around the vertical axis)
            const float pitch = 0.42f; // viewed slightly from above so the rows arc

            Vector2 Proj(float u, float v)
            {
                float ax = Mathf.Sin((u - 0.5f) * theta) / theta;   // x compresses toward the edges
                float sag = 1f - Mathf.Cos((u - 0.5f) * theta);     // edges recede in depth
                float y = (v - 0.5f) * Mathf.Cos(pitch) - sag * Mathf.Sin(pitch);
                return new Vector2(ax, y);
            }

            // Fit the projected grid into the icon box.
            float minx = float.MaxValue, maxx = float.MinValue, miny = float.MaxValue, maxy = float.MinValue;
            for (int i = 0; i <= 4; i++)
            for (int j = 0; j <= 4; j++)
            {
                Vector2 q = Proj(i / 4f, j / 4f);
                if (q.x < minx) minx = q.x; if (q.x > maxx) maxx = q.x;
                if (q.y < miny) miny = q.y; if (q.y > maxy) maxy = q.y;
            }
            // Fill the icon box on both axes so it isn't letterboxed thin at this size.
            float scx = box.width / Mathf.Max(maxx - minx, 1e-3f);
            float scy = box.height / Mathf.Max(maxy - miny, 1e-3f);
            Vector2 mid = new Vector2((minx + maxx) * 0.5f, (miny + maxy) * 0.5f), c = box.center;
            Vector2 Map(float u, float v) { Vector2 q = Proj(u, v); return new Vector2(c.x + (q.x - mid.x) * scx, c.y + (q.y - mid.y) * scy); }

            var p = mgc.painter2D;
            p.lineWidth = 1.3f;
            p.lineJoin = LineJoin.Round;
            p.lineCap = LineCap.Round;
            Color col = mgc.visualElement.resolvedStyle.color;
            p.strokeColor = col.a < 0.05f ? new Color(0.82f, 0.82f, 0.82f) : col;

            const int n = 3, seg = 8;
            for (int i = 0; i <= n; i++) // rows
            {
                float v = i / (float)n;
                p.BeginPath();
                p.MoveTo(Map(0f, v));
                for (int k = 1; k <= seg; k++) p.LineTo(Map(k / (float)seg, v));
                p.Stroke();
            }
            for (int j = 0; j <= n; j++) // columns
            {
                float u = j / (float)n;
                p.BeginPath();
                p.MoveTo(Map(u, 0f));
                for (int k = 1; k <= seg; k++) p.LineTo(Map(u, k / (float)seg));
                p.Stroke();
            }
        }

        public override void SetValueWithoutNotify(Curvature newValue)
        {
            base.SetValueWithoutNotify(newValue);
            m_AngleXField.SetValueWithoutNotify(value.x);
            m_AngleYField.SetValueWithoutNotify(value.y);
        }

        void UpdateCurvatureField()
        {
            var x = m_AngleXField.value;
            var y = m_AngleYField.value;
            // Zeroed angles author `none` rather than an explicit 0deg pair - the explicit pair is a flat
            // barrier that opts the subtree out of ancestor curvature (see UIRCurvatureGeometry.CountChain).
            value = x.value == 0f && y.value == 0f ? Curvature.None() : new Curvature(x, y);
        }
    }
}
