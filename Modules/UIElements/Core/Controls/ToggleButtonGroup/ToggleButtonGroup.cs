// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
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
    public class ToggleButtonGroup : BaseField<ToggleButtonGroupState>
    {
        private const int k_MaxToggleButtons = 64;
        private static readonly string k_MaxToggleButtonGroupMessage = $"The number of buttons added to ToggleButtonGroup has exceeds the maximum allowed ({k_MaxToggleButtons}). The newly added button will not be treated as part of this control.";

        internal static readonly BindingId isMultipleSelectionProperty = nameof(isMultipleSelection);
        internal static readonly BindingId allowEmptySelectionProperty = nameof(allowEmptySelection);

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<ToggleButtonGroupState>.UxmlSerializedData
        {
            #pragma warning disable 649
            [SerializeField] private bool isMultipleSelection;
            [SerializeField] private bool allowEmptySelection;
            #pragma warning restore 649

            public override object CreateInstance() => new ToggleButtonGroup();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (ToggleButtonGroup)obj;
                e.isMultipleSelection = isMultipleSelection;
                e.allowEmptySelection = allowEmptySelection;
            }
        }

        /// <summary>
        /// Instantiates a <see cref="ToggleButtonGroup"/>.
        /// </summary>
        /// <remarks>
        /// This class is added to every <see cref="VisualElement"/> that is created from UXML.
        /// </remarks>
        public new class UxmlFactory : UxmlFactory<ToggleButtonGroup, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="ToggleButtonGroup"/>.
        /// </summary>
        /// <remarks>
        /// This class defines the properties of a ToggleButtonGroup element that you can use in a UXML asset.
        /// </remarks>
        public new class UxmlTraits : BaseField<ToggleButtonGroupState>.UxmlTraits
        {
            private UxmlBoolAttributeDescription m_IsMultipleSelection = new() { name = "is-multiple-selection" };
            private UxmlBoolAttributeDescription m_AllowEmptySelection = new() { name = "allow-empty-selection" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var toggleButtonGroup = (ToggleButtonGroup)ve;
                toggleButtonGroup.isMultipleSelection = m_IsMultipleSelection.GetValueFromBag(bag, cc);
                toggleButtonGroup.allowEmptySelection = m_AllowEmptySelection.GetValueFromBag(bag, cc);
            }
        }

        /// <summary>
        /// USS class name of elements for this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-toggle-button-group";

        // TO-DO : Fix this - it's only being used as a name
        /// <summary>
        /// USS class name of container element of this type.
        /// </summary>
        public static readonly string containerUssClassName = ussClassName + "__container";

        /// <summary>
        /// USS class name of container element of this type.
        /// </summary>
        public static readonly string buttonGroupClassName = "unity-button-group";

        /// <summary>
        /// USS class name for any Buttons in the group.
        /// </summary>
        public static readonly string buttonClassName = buttonGroupClassName + "__button";

        /// <summary>
        /// USS class name for the leftmost Button in the group.
        /// </summary>
        public static readonly string buttonLeftClassName = buttonClassName + "--left";

        /// <summary>
        /// USS class name for any Buttons in the middle of the group.
        /// </summary>
        public static readonly string buttonMidClassName = buttonClassName + "--mid";

        /// <summary>
        /// USS class name for the rightmost Button in the group.
        /// </summary>
        public static readonly string buttonRightClassName = buttonClassName + "--right";

        /// <summary>
        /// USS class name for the Button if only one is available in the group.
        /// </summary>
        public static readonly string buttonStandaloneClassName = buttonClassName + "--standalone";

        // Main container that will hold the group of buttons.
        VisualElement m_ButtonGroupContainer;

        // Hold a list of available buttons on the group.
        List<Button> m_Buttons = new();

        private bool m_IsMultipleSelection;
        private bool m_AllowEmptySelection;

        /// <summary>
        /// Whether all buttons can be selected.
        /// </summary>
        [CreateProperty]
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
        public ToggleButtonGroup(string label) : this(label, new ToggleButtonGroupState(0, k_MaxToggleButtons)) { }

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
            AddToClassList(ussClassName);
            Add(m_ButtonGroupContainer = new VisualElement { name = containerUssClassName, classList = { buttonGroupClassName } });

            // Note: We are changing the workflow through these series of callback. The desired workflow is when a user
            //       adds a new button, we would take the button and apply the necessary style and give it the designed
            //       functionality for a ToggleButtonGroup's button. Because we are not overwriting the contentContainer
            //       of this control, we need to make sure that elementAdded is hooked for ToggleButtonGroup and its
            //       internal contentContainer separately, otherwise it would not receive the expected workflow when a
            //       control is added into this.
            elementAdded += OnToggleButtonGroupElementAdded;
            m_ButtonGroupContainer.elementAdded += OnButtonGroupContainerElementAdded;
            m_ButtonGroupContainer.elementRemoved += OnButtonGroupContainerElementRemoved;

            SetValueWithoutNotify(toggleButtonGroupState);
        }

        protected override void UpdateMixedValueContent()
        {
            if (showMixedValue)
            {
                foreach (var button in m_Buttons)
                {
                    button.pseudoStates &= ~(PseudoStates.Checked);
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
                newValue = new ToggleButtonGroupState(0, k_MaxToggleButtons);

            base.SetValueWithoutNotify(newValue);
            UpdateButtonStates(newValue);
        }

        void OnToggleButtonGroupElementAdded(VisualElement ve)
        {
            if (ve is not Button button)
                return;

            if (m_Buttons.Count > k_MaxToggleButtons)
            {
                Debug.LogWarning(k_MaxToggleButtonGroupMessage);
                return;
            }

            m_ButtonGroupContainer.Add(button);
        }

        void OnButtonGroupContainerElementAdded(VisualElement ve)
        {
            if (ve is not Button button)
            {
                // Since we only allow buttons, we move anything that is not a button outside of our contentContainer.
                Add(ve);
                return;
            }

            // The plus one being the button being added.
            if (m_Buttons.Count + 1 > k_MaxToggleButtons)
            {
                Debug.LogWarning(k_MaxToggleButtonGroupMessage);
                return;
            }

            // Assign the required class and functionality to the button being added.
            button.AddToClassList(buttonClassName);
            button.clickable.clickedWithEventInfo += OnOptionChange;

            // Since we aren't passing index back and forth, this is the best way for now to get the latest ordered list
            // of buttons.
            m_Buttons = m_ButtonGroupContainer.Query<Button>().ToList();
            UpdateButtonsStyling();

            var toggleButtonGroupState = value;
            var selected = toggleButtonGroupState.GetActiveOptions(stackalloc int[toggleButtonGroupState.length]);
            // If we don't allow empty selection, we set the first button to be checked.
            if (selected.Length == 0 && !allowEmptySelection)
            {
                toggleButtonGroupState[0] = true;
                SetValueWithoutNotify(toggleButtonGroupState);
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
                m_Buttons[checkedButtonIndex].pseudoStates &= ~(PseudoStates.Checked);

            m_Buttons.Remove(button);
            UpdateButtonsStyling();

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

                SetValueWithoutNotify(toggleButtonGroupState);
            }
        }

        void UpdateButtonStates(ToggleButtonGroupState options)
        {
            var span = options.GetActiveOptions(stackalloc int[value.length]);
            for (var i = 0; i < m_Buttons.Count; i++)
            {
                if (span.IndexOf(i) == -1)
                {
                    m_Buttons[i].pseudoStates &= ~(PseudoStates.Checked);
                    m_Buttons[i].IncrementVersion(VersionChangeType.Styles);
                    continue;
                }

                m_Buttons[i].pseudoStates |= PseudoStates.Checked;
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

                button.EnableInClassList(buttonStandaloneClassName, isStandaloneButton);
                button.EnableInClassList(buttonLeftClassName, isLeftButton);
                button.EnableInClassList(buttonRightClassName, isRightButton);
                button.EnableInClassList(buttonMidClassName, isMiddleButton);
            }
        }
    }
}
