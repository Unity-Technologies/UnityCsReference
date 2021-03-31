using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a field for editing an <see cref="Hash128"/>.
    /// </summary>
    public class Hash128Field : TextInputBaseField<Hash128>
    {
        // This property to alleviate the fact we have to cast all the time
        Hash128Input integerInput => (Hash128Input)textInputBase;

        /// <summary>
        /// Instantiates a <see cref="Hash128Field"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<Hash128Field, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="Hash128Field"/>.
        /// </summary>
        public new class UxmlTraits : TextValueFieldTraits<Hash128, UxmlHash128AttributeDescription> {}

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-hash128-field";

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// Initializes and returns an instance of Hash128Field.
        /// </summary>
        public Hash128Field()
            : this((string)null) {}

        /// <summary>
        /// Initializes and returns an instance of Hash128Field.
        /// </summary>
        /// <param name="maxLength">Maximum number of characters for the field.</param>
        public Hash128Field(int maxLength)
            : this(null, maxLength) {}

        /// <summary>
        /// Initializes and returns an instance of Hash128Field.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        /// <param name="maxLength">Maximum number of characters for the field.</param>
        public Hash128Field(string label, int maxLength = kMaxLengthNone)
            : base(label, maxLength, Char.MinValue, new Hash128Input())
        {
            SetValueWithoutNotify(new Hash128());
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        public override Hash128 value
        {
            get { return base.value; }
            set
            {
                base.value = value;
                if (integerInput.m_UpdateTextFromValue)
                    text = rawValue.ToString();
            }
        }

        public override void SetValueWithoutNotify(Hash128 newValue)
        {
            base.SetValueWithoutNotify(newValue);
            if (integerInput.m_UpdateTextFromValue)
            {
                // Value is the same but the text might not be in sync
                // In the case of an expression like 2+2, the text might not be equal to the result
                text = rawValue.ToString();
            }
        }

        class Hash128Input : TextInputBase
        {
            Hash128Field hash128Field => (Hash128Field)parent;

            internal Hash128Input()
            {
                m_UpdateTextFromValue = true;
            }

            internal bool m_UpdateTextFromValue;

            protected string allowedCharacters => "0123456789abcdefABCDEF";

            internal override bool AcceptCharacter(char c)
            {
                return base.AcceptCharacter(c) && c != 0 && allowedCharacters.IndexOf(c) != -1;
            }

            public string formatString => EditorGUI.kIntFieldFormatString;

            protected string ValueToString(Hash128 value)
            {
                return value.ToString();
            }

            protected override Hash128 StringToValue(string str)
            {
                // Hash128.Parse does not accept strings of Length == 1, but works well with Length in the range [2, 32]
                if (str.Length == 1 && ulong.TryParse(str, out var val))
                    return new Hash128(val, 0L);

                return Hash128.Parse(str);
            }

            protected override void ExecuteDefaultActionAtTarget(EventBase evt)
            {
                base.ExecuteDefaultActionAtTarget(evt);

                bool hasChanged = false;
                if (evt.eventTypeId == KeyDownEvent.TypeId())
                {
                    KeyDownEvent kde = evt as KeyDownEvent;

                    if ((kde?.character == 3) ||     // KeyCode.KeypadEnter
                        (kde?.character == '\n'))    // KeyCode.Return
                    {
                        // Here we should update the value, but it will be done when the blur event is handled...
                        parent.Focus();
                        evt.StopPropagation();
                        evt.PreventDefault();
                    }
                    else if (!isReadOnly)
                    {
                        hasChanged = true;
                    }
                }
                else if (!isReadOnly && evt.eventTypeId == ExecuteCommandEvent.TypeId())
                {
                    ExecuteCommandEvent commandEvt = evt as ExecuteCommandEvent;
                    string cmdName = commandEvt.commandName;
                    if (cmdName == EventCommandNames.Paste || cmdName == EventCommandNames.Cut)
                    {
                        hasChanged = true;
                    }
                }

                if (!hash128Field.isDelayed && hasChanged)
                {
                    // Prevent text from changing when the value change
                    // This allow expression (2+2) or string like 00123 to remain as typed in the TextField until enter is pressed
                    m_UpdateTextFromValue = false;
                    try
                    {
                        UpdateValueFromText();
                    }
                    finally
                    {
                        m_UpdateTextFromValue = true;
                    }
                }
            }

            protected override void ExecuteDefaultAction(EventBase evt)
            {
                base.ExecuteDefaultAction(evt);

                if (evt == null)
                {
                    return;
                }

                if (evt.eventTypeId == BlurEvent.TypeId())
                {
                    if (string.IsNullOrEmpty(text))
                    {
                        // Make sure that empty field gets the default value
                        hash128Field.value = new Hash128();
                    }
                    else
                    {
                        UpdateValueFromText();
                    }
                }
            }
        }
    }
}
