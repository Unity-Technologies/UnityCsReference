// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{

    /// <summary>
    /// The <see cref="IViewContext"/>> for a <see cref="RootGroupConditionView"/>.
    /// </summary>
    [UnityRestricted]
    internal class RootGroupConditionViewContext : IViewContext
    {
        static RootGroupConditionViewContext s_Default = new RootGroupConditionViewContext();
        public static RootGroupConditionViewContext Default = s_Default;
        public bool Equals(IViewContext other)
        {
            return ReferenceEquals(this, other);
        }
    }

    /// <summary>
    /// The view for a <see cref="GroupConditionModel"/> That is the root condition in a transition.
    /// </summary>
    [UnityRestricted]
    internal class RootGroupConditionView : GroupConditionView
    {
        /// <summary>
        /// The USS class name added to this element.
        /// </summary>
        public new static readonly string ussClassName = "ge-root-group-condition-view";

        /// <summary>
        /// The USS class name added to the add button.
        /// </summary>
        public static readonly string addButtonUssClassName = ussClassName.WithUssElement("add-button");

        ToolbarMenu m_AddButton;

        /// <summary>
        /// Creates a new instance of <see cref="RootGroupConditionView"/>.
        /// </summary>
        /// <param name="conditionEditor">The containing <see cref="ConditionEditor"/>.</param>
        public RootGroupConditionView(ConditionEditor conditionEditor)
            : base(conditionEditor) { }

        /// <inheritdoc />
        protected override void BuildUI()
        {
            base.BuildUI();
            AddToClassList(ussClassName);

            m_AddButton = new ToolbarMenu();
            m_AddButton.AddToClassList(addButtonUssClassName);
            m_AddButton.AddToClassList(Button.ussClassName);
            m_AddButton.RemoveFromClassList(ToolbarMenu.ussClassName);
            m_TitleContainer.Add(m_AddButton);
            CreateAddDropDownMenu();

            m_EmptyLabel.text = L10n.Tr("There are no conditions in this transition.");
        }

        /// <inheritdoc />
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            // we don't want the root group condition view to add elements.
        }

        /// <inheritdoc />
        public override void UpdateIndentation(int rank)
        {
        }

        void CreateAddDropDownMenu()
        {
            var menu = m_AddButton.menu;
            var addConditionOptions = ((ModelInspectorView)RootView).ModelInspectorViewModel.GraphModelState.GraphModel.GetAddConditionOptions();
            bool first = true;
            foreach (var (name, factory) in addConditionOptions)
            {
                menu.AppendAction(name, _ =>
                {
                    var selectedItem = ConditionEditor.SelectionManager.SelectedElements.Count > 0 ? ConditionEditor.SelectionManager.SelectedElements[0] : this;
                    var selectedGroup = selectedItem.GetFirstOfType<GroupConditionView>();
                    var newCondition = factory(selectedGroup.GroupConditionModel);

                    int insertIndex = -1;
                    if (selectedItem != selectedGroup)
                    {
                        insertIndex = selectedItem.ConditionModel.IndexInParent + 1;
                    }

                    RootView.Dispatch(new AddConditionCommand(selectedGroup.GroupConditionModel, newCondition, insertIndex));
                });

                if (first && addConditionOptions.Count > 1)
                {
                    menu.AppendSeparator();
                    first = false;
                }
            }
        }
    }


}
