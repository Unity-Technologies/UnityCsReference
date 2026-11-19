// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    class VariableSetValueInGraphPropertyField : BaseModelPropertyField
    {
        /// <summary>
        /// The USS class name added to a <see cref="VariableSetValueInGraphPropertyField"/>.
        /// </summary>
        public new static readonly string ussClassName = "variable-set-value-in-graph-property-field";

        /// <summary>
        /// The USS class name added to the label of a <see cref="VariableSetValueInGraphPropertyField"/>.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName.WithUssElement(GraphElementHelper.labelName);

        /// <summary>
        /// The USS class name added to the control of a <see cref="VariableSetValueInGraphPropertyField"/>.
        /// </summary>
        public static readonly string controlUssClassName = ussClassName.WithUssElement("control");

        /// <summary>
        /// The USS class name added to the change button of a <see cref="VariableSetValueInGraphPropertyField"/>.
        /// </summary>
        public static readonly string changeButtonUssClassName = ussClassName.WithUssElement(changeButtonName);

        Toggle ChangeButton => Field as Toggle;

        readonly IReadOnlyList<VariableNodeModel> m_VariableNodeModels;

        public VariableSetValueInGraphPropertyField(RootView rootView, IReadOnlyList<VariableNodeModel> variableNodeModels, string labelText)
            : base(rootView)
        {
            m_VariableNodeModels = variableNodeModels;

            AddToClassList(ussClassName);
            this.AddPackageStylesheet("VariableSetValueInGraphPropertyField.uss");

            LabelElement = new Label { text = labelText };
            LabelElement.AddToClassList(labelUssClassName);
            LabelElement.AddToClassList(BaseField<int>.labelUssClassName);
            Add(LabelElement);

            Field = new Toggle();
            Field.AddToClassList(changeButtonUssClassName);
            Field.AddToClassList(controlUssClassName);
            Add(Field);

            Field.RegisterCallback<ChangeEvent<bool>>(OnToggleChanged);

            Setup(LabelElement, Field, null);
        }

        void OnToggleChanged(ChangeEvent<bool> e)
        {
            CommandTarget.Dispatch(new ToggleSetVariableNodeCommand(e.newValue ? VariableNodeMode.Set : VariableNodeMode.Get, m_VariableNodeModels));
        }

        public override void UpdateDisplayedValue()
        {
            if (m_VariableNodeModels.Count < 1)
                return;

            var same = true;
            var notAllowed = false;

            var firstMode = m_VariableNodeModels[0].Mode;
            foreach (var variableNode in m_VariableNodeModels)
            {
                if (variableNode.VariableDeclarationModel?.CanCreateSetVariableNode != true)
                    notAllowed = true;

                if (variableNode.Mode != firstMode)
                    same = false;
            }

            if (same)
                ChangeButton.SetValueWithoutNotify(firstMode == VariableNodeMode.Set);

            ChangeButton.showMixedValue = !same;

            // If the variable can be a SetVariableNode, display the 'Allow to set value in graph' property, else grey out the field.
            SetEnabled(!notAllowed);
            tooltip = notAllowed ? "Set variable nodes are not allowed for subgraph input/output variables." : "";
        }
    }
}

