// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.GraphToolkit.Editor.ContextualMenuItems;
using Unity.GraphToolkit.InternalBridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// The view for a condition.
    /// </summary>
    [UnityRestricted]
    internal abstract class ConditionView : ModelView, ISelectableElement
    {
        /// <summary>
        /// The USS class name added to a <see cref="ConditionView"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-condition-view";

        /// <summary>
        /// The USS class added to the condition when it is selected.
        /// </summary>
        public static readonly string selectedUssClassName = ussClassName.WithUssModifier(GraphElementHelper.selectedUssModifier);

        /// <summary>
        /// The USS class added to the icon.
        /// </summary>
        public static readonly string iconUssClassName = ussClassName.WithUssElement(GraphElementHelper.iconName);

        /// <summary>
        /// The USS class added to the drag handle.
        /// </summary>
        public static readonly string dragHandleUssClassName = ussClassName.WithUssElement("drag-handle");

        /// <summary>
        /// The icon for the condition.
        /// </summary>
        protected Image m_Icon;

        /// <summary>
        /// The size of one indentation.
        /// </summary>
        protected const float k_IndentationWidth = 16;

        bool m_IsSelected;

        bool ISelectableElement.IsSelected
        {
            get => m_IsSelected;
            set
            {
                if (m_IsSelected == value)
                    return;
                m_IsSelected = value;
                if (m_IsSelected)
                    AddToClassList(selectedUssClassName);
                else
                    RemoveFromClassList(selectedUssClassName);
            }
        }

        /// <summary>
        /// The indentation spacer element for the condition.
        /// </summary>
        protected virtual VisualElement IndentationSpacer { get; }

        /// <summary>
        /// Whether this <see cref="ConditionView"/> is selected.
        /// </summary>
        public bool IsSelected => m_IsSelected;

        /// <summary>
        /// The <see cref="ConditionModel"/> displayed by this view.
        /// </summary>
        public ConditionModel ConditionModel => (ConditionModel)Model;

        /// <summary>
        /// The <see cref="TransitionSupportModel"/> in which this condition is.
        /// </summary>
        public TransitionSupportModel TransitionSupportModel { get; set; }

        /// <summary>
        /// The <see cref="TransitionModel"/> in which this condition is.
        /// </summary>
        public TransitionModel TransitionModel { get; set; }

        /// <summary>
        /// Updates the visual indentation base on the rank specified.
        /// </summary>
        /// <param name="rank">The indentation rank.</param>
        public virtual void UpdateIndentation(int rank)
        {
            if (IndentationSpacer != null)
                IndentationSpacer.style.width = k_IndentationWidth * rank;
        }

        /// <inheritdoc />
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (!IsCorrectTarget(evt.target as VisualElement))
                return;

            var menuItems = GetContextualMenuItems();
            if (menuItems == null)
                return; // Selected: defer to ConditionEditor, which builds a selection-aware menu.

            foreach (var menuItem in menuItems)
                evt.menu.AppendAction(menuItem.Item1, menuItem.Item2);

            ContextualMenuUserEntries.AppendConditionEntries(evt, TransitionSupportModel, TransitionModel, ConditionModel);

            evt.StopPropagation();
        }

        /// <summary>
        /// Whether this view is the <see cref="ConditionView"/> a contextual menu event targets.
        /// </summary>
        /// <param name="target">The element the event targets.</param>
        /// <returns><c>true</c> when <paramref name="target"/> is null or sits inside this view.</returns>
        /// <remarks>
        /// The target can be an enclosing group or, when a value field or colour swatch handles the
        /// pointer itself, a child of the row. Only the row the cursor is actually over may populate
        /// the menu; an ancestor would add items acting on the wrong <see cref="ConditionModel"/>.
        /// </remarks>
        internal bool IsCorrectTarget(VisualElement target)
        {
            return target == null || target.GetFirstOfType<ConditionView>() == this;
        }

        /// <summary>
        /// The menu items this view contributes, as label and action pairs, or <c>null</c> when it
        /// contributes none.
        /// </summary>
        /// <remarks>
        /// Returned rather than appended so a test can execute an item without displaying a menu: a
        /// real contextual menu never closes in a test and hangs the run on Windows.
        /// </remarks>
        internal List<Tuple<string, Action<DropdownMenuAction>>> GetContextualMenuItems()
        {
            if (IsSelected)
                return null;

            return new List<Tuple<string, Action<DropdownMenuAction>>>
            {
                new(CommandMenuItemNames.Delete,
                    _ => RootView.Dispatch(new DeleteConditionsCommand(new[] { ConditionModel }))),
                new(CommandMenuItemNames.Duplicate,
                    _ => RootView.Dispatch(new DuplicateConditionsCommand(new[] { ConditionModel })))
            };
        }

        /// <inheritdoc />
        protected override void BuildUI()
        {
            m_Icon = new Image();
            m_Icon.AddToClassList(iconUssClassName);
            Add(m_Icon);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            IndentationSpacer?.AddToClassList(ussClassName.WithUssElement("indentation-spacer"));
            base.PostBuildUI();
            AddToClassList(ussClassName);
        }

        internal void CallBuildContextualMenuForTests(ContextualMenuPopulateEvent evt) => BuildContextualMenu(evt);
    }
}
