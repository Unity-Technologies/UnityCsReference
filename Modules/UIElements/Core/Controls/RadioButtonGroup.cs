// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Properties;
using UnityEngine.Pool;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A control that allows single selection out of a logical group of <see cref="RadioButton"/> elements. Selecting one will deselect the others. For more information, refer to [[wiki:UIE-uxml-element-RadioButtonGroup|UXML element RadioButtonGroup]].
    /// </summary>
    public class RadioButtonGroup : BaseField<int>, IGroupBox
    {
        internal static readonly BindingId choicesProperty = nameof(choices);

        [UnityEngine.Internal.ExcludeFromDocs, Serializable]
        public new class UxmlSerializedData : BaseField<int>.UxmlSerializedData
        {
            [Conditional("UNITY_EDITOR")]
            public new static void Register()
            {
                BaseField<int>.UxmlSerializedData.Register();
                UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), new UxmlAttributeNames[]
                {
                    new (nameof(choicesList), "choices")
                });
            }

            #pragma warning disable 649
            [UxmlAttribute("choices"), UxmlAttributeBindingPath(nameof(choices))]
            [SerializeField] List<string> choicesList;
            [SerializeField, UxmlIgnore, HideInInspector] UxmlAttributeFlags choicesList_UxmlAttributeFlags;
            #pragma warning restore 649

            public override object CreateInstance() => new RadioButtonGroup();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                if (ShouldWriteAttributeValue(choicesList_UxmlAttributeFlags))
                {
                    var e = (RadioButtonGroup)obj;
                    e.choicesList = choicesList;
                }
            }
        }

        /// <summary>
        /// Instantiates a <see cref="RadioButtonGroup"/> using data from a UXML file.
        /// </summary>
        [Obsolete("UxmlFactory is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlFactory : UxmlFactory<RadioButtonGroup, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="RadioButtonGroup"/>.
        /// </summary>
        [Obsolete("UxmlTraits is deprecated and will be removed. Use UxmlElementAttribute instead.", false)]
        public new class UxmlTraits : BaseFieldTraits<int, UxmlIntAttributeDescription>
        {
            UxmlStringAttributeDescription m_Choices = new UxmlStringAttributeDescription { name = "choices" };

            /// <summary>
            /// Initializes <see cref="RadioButtonGroup"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                var f = (RadioButtonGroup)ve;
                f.choicesList = UxmlUtility.ParseStringListAttribute(m_Choices.GetValueFromBag(bag, cc));
            }
        }

        /// <summary>
        /// USS class name for RadioButtonGroup elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every instance of the RadioButtonGroup element. Any styling applied to
        /// this class affects every RadioButtonGroup located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public new static readonly string ussClassName = "unity-radio-button-group";
        /// <summary>
        /// USS class name of container element of this type.
        /// </summary>
        public static readonly string containerUssClassName = ussClassName + "__container";
        /// <summary>
        /// Name of the content container element of this type.
        /// </summary>
        internal static readonly string containerName =  "contentContainer";
        /// <summary>
        /// Name of the container element where the RadioButtons created from choices are added.
        /// </summary>
        internal static readonly string choicesContainerName =  "choicesContentContainer";

        VisualElement m_ChoiceRadioButtonContainer;
        VisualElement m_ContentContainer;
        UQueryBuilder<RadioButton> m_GetAllRadioButtonsQuery;

        /// <summary>
        /// List of all RadioButtons registered to this RadioButtonGroup.
        /// </summary>
        /// <see cref="choices"/>
        readonly List<RadioButton> m_RegisteredRadioButtons = new();

        /// <summary>
        /// The selected RadioButton.
        /// </summary>
        RadioButton m_SelectedRadioButton;

        EventCallback<ChangeEvent<bool>> m_RadioButtonValueChangedCallback;

        private bool m_UpdatingButtons;

        private List<string> m_Choices = new();

        /// <summary>
        /// The list of available choices in the group.
        /// </summary>
        /// <remarks>
        /// Writing to this property removes existing <see cref="RadioButton"/> elements, except those added explicitly
        /// to hierarchy, and re-creates them to display the new list.
        /// </remarks>
        [CreateProperty]
        public IEnumerable<string> choices
        {
            get
            {
                using var _ = ListPool<RadioButton>.Get(out var radioButtons);

                GetAllRadioButtons(radioButtons);

                foreach( var button in radioButtons)
                {
                    yield return button.text;
                }
            }
            set
            {
                if ((value != null && AreListEqual(m_Choices, value))
                    || (value == null && m_Choices.Count == 0))
                    return;

                m_Choices.Clear();

                if (value != null)
                    m_Choices.AddRange(value);
                RebuildRadioButtonsFromChoices();
                NotifyPropertyChanged(choicesProperty);
                return;

                bool AreListEqual(List<string> list1, IEnumerable<string> list2)
                {
                    var list2Count = 0;

                    using (var enumerator = list2.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            list2Count++;
                        }
                    }

                    if (list1.Count != list2Count)
                        return false;

                    var i = 0;
                    using (var enumerator = list2.GetEnumerator())
                    {
                        while (enumerator.MoveNext())
                        {
                            if (!string.Equals(list1[i], enumerator.Current))
                                return false;
                            ++i;
                        }
                    }

                    return true;
                }
            }
        }

        private void RebuildRadioButtonsFromChoices()
        {
            if (m_Choices.Count == 0)
            {
                m_ChoiceRadioButtonContainer.Clear();
            }
            else
            {
                var i = 0;
                foreach (var choice in m_Choices)
                {
                    if (i < m_ChoiceRadioButtonContainer.childCount)
                    {
                        (m_ChoiceRadioButtonContainer[i] as RadioButton).text = choice;
                        ScheduleRadioButtons();
                    }
                    else
                    {
                        var radioButton = new RadioButton { text = choice };

                        m_ChoiceRadioButtonContainer.Add(radioButton);
                    }
                    i++;
                }

                var lastIndex = m_ChoiceRadioButtonContainer.childCount - 1;
                for (var j = lastIndex; j >= i; j--)
                {
                    // RadioButton will be removed from m_ChoiceRadioButtons in the OnOptionRemoved method below.
                    // The value change callback will be unregistered there as well.
                    m_ChoiceRadioButtonContainer[j].RemoveFromHierarchy();
                }
            }
        }

        internal List<string> choicesList
        {
            get => m_Choices;
            set => choices = value;
        }

        public override VisualElement contentContainer => m_ContentContainer ?? this;

        /// <summary>
        /// Initializes and returns an instance of RadioButtonGroup.
        /// </summary>
        public RadioButtonGroup()
            : this(null) {}

        /// <summary>
        /// Initializes and returns an instance of RadioButtonGroup.
        /// </summary>
        /// <param name="label">The label for this group</param>
        /// <param name="radioButtonChoices">The choices to display in this group</param>
        public RadioButtonGroup(string label, List<string> radioButtonChoices = null)
            : base(label, null)
        {
            AddToClassList(ussClassName);

            visualInput.Add(m_ChoiceRadioButtonContainer = new VisualElement { name = choicesContainerName });
            m_ChoiceRadioButtonContainer.AddToClassList(containerUssClassName);

            visualInput.Add(m_ContentContainer = new VisualElement { name = containerName });
            m_ContentContainer.AddToClassList(containerUssClassName);

            m_GetAllRadioButtonsQuery = this.Query<RadioButton>();

            m_RadioButtonValueChangedCallback = RadioButtonValueChangedCallback;

            choices = radioButtonChoices;
            value = -1;
            visualInput.focusable = false;
            delegatesFocus = true;
        }

        void RadioButtonValueChangedCallback(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                var radioButton = evt.target as RadioButton;
                using var _ = ListPool<RadioButton>.Get(out var radioButtons);
                GetAllRadioButtons(radioButtons);

                value = radioButtons.IndexOf(radioButton);
                evt.StopPropagation();
            }
        }

        public override void SetValueWithoutNotify(int newValue)
        {
            base.SetValueWithoutNotify(newValue);
            UpdateRadioButtons(true);
        }

        /// <summary>
        /// Returns the list of all radio buttons sorted according their hierarchical order.
        /// </summary>
        /// <returns>Returns the list of sorted radio buttons</returns>
        void GetAllRadioButtons(List<RadioButton> radioButtons)
        {
            radioButtons.Clear();
            m_GetAllRadioButtonsQuery.ForEach(radioButtons.Add);
        }

        /// <summary>
        /// Updates the toggled state of the all radio buttons according to the current value of this field.
        /// The RadioButton at the index specified by the value gets toggled while the others get untoggled.
        /// This index is relative to the list of all radio buttons (generated from choices or explicitly added
        /// to the hierarchy).
        /// All the buttons get untoggled if the index is out of bound.
        /// </summary>
        void UpdateRadioButtons(bool notify)
        {
            if (panel == null)
                return;

            using var _ = ListPool<RadioButton>.Get(out var radioButtons);
            GetAllRadioButtons(radioButtons);

            m_SelectedRadioButton = null;
            if (value >= 0 && value < radioButtons.Count)
            {
                m_SelectedRadioButton = radioButtons[value];
                if (notify)
                    m_SelectedRadioButton.value = true;
                else
                    m_SelectedRadioButton.SetValueWithoutNotify(true);

                // Set the rest to false
                foreach (var radioButton in radioButtons)
                {
                    if (radioButton != m_SelectedRadioButton)
                        if (notify)
                            radioButton.value = false;
                        else
                            radioButton.SetValueWithoutNotify(false);
                }
            }
            else
            {
                foreach (var radioButton in radioButtons)
                {
                    if (notify)
                        radioButton.value = false;
                    else
                        radioButton.SetValueWithoutNotify(false);
                }
            }

            m_UpdatingButtons = false;
        }

        void ScheduleRadioButtons()
        {
            if (m_UpdatingButtons)
                return;
            schedule.Execute(() => UpdateRadioButtons(false));
            m_UpdatingButtons = true;
        }

        /// <summary>
        /// Registers the specified RadioButton to this RadioButtonGroup.
        /// </summary>
        /// <param name="radioButton">The button to register</param>
        private void RegisterRadioButton(RadioButton radioButton)
        {
            if (m_RegisteredRadioButtons.Contains(radioButton))
                return;
            m_RegisteredRadioButtons.Add(radioButton);
            radioButton.RegisterValueChangedCallback(m_RadioButtonValueChangedCallback);

            // If the user sets a value before the radio button is registered, we need to update the radio button group's value
            if (value == -1 && radioButton.value)
            {
                using var _ = ListPool<RadioButton>.Get(out var radioButtons);
                GetAllRadioButtons(radioButtons);

                SetValueWithoutNotify(radioButtons.IndexOf(radioButton));
            }
            ScheduleRadioButtons();
        }

        /// <summary>
        /// Unregister the specified RadioButton from this RadioButtonGroup.
        /// </summary>
        /// <param name="radioButton">The button to unregister</param>
        private void UnregisterRadioButton(RadioButton radioButton)
        {
            if (!m_RegisteredRadioButtons.Contains(radioButton))
                return;
            m_RegisteredRadioButtons.Remove(radioButton);
            radioButton.UnregisterValueChangedCallback(m_RadioButtonValueChangedCallback);
            ScheduleRadioButtons();
        }

        void IGroupBox.OnOptionAdded(IGroupBoxOption option)
        {
            if (!(option is RadioButton radioButton))
                throw new ArgumentException("[UI Toolkit] Internal group box error. Expected a radio button element. Please report this using Help -> Report a bug...");
            RegisterRadioButton(radioButton);
        }

        void IGroupBox.OnOptionRemoved(IGroupBoxOption option)
        {
            if (!(option is RadioButton radioButton))
                throw new ArgumentException("[UI Toolkit] Internal group box error. Expected a radio button element. Please report this using Help -> Report a bug...");

            UnregisterRadioButton(radioButton);

            if (m_SelectedRadioButton == radioButton)
            {
                m_SelectedRadioButton = null;
                value = -1;
            }
        }
    }
}
