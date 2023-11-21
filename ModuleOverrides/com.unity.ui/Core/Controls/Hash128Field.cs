// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Makes a field for editing an <see cref="Hash128"/>. For more information, refer to [[wiki:UIE-uxml-element-Hash128Field|UXML element Hash128Field]].
    /// </summary>
    [MovedFrom(true, UpgradeConstants.EditorNamespace, UpgradeConstants.EditorAssembly)]
    public class Hash128Field : TextInputBaseField<Hash128>
    {
        // This property to alleviate the fact we have to cast all the time
        Hash128Input integerInput => (Hash128Input)textInputBase;
        internal bool m_UpdateTextFromValue;

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
            m_UpdateTextFromValue = true;
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
                if (m_UpdateTextFromValue)
                    text = rawValue.ToString();
            }
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

        public override void SetValueWithoutNotify(Hash128 newValue)
        {
            base.SetValueWithoutNotify(newValue);
            if (m_UpdateTextFromValue)
            {
                // Value is the same but the text might not be in sync
                // In the case of an expression like 2+2, the text might not be equal to the result
                text = rawValue.ToString();
            }
        }

        protected override string ValueToString(Hash128 value)
        {
            return value.ToString();
        }

        protected override Hash128 StringToValue(string str)
        {
            return Hash128Input.Parse(str);
        }

        [EventInterest(typeof(BlurEvent))]
        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt == null || textEdition.isReadOnly)
                return;

            if (evt.eventTypeId == BlurEvent.TypeId())
            {
                if (string.IsNullOrEmpty(text))
                {
                    // Make sure that empty field gets the default value
                    value = new Hash128();
                }
                else
                {
                    textInputBase.UpdateValueFromText();
                    textInputBase.UpdateTextFromValue();
                }
            }
        }
        class Hash128Input : TextInputBase
        {
            Hash128Field hash128Field => (Hash128Field)parent;

            internal Hash128Input()
            {
                textEdition.AcceptCharacter = AcceptCharacter;
            }

            protected string allowedCharacters => "0123456789abcdefABCDEF";

            internal override bool AcceptCharacter(char c)
            {
                return base.AcceptCharacter(c) && c != 0 && allowedCharacters.IndexOf(c) != -1;
            }

            public string formatString => UINumericFieldsUtils.k_IntFieldFormatString;

            protected string ValueToString(Hash128 value)
            {
                return value.ToString();
            }

            protected override Hash128 StringToValue(string str)
            {
                return Parse(str);
            }

            internal static Hash128 Parse(string str)
            {
                // Hash128.Parse does not accept strings of Length == 1, but works well with Length in the range [2, 32]
                if (str.Length == 1 && ulong.TryParse(str, out var val))
                    return new Hash128(val, 0L);

                return Hash128.Parse(str);
            }
        }
    }
}
