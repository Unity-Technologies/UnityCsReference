// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

                if (EnumFieldHelpers.ExtractValue(bag, cc, out var resEnumType, out var resEnumValue, out var resIncludeObsoleteValues))
                {
                    EnumFlagsField enumField = (EnumFlagsField)ve;
                    enumField.Init(resEnumValue, resIncludeObsoleteValues);
                }
                // If we didn't have a valid value, try to set the type.
                else if (null != resEnumType)
                {
                    EnumFlagsField enumField = (EnumFlagsField)ve;

                    enumField.m_EnumType = resEnumType;
                    if (enumField.m_EnumType != null)
                        enumField.PopulateDataFromType(enumField.m_EnumType);
                    enumField.value = null;
                }
                else
                {
                    var enumField = (EnumFlagsField)ve;
                    enumField.m_EnumType = null;
                    enumField.value = null;
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
        private bool m_IncludeObsoleteValues;
        private EnumData m_EnumData;


        // These properties exist so that the UIBuilder can read them.
        internal Type type => m_EnumType;
        internal bool includeObsoleteValues => m_IncludeObsoleteValues;

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

            m_IncludeObsoleteValues = includeObsoleteValues;
            PopulateDataFromType(defaultValue.GetType());

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
        internal void PopulateDataFromType(Type enumType)
        {
            m_EnumType = enumType;
            m_EnumData = EnumDataUtility.GetCachedEnumData(m_EnumType, !includeObsoleteValues);
        }
    }
}
