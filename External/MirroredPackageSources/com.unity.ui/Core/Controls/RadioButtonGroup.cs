using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.UIElements
{
    public class RadioButtonGroup : BaseField<int>, IGroupBox
    {
        /// <summary>
        /// Instantiates a <see cref="RadioButtonGroup"/> using data from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<RadioButtonGroup, UxmlTraits> {}

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
                // Need to update choices first so that radio buttons are created before the value is set.
                ((RadioButtonGroup)ve).choices = m_Choices.GetValueFromBag(bag, cc).Split(',').Select(e => e.Trim()).ToList();
                base.Init(ve, bag, cc);
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

        IEnumerable<string> m_Choices;
        List<RadioButton> m_RadioButtons = new List<RadioButton>();
        EventCallback<ChangeEvent<bool>> m_RadioButtonValueChangedCallback;

        public IEnumerable<string> choices
        {
            get => m_Choices;
            set
            {
                m_Choices = value;

                foreach (var radioButton in m_RadioButtons)
                {
                    radioButton.UnregisterValueChangedCallback(m_RadioButtonValueChangedCallback);
                    radioButton.RemoveFromHierarchy();
                }

                m_RadioButtons.Clear();

                if (m_Choices != null)
                {
                    foreach (var choice in m_Choices)
                    {
                        var radioButton = new RadioButton() { text = choice };
                        radioButton.RegisterValueChangedCallback(m_RadioButtonValueChangedCallback);
                        m_RadioButtons.Add(radioButton);
                        visualInput.Add(radioButton);
                    }
                }
            }
        }

        public RadioButtonGroup()
            : this(null)
        {
        }

        public RadioButtonGroup(string label, List<string> radioButtonChoices = null)
            : base(label, null)
        {
            AddToClassList(ussClassName);

            m_RadioButtonValueChangedCallback = RadioButtonValueChangedCallback;
            choices = radioButtonChoices;
            value = -1;
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
    }
}
