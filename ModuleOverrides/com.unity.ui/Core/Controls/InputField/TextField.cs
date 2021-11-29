// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
                textEdition.UpdateText(rawValue);
            }
        }

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);
            textEdition.UpdateText(rawValue);
        }

        [EventInterest(typeof(KeyDownEvent), typeof(ExecuteCommandEvent),
                typeof(NavigationSubmitEvent), typeof(NavigationCancelEvent), typeof(NavigationMoveEvent))]
        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt is KeyDownEvent kde)
            {
                if (!isDelayed || (!multiline && ((kde?.keyCode == KeyCode.KeypadEnter) || (kde?.keyCode == KeyCode.Return))))
                {
                    value = text;
                }

                if (multiline)
                {
                    // For multiline text fields, make sure tab doesn't trigger a focus change.
                    if (hasFocus && (kde?.keyCode == KeyCode.Tab || kde?.character == '\t'))
                    {
                        evt.StopPropagation();
                        evt.PreventDefault();
                    }
                    else if (((kde?.character == 3) && (kde?.shiftKey == true)) || // KeyCode.KeypadEnter
                             ((kde?.character == '\n') && (kde?.shiftKey == true))) // KeyCode.Return
                    {
                        Focus();
                        evt.StopPropagation();
                        evt.PreventDefault();
                    }
                }
                else if ((kde?.character == 3) ||     // KeyCode.KeypadEnter
                        (kde?.character == '\n'))    // KeyCode.Return
                {
                    if (hasFocus)
                        Focus();
                    else
                        textInput.textElement.Focus();
                    evt.StopPropagation();
                    evt.PreventDefault();
                }
            }
            else if (evt is ExecuteCommandEvent commandEvt)
            {
                string cmdName = commandEvt.commandName;
                if (!isDelayed && (cmdName == EventCommandNames.Paste || cmdName == EventCommandNames.Cut))
                {
                    value = text;
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

        [EventInterest(typeof(BlurEvent))]
        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (isDelayed && evt?.eventTypeId == BlurEvent.TypeId())
            {
                value = text;
            }
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

            public bool multiline
            {
                get { return textEdition.multiline; }
                set
                {
                    textEdition.multiline = value;
                    if (!value)
                        text = text.Replace("\n", "");
                    SetTextAlign();
                }
            }

            private void SetTextAlign()
            {
                if (multiline)
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

            protected override string StringToValue(string str) => str;
        }
    }
}
