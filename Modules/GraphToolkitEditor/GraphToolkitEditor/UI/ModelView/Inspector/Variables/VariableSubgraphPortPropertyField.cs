// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    class VariableSubgraphPortPropertyField : BaseModelPropertyField
    {
        /// <summary>
        /// The USS class name added to a <see cref="VariableSubgraphPortPropertyField"/>.
        /// </summary>
        public new static readonly string ussClassName = "variable-subgraph-ports-property-field";

        /// <summary>
        /// The USS class name added to a <see cref="VariableSubgraphPortPropertyField"/>.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName.WithUssElement(GraphElementHelper.labelName);

        /// <summary>
        /// The USS class name added to a <see cref="VariableSubgraphPortPropertyField"/>.
        /// </summary>
        public static readonly string changeButtonUssClassName = ussClassName.WithUssElement(changeButtonName);

        /// <summary>
        /// The USS class name added to the buttons to change the variable's direction.
        /// </summary>
        public static readonly string inputOutputButtonsUssClassName = ussClassName.WithUssElement("input-output");

        /// <summary>
        /// The USS class name added to the control of a <see cref="VariableSubgraphPortPropertyField"/>.
        /// </summary>
        public static readonly string controlUssClassName = ussClassName.WithUssElement("control");

        /// <summary>
        /// The USS class name added to a <see cref="VariableSubgraphPortPropertyField"/> that has a toggle.
        /// </summary>
        public static readonly string withToggleUssClassName = ussClassName.WithUssModifier("with-toggle");

        /// <summary>
        /// The USS class name added to the toggle container of a <see cref="VariableSubgraphPortPropertyField"/>.
        /// </summary>
        public static readonly string toggleContainerUssClassName = ussClassName.WithUssElement("toggle-content");

        /// <summary>
        /// The USS class name added to a line in a <see cref="VariableSubgraphPortPropertyField"/>.
        /// </summary>
        public static readonly string lineUssClassName = ussClassName.WithUssElement("line");

        /// <summary>
        /// The USS class name added to the unchecked change button of a <see cref="VariableSubgraphPortPropertyField"/>.
        /// </summary>
        public static readonly string changeButtonUncheckedUssClassName = changeButtonUssClassName.WithUssModifier("unchecked");

        /// <summary>
        /// Name for the display settings field.
        /// </summary>
        public static readonly string displaySettingsName = "display-settings";

        Toggle m_ChangeButton;
        ToggleButtonGroup m_InputOutput;

        IReadOnlyList<VariableDeclarationModelBase> m_Variables;

        RootView m_RootView;
        VisualElement m_ToggleContentContainer;
        DropdownField m_DisplaySettingsDropdown;

        enum InputOutputValues
        {
            Input = 0,
            Output = 1
        }

        static readonly List<string> k_DisplaySettings = new() { "Node and Inspector", "Inspector Only" };

        const int k_InputOutputValuesCount = 2;

        public VariableSubgraphPortPropertyField(RootView rootView, IReadOnlyList<VariableDeclarationModelBase> variables, bool showToggle)
            : base(rootView)
        {
            m_Variables = variables;
            m_RootView = rootView;

            AddToClassList(ussClassName);
            this.AddPackageStylesheet("VariableSubgraphPortPropertyField.uss");

            if (showToggle)
            {
                var firstLine = new VisualElement();
                firstLine.AddToClassList(lineUssClassName);

                var label = new Label() { text = "Use as port on subgraph node" };
                label.AddToClassList(labelUssClassName);
                label.AddToClassList(BaseModelPropertyField.labelUssClassName);
                label.AddToClassList(BaseField<int>.labelUssClassName);
                firstLine.Add(label);

                //We don't set LabelElement because we don't want other label to align to this long label

                m_ChangeButton = new Toggle();
                m_ChangeButton.AddToClassList(changeButtonUssClassName);
                m_ChangeButton.AddToClassList(controlUssClassName);
                firstLine.Add(m_ChangeButton);

                m_ChangeButton.RegisterCallback<ChangeEvent<bool>>(OnToggleChange);
                Add(firstLine);
            }

            m_ToggleContentContainer = new VisualElement();
            m_ToggleContentContainer.AddToClassList(toggleContainerUssClassName);
            Add(m_ToggleContentContainer);

            var secondLine = new VisualElement();
            secondLine.AddToClassList(lineUssClassName);

            var directionLabel = new Label("Flow Direction");
            directionLabel.AddToClassList(BaseModelPropertyField.labelUssClassName);
            directionLabel.EnableInClassList(withToggleUssClassName, showToggle);
            secondLine.Add(directionLabel);

            var spacer = new VisualElement();
            spacer.AddToClassList("spacer");
            secondLine.Add(spacer);
            m_InputOutput = new ToggleButtonGroup();
            m_InputOutput.Add(new Button { text = "Input" });
            m_InputOutput.Add(new Button { text = "Output" });
            m_InputOutput.AddToClassList(controlUssClassName);
            m_InputOutput.AddToClassList(inputOutputButtonsUssClassName);
            m_InputOutput.RegisterCallback<ChangeEvent<ToggleButtonGroupState>>(OnInputOutputChange);
            m_InputOutput.isMultipleSelection = false;
            m_InputOutput.allowEmptySelection = false;
            secondLine.Add(m_InputOutput);
            m_ToggleContentContainer.Add(secondLine);

            // If at least one output is selected, we don't show the display settings as it is not relevant.
            if (!m_Variables.Any(f => f.IsOutput))
            {
                var thirdLine = new VisualElement();
                thirdLine.AddToClassList(lineUssClassName);

                var displaySettingsLabel = new Label("Show on") { name = displaySettingsName };
                displaySettingsLabel.AddToClassList(BaseModelPropertyField.labelUssClassName);
                thirdLine.Add(displaySettingsLabel);

                m_DisplaySettingsDropdown = new DropdownField { name = displaySettingsName, choices = k_DisplaySettings };
                m_DisplaySettingsDropdown.AddToClassList(controlUssClassName);
                m_DisplaySettingsDropdown.RegisterCallback<ChangeEvent<string>>(OnDisplaySettingsChange);
                thirdLine.Add(m_DisplaySettingsDropdown);
                m_ToggleContentContainer.Add(thirdLine);
                displaySettingsLabel.EnableInClassList(withToggleUssClassName, showToggle);
            }
        }

        void OnToggleChange(ChangeEvent<bool> e)
        {
            m_ToggleContentContainer.EnableInClassList(GraphElementHelper.hiddenUssModifier, !e.newValue);
            m_RootView.Dispatch(new ChangeVariableModifiersCommand(e.newValue ? ModifierFlags.Read : ModifierFlags.None, m_Variables));
        }

        void OnInputOutputChange(ChangeEvent<ToggleButtonGroupState> e)
        {
            bool input = e.newValue.length == 0 || e.newValue.GetActiveOptions(stackalloc int[k_InputOutputValuesCount])[0] == (int)InputOutputValues.Input;

            m_RootView.Dispatch(new ChangeVariableModifiersCommand(input ? ModifierFlags.Read : ModifierFlags.Write, m_Variables));
        }

        void OnDisplaySettingsChange(ChangeEvent<string> e)
        {
            var showOnInspectorOnly = e.newValue == k_DisplaySettings[1];
            m_RootView.Dispatch(new ChangeVariableDisplaySettingsCommand(showOnInspectorOnly, m_Variables));
        }

        public override void UpdateDisplayedValue()
        {
            if (m_Variables.Count < 1)
                return;

            bool sameActivation = true;
            bool sameDirection = true;
            bool sameDisplaySettings = true;

            var firstModifiers = m_Variables[0].Modifiers;
            for (int i = 1; i < m_Variables.Count; ++i)
            {
                if (m_Variables[i].ShowOnInspectorOnly != m_Variables[0].ShowOnInspectorOnly)
                {
                    sameDisplaySettings = false;
                }

                if ((m_Variables[i].Modifiers == ModifierFlags.None) != (firstModifiers == ModifierFlags.None))
                {
                    sameActivation = false;
                }

                if (m_Variables[i].Modifiers.HasFlag(ModifierFlags.Read) != firstModifiers.HasFlag(ModifierFlags.Read))
                {
                    sameDirection = false;
                    break;
                }
            }

            if (sameActivation)
            {
                var showToggleContent = firstModifiers != ModifierFlags.None;
                m_ChangeButton?.SetValueWithoutNotify(firstModifiers != ModifierFlags.None);
                m_ToggleContentContainer.visible = showToggleContent;
            }
            else
            {
                // at least one of the variables is not a port on subgraph node, don't show the toggle content
                m_ToggleContentContainer.visible = false;
            }

            EnableInClassList(changeButtonUncheckedUssClassName, !sameActivation || firstModifiers == ModifierFlags.None);

            if (m_ChangeButton != null)
            {
                m_ChangeButton.showMixedValue = !sameActivation;
            }

            if (sameDirection)
            {
                m_InputOutput.allowEmptySelection = false;
                if (firstModifiers.HasFlag(ModifierFlags.Read))
                {
                    m_InputOutput.SetValueWithoutNotify(new ToggleButtonGroupState(1 << (int)InputOutputValues.Input, k_InputOutputValuesCount));
                }
                else
                {
                    m_InputOutput.SetValueWithoutNotify(new ToggleButtonGroupState(1 << (int)InputOutputValues.Output, k_InputOutputValuesCount));
                }
            }
            //TODO use showMixedValue when working
            else
            {
                m_InputOutput.allowEmptySelection = true;
                m_InputOutput.SetValueWithoutNotify(new ToggleButtonGroupState(0, k_InputOutputValuesCount));
            }

            if (m_DisplaySettingsDropdown != null)
            {
                if (sameDisplaySettings)
                {
                    m_DisplaySettingsDropdown.SetValueWithoutNotify(m_Variables[0].ShowOnInspectorOnly ? k_DisplaySettings[1] : k_DisplaySettings[0]);
                }
                else
                {
                    m_DisplaySettingsDropdown.showMixedValue = true;
                }
            }
        }
    }
}
