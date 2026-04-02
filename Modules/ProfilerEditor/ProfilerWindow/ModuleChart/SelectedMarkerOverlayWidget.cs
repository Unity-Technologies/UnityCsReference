// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Profiling.Editor.UI;
using UnityEngine.UIElements;

namespace Unity.Profiling.Editor
{
    /// <summary>
    /// SelectedMarkerOverlayWidget displays the currently selected marker name in the top-right corner of the chart.
    /// This replicates the functionality that was lost when DrawChartOverlay was removed during the UITK migration.
    /// </summary>
    internal class SelectedMarkerOverlayWidget
    {
        const string k_UxmlIdentifier_SelectedMarkerLabel = "profiler-chart-view__chart__selected-marker-label";

        readonly VisualElement m_Root;
        readonly VisualElement m_GroupRoot;

        readonly Label m_SelectedMarkerLabel;

        public SelectedMarkerOverlayWidget(VisualElement root)
        {
            m_Root = root;

            // Create the group root that will contain our overlay elements
            m_GroupRoot = new VisualElement();
            m_GroupRoot.name = GetType().Name;
            m_GroupRoot.StretchToParentSize();
            m_Root.Add(m_GroupRoot);

            // Create the label for the selected marker
            m_SelectedMarkerLabel = new Label();
            m_SelectedMarkerLabel.AddToClassList(k_UxmlIdentifier_SelectedMarkerLabel);
            // TODO: Decide if/how we want tooltips (or similar). Right now, the width of certain callstacks can blow
            // past the limit of the max tooltip width (300px, seemingly unchangeable), which makes them hard to read.
            // m_SelectedMarkerLabel.displayTooltipWhenElided = false; // Don't overwrite our tooltips
            m_GroupRoot.Add(m_SelectedMarkerLabel);
        }

        /// <summary>
        /// Updates the overlay with the current selection information.
        /// </summary>
        /// <param name="selectionText">The text to display (e.g., "Selected: SampleName")</param>
        /// <param name="tooltipText">The tooltip text to show on hover</param>
        public void Update(string selectionText, string tooltipText)
        {
            if (string.IsNullOrEmpty(selectionText))
            {
                UIUtility.SetElementDisplay(m_SelectedMarkerLabel, false);
                return;
            }

            m_SelectedMarkerLabel.text = selectionText;
            // TODO: See above re: tooltips.
            // m_SelectedMarkerLabel.tooltip = tooltipText;
            UIUtility.SetElementDisplay(m_SelectedMarkerLabel, true);
        }

        /// <summary>
        /// Clears the overlay.
        /// </summary>
        public void Clear()
        {
            UIUtility.SetElementDisplay(m_SelectedMarkerLabel, false);
        }
    }
}
