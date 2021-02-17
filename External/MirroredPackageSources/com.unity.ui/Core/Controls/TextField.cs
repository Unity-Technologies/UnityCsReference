using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// A textfield is a rectangular area where the user can edit a string.
    /// </summary>
    public class TextField : TextInputBaseField<string>
    {
        // This property to alleviate the fact we have to cast all the time
        TextInput textInput => (TextInput)textInputBase;

        /// <summary>
        /// Instantiates a <see cref="TextField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<TextField, UxmlTraits> {}
        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="TextField"/>.
        /// </summary>
        public new class UxmlTraits : TextInputBaseField<string>.UxmlTraits
        {
            UxmlBoolAttributeDescription m_Multiline = new UxmlBoolAttributeDescription { name = "multiline" };

            /// <summary>
            /// Initialize <see cref="TextField"/> properties using values from the attribute bag.
            /// </summary>
            /// <param name="ve">The object to initialize.</param>
            /// <param name="bag">The attribute bag.</param>
            /// <param name="cc">The creation context; unused.</param>
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                TextField field = ((TextField)ve);
                field.multiline = m_Multiline.GetValueFromBag(bag, cc);
                base.Init(ve, bag, cc);
            }
        }

        /// <summary>
        /// Set this to true to allow multiple lines in the textfield and false if otherwise.
        /// </summary>
        public bool multiline
        {
            get { return textInput.multiline; }
            set { textInput.multiline = value; }
        }


        /// <summary>
        /// Selects text in the textfield between cursorIndex and selectionIndex.
        /// </summary>
        /// <param name="selectionIndex">The selection end position.</param>
        public void SelectRange(int rangeCursorIndex, int selectionIndex)
        {
            textInput.SelectRange(rangeCursorIndex, selectionIndex);
        }

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-text-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Creates a new textfield.
        /// </summary>
        public TextField()
            : this(null) {}

        /// <summary>
        /// Creates a new textfield.
        /// </summary>
        /// <param name="maxLength">The maximum number of characters this textfield can hold. If 0, there is no limit.</param>
        /// <param name="multiline">Set this to true to allow multiple lines in the textfield and false if otherwise.</param>
        /// <param name="isPasswordField">Set this to true to mask the characters and false if otherwise.</param>
        /// <param name="maskChar">The character used for masking in a password field.</param>
        public TextField(int maxLength, bool multiline, bool isPasswordField, char maskChar)
            : this(null, maxLength, multiline, isPasswordField, maskChar) {}

        /// <summary>
        /// Creates a new textfield.
        /// </summary>
        public TextField(string label)
            : this(label, kMaxLengthNone, false, false, kMaskCharDefault) {}

        /// <summary>
        /// Creates a new textfield.
        /// </summary>
        /// <param name="maxLength">The maximum number of characters this textfield can hold. If 0, there is no limit.</param>
        /// <param name="multiline">Set this to true to allow multiple lines in the textfield and false if otherwise.</param>
        /// <param name="isPasswordField">Set this to true to mask the characters and false if otherwise.</param>
        /// <param name="maskChar">The character used for masking in a password field.</param>
        public TextField(string label, int maxLength, bool multiline, bool isPasswordField, char maskChar)
            : base(label, maxLength, maskChar, new TextInput())
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            pickingMode = PickingMode.Ignore;
            SetValueWithoutNotify("");
            this.multiline = multiline;
            this.isPasswordField = isPasswordField;
        }

        /// <summary>
        /// The string currently being exposed by the field.
        /// </summary>
        /// <remarks>
        /// Note that changing this will not trigger a change event to be sent.
        /// </remarks>
        public override string value
        {
            get { return base.value; }
            set
            {
                base.value = value;
                text = rawValue;
            }
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);
            text = rawValue;
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();

            string key = GetFullHierarchicalViewDataKey();

            OverwriteFromViewData(this, key);

            // Here we must make sure the value is restored on screen from the saved value !
            text = rawValue;
        }

        protected override string ValueToString(string value) => value;

        protected override string StringToValue(string str) => str;
        class TextInput : TextInputBase
        {
            TextField parentTextField => (TextField)parent;

            // Multiline (lossy behaviour when deactivated)
            bool m_Multiline;

            public bool multiline
            {
                get { return m_Multiline; }
                set
                {
                    m_Multiline = value;
                    if (!value)
                        text = text.Replace("\n", "");
                    SetTextAlign();
                }
            }

            private void SetTextAlign()
            {
                if (m_Multiline)
                {
                    RemoveFromClassList(singleLineInputUssClassName);
                    AddToClassList(multilineInputUssClassName);
                }
                else
                {
                    RemoveFromClassList(multilineInputUssClassName);
                    AddToClassList(singleLineInputUssClassName);
                }
            }

            // Password field (indirectly lossy behaviour when activated via multiline)
            public override bool isPasswordField
            {
                set
                {
                    base.isPasswordField = value;
                    if (value)
                        multiline = false;
                }
            }

            protected override string StringToValue(string str)
            {
                return str;
            }

            public void SelectRange(int cursorIndex, int selectionIndex)
            {
                if (editorEngine != null)
                {
                    editorEngine.cursorIndex = cursorIndex;
                    editorEngine.selectIndex = selectionIndex;
                }
            }

            internal override void SyncTextEngine()
            {
                if (parentTextField != null)
                {
                    editorEngine.multiline = multiline;
                    editorEngine.isPasswordField = isPasswordField;
                }

                base.SyncTextEngine();
            }

            protected override void ExecuteDefaultActionAtTarget(EventBase evt)
            {
                base.ExecuteDefaultActionAtTarget(evt);

                if (evt == null)
                {
                    return;
                }

                if (evt.eventTypeId == KeyDownEvent.TypeId())
                {
                    KeyDownEvent kde = evt as KeyDownEvent;

                    if (!parentTextField.isDelayed || (!multiline && ((kde?.keyCode == KeyCode.KeypadEnter) || (kde?.keyCode == KeyCode.Return))))
                    {
                        parentTextField.value = text;
                    }

                    if (multiline)
                    {
                        if (kde?.character == '\t' && kde.modifiers == EventModifiers.None)
                        {
                            kde?.StopPropagation();
                            kde?.PreventDefault();
                        }
                        else if (((kde?.character == 3) && (kde?.shiftKey == true)) || // KeyCode.KeypadEnter
                                 ((kde?.character == '\n') && (kde?.shiftKey == true))) // KeyCode.Return
                        {
                            parent.Focus();
                            evt.StopPropagation();
                            evt.PreventDefault();
                        }
                    }
                    else if ((kde?.character == 3) ||    // KeyCode.KeypadEnter
                             (kde?.character == '\n'))   // KeyCode.Return
                    {
                        parent.Focus();
                        evt.StopPropagation();
                        evt.PreventDefault();
                    }
                }
                else if (evt.eventTypeId == ExecuteCommandEvent.TypeId())
                {
                    ExecuteCommandEvent commandEvt = evt as ExecuteCommandEvent;
                    string cmdName = commandEvt.commandName;
                    if (!parentTextField.isDelayed && (cmdName == EventCommandNames.Paste || cmdName == EventCommandNames.Cut))
                    {
                        parentTextField.value = text;
                    }
                }
                // Prevent duplicated navigation events, since we're observing KeyDownEvents instead
                else if (evt.eventTypeId == NavigationSubmitEvent.TypeId() ||
                         evt.eventTypeId == NavigationCancelEvent.TypeId() ||
                         evt.eventTypeId == NavigationMoveEvent.TypeId())
                {
                    evt.StopPropagation();
                    evt.PreventDefault();
                }
            }

            protected override void ExecuteDefaultAction(EventBase evt)
            {
                base.ExecuteDefaultAction(evt);

                if (parentTextField.isDelayed && evt?.eventTypeId == BlurEvent.TypeId())
                {
                    parentTextField.value = text;
                }
            }
        }
    }
}
