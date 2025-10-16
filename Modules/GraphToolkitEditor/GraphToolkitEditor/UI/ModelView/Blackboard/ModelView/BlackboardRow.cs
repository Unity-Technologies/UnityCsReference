// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A BlackboardElement to display a <see cref="VariableDeclarationModelBase"/> as a collapsible row in the blackboard.
    /// </summary>
    [UnityRestricted]
    internal class BlackboardRow : BlackboardElement
    {
        public new static readonly string ussClassName = "ge-blackboard-row";
        public static readonly string headerUssClassName = ussClassName.WithUssElement(GraphElementHelper.headerName);
        public static readonly string headerContainerUssClassName = ussClassName.WithUssElement("header-container");
        public static readonly string propertyViewUssClassName = ussClassName.WithUssElement("property-view-container");

        public static readonly string rowFieldPartName = "blackboard-row-field-part";

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
        }

        /// <inheritdoc />
        protected override void BuildUI()
        {
            base.BuildUI();

            var header = new VisualElement { name = "row-header" };
            header.AddToClassList(headerUssClassName);

            m_HeaderContainer = new VisualElement { name = "row-header-container" };
            m_HeaderContainer.AddToClassList(headerContainerUssClassName);
            header.Add(m_HeaderContainer);

            Add(header);

            m_PropertyViewContainer = new VisualElement { name = "property-view-container" };
            m_PropertyViewContainer.AddToClassList(propertyViewUssClassName);
            Add(m_PropertyViewContainer);

            pickingMode = PickingMode.Ignore;
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
        }

        void OnShowItemLibrary(PromptItemLibraryEvent e)
        {
            if (!(Model is VariableDeclarationModel vdm))
            {
                return;
            }

            ItemLibraryService.ShowTypesForVariable(
                RootView,
                RootView.GraphTool.Preferences,
                vdm,
                e.MenuPosition,
                (t) =>
                {
                    BlackboardView.Dispatch(new ChangeVariableTypeCommand(vdm, t));
                });

            e.StopPropagation();
        }

        public override bool HandlePasteOperation(PasteOperation operation, string operationName, Vector2 delta, CopyPasteData copyPasteData)
        {
            if (copyPasteData.HasVariableContent() && Model is VariableDeclarationModelBase variableDeclarationModel && variableDeclarationModel.ParentGroup is GroupModel parentGm)
            {
                BlackboardView.Dispatch(new PasteDataCommand(operation, operationName, delta, copyPasteData, parentGm));
                return true;
            }

            return false;
        }
    }
}
