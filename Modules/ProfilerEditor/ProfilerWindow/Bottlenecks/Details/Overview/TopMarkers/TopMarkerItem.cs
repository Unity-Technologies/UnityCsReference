// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEngine.UIElements;
using static Unity.Profiling.Editor.UI.TopMarkersModel;

namespace Unity.Profiling.Editor.UI
{
    class TopMarkerItem : VisualElement, ISelectedDetailsViewElement
    {
        static class Content
        {
            public static readonly string k_OccurrenceInFrameFormat = L10n.Tr("{0} occurrence in frame {1}.");
            public static readonly string k_OccurrencesInFrameFormat = L10n.Tr("{0} occurrences in frame {1}.");
            public static readonly string k_ClickForMoreDetails = L10n.Tr("Click for more details");
        }

        readonly VisualElement m_Bar;
        readonly Label m_Label;
        readonly Label m_FrameLabel;

        public TopMarkerItem()
        {
            name = "top-marker-item";
            RegisterCallback<PointerEnterEvent>(OnPointerEnter);
            RegisterCallback<PointerLeaveEvent>(OnPointerLeave);

            var container = new VisualElement()
            {
                name = "top-marker-item__container",
                pickingMode = PickingMode.Ignore
            };
            Add(container);

            m_Bar = new VisualElement()
            {
                name = "top-marker-item__bar",
            };
            container.Add(m_Bar);

            m_Label = new Label()
            {
                name = "top-marker-item__label",
                displayTooltipWhenElided = false,
            };
            container.Add(m_Label);

            m_FrameLabel = new Label()
            {
                name = "top-marker-item__frame-label"
            };
            Add(m_FrameLabel);

            ApplyStyleSheet();
        }

        public void Configure(
            Marker marker,
            float markerTimeNormalized,
            string title)
        {
            // Set label and tooltip
            m_Label.text = $"{marker.Name} ({marker.FormatValue()})";
            var numberOfInstances = marker.NumberOfInstances;
            var sb = new System.Text.StringBuilder();
            var description = MarkersInformationProvider.GetMarkerInfo(marker.Name);
            sb.Append($"{marker.Name} ({marker.FormatValue()})\n\n");
            if (!string.IsNullOrEmpty(description))
                sb.AppendLine(description).AppendLine();
            var frameDisplay = FrameIndexFormatterUtility.DisplayStringForFrameIndex(marker.FrameIndex);
            var occurrenceFormat = (numberOfInstances > 1) ? Content.k_OccurrencesInFrameFormat : Content.k_OccurrenceInFrameFormat;
            sb.AppendLine(string.Format(occurrenceFormat, numberOfInstances, frameDisplay));
            sb.Append(Content.k_ClickForMoreDetails);
            m_Label.tooltip = sb.ToString();

            // Set frame label
            if (string.IsNullOrEmpty(title))
            {
                UIUtility.SetElementDisplay(m_FrameLabel, false);
            }
            else
            {
                UIUtility.SetElementDisplay(m_FrameLabel, true);
                m_FrameLabel.text = title;
            }
            m_Bar.style.width = new StyleLength(new Length(markerTimeNormalized * 100f, LengthUnit.Percent));
        }

        public void SetSelected(bool value)
        {
            m_Bar.SetCheckedPseudoState(value);
        }

        void OnPointerLeave(PointerLeaveEvent evt)
        {
            m_Bar.SetActivePseudoState(false);
        }

        void OnPointerEnter(PointerEnterEvent evt)
        {
            m_Bar.SetActivePseudoState(true);
        }

        void ApplyStyleSheet()
        {
            var uss = EditorGUIUtility.Load("TopMarkerItem.uss") as StyleSheet;
            styleSheets.Add(uss);

            const string k_UssClass_Dark = "top-marker-item__dark";
            const string k_UssClass_Light = "top-marker-item__light";
            var themeUssClass = (EditorGUIUtility.isProSkin) ? k_UssClass_Dark : k_UssClass_Light;
            AddToClassList(themeUssClass);
        }
    }
}
