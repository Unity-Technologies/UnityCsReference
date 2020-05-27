using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// This is the <see cref="Toggle"/> field.
    /// </summary>
    public class Toggle : BaseField<bool>
    {
        /// <summary>
        /// Instantiates a <see cref="Toggle"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<Toggle, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Toggle"/>.
        /// </summary>
        public new class UxmlTraits : BaseFieldTraits<bool, UxmlBoolAttributeDescription>
        {
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };

            /// <summary>
            /// Initialize <see cref="Toggle"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((Toggle)ve).text = m_Text.GetValueFromBag(bag, cc);
            }
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-toggle";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        /// <summary>
        /// USS class name of elements of this type, when there is no text.
        /// </summary>
        public static readonly string noTextVariantUssClassName = ussClassName + "--no-text";
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string checkmarkUssClassName = ussClassName + "__checkmark";
        /// <summary>
        /// USS class name of text elements in elements of this type.
        /// </summary>
        public static readonly string textUssClassName = ussClassName + "__text";

        private Label m_Label;

        public Toggle()
            : this(null) {}

        public Toggle(string label)
            : base(label, null)
        {
            AddToClassList(ussClassName);
            AddToClassList(noTextVariantUssClassName);

            visualInput.AddToClassList(inputUssClassName);
            labelElement.AddToClassList(labelUssClassName);

            // Allocate and add the checkmark to the hierarchy
            var checkMark = new VisualElement() { name = "unity-checkmark", pickingMode = PickingMode.Ignore };
            checkMark.AddToClassList(checkmarkUssClassName);
            visualInput.Add(checkMark);

            // The picking mode needs to be Position in order to have the Pseudostate Hover applied...
            visualInput.pickingMode = PickingMode.Position;

            // Set-up the label and text...
            text = null;
            this.AddManipulator(new Clickable(OnClickEvent));
        }

        /// <summary>
        /// Optional text after the toggle.
        /// </summary>
        public string text
        {
            get { return m_Label?.text; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    // Lazy allocation of label if needed...
                    if (m_Label == null)
                    {
                        m_Label = new Label
                        {
                            pickingMode = PickingMode.Ignore
                        };
                        m_Label.AddToClassList(textUssClassName);
                        RemoveFromClassList(noTextVariantUssClassName);
                        visualInput.Add(m_Label);
                    }

                    m_Label.text = value;
                }
                else if (m_Label != null)
                {
                    m_Label.RemoveFromHierarchy();
                    AddToClassList(noTextVariantUssClassName);
                    m_Label = null;
                }
            }
        }

        public override void SetValueWithoutNotify(bool newValue)
        {
            if (newValue)
            {
                visualInput.pseudoStates |= PseudoStates.Checked;
                pseudoStates |= PseudoStates.Checked;
            }
            else
            {
                visualInput.pseudoStates &= ~PseudoStates.Checked;
                pseudoStates &= ~PseudoStates.Checked;
            }
            base.SetValueWithoutNotify(newValue);
        }

        void OnClickEvent(EventBase evt)
        {
            if ((evt as MouseUpEvent)?.button == (int)MouseButton.LeftMouse)
            {
                OnClick();
            }
        }

        void OnClick()
        {
            value = !value;
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt == null)
            {
                return;
            }

            if (((evt as KeyDownEvent)?.keyCode == KeyCode.KeypadEnter) ||
                ((evt as KeyDownEvent)?.keyCode == KeyCode.Return))
            {
                OnClick();
                evt.StopPropagation();
            }
        }
    }
}
