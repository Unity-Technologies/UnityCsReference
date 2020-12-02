namespace UnityEngine.UIElements
{
    public class RadioButton : BaseBoolField, IGroupBoxOption
    {
        /// <summary>
        /// Instantiates a <see cref="RadioButton"/> using data from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<RadioButton, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="RadioButton"/>.
        /// </summary>
        public new class UxmlTraits : BaseFieldTraits<bool, UxmlBoolAttributeDescription>
        {
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };

            /// <summary>
            /// Initializes <see cref="RadioButton"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((RadioButton)ve).text = m_Text.GetValueFromBag(bag, cc);
            }
        }

        /// <summary>
        /// USS class name for RadioButton elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every instance of the RadioButton element. Any styling applied to
        /// this class affects every RadioButton located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public new static readonly string ussClassName = "unity-radio-button";
        /// <summary>
        /// USS class name for Labels in RadioButton elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the <see cref="Label"/> sub-element of the <see cref="RadioButton"/> if the RadioButton has a Label.
        /// </remarks>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in RadioButton elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the input sub-element of the <see cref="RadioButton"/>. The input sub-element provides
        /// responses to the manipulator.
        /// </remarks>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        /// <summary>
        /// USS class name of checkmark background in RadioButton elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the checkmark background sub-element of the <see cref="RadioButton"/>.
        /// </remarks>
        public static readonly string checkmarkBackgroundUssClassName = ussClassName + "__checkmark-background";
        /// <summary>
        /// USS class name of checkmark in RadioButton elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the checkmark sub-element of the <see cref="RadioButton"/>.
        /// </remarks>
        public static readonly string checkmarkUssClassName = ussClassName + "__checkmark";
        /// <summary>
        /// USS class name of Text elements in RadioButton elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to Text sub-elements of the <see cref="RadioButton"/>.
        /// </remarks>
        public static readonly string textUssClassName = ussClassName + "__text";

        VisualElement m_CheckmarkBackground;

        public override bool value
        {
            get => base.value;
            set
            {
                if (base.value != value)
                {
                    base.value = value;
                    UpdateCheckmark();

                    if (value)
                    {
                        this.OnOptionSelected();
                    }
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="RadioButton"/> with no label.
        /// </summary>
        public RadioButton()
            : this(null) {}

        /// <summary>
        /// Creates a <see cref="RadioButton"/> with a Label and a default manipulator.
        /// </summary>
        /// <remarks>
        /// The default manipulator makes it possible to activate the RadioButton with a left mouse click.
        /// </remarks>
        /// <param name="label">The Label text.</param>
        public RadioButton(string label)
            : base(label)
        {
            AddToClassList(ussClassName);

            visualInput.AddToClassList(inputUssClassName);
            labelElement.AddToClassList(labelUssClassName);

            // Reorder checkmark hierarchy to add a background.
            m_CheckMark.RemoveFromHierarchy();
            m_CheckmarkBackground = new VisualElement() { pickingMode = PickingMode.Ignore };
            m_CheckmarkBackground.Add(m_CheckMark);
            m_CheckmarkBackground.AddToClassList(checkmarkBackgroundUssClassName);
            m_CheckMark.AddToClassList(checkmarkUssClassName);
            visualInput.Add(m_CheckmarkBackground);
            UpdateCheckmark();

            this.RegisterGroupBoxOptionCallbacks();
        }

        protected override void InitLabel()
        {
            base.InitLabel();
            m_Label.AddToClassList(textUssClassName);
        }

        protected override void ToggleValue()
        {
            // Radio button active value does not toggle off once checked.
            if (!value)
            {
                value = true;
            }
        }

        public void SetSelected(bool selected)
        {
            // We're using value here and not SetValueWithoutNotify, to allow users to receive events when this gets
            // changed from other options. Checks in the setter and the group manager will prevent infinite loops.
            value = selected;
        }

        public override void SetValueWithoutNotify(bool newValue)
        {
            base.SetValueWithoutNotify(newValue);
            UpdateCheckmark();
        }

        void UpdateCheckmark()
        {
            m_CheckMark.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected override void UpdateMixedValueContent()
        {
            base.UpdateMixedValueContent();
            if (showMixedValue)
            {
                m_CheckmarkBackground.RemoveFromHierarchy();
            }
            else
            {
                m_CheckmarkBackground.Add(m_CheckMark);
                visualInput.Add(m_CheckmarkBackground);
            }
        }
    }
}
