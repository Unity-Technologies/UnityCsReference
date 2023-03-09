// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A BlackboardElement to display the properties of a <see cref="VariableDeclarationModel"/>.
    /// </summary>
    class BlackboardVariablePropertyView : BlackboardElement
    {
        public static new readonly string ussClassName = "ge-blackboard-variable-property-view";
        public static readonly string rowUssClassName = ussClassName.WithUssElement("row");
        public static readonly string rowLabelUssClassName = ussClassName.WithUssElement("row-label");
        public static readonly string rowControlUssClassName = ussClassName.WithUssElement("row-control");

        public static readonly string rowTypeSelectorUssClassName = ussClassName.WithUssElement("row-type-selector");
        public static readonly string rowExposedUssClassName = ussClassName.WithUssElement("row-exposed");
        public static readonly string rowTooltipUssClassName = ussClassName.WithUssElement("row-tooltip");
        public static readonly string rowInitValueUssClassName = ussClassName.WithUssElement("row-init-value");

        protected VisualElement m_ValueEditor;
        protected Button m_TypeSelectorButton;
        protected Toggle m_ExposedToggle;
        protected TextField m_TooltipTextField;

        Constant m_CurrentConstant;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardVariablePropertyView"/> class.
        /// </summary>
        public BlackboardVariablePropertyView()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            base.BuildElementUI();
            BuildRows();
        }

        protected virtual void BuildRows()
        {
            AddTypeSelector();
            AddExposedToggle();
            AddInitializationField();
            AddTooltipField();
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            if (Model is VariableDeclarationModel variableDeclarationModel)
            {
                if (m_ValueEditor is BaseModelPropertyField baseModelPropertyField)
                {
                    if (m_CurrentConstant != variableDeclarationModel.InitializationModel)
                        ChangeInitializationFieldEditor();
                    baseModelPropertyField.UpdateDisplayedValue();
                }

                if (m_TypeSelectorButton != null)
                    m_TypeSelectorButton.text = GetTypeDisplayText();

                m_ExposedToggle?.SetValueWithoutNotify(variableDeclarationModel.IsExposed);
                m_TooltipTextField?.SetValueWithoutNotify(variableDeclarationModel.Tooltip);
            }
        }

        protected string GetTypeDisplayText()
        {
            if (Model is VariableDeclarationModel variableDeclarationModel)
            {
                var stencil = RootView.GraphTool.ToolState.GraphModel.Stencil;
                return variableDeclarationModel.DataType.GetMetadata(stencil).FriendlyName;
            }

            return "";
        }

        protected static VisualElement MakeRow(string labelText, VisualElement control, string newRowUssClassName)
        {
            var row = new VisualElement { name = "blackboard-variable-property-view-row" };
            row.AddToClassList(rowUssClassName);
            if (!string.IsNullOrEmpty(newRowUssClassName))
                row.AddToClassList(newRowUssClassName);

            // TODO: Replace this with a variable pill/token and set isExposed appropriately
            var label = new Label(labelText);
            label.AddToClassList(rowLabelUssClassName);
            row.Add(label);

            if (control != null)
            {
                control.AddToClassList(rowControlUssClassName);
                row.Add(control);
            }

            return row;
        }

        protected void AddRow(string labelText, VisualElement control, string newRowUssClassName)
        {
            var row = MakeRow(labelText, control, newRowUssClassName);
            Add(row);
        }

        protected void InsertRow(int index, string labelText, VisualElement control, string newRowUssClassName)
        {
            var row = MakeRow(labelText, control, newRowUssClassName);
            Insert(index, row);
        }

        protected void AddTypeSelector()
        {
            void OnClick()
            {
                var pos = new Vector2(m_TypeSelectorButton.layout.xMin, m_TypeSelectorButton.layout.yMax);
                pos = m_TypeSelectorButton.parent.LocalToWorld(pos);
                pos.y += 21;

                ItemLibraryService.ShowVariableTypes(
                    (Stencil)GraphElementModel.GraphModel.Stencil,
                    RootView.GraphTool.Preferences,
                    pos,
                    (t, _) => OnTypeChanged(t)
                );
            }

            m_TypeSelectorButton = new Button(OnClick) { text = GetTypeDisplayText() };
            AddRow("Type", m_TypeSelectorButton, rowTypeSelectorUssClassName);
        }

        protected void AddExposedToggle()
        {
            if (Model is VariableDeclarationModel variableDeclarationModel)
            {
                m_ExposedToggle = new Toggle { value = variableDeclarationModel.IsExposed };
                m_ExposedToggle.RegisterValueChangedCallback(OnExposedChanged);
                AddRow("Exposed", m_ExposedToggle, rowExposedUssClassName);
            }
        }

        protected void AddTooltipField()
        {
            if (Model is VariableDeclarationModel variableDeclarationModel)
            {
                m_TooltipTextField = new TextField
                {
                    isDelayed = true,
                    value = variableDeclarationModel.Tooltip
                };
                AddRow("Tooltip", m_TooltipTextField, rowTooltipUssClassName);
            }
        }

        protected void AddInitializationField()
        {
            if (Model is VariableDeclarationModel variableDeclarationModel)
            {
                if (variableDeclarationModel.InitializationModel == null)
                {
                    var stencil = (Stencil)RootView.GraphTool.ToolState.GraphModel.Stencil;
                    if (stencil.RequiresInitialization(variableDeclarationModel))
                    {
                        m_ValueEditor = new Button(OnInitializationButton) { text = "Create Init value" };
                    }
                }
                else
                {
                    m_ValueEditor = InlineValueEditor.CreateEditorForConstants(RootView, new[] { variableDeclarationModel },
                        new[] { variableDeclarationModel.InitializationModel }, false);
                    m_CurrentConstant = variableDeclarationModel.InitializationModel;
                }
            }

            if (m_ValueEditor != null)
                AddRow("Initialization", m_ValueEditor, rowInitValueUssClassName);
        }

        protected virtual void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_ExposedToggle?.UnregisterValueChangedCallback(OnExposedChanged);
            m_TooltipTextField.UnregisterValueChangedCallback(OnTooltipChanged);
        }

        protected virtual void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_ExposedToggle?.RegisterValueChangedCallback(OnExposedChanged);
            m_TooltipTextField.RegisterValueChangedCallback(OnTooltipChanged);
        }

        protected void OnInitializationButton()
        {
            BlackboardView.Dispatch(new InitializeVariableCommand(Model as VariableDeclarationModel));
        }

        protected void OnTypeChanged(TypeHandle handle)
        {
            BlackboardView.Dispatch(new ChangeVariableTypeCommand(Model as VariableDeclarationModel, handle));
        }

        protected void OnExposedChanged(ChangeEvent<bool> evt)
        {
            BlackboardView.Dispatch(new ExposeVariableCommand(m_ExposedToggle.value, Model as VariableDeclarationModel));
        }

        protected void OnTooltipChanged(ChangeEvent<string> evt)
        {
            BlackboardView.Dispatch(new UpdateTooltipCommand(m_TooltipTextField.value, Model as VariableDeclarationModel));
        }

        void ChangeInitializationFieldEditor()
        {
            var rowInitIndex = -1;
            var children = Children().ToArray();
            for (var i = 0; i < childCount; i++)
            {
                if (children[i].ClassListContains(rowInitValueUssClassName))
                    rowInitIndex = i;
            }

            if (rowInitIndex != -1 && Model is VariableDeclarationModel variableDeclarationModel)
            {
                RemoveAt(rowInitIndex);

                m_ValueEditor = InlineValueEditor.CreateEditorForConstants(RootView, new[] { variableDeclarationModel },
                    new[] { variableDeclarationModel.InitializationModel }, false);

                if (m_ValueEditor != null)
                    InsertRow(rowInitIndex, "Initialization", m_ValueEditor, rowInitValueUssClassName);

                m_CurrentConstant = variableDeclarationModel.InitializationModel;
            }
        }
    }
}
