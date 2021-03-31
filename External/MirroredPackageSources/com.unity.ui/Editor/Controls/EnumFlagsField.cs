using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a dropdown for switching between enum flag values that are marked with the Flags attribute.
    /// </summary>
    public class EnumFlagsField : BaseMaskField<Enum>
    {
        /// <summary>
        /// Instantiates a <see cref="EnumFlagsField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<EnumFlagsField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="EnumFlagsField"/>.
        /// </summary>
        public new class UxmlTraits : BaseMaskField<Enum>.UxmlTraits
        {
#pragma warning disable 414
            private UxmlTypeAttributeDescription<Enum> m_Type = EnumFieldHelpers.type;
            private UxmlStringAttributeDescription m_Value = EnumFieldHelpers.value;
            private UxmlBoolAttributeDescription m_IncludeObsoleteValues = EnumFieldHelpers.includeObsoleteValues;
#pragma warning restore 414

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                Enum resEnumValue;
                bool resIncludeObsoleteValues;
                if (EnumFieldHelpers.ExtractValue(bag, cc, out resEnumValue, out resIncludeObsoleteValues))
                {
                    EnumFlagsField enumField = (EnumFlagsField)ve;
                    enumField.Init(resEnumValue, resIncludeObsoleteValues);
                }
            }
        }

        /// <summary>
        /// USS class name for elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-enum-flags-field";
        /// <summary>
        /// USS class name for labels of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name for input elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        private Type m_EnumType;
        private EnumData m_EnumData;

        /// <summary>
        /// Constructs an EnumFlagsField with a default value, and initializes its underlying type.
        /// </summary>
        /// <param name="defaultValue">Initial value. This also detects the Enum type.</param>
        public EnumFlagsField(Enum defaultValue)
            : this(null, defaultValue, false) {}

        /// <summary>
        /// Constructs an EnumFlagsField with a default value, and initializes its underlying type.
        /// </summary>
        /// <param name="defaultValue">Initial value. This also detects the Enum type.</param>
        public EnumFlagsField(Enum defaultValue, bool includeObsoleteValues)
            : this(null, defaultValue, includeObsoleteValues) {}

        /// <summary>
        /// Constructs an EnumFlagsField with a default value, and initializes its underlying type.
        /// </summary>
        /// <param name="defaultValue">Initial value. This also detects the Enum type.</param>
        public EnumFlagsField(string label, Enum defaultValue)
            : this(label, defaultValue, false)
        {
        }

        /// <summary>
        /// Constructs an EnumFlagsField with a default value, and initializes its underlying type.
        /// </summary>
        public EnumFlagsField()
            : this(null, null, false) {}


        /// <summary>
        /// Constructs an EnumFlagsField with a default value, and initializes its underlying type.
        /// </summary>
        /// <param name="defaultValue">Initial value. This also detects the Enum type.</param>
        public EnumFlagsField(string label, Enum defaultValue, bool includeObsoleteValues)
            : this(label)
        {
            if (defaultValue != null)
            {
                Init(defaultValue, includeObsoleteValues);
            }
        }

        /// <summary>
        /// Initializes the EnumFlagsField with a default value, and initializes its underlying type.
        /// </summary>
        /// <param name="defaultValue">The typed enum value.</param>
        /// <param name="includeObsoleteValues">Set to true to display obsolete values as choices.</param>
        public void Init(Enum defaultValue, bool includeObsoleteValues = false)
        {
            if (defaultValue == null)
            {
                throw new ArgumentNullException(nameof(defaultValue));
            }

            m_EnumType = defaultValue.GetType();
            m_EnumData = EnumDataUtility.GetCachedEnumData(m_EnumType, !includeObsoleteValues);

            if (!m_EnumData.flags)
                Debug.LogWarning("EnumMaskField is not bound to enum type with the [Flags] attribute");

            choices = new List<string>(m_EnumData.displayNames);
            choicesMasks = new List<int>(m_EnumData.flagValues);

            SetValueWithoutNotify(defaultValue);
        }

        /// <summary>
        /// Constructs an EnumFlagsField with a default value, and initializes its underlying type.
        /// </summary>
        public EnumFlagsField(string label)
            : base(label)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
        }

        internal override Enum MaskToValue(int newMask)
        {
            if (m_EnumType == null)
                return null;

            return EnumDataUtility.IntToEnumFlags(m_EnumType, newMask);
        }

        internal override int ValueToMask(Enum value)
        {
            if (m_EnumType == null)
                return 0;

            return EnumDataUtility.EnumFlagsToInt(m_EnumData, value);
        }
    }
}
