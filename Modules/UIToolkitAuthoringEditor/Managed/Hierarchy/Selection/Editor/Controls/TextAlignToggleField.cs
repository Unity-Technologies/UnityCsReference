// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    /// <summary>
    /// Makes a style field for editing the TextAlign property.
    /// </summary>
    internal class TextAlignToggleField : BaseField<TextAnchor>
    {
        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<TextAnchor>.UxmlSerializedData
        {
            [RegisterUxmlCache]
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<TextAnchor>.UxmlSerializedData.Register();
            }

            public override object CreateInstance() => new TextAlignToggleField();
        }

        static readonly string s_UssClassName = "unity-text-align-toggle-field";
        static readonly string s_ButtonStripContainerClassName = s_UssClassName + "__button-strip-container";

        static readonly string[] s_HorizontalChoices = { "left", "center", "right" };
        static readonly string[] s_VerticalChoices = { "upper", "middle", "lower" };

        ToggleButtonGroup m_HorizontalButtonGroup;
        ToggleButtonGroup m_VerticalButtonGroup;
        ToggleButtonGroupState m_HorizontalToggleButtonGroupState;
        ToggleButtonGroupState m_VerticalToggleButtonGroupState;

        public ToggleButtonGroup horizontalButtonGroup => m_HorizontalButtonGroup;
        public ToggleButtonGroup verticalButtonGroup => m_VerticalButtonGroup;

        public TextAlignToggleField()
            : this(null) { }

        public TextAlignToggleField(string label)
            : base(label, new VisualElement())
        {
            AddToClassList(s_UssClassName);

            visualInput.tabIndex = -1;
            visualInput.AddToClassList(s_ButtonStripContainerClassName);

            m_HorizontalButtonGroup = new ToggleButtonGroup();
            m_HorizontalButtonGroup.AddToClassList($"{ToggleButtonGroup.ussClassName}_text-align-field");

            m_HorizontalButtonGroup.name = "horizontal-align-toggle-field";
            m_HorizontalButtonGroup.RegisterValueChangedCallback(OnHorizontalValueChange);
            m_HorizontalButtonGroup.Add(new Button() { name = "left", tooltip = "Aligns text to the left edge of the container." });
            m_HorizontalButtonGroup.Add(new Button() { name = "center", tooltip = "Aligns text to the center of the container." });
            m_HorizontalButtonGroup.Add(new Button() { name = "right", tooltip = "Aligns text to the right edge of the container." });
            visualInput.Add(m_HorizontalButtonGroup);
            m_HorizontalToggleButtonGroupState = new ToggleButtonGroupState(0, s_HorizontalChoices.Length);

            m_VerticalButtonGroup = new ToggleButtonGroup();
            m_VerticalButtonGroup.name = "vertical-align-toggle-field";
            m_VerticalButtonGroup.AddToClassList($"{ToggleButtonGroup.ussClassName}_text-align-field");
            m_VerticalButtonGroup.Add(new Button() { name = "upper", tooltip = "Aligns text to the upper edge of the container." });
            m_VerticalButtonGroup.Add(new Button() { name = "middle", tooltip = "Aligns text to the middle of the container." });
            m_VerticalButtonGroup.Add(new Button() { name = "lower", tooltip = "Aligns text to the lower edge of the container." });
            m_VerticalButtonGroup.RegisterValueChangedCallback(OnVerticalValueChange);
            visualInput.Add(m_VerticalButtonGroup);
            m_VerticalToggleButtonGroupState = new ToggleButtonGroupState(0, s_VerticalChoices.Length);
        }

        public override void SetValueWithoutNotify(TextAnchor newValue)
        {
            base.SetValueWithoutNotify(newValue);

            var kebabCase = newValue.ToString().ToKebabCase();
            var vertical = kebabCase.Split("-")[0];
            m_VerticalToggleButtonGroupState.ResetAllOptions();
            m_VerticalToggleButtonGroupState[Array.FindIndex(s_VerticalChoices, choice => choice.Contains(vertical))] = true;
            m_VerticalButtonGroup.SetValueWithoutNotify(m_VerticalToggleButtonGroupState);

            var horizontal = kebabCase.Split("-")[1];
            m_HorizontalToggleButtonGroupState.ResetAllOptions();
            m_HorizontalToggleButtonGroupState[Array.FindIndex(s_HorizontalChoices, choice => choice.Contains(horizontal))] = true;
            m_HorizontalButtonGroup.SetValueWithoutNotify(m_HorizontalToggleButtonGroupState);
        }

        void OnHorizontalValueChange(ChangeEvent<ToggleButtonGroupState> evt)
        {
            var verticalSelected = m_VerticalButtonGroup.value.GetActiveOptions(stackalloc int[m_VerticalButtonGroup.value.length]);
            var horizontalSelected = evt.newValue.GetActiveOptions(stackalloc int[evt.newValue.length]);

            var newValue = s_VerticalChoices[verticalSelected[0]] + s_HorizontalChoices[horizontalSelected[0]];
            base.value = Enum.Parse<TextAnchor>(newValue, true);
        }

        void OnVerticalValueChange(ChangeEvent<ToggleButtonGroupState> evt)
        {
            var verticalSelected = evt.newValue.GetActiveOptions(stackalloc int[evt.newValue.length]);
            var horizontalSelected = m_HorizontalButtonGroup.value.GetActiveOptions(stackalloc int[m_HorizontalButtonGroup.value.length]);

            var newValue = s_VerticalChoices[verticalSelected[0]] + s_HorizontalChoices[horizontalSelected[0]];
            base.value = Enum.Parse<TextAnchor>(newValue, true);
        }
    }
}
