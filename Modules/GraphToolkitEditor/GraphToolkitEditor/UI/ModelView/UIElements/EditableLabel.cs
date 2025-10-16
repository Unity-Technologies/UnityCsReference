// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A label that changes to a text field when double-clicked.
    /// </summary>
    [UnityRestricted]
    internal class EditableLabel : VisualElement
    {
        /// <summary>
        /// The USS class name added to a <see cref="EditableLabel"/>.
        /// </summary>
        public static readonly string ussClassName = "ge-editable-label";

        /// <summary>
        /// The name of a label field.
        /// </summary>
        public static readonly string labelName = GraphElementHelper.labelName;

        /// <summary>
        /// The name of a text field.
        /// </summary>
        public static readonly string textFieldName = "text-field";

        /// <summary>
        /// The USS class name added when hover is enabled.
        /// </summary>
        public static readonly string hoverEnabledUssClassName = ussClassName.WithUssModifier("hover-enabled");

        protected Label m_Label;

        protected TextField m_TextField;

        protected string m_CurrentValue;

        protected bool m_ClickToToEditDisabled;

        ContextualMenuManipulator m_ContextualMenuManipulator;

        private const int k_RenamingDelayMs = 500;
        IVisualElementScheduledItem m_ScheduledItem;

        protected ContextualMenuManipulator ContextualMenuManipulator
        {
            get => m_ContextualMenuManipulator;
            set => this.ReplaceManipulator(ref m_ContextualMenuManipulator, value);
        }

        public bool Multiline
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
        /// Set this to true to disable editing by clicking or double clicking on the label.
        /// </summary>
        public bool ClickToEditDisabled
        {
            get => m_ClickToToEditDisabled;
            set
            {
                m_ClickToToEditDisabled = value;
                EnableInClassList(hoverEnabledUssClassName, !m_ClickToToEditDisabled);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EditableLabel"/> class.
        /// </summary>
        public EditableLabel()
        {
            focusable = true;

            AddToClassList(ussClassName);

            m_Label = new Label() { name = GraphElementHelper.labelName };
            Add(m_Label);

            m_TextField = new TextField() { name = textFieldName };
            Add(m_TextField);

            this.AddPackageStylesheet("EditableLabel.uss");

            m_Label.RegisterCallback<PointerDownEvent>(OnLabelPointerDown);
            m_Label.RegisterCallback<PointerUpEvent>(OnLabelPointerUpEvent);

            m_TextField.style.display = DisplayStyle.None;
            m_TextField.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            m_TextField.RegisterCallback<FocusOutEvent>(OnFieldFocusOut);
            m_TextField.RegisterCallback<ChangeEvent<string>>(OnChange);
            m_TextField.isDelayed = true;

            ClickToEditDisabled = false;

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
        { }

        /// <summary>
        /// Starts text editing: the text label is replaced by a text field.
        /// </summary>
        public void BeginEditing()
        {
            m_CurrentValue = m_Label.text;

            m_Label.style.display = DisplayStyle.None;
            m_TextField.style.display = DisplayStyle.Flex;
            m_TextField.selectAllOnMouseUp = false;
            m_TextField.SafeQ(TextField.textInputUssName).Focus();
        }


        bool m_FirstClick;

        void OnLabelPointerDown(PointerDownEvent evt)
        {
            if (ClickToEditDisabled)
                return;

            if (evt.target == evt.currentTarget)
            {
                if (evt.button == (int)MouseButton.LeftMouse)
                {
                    if (evt.clickCount < 2 && ShouldPreventHandlingNextPointerUp())
                    {
                        m_FirstClick = true;
                    }
                    else if (evt.clickCount == 2)
                    {
                        BeginEditing();
                        evt.StopPropagation();
                        focusController.IgnoreEvent(evt);
                    }
                }
            }
        }

        /// <summary>
        /// The EditableLabel can switch to Edit Mode when the user double clicks on it or the user single clicks and the following is true :
        /// - The EditableLabel is focused
        ///   OR
        /// - The EditableLabel is inside a GraphElement that is selected
        /// ShouldPreventHandlingNextPointerUp returns false if either of those conditions are met in order to prevent handling the next pointer up, thus preventing entering edit mode.
        /// </summary>
        /// <returns>True if the next pointer up should be ignored, otherwise false.</returns>
        bool ShouldPreventHandlingNextPointerUp()
        {
            if (panel.focusController.focusedElement != this)
            {
                var parentElement = GetFirstAncestorOfType<ModelView>();
                return parentElement switch
                {
                    GraphElement graphElement => !graphElement.IsSelected(),
                    BlackboardElement blackboardElement => !blackboardElement.IsSelected(),
                    _ => true
                };
            }

            return false;
        }

        void OnLabelPointerUpEvent(PointerUpEvent evt)
        {
            if (ClickToEditDisabled)
                return;

            m_ScheduledItem?.Pause();
            if (evt.button != (int)MouseButton.LeftMouse)
                return;

            if (m_FirstClick)
            {
                m_FirstClick = false;
                return;
            }

            if (evt.clickCount == 1)
            {
                m_ScheduledItem = schedule.Execute(() =>
                {
                    BeginEditing();
                    m_ScheduledItem = null;
                }).StartingIn(k_RenamingDelayMs);
            }
        }

        void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode == KeyCode.Escape)
            {
                m_TextField.SetValueWithoutNotify(m_CurrentValue);
                m_TextField.Blur();
                schedule.Execute(Focus).StartingIn(0);
            }
            else if (e.keyCode == KeyCode.Return)
            {
                Focus();
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
