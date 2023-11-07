// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;

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
            #pragma warning disable 649
            [UxmlAttribute("choices")]
            [SerializeField] private List<string> choicesList;
            #pragma warning restore 649

            public override object CreateInstance() => new RadioButtonGroup();

            public override void Deserialize(object obj)
            {
                base.Deserialize(obj);

                var e = (RadioButtonGroup)obj;
                e.choicesList = choicesList;
            }
        }

        /// <summary>
        /// Instantiates a <see cref="RadioButtonGroup"/> using data from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<RadioButtonGroup, UxmlTraits> { }

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="RadioButtonGroup"/>.
        /// </summary>
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
                f.choices = UxmlUtility.ParseStringListAttribute(m_Choices.GetValueFromBag(bag, cc));
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

        List<RadioButton> m_RadioButtons = new List<RadioButton>();
        EventCallback<ChangeEvent<bool>> m_RadioButtonValueChangedCallback;

        /// <summary>
        /// The list of available choices in the group.
        /// </summary>
        /// <remarks>
        /// Writing to this property removes existing <see cref="RadioButton"/> elements and
        /// re-creates them to display the new list.
        /// </remarks>
        [CreateProperty]
        public IEnumerable<string> choices
        {
            get
            {
                foreach (var radioButton in m_RadioButtons)
                {
                    yield return radioButton.text;
                }
            }
            set
            {
                if (!value.HasValues())
                {
                    m_RadioButtonContainer.Clear();

                    // Only need to clear radio buttons if we're attached to a panel, because we already
                    // do the cleaning on detach to panel otherwise.
                    if (panel != null)
                    {
                        return;
                    }

                    foreach (var radioButton in m_RadioButtons)
                    {
                        radioButton.UnregisterValueChangedCallback(m_RadioButtonValueChangedCallback);
                    }
                    m_RadioButtons.Clear();

                    return;
                }

                var i = 0;
                foreach (var choice in value)
                {
                    if (i < m_RadioButtons.Count)
                    {
                        m_RadioButtons[i].text = choice;
                        m_RadioButtonContainer.Insert(i, m_RadioButtons[i]);
                    }
                    else
                    {
                        var radioButton = new RadioButton { text = choice };
                        radioButton.RegisterValueChangedCallback(m_RadioButtonValueChangedCallback);
                        m_RadioButtons.Add(radioButton);
                        m_RadioButtonContainer.Add(radioButton);
                    }

                    i++;
                }

                var lastIndex = m_RadioButtons.Count - 1;
                for (var j = lastIndex; j >= i; j--)
                {
                    // RadioButton will be removed from m_RadioButton in the OnOptionRemoved method below.
                    // The value change callback will be unregistered there as well.
                    m_RadioButtons[j].RemoveFromHierarchy();
                }

                UpdateRadioButtons();
                NotifyPropertyChanged(choicesProperty);
            }
        }

        internal List<string> choicesList
        {
            get => choices.ToList();
            set => choices = value;
        }

        VisualElement m_RadioButtonContainer;

        public override VisualElement contentContainer => m_RadioButtonContainer ?? this;

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

            visualInput.Add(m_RadioButtonContainer = new VisualElement { name = containerUssClassName });
            m_RadioButtonContainer.AddToClassList(containerUssClassName);
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
                value = m_RadioButtons.IndexOf(evt.target as RadioButton);
                evt.StopPropagation();
            }
        }

        public override void SetValueWithoutNotify(int newValue)
        {
            base.SetValueWithoutNotify(newValue);
            UpdateRadioButtons();
        }

        void UpdateRadioButtons()
        {
            if (value >= 0 && value < m_RadioButtons.Count)
            {
                m_RadioButtons[value].value = true;
            }
            else
            {
                foreach (var radioButton in m_RadioButtons)
                {
                    radioButton.value = false;
                }
            }
        }

        void IGroupBox.OnOptionAdded(IGroupBoxOption option)
        {
            if (!(option is RadioButton radioButton))
                throw new ArgumentException("[UI Toolkit] Internal group box error. Expected a radio button element. Please report this using Help -> Report a bug...");

            if (m_RadioButtons.Contains(radioButton))
                return;

            radioButton.RegisterValueChangedCallback(m_RadioButtonValueChangedCallback);
            var indexInContainer = m_RadioButtonContainer.IndexOf(radioButton);
            if (indexInContainer < 0 || indexInContainer > m_RadioButtons.Count)
            {
                m_RadioButtons.Add(radioButton);
                m_RadioButtonContainer.Add(radioButton);
            }
            else
            {
                m_RadioButtons.Insert(indexInContainer, radioButton);
            }
        }

        void IGroupBox.OnOptionRemoved(IGroupBoxOption option)
        {
            if (!(option is RadioButton radioButton))
                throw new ArgumentException("[UI Toolkit] Internal group box error. Expected a radio button element. Please report this using Help -> Report a bug...");

            var index = m_RadioButtons.IndexOf(radioButton);
            radioButton.UnregisterValueChangedCallback(m_RadioButtonValueChangedCallback);
            m_RadioButtons.Remove(radioButton);

            // Reset value if the selected option is removed
            if (value == index)
                value = -1;
        }
    }
}
