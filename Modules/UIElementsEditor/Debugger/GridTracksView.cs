// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Text;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Debugger
{
    // Refreshed on every debugger update, so the text is only rebuilt when the resolved tracks change.
    internal class GridTracksView : VisualElement
    {
        readonly TextField m_Columns;
        readonly TextField m_Rows;
        readonly StringBuilder m_Builder = new();
        float[] m_ShownColumns = Array.Empty<float>();
        float[] m_ShownRows = Array.Empty<float>();
        bool m_Shown = true;

        internal TextField columnsField => m_Columns;
        internal TextField rowsField => m_Rows;

        public GridTracksView()
        {
            Add(m_Columns = new TextField(L10n.Tr("Grid Columns", null)) { isReadOnly = true, value = "none" });
            Add(m_Rows = new TextField(L10n.Tr("Grid Rows", null)) { isReadOnly = true, value = "none" });
            SetShown(false);
        }

        public void Update(VisualElement element)
        {
            var isGrid = element != null && element.resolvedStyle.display == DisplayStyle.Grid;
            SetShown(isGrid);
            if (!isGrid)
                return;

            ref var node = ref element.layoutNode;
            Refresh(node.GridResolvedColumnSizes, ref m_ShownColumns, m_Columns);
            Refresh(node.GridResolvedRowSizes, ref m_ShownRows, m_Rows);
        }

        void SetShown(bool shown)
        {
            if (m_Shown == shown)
                return;

            m_Shown = shown;
            style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void Refresh(ReadOnlySpan<float> tracks, ref float[] shownTracks, TextField field)
        {
            if (tracks.SequenceEqual(shownTracks))
                return;

            shownTracks = tracks.ToArray();
            m_Builder.Clear();
            for (var i = 0; i < tracks.Length; ++i)
            {
                if (i > 0)
                    m_Builder.Append(' ');
                m_Builder.Append(tracks[i].ToString("0.##", CultureInfo.InvariantCulture)).Append("px");
            }

            field.value = tracks.Length > 0 ? m_Builder.ToString() : "none";
        }
    }
}
