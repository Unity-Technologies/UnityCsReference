// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Makes a field for editing an <see cref="GUID"/>.
    /// </summary>
    [UxmlElement]
    public partial class GUIDField : TextInputBaseField<GUID>
    {
        // This property to alleviate the fact we have to cast all the time
        GUIDInput integerInput => (GUIDInput)textInputBase;

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-guid-field";
        internal new static readonly UniqueStyleString ussClassNameUnique = new(ussClassName);

        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        internal new static readonly UniqueStyleString labelUssClassNameUnique = new(labelUssClassName);

        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";
        internal new static readonly UniqueStyleString inputUssClassNameUnique = new(inputUssClassName);

        /// <summary>
        /// Initializes and returns an instance of GUIDField.
        /// </summary>
        public GUIDField()
            : this((string)null) {}

        /// <summary>
        /// Initializes and returns an instance of GUIDField.
        /// </summary>
        /// <param name="maxLength">Maximum number of characters for the field.</param>
        public GUIDField(int maxLength)
            : this(null, maxLength) {}

        /// <summary>
        /// Initializes and returns an instance of GUIDField.
        /// </summary>
        /// <param name="label">The text to use as a label.</param>
        /// <param name="maxLength">Maximum number of characters for the field.</param>
        public GUIDField(string label, int maxLength = kMaxLengthNone)
            : base(label, maxLength, Char.MinValue, new GUIDInput())
        {
            m_UpdateTextFromValue = true;
            SetValueWithoutNotify(new GUID());
            AddToClassList(ussClassNameUnique);
            labelElement.AddToClassList(labelUssClassNameUnique);
            visualInput.AddToClassList(inputUssClassNameUnique);
        }

        public override GUID value
        {
            get { return base.value; }
            set
            {
                base.value = value;
                if (m_UpdateTextFromValue)
                    text = rawValue.ToString();
            }
        }

        [UxmlAttribute("value"), Delayed]
        internal string valueAsString
        {
            get => value.ToString();
            set => this.value = new GUID(value);
        }

        internal override void UpdateValueFromText()
        {
            // Prevent text from changing when the value change
            // This allow expression (2+2) or string like 00123 to remain as typed in the TextField until enter is pressed
            m_UpdateTextFromValue = false;
            try
            {
                value = StringToValue(text);
            }
            finally
            {
                m_UpdateTextFromValue = true;
            }
        }

        internal override void UpdateTextFromValue()
        {
            text = ValueToString(rawValue);
        }

        public override void SetValueWithoutNotify(GUID newValue)
        {
            base.SetValueWithoutNotify(newValue);
            if (m_UpdateTextFromValue)
            {
                // Value is the same but the text might not be in sync
                // In the case of an expression like 2+2, the text might not be equal to the result
                text = rawValue.ToString();
            }
        }

        protected override string ValueToString(GUID value)
        {
            return value.ToString();
        }

        protected override GUID StringToValue(string str)
        {
            return GUIDInput.Parse(str);
        }

        [EventInterest(typeof(FocusOutEvent))]
        protected override void HandleEventBubbleUp(EventBase evt)
        {
            base.HandleEventBubbleUp(evt);

            if (isReadOnly)
                return;

            if (evt.eventTypeId == FocusOutEvent.TypeId())
            {
                if (string.IsNullOrEmpty(text))
                {
                    // Make sure that empty field gets the default value
                    value = new GUID();
                }
                else
                {
                    textInputBase.UpdateValueFromText();
                    textInputBase.UpdateTextFromValue();
                }
            }
        }

        class GUIDInput : TextInputBase
        {
            GUIDField GUIDField => (GUIDField)parent;

            internal GUIDInput()
            {
                textEdition.AcceptCharacter = AcceptCharacter;
            }

            protected string allowedCharacters => "0123456789abcdefABCDEF";

            internal override bool AcceptCharacter(char c)
            {
                return base.AcceptCharacter(c) && c != 0 && allowedCharacters.IndexOf(c, StringComparison.Ordinal) != -1;
            }

            public string formatString => UINumericFieldsUtils.k_IntFieldFormatString;

            protected string ValueToString(GUID value)
            {
                return value.ToString();
            }

            protected override GUID StringToValue(string str)
            {
                return Parse(str);
            }

            internal static GUID Parse(string str)
            {
                if (str.Contains("-")) // Handle dashed format
                    str = str.Replace("-", "");
                return new GUID(str);
            }
        }
    }
}
