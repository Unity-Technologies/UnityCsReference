// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    [UxmlElement]
    internal partial class TextAlignStrip : BaseField<string>
    {
        static readonly string s_UssPath = BuilderConstants.UtilitiesPath + "/TextAlignStrip/TextAlignStrip.uss";

        static readonly string s_UssClassName = "unity-text-align-strip";
        static readonly string s_ButtonStripContainerClassName = s_UssClassName + "__button-strip-container";

        static readonly string[] s_HorizontalChoices = { "left", "center", "right" };
        static readonly string[] s_VerticalChoices = { "upper", "middle", "lower" };

        VisualElement m_ButtonStripContainer;
        ToggleButtonGroup m_HorizontalButtonGroup;
        ToggleButtonGroup m_VerticalButtonGroup;
        ToggleButtonGroupState m_HorizontalToggleButtonGrouptState;
        ToggleButtonGroupState m_VerticalToggleButtonGrouptState;

        public TextAlignStrip() : this(null) {}

        public TextAlignStrip(string label) : base(label)
        {
            AddToClassList(s_UssClassName);

            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_UssPath));

            m_ButtonStripContainer = new VisualElement()
            {
                tabIndex = -1
            };
            m_ButtonStripContainer.AddToClassList(s_ButtonStripContainerClassName);

            m_HorizontalButtonGroup = new ToggleButtonGroup();
            m_HorizontalButtonGroup.name = "horizontal-align-strip";
            m_HorizontalButtonGroup.RegisterValueChangedCallback(OnHorizontalValueChange);
            m_HorizontalButtonGroup.Add(new Button() { name = "left", tooltip = "Left" });
            m_HorizontalButtonGroup.Add(new Button() { name = "center", tooltip = "Center" });
            m_HorizontalButtonGroup.Add(new Button() { name = "right", tooltip = "Right" });
            m_ButtonStripContainer.Add(m_HorizontalButtonGroup);
            m_HorizontalToggleButtonGrouptState = new ToggleButtonGroupState(0, s_HorizontalChoices.Length);

            m_VerticalButtonGroup = new ToggleButtonGroup();
            m_VerticalButtonGroup.name = "vertical-align-strip";
            m_VerticalButtonGroup.Add(new Button() { name = "upper", tooltip = "Upper" });
            m_VerticalButtonGroup.Add(new Button() { name = "middle", tooltip = "Middle" });
            m_VerticalButtonGroup.Add(new Button() { name = "lower", tooltip = "Lower" });
            m_VerticalButtonGroup.RegisterValueChangedCallback(OnVerticalValueChange);
            m_ButtonStripContainer.Add(m_VerticalButtonGroup);
            m_VerticalToggleButtonGrouptState = new ToggleButtonGroupState(0, s_VerticalChoices.Length);

            visualInput = m_ButtonStripContainer;
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);

            if (string.IsNullOrEmpty(newValue))
                return;

            var values = newValue.Split('-');

            m_VerticalToggleButtonGrouptState.ResetAllOptions();
            m_VerticalToggleButtonGrouptState[Array.FindIndex(s_VerticalChoices, choice => choice.Contains(values[0]))] = true;
            m_VerticalButtonGroup.SetValueWithoutNotify(m_VerticalToggleButtonGrouptState);

            m_HorizontalToggleButtonGrouptState.ResetAllOptions();
            m_HorizontalToggleButtonGrouptState[Array.FindIndex(s_HorizontalChoices, choice => choice.Contains(values[1]))] = true;
            m_HorizontalButtonGroup.SetValueWithoutNotify(m_HorizontalToggleButtonGrouptState);
        }

        void OnHorizontalValueChange(ChangeEvent<ToggleButtonGroupState> evt)
        {
            var verticalSelected = m_VerticalButtonGroup.value.GetActiveOptions(stackalloc int[m_VerticalButtonGroup.value.length]);
            var horizontalSelected = evt.newValue.GetActiveOptions(stackalloc int[evt.newValue.length]);

            var newValue = s_VerticalChoices[verticalSelected[0]] + "-" + s_HorizontalChoices[horizontalSelected[0]];
            base.value = newValue;
        }

        void OnVerticalValueChange(ChangeEvent<ToggleButtonGroupState> evt)
        {
            var verticalSelected = evt.newValue.GetActiveOptions(stackalloc int[evt.newValue.length]);
            var horizontalSelected = m_HorizontalButtonGroup.value.GetActiveOptions(stackalloc int[m_HorizontalButtonGroup.value.length]);

            var newValue = s_VerticalChoices[verticalSelected[0]] + "-" + s_HorizontalChoices[horizontalSelected[0]];
            base.value = newValue;
        }
    }
}
