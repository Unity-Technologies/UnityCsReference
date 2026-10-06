// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    // CSS Grid. A proportional schematic preview of a grid's explicit tracks: one
    // cell per column x row, sized by each track's weight. Read-only; the owner feeds it SetColumns/SetRows.
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal class GridTemplatePreview : VisualElement
    {
        List<GridTrackSize> m_Columns = new();
        List<GridTrackSize> m_Rows = new();

        public GridTemplatePreview()
        {
            AddToClassList(ussClassName);
            style.flexDirection = FlexDirection.Column;
            style.height = 120;

            var border = new Color(0f, 0f, 0f, 0.35f);
            style.borderLeftWidth = 1;
            style.borderRightWidth = 1;
            style.borderTopWidth = 1;
            style.borderBottomWidth = 1;
            style.borderLeftColor = border;
            style.borderRightColor = border;
            style.borderTopColor = border;
            style.borderBottomColor = border;

            Rebuild();
        }

        public void SetColumns(List<GridTrackSize> tracks)
        {
            m_Columns = tracks ?? new List<GridTrackSize>();
            Rebuild();
        }

        public void SetRows(List<GridTrackSize> tracks)
        {
            m_Rows = tracks ?? new List<GridTrackSize>();
            Rebuild();
        }

        void Rebuild()
        {
            Clear();

            if (m_Columns.Count == 0 && m_Rows.Count == 0)
            {
                var empty = new Label(L10n.Tr("No columns or rows defined", null));
                empty.AddToClassList(emptyLabelUssClassName);
                Add(empty);
                return;
            }

            var cols = ExpandForPreview(m_Columns);
            var rows = ExpandForPreview(m_Rows);

            var cellColor = new Color(0.5f, 0.5f, 0.5f, 0.18f);
            var lineColor = new Color(0f, 0f, 0f, 0.25f);

            foreach (var r in rows)
            {
                var rowEl = new VisualElement();
                rowEl.style.flexDirection = FlexDirection.Row;
                rowEl.style.flexGrow = Weight(r);
                rowEl.style.flexBasis = 0;

                foreach (var c in cols)
                {
                    var cell = new VisualElement();
                    cell.style.flexGrow = Weight(c);
                    cell.style.flexBasis = 0;
                    cell.style.marginLeft = 1;
                    cell.style.marginRight = 1;
                    cell.style.marginTop = 1;
                    cell.style.marginBottom = 1;
                    cell.style.backgroundColor = cellColor;
                    cell.style.borderLeftWidth = 1;
                    cell.style.borderRightWidth = 1;
                    cell.style.borderTopWidth = 1;
                    cell.style.borderBottomWidth = 1;
                    cell.style.borderLeftColor = lineColor;
                    cell.style.borderRightColor = lineColor;
                    cell.style.borderTopColor = lineColor;
                    cell.style.borderBottomColor = lineColor;
                    rowEl.Add(cell);
                }

                Add(rowEl);
            }
        }

        // Auto-fill / auto-fit have no fixed count until layout; show a representative few in the preview.
        static List<GridTrackSize> ExpandForPreview(List<GridTrackSize> tracks)
        {
            var res = new List<GridTrackSize>();
            if (tracks != null)
            {
                foreach (var t in tracks)
                {
                    if (t.isAutoFill || t.isAutoFit)
                        for (int k = 0; k < 3; ++k) res.Add(t);
                    else
                        res.Add(t);
                }
            }
            if (res.Count == 0)
                res.Add(GridTrackSize.Fraction(1));
            return res;
        }

        static float Weight(GridTrackSize t)
        {
            switch (t.maxUnit)
            {
                case GridTrackSizeUnit.Fraction: return Mathf.Max(0.1f, t.maxValue);
                case GridTrackSizeUnit.Percent: return Mathf.Max(0.1f, t.maxValue / 100f * 3f);
                case GridTrackSizeUnit.Pixel: return Mathf.Max(0.1f, t.maxValue / 80f);
                default: return 1f; // auto / min-content / max-content
            }
        }

        public static readonly string ussClassName = "grid-template-preview";
        public static readonly string emptyLabelUssClassName = ussClassName + "__empty-label";
    }
}
