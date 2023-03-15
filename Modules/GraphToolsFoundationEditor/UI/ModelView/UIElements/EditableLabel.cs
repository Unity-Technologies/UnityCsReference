// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// A label that changes to a text field when double-clicked.
    /// </summary>
    class EditableLabel : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<EditableLabel, UxmlTraits> {}
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlBoolAttributeDescription m_Multiline = new UxmlBoolAttributeDescription { name = "multiline" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                EditableLabel field = ((EditableLabel)ve);
                field.multiline = m_Multiline.GetValueFromBag(bag, cc);
                base.Init(ve, bag, cc);
            }
        }

        public static readonly string labelName = "label";
        public static readonly string textFieldName = "text-field";

        protected Label m_Label;

        protected TextField m_TextField;

        protected string m_CurrentValue;

        ContextualMenuManipulator m_ContextualMenuManipulator;

        protected ContextualMenuManipulator ContextualMenuManipulator
        {
            get => m_ContextualMenuManipulator;
            set => this.ReplaceManipulator(ref m_ContextualMenuManipulator, value);
        }

        public bool multiline
        {
            set => m_TextField.multiline = value;
        }

        /// <summary>
        /// The name of the contextual menu action to edit the text.
        /// </summary>
        public string EditActionName { get; set; } = "Edit";

        /// <summary>
        /// Whether the field is in edit mode, displaying a text field, or in static mode, displaying a label.
        /// </summary>
        public bool IsInEditMode => m_TextField.style.display != DisplayStyle.None;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditableLabel"/> class.
        /// </summary>
        public EditableLabel()
        {
            isCompositeRoot = true;
            focusable = true;

            GraphElementHelper_Internal.LoadTemplateAndStylesheet_Internal(this, "EditableLabel", "ge-editable-label");
            m_Label = this.SafeQ<Label>(name: labelName);
            m_TextField = this.SafeQ<TextField>(name: textFieldName);

            m_Label.RegisterCallback<MouseDownEvent>(OnLabelMouseDown);

            m_TextField.style.display = DisplayStyle.None;
            m_TextField.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            m_TextField.RegisterCallback<FocusOutEvent>(OnFieldFocusOut);
            m_TextField.RegisterCallback<ChangeEvent<string>>(OnChange);
            m_TextField.isDelayed = true;

            ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);
        }

        /// <summary>
        /// Set the value of the label without sending a ChangeEvent.
        /// </summary>
        /// <param name="value">The new value.</param>
        /// <param name="force">If false and the user is currently editing the field, the value will not be set.</param>
        public void SetValueWithoutNotify(string value, bool force = false)
        {
            if (force || !IsInEditMode)
            {
                ((INotifyValueChanged<string>)m_Label).SetValueWithoutNotify(value);
                m_TextField.SetValueWithoutNotify(value);
            }
        }

        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            if (!IsInEditMode)
            {
                evt.menu.AppendAction(EditActionName, _ => BeginEditing());
            }
        }

        /// <summary>
        /// Starts text editing: the text label is replaced by a text field.
        /// </summary>
        public void BeginEditing()
        {
            m_CurrentValue = m_Label.text;

            m_Label.style.display = DisplayStyle.None;
            m_TextField.style.display = DisplayStyle.Flex;

            m_TextField.SafeQ(TextField.textInputUssName).Focus();
            m_TextField.SelectAll();
        }

        protected void OnLabelMouseDown(MouseDownEvent e)
        {
            if (e.target == e.currentTarget)
            {
                if (e.button == (int)MouseButton.LeftMouse)
                {
                    if (e.clickCount == 2)
                    {
                        BeginEditing();

                        e.StopPropagation();
                        focusController.IgnoreEvent(e);
                    }
                }
            }
        }

        protected void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.Escape)
            {
                m_TextField.SetValueWithoutNotify(m_CurrentValue);
                m_TextField.Blur();
            }
        }

        protected void OnFieldFocusOut(FocusOutEvent e)
        {
            m_Label.style.display = DisplayStyle.Flex;
            m_TextField.style.display = DisplayStyle.None;
        }

        protected void OnChange(ChangeEvent<string> e)
        {
            if (e.target == e.currentTarget)
                ((INotifyValueChanged<string>)m_Label).SetValueWithoutNotify(m_TextField.value);
        }
    }
}
