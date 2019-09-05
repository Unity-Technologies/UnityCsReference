// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

        public new static readonly string ussClassName = "unity-enum-flags-field";
        public new static readonly string labelUssClassName = ussClassName + "__label";
        public new static readonly string inputUssClassName = ussClassName + "__input";

        private Type m_EnumType;
        private EnumData m_EnumData;

        public EnumFlagsField(Enum defaultValue)
            : this(null, defaultValue, false) {}

        public EnumFlagsField(Enum defaultValue, bool includeObsoleteValues)
            : this(null, defaultValue, includeObsoleteValues) {}

        public EnumFlagsField(string label, Enum defaultValue)
            : this(label, defaultValue, false)
        {
        }

        public EnumFlagsField()
            : this(null, null, false) {}


        public EnumFlagsField(string label, Enum defaultValue, bool includeObsoleteValues)
            : this(label)
        {
            if (defaultValue != null)
            {
                Init(defaultValue, includeObsoleteValues);
            }
        }

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
