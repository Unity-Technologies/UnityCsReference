using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    static class EnumFieldHelpers
    {
        internal static readonly UxmlTypeAttributeDescription<Enum> type = new UxmlTypeAttributeDescription<Enum> { name = "type" };
        internal static readonly UxmlStringAttributeDescription value = new UxmlStringAttributeDescription { name = "value" };
        internal static readonly UxmlBoolAttributeDescription includeObsoleteValues = new UxmlBoolAttributeDescription() { name = "include-obsolete-values", defaultValue = false };

        internal static bool ExtractValue(IUxmlAttributes bag, CreationContext cc, out Enum resEnumValue, out bool resIncludeObsoleteValues)
        {
            resIncludeObsoleteValues = false;
            resEnumValue = null;

            var systemType = type.GetValueFromBag(bag, cc);
            if (systemType == null)
            {
                return false;
            }

            string specifiedValue = null;
            if (value.TryGetValueFromBag(bag, cc, ref specifiedValue) && !Enum.IsDefined(systemType, specifiedValue))
            {
                Debug.LogErrorFormat("EnumField: Could not parse value of '{0}', because it isn't defined in the {1} enum.", specifiedValue, systemType.FullName);
                return false;
            }

            resEnumValue = specifiedValue != null ? (Enum)Enum.Parse(systemType, specifiedValue) : (Enum)Enum.ToObject(systemType, 0);
            resIncludeObsoleteValues = includeObsoleteValues.GetValueFromBag(bag, cc);

            return true;
        }
    }

    /// <summary>
    /// Makes a dropdown for switching between enum flag values that are marked with the Flags attribute.
    /// </summary>
    public class EnumFlagsField : BaseMaskField<Enum>
    {
        public new class UxmlFactory : UxmlFactory<EnumFlagsField, UxmlTraits> {}

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
