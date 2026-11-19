// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The view for a <see cref="VariableConditionModel"/>: a variable field, an operator and a value editor.
    /// </summary>
    internal class VariableConditionView : SingleConditionView
    {
        /// <summary>
        /// The USS class name added to this element.
        /// </summary>
        public new static readonly string ussClassName = "ge-variable-condition-view";

        /// <summary>
        /// The USS class name added to the variable field.
        /// </summary>
        public static readonly string variableUssClassName = ussClassName.WithUssElement("variable");

        /// <summary>
        /// The USS class name added to the operator field.
        /// </summary>
        public static readonly string operatorUssClassName = ussClassName.WithUssElement("operator");

        /// <summary>
        /// The USS class name added to the value field.
        /// </summary>
        public static readonly string valueUssClassName = ussClassName.WithUssElement("value");

        VariableConditionModel VariableConditionModel => (VariableConditionModel)Model;

        VariableConditionField m_VariableField;
        PopupField<ConditionComparison> m_OperatorField;
        ConstantField m_ValueField;
        TypeHandle m_BuiltType;
        bool m_FieldsBuilt;

        /// <inheritdoc />
        protected override void BuildUI()
        {
            AddToClassList(ussClassName);
            base.BuildUI();

            m_VariableField = new VariableConditionField(RootView, VariableConditionModel.GraphModel);
            m_VariableField.AddToClassList(variableUssClassName);
            m_VariableField.variableChosen += OnVariableChosen;
            m_Container.Add(m_VariableField);

            RebuildOperatorAndValue();
        }

        /// <inheritdoc />
        public override bool HasModelDependenciesChanged() => true;

        /// <inheritdoc />
        public override void AddModelDependencies()
        {
            var variable = VariableConditionModel.Variable;
            if (variable != null)
                Dependencies.AddModelDependency(variable);
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            var model = VariableConditionModel;
            m_VariableField.SetVariable(model);

            var type = model.Variable?.DataType ?? default;
            if (!m_FieldsBuilt || type != m_BuiltType)
            {
                RebuildOperatorAndValue();
            }
            else
            {
                m_OperatorField?.SetValueWithoutNotify(model.Comparison);
                m_ValueField?.UpdateDisplayedValue();
            }
        }

        void OnVariableChosen(VariableDeclarationModelBase variable)
        {
            RootView.Dispatch(new SetVariableConditionVariableCommand(VariableConditionModel, variable));
        }

        void RebuildOperatorAndValue()
        {
            m_OperatorField?.RemoveFromHierarchy();
            m_OperatorField = null;
            m_ValueField?.RemoveFromHierarchy();
            m_ValueField = null;

            var model = VariableConditionModel;
            var variable = model.Variable;
            m_BuiltType = variable?.DataType ?? default;
            m_FieldsBuilt = true;

            if (variable == null)
                return;

            m_OperatorField = ConditionComparisonExtensions.CreateComparisonPopup(
                variable.DataType.Resolve(), model, RootView);
            m_OperatorField.AddToClassList(operatorUssClassName);
            m_Container.Add(m_OperatorField);

            if (model.Value != null)
            {
                m_ValueField = new ConstantField(new[] { model.Value }, new[] { (GraphElementModel)model }, RootView);
                m_ValueField.AddToClassList(valueUssClassName);
                m_Container.Add(m_ValueField);
            }
        }
    }

    /// <summary>
    /// Creates the view for a <see cref="VariableConditionModel"/>.
    /// </summary>
    [GraphElementsExtensionMethodsCache(typeof(ModelInspectorView))]
    internal static class VariableConditionViewFactory
    {
        public static ModelView CreateVariableConditionView(this ElementBuilder elementBuilder, VariableConditionModel model)
        {
            var ui = new VariableConditionView();
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
