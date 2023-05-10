// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A BlackboardElement to display a <see cref="VariableDeclarationModel"/>.
    /// </summary>
    class BlackboardField : BlackboardElement
    {
        /// <summary>
        /// The uss class name for this element.
        /// </summary>
        public static new readonly string ussClassName = "ge-blackboard-field";

        /// <summary>
        /// The uss class name for the capsule.
        /// </summary>
        public static readonly string capsuleUssClassName = ussClassName.WithUssElement("capsule");

        /// <summary>
        /// The uss class name for the name label.
        /// </summary>
        public static readonly string nameLabelUssClassName = ussClassName.WithUssElement("name-label");

        /// <summary>
        /// The uss class name for the icon.
        /// </summary>
        public static readonly string iconUssClassName = ussClassName.WithUssElement("icon");

        /// <summary>
        /// The uss class name for the type label.
        /// </summary>
        public static readonly string typeLabelUssClassName = ussClassName.WithUssElement("type-label");

        /// <summary>
        /// The uss class name for the highlighted modifier.
        /// </summary>
        public static readonly string highlightedModifierUssClassName = ussClassName.WithUssModifier("highlighted");

        /// <summary>
        /// The uss class name for the placeholder modifier.
        /// </summary>
        public static readonly string placeholderModifierUssClassName = ussClassName.WithUssModifier("placeholder");

        /// <summary>
        /// The uss class name for the read only modifier.
        /// </summary>
        public static readonly string readOnlyModifierUssClassName = ussClassName.WithUssModifier("read-only");

        /// <summary>
        /// The uss class name for the write only modifier.
        /// </summary>
        public static readonly string writeOnlyModifierUssClassName = ussClassName.WithUssModifier("write-only");

        /// <summary>
        /// The uss class name for the exposed modifier.
        /// </summary>
        public static readonly string iconExposedModifierUssClassName = iconUssClassName.WithUssModifier("exposed");

        /// <summary>
        /// The label containing the type name.
        /// </summary>
        protected Label m_TypeLabel;

        /// <summary>
        /// The element containing the icon.
        /// </summary>
        protected VisualElement m_Icon;

        SelectionDropper m_SelectionDropper;
        TypeHandle m_CurrentTypeHandle;

        /// <summary>
        /// The <see cref="EditableLabel"/> containing the name of the field.
        /// </summary>
        public EditableLabel NameLabel { get; protected set; }

        internal bool IsHighlighted_Internal() => ClassListContains(highlightedModifierUssClassName);

        /// <summary>
        /// The selection dropper for the element.
        /// </summary>
        protected SelectionDropper SelectionDropper
        {
            get => m_SelectionDropper;
            set => this.ReplaceManipulator(ref m_SelectionDropper, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardField"/> class.
        /// </summary>
        public BlackboardField():base(false)
        {
            SelectionDropper = new SelectionDropper();
        }

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            base.BuildElementUI();

            var capsule = new VisualElement();
            capsule.AddToClassList(capsuleUssClassName);
            Add(capsule);

            m_Icon = new Image();
            m_Icon.AddToClassList(iconUssClassName);
            m_Icon.AddStylesheet_Internal("TypeIcons.uss");
            capsule.Add(m_Icon);

            NameLabel = new EditableLabel { name = "name", EditActionName = "Rename"};
            NameLabel.AddToClassList(nameLabelUssClassName);
            capsule.Add(NameLabel);

            m_TypeLabel = new Label() { name = "type-label" };
            m_TypeLabel.AddToClassList(typeLabelUssClassName);
            Add(m_TypeLabel);

            var selectionBorder = CreateSelectionBorder();
            capsule.Add(selectionBorder);
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
                if (m_CurrentTypeHandle != variableDeclarationModel.DataType)
                {
                    RootView.TypeHandleInfos.RemoveUssClasses(Port.dataTypeClassPrefix, m_Icon, m_CurrentTypeHandle);
                    m_CurrentTypeHandle = variableDeclarationModel.DataType;
                    RootView.TypeHandleInfos.AddUssClasses(Port.dataTypeClassPrefix, m_Icon, m_CurrentTypeHandle);

                }

                if (variableDeclarationModel.GraphModel != null)
                {
                    var dataTypeName = variableDeclarationModel.DataType
                        .GetMetadata(variableDeclarationModel.GraphModel.Stencil).FriendlyName;
                    m_TypeLabel.text = dataTypeName;
                }

                NameLabel.SetValueWithoutNotify(variableDeclarationModel.DisplayTitle);
                EnableInClassList(readOnlyModifierUssClassName, (variableDeclarationModel.Modifiers & ModifierFlags.Read) != 0);
                EnableInClassList(writeOnlyModifierUssClassName, (variableDeclarationModel.Modifiers & ModifierFlags.Write) != 0);

                var highlight = BlackboardView.GraphTool.HighlighterState.GetDeclarationModelHighlighted(variableDeclarationModel);

                // Do not show highlight if selected.
                if (highlight && BlackboardView.BlackboardViewModel.SelectionState.IsSelected(variableDeclarationModel))
                {
                    highlight = false;
                }
                EnableInClassList(placeholderModifierUssClassName, variableDeclarationModel is IPlaceholder);
                EnableInClassList(highlightedModifierUssClassName, highlight);
            }
        }

        /// <inheritdoc />
        public override void ActivateRename()
        {
            NameLabel.BeginEditing();
        }
    }
}
