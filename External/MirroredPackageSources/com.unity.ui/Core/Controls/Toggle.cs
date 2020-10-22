namespace UnityEngine.UIElements
{
    /// <summary>
    /// A <see cref="Toggle"/> is a clickable element that represents a boolean value.
    /// </summary>
    /// <remarks>
    /// A Toggle control consists of a label and an input field. The input field contains a sprite for the control. By default,
    /// this is a checkbox (Unity does not provide a separate checkbox control type) in all of its possible states, for example,
    /// normal, hovered, checked, and unchecked. You can style a Toggle control to change its appearance to something else, for
    /// example, an on/off switch.
    ///
    /// When a Toggle is clicked, its state alternates between between true and false. You can also think of these states  as
    /// on and off, or enabled and disabled.
    ///
    /// To bind the Toggle's state to a boolean variable, set the`binding-path` property in a UI Document (.uxml file), or
    /// the C# `bindingPath` to the variable name.
    /// </remarks>
    public class Toggle : BaseField<bool>
    {
        /// <summary>
        /// Instantiates a <see cref="Toggle"/> using data from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<Toggle, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Toggle"/>.
        /// </summary>
        public new class UxmlTraits : BaseFieldTraits<bool, UxmlBoolAttributeDescription>
        {
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };

            /// <summary>
            /// Initializes <see cref="Toggle"/> properties using values from the attribute bag.
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
        /// USS class name for Toggle elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to every instance of the Toggle element. Any styling applied to
        /// this class affects every Toggle located beside, or below the stylesheet in the visual tree.
        /// </remarks>
        public new static readonly string ussClassName = "unity-toggle";
        /// <summary>
        /// USS class name for Labels in Toggle elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the <see cref="Label"/> sub-element of the <see cref="Toggle"/> if the Toggle has a Label.
        /// </remarks>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in Toggle elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the input sub-element of the <see cref="Toggle"/>. The input sub-element provides
        /// responses to the manipulator.
        /// </remarks>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        /// <summary>
        /// USS class name of Toggle elements that have no text.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the <see cref="Toggle"/> if the Toggle does not have a label.
        /// </remarks>
        public static readonly string noTextVariantUssClassName = ussClassName + "--no-text";
        /// <summary>
        /// USS class name of Images in Toggle elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to the Image sub-element of the <see cref="Toggle"/> that contains the checkmark image.
        /// </remarks>
        public static readonly string checkmarkUssClassName = ussClassName + "__checkmark";
        /// <summary>
        /// USS class name of Text elements in Toggle elements.
        /// </summary>
        /// <remarks>
        /// Unity adds this USS class to Text sub-elements of the <see cref="Toggle."/>
        /// </remarks>
        public static readonly string textUssClassName = ussClassName + "__text";

        private Label m_Label;
        private readonly VisualElement m_CheckMark;

        /// <summary>
        /// Creates a <see cref="Toggle"/> with no label.
        /// </summary>
        public Toggle()
            : this(null) {}

        /// <summary>
        /// Creates a <see cref="Toggle"/> with a Label and a default manipulator.
        /// </summary>
        /// <remarks>
        /// The default manipulator makes it possible to activate the Toggle with a left mouse click.
        /// </remarks>
        /// <param name="label">The Label text.</param>
        public Toggle(string label)
            : base(label, null)
        {
            AddToClassList(ussClassName);
            AddToClassList(noTextVariantUssClassName);

            visualInput.AddToClassList(inputUssClassName);
            labelElement.AddToClassList(labelUssClassName);

            // Allocate and add the checkmark to the hierarchy
            m_CheckMark = new VisualElement() { name = "unity-checkmark", pickingMode = PickingMode.Ignore };
            m_CheckMark.AddToClassList(checkmarkUssClassName);
            visualInput.Add(m_CheckMark);

            // The picking mode needs to be Position in order to have the Pseudostate Hover applied...
            visualInput.pickingMode = PickingMode.Position;

            // Set-up the label and text...
            text = null;
            this.AddManipulator(new Clickable(OnClickEvent));

            RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void OnNavigationSubmit(NavigationSubmitEvent evt)
        {
            ToggleValue();
            evt.StopPropagation();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (panel?.contextType != ContextType.Editor)
                return;

            // KeyCodes are hardcoded in the Editor, but in runtime we should use the more versatile NavigationSubmit.
            if (evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.Space)
            {
                ToggleValue();
                evt.StopPropagation();
            }
        }

        private string m_OriginalText;

        /// <summary>
        /// Optional text that appears after the Toggle.
        /// </summary>
        /// <remarks>
        /// Unity creates a <see cref="Label"/> automatically if one does not exist.
        /// </remarks>
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

        /// <summary>
        /// Sets the value of the <see cref="Toggle"/>, but does not notify the rest of the hierarchy of the change.
        /// </summary>
        /// <remarks>
        /// This method is useful when you want to change the control's value without triggering events. For example, you can use it when you initialize UI
        /// to avoid triggering unnecessary events, and to prevent situations where changing the value of one control triggers an event that tries to update
        /// another control that hasn't been initialized, and may not exist yet.
        ///
        /// This method is also useful for preventing circular updates. Let's say you link two controls so they always have the same value. When a user changes
        /// the value of the first control, it fires an event to update the value of the second control. If you update the second control's value "normally,"
        /// as though a user changed it, it will fire another event to update the first control's value, which will fire an event to update the second control's
        /// value again, and so on. If one control update's the other's value using SetValueWithoutNotify, the update does not trigger an event, which prevents
        /// the circular update loop.
        /// </remarks>
        /// <param name="newValue"></param>
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
            if (evt.eventTypeId == MouseUpEvent.TypeId())
            {
                var ce = (IMouseEvent)evt;
                if (ce.button == (int)MouseButton.LeftMouse)
                {
                    ToggleValue();
                }
            }
            else if (evt.eventTypeId == PointerUpEvent.TypeId() || evt.eventTypeId == ClickEvent.TypeId())
            {
                var ce = (IPointerEvent)evt;
                if (ce.button == (int)MouseButton.LeftMouse)
                {
                    ToggleValue();
                }
            }
        }

        void ToggleValue()
        {
            value = !value;
        }

        protected override void UpdateMixedValueContent()
        {
            if (showMixedValue)
            {
                visualInput.pseudoStates &= ~PseudoStates.Checked;
                pseudoStates &= ~PseudoStates.Checked;

                m_CheckMark.RemoveFromHierarchy();
                visualInput.Add(mixedValueLabel);
                m_OriginalText = text;
                text = "";
            }
            else
            {
                mixedValueLabel.RemoveFromHierarchy();
                visualInput.Add(m_CheckMark);
                if (m_OriginalText != null)
                    text = m_OriginalText;
            }
        }
    }
}
