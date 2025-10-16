// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using Unity.GraphToolkit.CSO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class TextAlignmentFieldBuilder : ICustomPropertyFieldBuilder<TextAlignment>
    {
        const string k_UssPath = "TextAlignmentButtonGroup/TextAlignmentButtonGroup.uss";
        static readonly string k_ButtonIconClassName = ToggleButtonGroup.buttonGroupClassName + "__button-icon";

        /// <summary>
        /// The class name for this TextAlignment Fields.
        /// </summary>
        public static readonly string ussClassName = "ge-text-alignment-field";

        ToggleButtonGroup m_ToggleButtonGroup;

        /// <inheritdoc />
        public bool UpdateDisplayedValue(TextAlignment value)
        {
            var valueBitMask = 0x1UL << (int)value;
            var toggleButtonGroupState = new ToggleButtonGroupState(valueBitMask, m_ToggleButtonGroup.value.length);
            m_ToggleButtonGroup.SetValueWithoutNotify(toggleButtonGroupState);

            return true;
        }

        public void SetMixed()
        {
            m_ToggleButtonGroup.showMixedValue = true;
        }

        void OnValueChanged(ChangeEvent<ToggleButtonGroupState> e)
        {
            var numValues = Math.Max(e.previousValue.length, e.newValue.length);
            Span<int> activeValues = stackalloc int[numValues];

            e.previousValue.GetActiveOptions(activeValues);
            var previousValue = (TextAlignment)activeValues[0];

            e.newValue.GetActiveOptions(activeValues);
            var newValue = (TextAlignment)activeValues[0];

            using (var ee = ChangeEvent<TextAlignment>.GetPooled(previousValue, newValue))
            {
                ee.target = m_ToggleButtonGroup;
                m_ToggleButtonGroup.SendEvent(ee);
            }

            e.StopPropagation();
        }

        Button MakeButton()
        {
            var button = new Button();
            m_ToggleButtonGroup.Add(button);
            var icon = new Image();
            icon.AddToClassList(k_ButtonIconClassName);
            button.Add(icon);
            return button;
        }

        /// <inheritdoc />
        public (Label, VisualElement) Build(ICommandTarget commandTargetView, string label, string fieldTooltip, IReadOnlyList<object> objs, string propertyName)
        {
            var numButtons = Enum.GetNames(typeof(TextAlignment)).Length;
            var toggleButtonGroupState = new ToggleButtonGroupState(1, numButtons);
            m_ToggleButtonGroup = new ToggleButtonGroup(toggleButtonGroupState) { label = ObjectNames.NicifyVariableName(propertyName), allowEmptySelection = false, isMultipleSelection = false };
            for (var i = 0; i < numButtons; i++)
            {
                m_ToggleButtonGroup.Add(MakeButton());
            }

            m_ToggleButtonGroup.RegisterCallback<ChangeEvent<ToggleButtonGroupState>>(OnValueChanged);

            m_ToggleButtonGroup.AddToClassList(ussClassName);
            m_ToggleButtonGroup.AddStylesheetWithSkinVariants(k_UssPath);

            return (null, m_ToggleButtonGroup);
        }
    }
}
