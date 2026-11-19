// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    [UnityRestricted]
    internal class VariableExposedPropertyField : BaseModelPropertyField
    {
        /// <summary>
        /// The USS class name added to a <see cref="VariableExposedPropertyField"/>.
        /// </summary>
        public new static readonly string ussClassName = "ge-variable-exposed-property-field";

        /// <summary>
        /// The USS class name added to the label of a <see cref="VariableExposedPropertyField"/>.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName.WithUssElement(GraphElementHelper.labelName);

        /// <summary>
        /// The USS class name added to the change button of a <see cref="VariableExposedPropertyField"/>.
        /// </summary>
        public static readonly string changeButtonUssClassName = ussClassName.WithUssElement(changeButtonName);

        Toggle ChangeButton => Field as Toggle;

        IReadOnlyList<VariableDeclarationModelBase> m_Variables;

        public VariableExposedPropertyField(RootView rootView, IReadOnlyList<VariableDeclarationModelBase> variables, string labelText)
            : base(rootView)
        {
            m_Variables = variables;

            AddToClassList(ussClassName);
            this.AddPackageStylesheet("VariableExposedPropertyField.uss");

            LabelElement = new Label() { text = labelText };
            LabelElement.AddToClassList(labelUssClassName);
            LabelElement.AddToClassList(BaseField<int>.labelUssClassName);
            Add(LabelElement);
            //We don't set LabelElement because we don't want other label to align to this long label

            Field = new Toggle();
            Field.AddToClassList(changeButtonUssClassName);
            Add(Field);

            Field.RegisterCallback<ChangeEvent<bool>>(OnToggleChange);

            Setup(LabelElement, Field, null);
        }

        void OnToggleChange(ChangeEvent<bool> e)
        {
            CommandTarget.Dispatch(new ChangeVariableScopeCommand(e.newValue ? VariableScope.Exposed : VariableScope.Local, m_Variables));
        }

        public override void UpdateDisplayedValue()
        {
            if (m_Variables.Count < 1)
                return;

            bool same = true;

            var firstScope = m_Variables[0].Scope;
            for (int i = 1; i < m_Variables.Count; ++i)
            {
                if (m_Variables[i].Scope != firstScope)
                {
                    same = false;
                    break;
                }
            }

            if (same)
                ChangeButton.SetValueWithoutNotify(firstScope == VariableScope.Exposed);

            ChangeButton.showMixedValue = !same;
        }
    }
}
