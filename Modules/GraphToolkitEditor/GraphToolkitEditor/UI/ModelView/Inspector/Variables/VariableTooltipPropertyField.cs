// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.CSO;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    class VariableTooltipPropertyField : BaseModelPropertyField
    {
        /// <summary>
        /// The USS class name added to a <see cref="VariableTooltipPropertyField"/>.
        /// </summary>
        public new static readonly string ussClassName = "variable-tooltip-property-field";

        /// <summary>
        /// The USS class name added to the field of a <see cref="VariableTooltipPropertyField"/>.
        /// </summary>
        public static readonly string fieldUssClassName = ussClassName.WithUssElement(fieldName);

        IReadOnlyList<VariableDeclarationModelBase> m_Variables;
        TextField TextField => Field as TextField;

        public VariableTooltipPropertyField(ICommandTarget commandTarget, IReadOnlyList<VariableDeclarationModelBase> variables)
            : base(commandTarget)
        {
            m_Variables = variables;

            AddToClassList(ussClassName);
            this.AddPackageStylesheet("VariableTooltipPropertyField.uss");
            this.AddPackageStylesheet("Field.uss");

            var label = new Label { text = "Tooltip" };
            label.AddToClassList(labelUssClassName);
            label.AddToClassList(BaseField<int>.labelUssClassName);
            Add(label);
            LabelElement = label;

            Field = new TextField();
            Add(Field);
            TextField.isDelayed = true;
            Field.AddToClassList(fieldUssClassName);
            Field.RegisterCallback<ChangeEvent<string>>(OnTooltipChange);
        }

        void OnTooltipChange(ChangeEvent<string> e)
        {
            if (e.newValue == mixedValueString)
                return;

            var newValue = e.newValue;
            if (newValue.Contains(mixedValueString))
                newValue = newValue.Replace(mixedValueString, string.Empty);

            CommandTarget.Dispatch(new UpdateTooltipCommand(newValue, m_Variables));
        }

        public override void UpdateDisplayedValue()
        {
            if (m_Variables.Count < 1)
                return;

            var same = true;
            var firstTooltip = m_Variables[0].Tooltip;
            for (var i = 1; i < m_Variables.Count; ++i)
            {
                if (m_Variables[i].Tooltip != firstTooltip)
                {
                    same = false;
                    break;
                }
            }

            TextField.SetValueWithoutNotify(same ? m_Variables[0].Tooltip : mixedValueString);
        }
    }
}
