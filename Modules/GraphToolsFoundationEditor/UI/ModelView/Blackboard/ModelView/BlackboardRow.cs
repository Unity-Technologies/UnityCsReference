// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A BlackboardElement to display a <see cref="VariableDeclarationModel"/> as a collapsible row in the blackboard.
    /// </summary>
    class BlackboardRow : BlackboardElement
    {
        public static new readonly string ussClassName = "ge-blackboard-row";
        public static readonly string headerUssClassName = ussClassName.WithUssElement("header");
        public static readonly string headerContainerUssClassName = ussClassName.WithUssElement("header-container");
        public static readonly string collapseButtonUssClassName = ussClassName.WithUssElement("collapse-button");
        public static readonly string propertyViewUssClassName = ussClassName.WithUssElement("property-view-container");
        public static readonly string expandedModifierUssClassName = ussClassName.WithUssModifier("collapsed");

        public static readonly string rowFieldPartName = "blackboard-row-field-part";
        public static readonly string rowPropertiesPartName = "blackboard-row-properties-part";

        protected VisualElement m_HeaderContainer;
        protected VisualElement m_PropertyViewContainer;
        protected CollapseButton m_CollapseButton;

        public VisualElement FieldSlot => m_HeaderContainer;
        public VisualElement PropertiesSlot => m_PropertyViewContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardRow"/> class.
        /// </summary>
        public BlackboardRow()
        {
            RegisterCallback<PromptItemLibraryEvent>(OnShowItemLibrary);
        }

        /// <inheritdoc />
        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.AppendPart(BlackboardVariablePart.Create(rowFieldPartName, Model, this, ussClassName));
            PartList.AppendPart(BlackboardVariablePropertiesPart.Create(rowPropertiesPartName, Model, this, ussClassName));
        }

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            base.BuildElementUI();

            var header = new VisualElement { name = "row-header" };
            header.AddToClassList(headerUssClassName);

            m_CollapseButton = new CollapseButton();
            m_CollapseButton.AddToClassList(collapseButtonUssClassName);
            header.Add(m_CollapseButton);

            m_HeaderContainer = new VisualElement { name = "row-header-container" };
            m_HeaderContainer.AddToClassList(headerContainerUssClassName);
            header.Add(m_HeaderContainer);

            Add(header);

            m_PropertyViewContainer = new VisualElement { name = "property-view-container" };
            m_PropertyViewContainer.AddToClassList(propertyViewUssClassName);
            Add(m_PropertyViewContainer);

            m_CollapseButton.RegisterCallback<ChangeEvent<bool>>(OnCollapseButtonChange);
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

            if (Model is VariableDeclarationModel vdm)
            {
                bool isExpanded = BlackboardView.BlackboardViewModel.ViewState?.GetVariableDeclarationModelExpanded(vdm) ?? false;

                EnableInClassList(expandedModifierUssClassName, !isExpanded);
                m_CollapseButton.SetValueWithoutNotify(!isExpanded);
            }
        }

        protected void OnCollapseButtonChange(ChangeEvent<bool> e)
        {
            if (Model is VariableDeclarationModel vdm)
            {
                RootView.Dispatch(new ExpandVariableDeclarationCommand_Internal(vdm, !e.newValue));
            }
        }

        void OnShowItemLibrary(PromptItemLibraryEvent e)
        {
            if (!(Model is VariableDeclarationModel vdm))
            {
                return;
            }

            ItemLibraryService.ShowVariableTypes(
                (Stencil)GraphElementModel.GraphModel.Stencil,
                RootView.GraphTool.Preferences,
                e.MenuPosition,
                (t, _) =>
                {
                    BlackboardView.Dispatch(new ChangeVariableTypeCommand(vdm, t));
                });

            e.StopPropagation();
        }
        public override bool HandlePasteOperation(PasteOperation operation, string operationName, Vector2 delta, CopyPasteData copyPasteData)
        {
            if (copyPasteData.HasVariableContent_Internal() && Model is VariableDeclarationModel variableDeclarationModel)
            {
                BlackboardView.Dispatch(new PasteSerializedDataCommand(operation, operationName, delta, copyPasteData, variableDeclarationModel.ParentGroup));
                return true;
            }

            return false;
        }
    }
}
