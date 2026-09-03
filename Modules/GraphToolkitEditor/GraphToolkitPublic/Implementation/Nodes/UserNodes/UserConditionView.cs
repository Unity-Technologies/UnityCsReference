// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor.Implementation
{
    /// <summary>
    /// The <see cref="SingleValueConditionView"/> for a user-defined <see cref="Condition"/> backed by a
    /// <see cref="UserConditionModelImp"/>. It displays the single serialized value field declared on the user
    /// <see cref="Condition"/> type and, when the condition opts in, a title label and a dropdown to select its
    /// comparison operator. When a <see cref="ConditionView{T}"/> exists for the condition type, it is built with
    /// the row and its lifecycle hooks are invoked.
    /// </summary>
    partial class UserConditionView : SingleValueConditionView, IConditionView
    {
        /// <summary>
        /// The USS class name added to the title label.
        /// </summary>
        public static readonly string titleUssClassName = ussClassName.WithUssElement("title");

        /// <summary>
        /// The USS class name added to the operator field.
        /// </summary>
        public static readonly string operatorUssClassName = ussClassName.WithUssElement("operator");

        [AutoStaticsCleanupOnCodeReload]
        static readonly UserConditionViewBuilderLookup s_BuilderLookup = new();

        UserConditionModelImp UserConditionModel => (UserConditionModelImp)Model;

        PopupField<ConditionComparison> m_OperatorField;

        readonly IUserConditionView m_UserView;

        readonly bool m_HasValueField;

        /// <inheritdoc />
        public VisualElement Root => m_Container;

        /// <inheritdoc />
        protected override bool DisplayValueField => m_UserView?.DisplayValueField ?? true;

        /// <inheritdoc />
        protected override bool DisplayTitleLabel => m_UserView?.DisplayTitleLabel ?? true;

        /// <summary>
        /// Creates a new instance of the <see cref="UserConditionView"/> class.
        /// </summary>
        /// <param name="condition">The user condition, used to look up its <see cref="ConditionView{T}"/>.</param>
        /// <param name="fieldInfo">The <see cref="FieldInfo"/> of the condition's value field.</param>
        /// <param name="displayName">The display name for the value field.</param>
        /// <param name="tooltip">An optional tooltip for the value field.</param>
        public UserConditionView(Condition condition, FieldInfo fieldInfo, string displayName, string tooltip)
            : base(fieldInfo, displayName, tooltip)
        {
            m_HasValueField = fieldInfo != null;

            try
            {
                m_UserView = s_BuilderLookup.Build(condition, this);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <inheritdoc />
        protected override void BuildUI()
        {
            base.BuildUI();

            var model = UserConditionModel;
            var condition = model.Condition;
            var displayComparisonDropdown = condition is { DisplayComparisonDropdownInternal: true };

            var displayTitleLabel = condition != null
                && (displayComparisonDropdown || (m_HasValueField && !DisplayValueField))
                && DisplayTitleLabel;

            if (displayTitleLabel)
            {
                var titleLabel = new Label(condition.DisplayNameInternal) { tooltip = condition.Tooltip };
                titleLabel.AddToClassList(titleUssClassName);
                m_Container.Insert(0, titleLabel);
            }

            if (displayComparisonDropdown)
            {
                var supportedComparisons = condition.SupportedComparisonsInternal;
                m_OperatorField = supportedComparisons is { Count: > 0 }
                    ? ConditionComparisonExtensions.CreateComparisonPopup(supportedComparisons, model, RootView)
                    : ConditionComparisonExtensions.CreateComparisonPopup(model.GetValueFieldInfo().FieldType, model, RootView);
                m_OperatorField.AddToClassList(operatorUssClassName);
                m_Container.Insert(displayTitleLabel ? 1 : 0, m_OperatorField);
            }

            if (m_UserView == null)
                return;

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);

            try
            {
                m_UserView.OnViewBuilt();
            }
            catch (Exception e)
            {
                Debug.LogException(e);

                // The failed view may have suppressed the value field; restore it so the row stays editable.
                BuildValueField();
            }
        }

        void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            try
            {
                m_UserView.OnViewAttached();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <inheritdoc />
        protected override void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            base.OnDetachedFromPanel(evt);

            if (m_UserView == null)
                return;

            try
            {
                m_UserView.OnViewDetached();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <inheritdoc />
        public override void UpdateUIFromModel(UpdateFromModelVisitor visitor)
        {
            base.UpdateUIFromModel(visitor);

            var comparison = UserConditionModel.Comparison;
            if (m_OperatorField != null && m_OperatorField.choices.Contains(comparison))
                m_OperatorField.SetValueWithoutNotify(comparison);
        }
    }

    /// <summary>
    /// Creates the view for a <see cref="UserConditionModelImp"/>. The condition's display name is its Title override
    /// when present, otherwise its <see cref="ConditionAttribute"/> title, otherwise the nicified condition type name.
    /// It labels the value field, except when the comparison dropdown is displayed, in which case the view shows it
    /// as a standalone title before the dropdown. A valueless condition renders as a label-only row showing the
    /// display name, while a model whose condition script is missing shows "Missing Condition".
    /// </summary>
    [GraphElementsExtensionMethodsCache(typeof(ModelInspectorView))]
    static class UserConditionViewFactory
    {
        public static ModelView CreateUserConditionView(this ElementBuilder elementBuilder, UserConditionModelImp model)
        {
            var condition = model.Condition;
            var displayName = condition switch
            {
                null => "Missing Condition",
                { DisplayComparisonDropdownInternal: true } => string.Empty,
                _ => condition.DisplayNameInternal
            };
            var ui = new UserConditionView(condition, model.GetValueFieldInfo(), displayName, condition?.Tooltip);
            ui.SetupBuildAndUpdate(model, elementBuilder.View, elementBuilder.Context);
            return ui;
        }
    }
}
