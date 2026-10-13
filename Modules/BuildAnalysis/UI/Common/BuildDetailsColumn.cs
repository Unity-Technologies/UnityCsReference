// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    // Gives every visible row's label the widest one's width, so the values line up.
    // Must be inside a BuildDetailsGrid, which supplies the stylesheet.
    // Addressables' SummaryTabEmbedded.uxml uses this tag and its attributes.
    // To rename one, keep the old name with [MovedFrom] or [UxmlAttribute("new-name", "old-name")].
    [UxmlElement]
    internal sealed partial class BuildDetailsColumn : VisualElement
    {
        public static readonly string ussClassName = "build-details-column";
        public static readonly string leftUssClassName = ussClassName + "--left";
        public static readonly string rightUssClassName = ussClassName + "--right";
        public static readonly string rowsUssClassName = ussClassName + "__rows";
        public static readonly string spacerUssClassName = ussClassName + "__spacer";

        private readonly VisualElement m_Rows;
        private readonly VisualElement m_Spacer;
        private BuildDetailsGrid.Column m_Side;
        private IVisualElementScheduledItem m_PendingAlign;

        public override VisualElement contentContainer => m_Rows;

        [UxmlAttribute]
        public BuildDetailsGrid.Column side
        {
            get => m_Side;
            set
            {
                m_Side = value;
                EnableInClassList(leftUssClassName, value == BuildDetailsGrid.Column.Left);
                EnableInClassList(rightUssClassName, value == BuildDetailsGrid.Column.Right);
                m_Spacer.style.display = value == BuildDetailsGrid.Column.Left ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public BuildDetailsColumn()
        {
            AddToClassList(ussClassName);

            m_Rows = new VisualElement();
            m_Rows.AddToClassList(rowsUssClassName);
            hierarchy.Add(m_Rows);

            m_Spacer = new VisualElement();
            m_Spacer.AddToClassList(spacerUssClassName);
            hierarchy.Add(m_Spacer);

            side = BuildDetailsGrid.Column.Left;

            m_Rows.RegisterCallback<GeometryChangedEvent>(_ => AlignLabels());
        }

        public BuildDetailsRow AddRow(string label, string valueName, string labelName = null)
        {
            var row = new BuildDetailsRow { label = label, valueName = valueName, labelName = labelName };
            Add(row);
            return row;
        }

        // Swapping which rows are shown can leave the column's size unchanged, so no GeometryChangedEvent follows.
        internal void ScheduleAlignLabels()
        {
            m_PendingAlign ??= schedule.Execute(AlignLabels);
            m_PendingAlign.ExecuteLater(0);
        }

        private void AlignLabels()
        {
            var width = 0f;
            foreach (var child in m_Rows.Children())
            {
                if (child is BuildDetailsRow row && row.resolvedStyle.display != DisplayStyle.None)
                    width = Mathf.Max(width, row.MeasureLabelWidth());
            }

            foreach (var child in m_Rows.Children())
            {
                if (child is BuildDetailsRow row)
                    row.SetLabelWidth(width);
            }
        }
    }
}
