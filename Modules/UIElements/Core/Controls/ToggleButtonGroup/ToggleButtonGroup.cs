// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A control that allows single or multiple selection out of a logical group of <see cref="Button"/> elements.
    /// </summary>
    /// <remarks>
    /// The ToggleButtonGroup has a label and a group of interactable <see cref="Button"/> elements.
    ///
    /// To create buttons, add <see cref="Button"/> elements directly to the ToggleButtonGroup. This will automatically
    /// style and configure the button to work properly.
    /// </remarks>
    [UxmlElement(null, typeof(Button), libraryPath = "Controls")]
    [Icon("UIToolkit/Icons/ToggleButtonGroup.png")]
    public partial class ToggleButtonGroup : BaseField<ToggleButtonGroupState>
    {
        private static readonly string k_MaxToggleButtonGroupMessage = $"The number of buttons added to ToggleButtonGroup exceeds the maximum allowed ({ToggleButtonGroupState.maxLength}). The newly added button will not be treated as part of this control.";

        internal static readonly BindingId isMultipleSelectionProperty = nameof(isMultipleSelection);
        internal static readonly BindingId allowEmptySelectionProperty = nameof(allowEmptySelection);

        /// <summary>
        /// USS class name of elements for this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-toggle-button-group";
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        // TO-DO : Fix this - it's only being used as a name
        /// <summary>
        /// USS class name of container element of this type.
        /// </summary>
        public static readonly string containerUssClassName = ussClassName + "__container";
        internal static readonly UniqueStyleString containerUssClassNameUnique = new(containerUssClassName);

        /// <summary>
        /// USS class name of container element of this type.
        /// </summary>
        public static readonly string buttonGroupClassName = "unity-button-group";
        internal static readonly UniqueStyleString buttonGroupClassNameUnique = new(buttonGroupClassName);

        /// <summary>
        /// USS class name for any Buttons in the group.
        /// </summary>
        public static readonly string buttonClassName = buttonGroupClassName + "__button";
        internal static readonly UniqueStyleString buttonClassNameUnique = new(buttonClassName);

        /// <summary>
        /// USS class name for the leftmost Button in the group.
        /// </summary>
        public static readonly string buttonLeftClassName = buttonClassName + "--left";
        internal static readonly UniqueStyleString buttonLeftClassNameUnique = new(buttonLeftClassName);

        /// <summary>
        /// USS class name for any Buttons in the middle of the group.
        /// </summary>
        public static readonly string buttonMidClassName = buttonClassName + "--mid";
        internal static readonly UniqueStyleString buttonMidClassNameUnique = new(buttonMidClassName);

        /// <summary>
        /// USS class name for the rightmost Button in the group.
        /// </summary>
        public static readonly string buttonRightClassName = buttonClassName + "--right";
        internal static readonly UniqueStyleString buttonRightClassNameUnique = new(buttonRightClassName);

        /// <summary>
        /// USS class name for the Button if only one is available in the group.
        /// </summary>
        public static readonly string buttonStandaloneClassName = buttonClassName + "--standalone";
        internal static readonly UniqueStyleString buttonStandaloneClassNameUnique = new(buttonStandaloneClassName);

        /// <summary>
        /// USS class name for empty state label.
        /// </summary>
        public static readonly string emptyStateLabelClassName = buttonGroupClassName + "__empty-label";
        internal static readonly UniqueStyleString emptyStateLabelClassNameUnique = new(emptyStateLabelClassName);

        // Main container that will hold the group of buttons. This is what we set as the visualInput element.
        VisualElement m_ButtonGroupContainer;

        // Hold a list of available buttons on the group.
        List<Button> m_Buttons = new();

        // Used for the empty state.
        VisualElement m_EmptyLabel;
        const string k_EmptyStateLabel = "Group has no buttons.";

        private bool m_IsMultipleSelection;
        private bool m_AllowEmptySelection;

        bool m_AcceptClicksIfDisabled;

        /// <summary>
        /// Allow the buttons to accept click events when the elements are disabled.
        /// </summary>
        internal bool acceptClicksIfDisabled
        {
            get => m_AcceptClicksIfDisabled;
            set
            {
                if (m_AcceptClicksIfDisabled == value)
                    return;

                m_AcceptClicksIfDisabled = value;

                // In the events that this property is set after the buttons were created.
                foreach (var button in m_Buttons)
                {
                    button.clickable.acceptClicksIfDisabled = value;
                }
            }
        }

        /// <summary>
        /// Whether all buttons can be selected.
        /// </summary>
        [CreateProperty]
        [UxmlAttribute]
        public bool isMultipleSelection
        {
            get => m_IsMultipleSelection;
            set
            {
                if (m_IsMultipleSelection == value)
                    return;

                var toggleButtonGroupState = this.value;
                var selected = toggleButtonGroupState.GetActiveOptions(stackalloc int[toggleButtonGroupState.length]);
                if (selected.Length > 1 && m_Buttons.Count > 0)
                {
                    // Clear additional selected buttons and assign the first available to be selected
                    toggleButtonGroupState.ResetAllOptions();
                    toggleButtonGroupState[selected[0]] = true;

                    SetValueWithoutNotify(toggleButtonGroupState);
                }

                m_IsMultipleSelection = value;
                NotifyPropertyChanged(isMultipleSelectionProperty);
            }
        }

        /// <summary>
        /// Allows having all buttons to be unchecked when set to true.
        /// </summary>
        /// <remarks>
        /// When the property value is false, the control will automatically set the first available button to checked.
        /// </remarks>
        [CreateProperty]
        [UxmlAttribute]
        public bool allowEmptySelection
        {
            get => m_AllowEmptySelection;
            set
            {
                if (m_AllowEmptySelection == value)
                    return;

                // Select the first button if empty selection is not allowed
                if (!value)
                {
                    var toggleButtonGroupState = this.value;
                    var selected = toggleButtonGroupState.GetActiveOptions(stackalloc int[toggleButtonGroupState.length]);
                    if (selected.Length == 0 && m_Buttons.Count > 0)
                    {
                        toggleButtonGroupState[0] = true;
                        SetValueWithoutNotify(toggleButtonGroupState);
                    }
                }

                m_AllowEmptySelection = value;
                NotifyPropertyChanged(allowEmptySelectionProperty);
            }
        }

        /// <summary>
        /// Constructs a ToggleButtonGroup.
        /// </summary>
        public ToggleButtonGroup() : this(null) {}

        /// <summary>
        /// Constructs a ToggleButtonGroup.
        /// </summary>
        /// <param name="label">The text used as a label.</param>
        public ToggleButtonGroup(string label) : this(label, new ToggleButtonGroupState(0, ToggleButtonGroupState.maxLength)) { }

        /// <summary>
        /// Constructs a ToggleButtonGroup.
        /// </summary>
        /// <param name="toggleButtonGroupState">The ToggleButtonGroupState to be used by this control.</param>
        public ToggleButtonGroup(ToggleButtonGroupState toggleButtonGroupState) : this(null, toggleButtonGroupState) { }

        /// <summary>
        /// Constructs a ToggleButtonGroup.
        /// </summary>
        /// <param name="label">The text used as a label.</param>
        /// <param name="toggleButtonGroupState">The ToggleButtonGroupState to be used by this control.</param>
        public ToggleButtonGroup(string label, ToggleButtonGroupState toggleButtonGroupState)
            : base(label)
        {
            AddToClassList(ussClassNameUnique);
            visualInput = new ButtonGroupContainer(this) { name = containerUssClassName, delegatesFocus = true}.WithClassList(buttonGroupClassNameUnique);
            m_ButtonGroupContainer = visualInput;

            SetValueWithoutNotify(toggleButtonGroupState);
        }

        class ButtonGroupContainer : VisualElement
        {
            private readonly ToggleButtonGroup m_Group;
            public ButtonGroupContainer(ToggleButtonGroup group) { m_Group = group; }

            // Note: We are changing the workflow through these series of callback. The desired workflow is when a user
            //       adds a new button, we would take the button and apply the necessary style and give it the designed
            //       functionality for a ToggleButtonGroup's button. Because we are not overwriting the contentContainer
            //       of this control, we need to make sure that OnChildAdded is hooked for ToggleButtonGroup and its
            //       internal contentContainer separately, otherwise it would not receive the expected workflow when a
            //       control is added into this.
            internal override void OnChildAdded(VisualElement ve) => m_Group.OnButtonGroupContainerElementAdded(ve);
            internal override void OnChildRemoved(VisualElement ve) => m_Group.OnButtonGroupContainerElementRemoved(ve);
        }

        public override VisualElement contentContainer => m_ButtonGroupContainer ?? this;

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();
            UpdateButtonStates(value);
        }

        protected override void UpdateMixedValueContent()
        {
            if (showMixedValue)
            {
                foreach (var button in m_Buttons)
                {
                    button.SetCheckedPseudoState(false);
                    button.IncrementVersion(VersionChangeType.Styles);
                }
            }
            else
            {
                SetValueWithoutNotify(value);
            }
        }

        /// <summary>
        /// Sets a new value without triggering any change event.
        /// </summary>
        /// <param name="newValue">The new value.</param>
        public override void SetValueWithoutNotify(ToggleButtonGroupState newValue)
        {
            if (newValue.length == 0)
            {
                newValue = new ToggleButtonGroupState(0, 0);

                m_EmptyLabel ??= new Label(k_EmptyStateLabel) { name = emptyStateLabelClassName }.WithClassList(emptyStateLabelClassName);
                visualInput.Insert(0, m_EmptyLabel);
            }
            else
            {
                m_EmptyLabel?.RemoveFromHierarchy();
            }

            base.SetValueWithoutNotify(newValue);
            UpdateButtonStates(newValue);
        }

        /// <summary>
        /// Returns the button at the specified index.
        /// </summary>
        public Button GetButton(int index)
        {
            if (index < 0 || index >= m_Buttons.Count)
                return null;

            return m_Buttons[index];
        }

        void OnButtonGroupContainerElementAdded(VisualElement ve)
        {
            if (ve is not Button button)
            {
                // We want the empty label there. Early out.
                if (ve == m_EmptyLabel)
                    return;

                // Since we only allow buttons, we move anything that is not a button outside of our contentContainer.
                hierarchy.Add(ve);
                return;
            }

            // The plus one being the button being added.
            if (m_Buttons.Count + 1 > ToggleButtonGroupState.maxLength)
            {
                Debug.LogWarning(k_MaxToggleButtonGroupMessage);
                return;
            }

            // Assign the required class and functionality to the button being added.
            button.AddToClassList(buttonClassNameUnique);
            button.clickable.clickedWithEventInfo += OnOptionChange;
            button.clickable.acceptClicksIfDisabled = acceptClicksIfDisabled;

            // Since we aren't passing index back and forth, this is the best way for now to get the latest ordered list
            // of buttons.
            m_Buttons = m_ButtonGroupContainer.Query<Button>().ToList();
            UpdateButtonsStyling();

            var needsSetValue = false;
            var toggleButtonGroupState = value;

            // If there are more buttons than the ToggleButtonGroupState length, we need to increase it so that it doesn't throw.
            if (m_Buttons.Count >= value.length && m_Buttons.Count <= ToggleButtonGroupState.maxLength)
            {
                toggleButtonGroupState.length = m_Buttons.Count;
                needsSetValue = true;
            }

            // If we don't allow empty selection, we set the first button to be checked.
            if (value.data == 0 && !allowEmptySelection)
            {
                toggleButtonGroupState[0] = true;
                needsSetValue = true;
            }

            if (needsSetValue)
            {
                value = toggleButtonGroupState;
            }
        }

        void OnButtonGroupContainerElementRemoved(VisualElement ve)
        {
            if (ve is not Button button)
                return;

            var toggleButtonGroupState = value;
            var checkedButtonIndex = m_Buttons.IndexOf(button);
            var selected = toggleButtonGroupState.GetActiveOptions(stackalloc int[toggleButtonGroupState.length]);
            var isRemovedButtonChecked = selected.IndexOf(checkedButtonIndex) != -1;
            button.clickable.clickedWithEventInfo -= OnOptionChange;

            if (isRemovedButtonChecked)
                m_Buttons[checkedButtonIndex].SetCheckedPseudoState(false);

            m_Buttons.Remove(button);
            UpdateButtonsStyling();

            toggleButtonGroupState.length = m_Buttons.Count;

            if (m_Buttons.Count == 0)
            {
                toggleButtonGroupState.ResetAllOptions();
                SetValueWithoutNotify(toggleButtonGroupState);
            }
            else if (isRemovedButtonChecked)
            {
                toggleButtonGroupState[checkedButtonIndex] = false;

                if (!allowEmptySelection && selected.Length == 1)
                    toggleButtonGroupState[0] = true;

                value = toggleButtonGroupState;
            }
        }

        void UpdateButtonStates(ToggleButtonGroupState options)
        {
            var span = options.GetActiveOptions(stackalloc int[value.length]);
            for (var i = 0; i < m_Buttons.Count; i++)
            {
                if (span.IndexOf(i) == -1)
                {
                    m_Buttons[i].SetCheckedPseudoState(false);
                    m_Buttons[i].IncrementVersion(VersionChangeType.Styles);
                    continue;
                }

                m_Buttons[i].SetCheckedPseudoState(true);
                m_Buttons[i].IncrementVersion(VersionChangeType.Styles);
            }
        }

        void OnOptionChange(EventBase evt)
        {
            var button = evt.target as Button;
            var index = m_Buttons.IndexOf(button);
            var toggleButtonGroupState = value;
            var selected = toggleButtonGroupState.GetActiveOptions(stackalloc int[toggleButtonGroupState.length]);

            // With showMixedValue, we want to make sure we are starting with an empty state to match the logic inside
            // the updateMixedValueContent method. Additionally, this makes base.value trigger a valid value change.
            if (showMixedValue)
            {
                var emptiedState = value;
                emptiedState.ResetAllOptions();
                if (value != emptiedState)
                {
                    SetValueWithoutNotify(emptiedState);
                }
            }

            if (isMultipleSelection)
            {
                // Always have one selected even for a multiple selection ToggleButtonGroup - return if we're trying
                // to deselect the last active one
                if (!allowEmptySelection && selected.Length == 1 && toggleButtonGroupState[index])
                    return;

                if (toggleButtonGroupState[index])
                    toggleButtonGroupState[index] = false;
                else
                    toggleButtonGroupState[index] = true;
            }
            else
            {
                if (allowEmptySelection && selected.Length == 1 && toggleButtonGroupState[selected[0]])
                {
                    toggleButtonGroupState[selected[0]] = false;

                    if (index != selected[0])
                        toggleButtonGroupState[index] = true;
                }
                else
                {
                    toggleButtonGroupState.ResetAllOptions();
                    toggleButtonGroupState[index] = true;
                }
            }

            value = toggleButtonGroupState;
        }

        void UpdateButtonsStyling()
        {
            var buttonCount = m_Buttons.Count;
            for (var i = 0; i < buttonCount; i++)
            {
                var button = m_Buttons[i];
                var isStandaloneButton = buttonCount == 1;
                var isLeftButton = i == 0 && !isStandaloneButton;
                var isRightButton = i == buttonCount - 1 && !isStandaloneButton;
                var isMiddleButton = !isLeftButton && !isRightButton && !isStandaloneButton;

                button.EnableInClassList(buttonStandaloneClassNameUnique, isStandaloneButton);
                button.EnableInClassList(buttonLeftClassNameUnique, isLeftButton);
                button.EnableInClassList(buttonRightClassNameUnique, isRightButton);
                button.EnableInClassList(buttonMidClassNameUnique, isMiddleButton);
            }
        }
    }
}
