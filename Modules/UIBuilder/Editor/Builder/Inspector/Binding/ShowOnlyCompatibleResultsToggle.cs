// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    /// <summary>
    /// Toggle in a completer that indicates whether only compatible results should be displayed in the completer.
    /// </summary>
    class ShowOnlyCompatibleResultsToggle : VisualElement
    {
        static readonly string s_UssClassName = "show-only-compatible-toggle";

        private Toggle m_Toggle;
        private Label m_RatioLabel;

        public Toggle toggle => m_Toggle;

        /// <summary>
        /// Constructs a toggle
        /// </summary>
        public ShowOnlyCompatibleResultsToggle()
        {
            AddToClassList(s_UssClassName);
            m_Toggle = new Toggle() { text = BuilderConstants.BindingWindowShowOnlyCompatibleMessage, value = true };
            m_RatioLabel = new Label() { classList = { FieldSearchCompleterPopup.s_ResultLabelUssClassName } };

            Add(m_Toggle);
            Add(m_RatioLabel);
        }

        /// <summary>
        /// Sets the number of compatible results and the number of all results
        /// </summary>
        /// <param name="compatibleResultCount">The number of compatible results</param>
        /// <param name="maxResultCount">The number of all results</param>
        public void SetCompatibleResultCount(int compatibleResultCount, int maxResultCount)
        {
            m_RatioLabel.text = $"{compatibleResultCount}/{maxResultCount}";
        }
    }
}
