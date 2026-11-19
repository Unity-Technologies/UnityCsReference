// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    class VariableTypePropertyField : BaseModelPropertyField
    {
        /// <summary>
        /// The USS class name added to a <see cref="VariableTypePropertyField"/>.
        /// </summary>
        public new static readonly string ussClassName = "variable-type-property-field";

        /// <summary>
        /// The USS class name added to the label of a <see cref="VariableTypePropertyField"/>.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName.WithUssElement(labelName);

        /// <summary>
        /// The USS class name added to the label showing the type of the <see cref="VariableTypePropertyField"/>.
        /// </summary>
        public static readonly string typeNameUssClassName = ussClassName.WithUssElement("type-name");

        /// <summary>
        /// The USS class name added to the change button of a <see cref="VariableTypePropertyField"/>.
        /// </summary>
        public static readonly string changeButtonUssClassName = ussClassName.WithUssElement(changeButtonName);

        Label m_TypeName;
        Button m_ChangeButton;

        IReadOnlyList<VariableDeclarationModelBase> m_Variables;

        RootView m_RootView;

        public VariableTypePropertyField(RootView rootView, IReadOnlyList<VariableDeclarationModelBase> variables)
            : base(rootView)
        {
            m_Variables = variables;
            m_RootView = rootView;

            AddToClassList(ussClassName);
            this.AddPackageStylesheet("VariableTypePropertyField.uss");

            var label = new Label() { text = "Data Type" };
            label.AddToClassList(labelUssClassName);
            label.AddToClassList(BaseField<int>.labelUssClassName);
            label.AddToClassList(BaseModelPropertyField.labelUssClassName);
            Add(label);
            LabelElement = label;

            m_TypeName = new Label();
            m_TypeName.AddToClassList(typeNameUssClassName);
            Add(m_TypeName);

            m_ChangeButton = new Button() { text = "Change..." };
            m_ChangeButton.AddToClassList(changeButtonUssClassName);
            Add(m_ChangeButton);

            m_ChangeButton.clickable.clicked += OnClick;
        }

        void OnClick()
        {
            var pos = new Vector2(m_ChangeButton.layout.xMin, m_ChangeButton.layout.yMax);
            pos = m_ChangeButton.parent.LocalToWorld(pos);
            pos.y += 21;

            ItemLibraryService.ShowTypesForVariable(
                m_RootView,
                m_RootView.GraphTool.Preferences,
                m_Variables[0],
                pos, t => CommandTarget.Dispatch(new ChangeVariableTypeCommand(m_Variables, t))
            );
        }

        public override void UpdateDisplayedValue()
        {
            if (m_Variables.Count < 1)
                return;

            bool same = true;

            var firstType = m_Variables[0].DataType;
            for (int i = 1; i < m_Variables.Count; ++i)
            {
                if (m_Variables[i].DataType != firstType)
                {
                    same = false;
                    break;
                }
            }

            if (same)
                m_TypeName.text = firstType.FriendlyName;
            else
                m_TypeName.text = mixedValueString;
        }
    }
}
