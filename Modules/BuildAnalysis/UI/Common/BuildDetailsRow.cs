// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Analysis
{
    // Addressables' SummaryTabEmbedded.uxml uses this tag and its attributes.
    // To rename one, keep the old name with [MovedFrom] or [UxmlAttribute("new-name", "old-name")].
    [UxmlElement]
    internal sealed partial class BuildDetailsRow : VisualElement
    {
        public static readonly string ussClassName = "build-details-row";

        private readonly Label m_Label;
        private readonly Label m_Value;

        public Label labelElement => m_Label;
        public Label valueElement => m_Value;

        [UxmlAttribute]
        public string label
        {
            get => m_Label.text;
            set
            {
                m_Label.text = value ?? string.Empty;
                GetFirstAncestorOfType<BuildDetailsColumn>()?.ScheduleAlignLabels();
            }
        }

        [UxmlAttribute]
        public string valueName
        {
            get => m_Value.name;
            set => m_Value.name = value;
        }

        [UxmlAttribute]
        public string labelName
        {
            get => m_Label.name;
            set => m_Label.name = value;
        }

        public BuildDetailsRow()
        {
            AddToClassList(ussClassName);

            m_Label = new Label();
            m_Label.AddToClassList(BuildDetailsGrid.labelUssClassName);
            Add(m_Label);

            m_Value = new Label();
            m_Value.selection.isSelectable = true;
            m_Value.AddToClassList(BuildDetailsGrid.valueUssClassName);
            Add(m_Value);
        }

        internal float MeasureLabelWidth()
        {
            var text = m_Label.MeasureTextSize(m_Label.text, 0, MeasureMode.Undefined, 0, MeasureMode.Undefined);
            var style = m_Label.resolvedStyle;
            return Mathf.Ceil(text.x) + style.paddingLeft + style.paddingRight + style.borderLeftWidth + style.borderRightWidth;
        }

        // Skips no-op writes, so aligning on GeometryChangedEvent settles.
        internal void SetLabelWidth(float width)
        {
            var current = m_Label.style.width;
            if (current.keyword == StyleKeyword.Undefined && Mathf.Approximately(current.value.value, width))
                return;

            m_Label.style.width = width;
        }
    }
}
