// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    // Addressables' SummaryTabEmbedded.uxml uses this tag and its attributes.
    // To rename one, keep the old name with [MovedFrom] or [UxmlAttribute("new-name", "old-name")].
    [UxmlElement]
    internal sealed partial class BuildDetailsGrid : VisualElement
    {
        public enum Column
        {
            Left,
            Right,
        }

        public static readonly string ussClassName = "build-details-grid";
        public static readonly string labelUssClassName = "info-label";
        public static readonly string valueUssClassName = "info-value";
        public static readonly string actionButtonUssClassName = "info-action-button";

        private const string k_UssPath = "BuildAnalysis/StyleSheets/BuildDetails.uss";

        public BuildDetailsGrid()
        {
            var styleSheet = EditorGUIUtility.LoadRequired(k_UssPath) as StyleSheet;
            styleSheets.Add(styleSheet);
            AddToClassList(ussClassName);
        }

        public Label AddRow(string label, string valueName, Column column, string labelName = null)
        {
            return GetOrAddColumn(column).AddRow(label, valueName, labelName).valueElement;
        }

        public Label AddRowWithAction(string label, string valueName, string buttonText, Column column, out Button button, string buttonName = null)
        {
            var row = GetOrAddColumn(column).AddRow(label, valueName);
            button = new Button { name = buttonName, text = buttonText };
            button.AddToClassList(actionButtonUssClassName);
            row.Add(button);
            return row.valueElement;
        }

        public void SetRowVisible(Label value, bool visible)
        {
            var row = value.parent as BuildDetailsRow;
            if (row == null || row.GetFirstAncestorOfType<BuildDetailsGrid>() != this)
                throw new ArgumentException("The label is not a value in this grid.", nameof(value));

            row.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            row.GetFirstAncestorOfType<BuildDetailsColumn>()?.ScheduleAlignLabels();
        }

        private BuildDetailsColumn GetOrAddColumn(Column side)
        {
            foreach (var child in Children())
            {
                if (child is BuildDetailsColumn column && column.side == side)
                    return column;
            }

            var added = new BuildDetailsColumn { side = side };
            if (side == Column.Left)
                Insert(0, added);
            else
                Add(added);
            return added;
        }
    }
}
