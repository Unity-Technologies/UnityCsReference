// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings;

// specific enum version that binds on the index property of the BaseField<Enum>
class SerializedManagedEnumBinding : SerializedObjectBindingToBaseField<Enum, BaseField<Enum>>
{
    //we need to keep a copy of the last value since some fields will allocate when getting the value
    private int lastEnumValue;
    private Type managedType;

    public static void CreateBind(BaseField<Enum> field,  SerializedObjectBindingContext context,
        SerializedProperty property)
    {
        Type managedType;
        ScriptAttributeUtility.GetFieldInfoFromProperty(property, out managedType);

        if (managedType == null)
        {
            Debug.LogWarning(
                $"{field.GetType().FullName} is not compatible with property \"{property.propertyPath}\". " +
                "Make sure you're binding to a managed enum type");
            return;
        }

        var newBinding = new SerializedManagedEnumBinding();
        newBinding.isReleased = false;
        newBinding.SetBinding(field, context, property, managedType);
    }

    private void SetBinding(BaseField<Enum> c, SerializedObjectBindingContext context,
        SerializedProperty property, Type manageType)
    {
        this.managedType = manageType;
        property.unsafeMode = true;

        SetContext(context, property);

        int enumValueAsInt = property.intValue;

        Enum value = GetEnumFromSerializedFromInt(manageType, ref enumValueAsInt);

        if (c is EnumField)
            (c as EnumField).Init(value);
        else if (c is EnumFlagsField)
            (c as EnumFlagsField).Init(value);
        else
        {
            throw new InvalidOperationException(c.GetType() + " cannot be bound to a enum");
        }

        lastEnumValue = enumValueAsInt;

        var previousValue = c.value;

        c.value = value;

        BindingsStyleHelpers.RegisterRightClickMenu(c, property);

        // Make sure to write this property only after setting a first value into the field
        // This avoid any null checks in regular update methods
        this.field = c;

        if (!EqualityComparer<Enum>.Default.Equals(previousValue, c.value))
        {
            if (c is VisualElement handler)
            {
                using (ChangeEvent<Enum> evt = ChangeEvent<Enum>.GetPooled(previousValue, previousValue))
                {
                    evt.target = handler;
                    handler.SendEvent(evt);
                }
            }
        }
    }

    static Enum GetEnumFromSerializedFromInt(Type managedType, ref int enumValueAsInt)
    {
        var enumData = EnumDataUtility.GetCachedEnumData(managedType);

        if (enumData.flags)
            return EnumDataUtility.IntToEnumFlags(managedType, enumValueAsInt);

        int valueIndex = Array.IndexOf(enumData.flagValues, enumValueAsInt);
        if (valueIndex != -1)
            return enumData.values[valueIndex];

        // For binding, return the minimal default value if enumValueAsInt is smaller than the smallest enum value,
        // especially if no default enum is defined
        if (enumData.flagValues.Length != 0)
        {
            var minIntValue = enumData.flagValues[0];
            var minIntValueIndex = 0;
            for (int i = 1; i < enumData.flagValues.Length; i++)
            {
                if (enumData.flagValues[i] < minIntValue)
                {
                    minIntValueIndex = i;
                    minIntValue = enumData.flagValues[i];
                }
            }

            if (enumValueAsInt <= minIntValue || (enumValueAsInt == 0 && minIntValue < 0))
            {
                enumValueAsInt = minIntValue;
                return enumData.values[minIntValueIndex];
            }
        }

        Debug.LogWarning("Error: invalid enum value " + enumValueAsInt + " for type " + managedType);
        return null;
    }

    protected override void SyncPropertyToField(BaseField<Enum> c, SerializedProperty p)
    {
        if (p == null)
        {
            throw new ArgumentNullException(nameof(p));
        }
        if (c == null)
        {
            throw new ArgumentNullException(nameof(c));
        }

        int enumValueAsInt = p.intValue;
        field.value = GetEnumFromSerializedFromInt(managedType, ref enumValueAsInt);
        lastEnumValue = enumValueAsInt;
    }

    protected override void UpdateLastFieldValue()
    {
        if (field == null || managedType == null)
        {
            lastEnumValue = 0;
            return;
        }

        var enumData = EnumDataUtility.GetCachedEnumData(managedType);

        Enum fieldValue = field?.value;

        if (enumData.flags)
            lastEnumValue = EnumDataUtility.EnumFlagsToInt(enumData, fieldValue);
        else
        {
            int valueIndex = Array.IndexOf(enumData.values, fieldValue);

            if (valueIndex != -1)
                lastEnumValue = enumData.flagValues[valueIndex];
            else
            {
                lastEnumValue = 0;
                if (field != null)
                    Debug.LogWarning("Error: invalid enum value " + fieldValue + " for type " + managedType);
            }
        }
    }

    protected override bool SyncFieldValueToProperty()
    {
        if (lastEnumValue == boundProperty.intValue)
            return false;

        // When the value is a negative we need to convert it or it will be clamped.
        var underlyingType = managedType.GetEnumUnderlyingType();
        if (lastEnumValue < 0 && (underlyingType == typeof(uint) || underlyingType == typeof(ushort) || underlyingType == typeof(byte)))
        {
            boundProperty.longValue = (uint)lastEnumValue;
        }
        else
        {
            boundProperty.intValue = lastEnumValue;
        }
        boundProperty.m_SerializedObject.ApplyModifiedProperties();
        return true;
    }

    public override void Release()
    {
        if (isReleased)
            return;

        if (FieldBinding == this)
        {
            // Make sure to nullify the field to unbind before reverting the enum value
            var saveField = field;
            BindingsStyleHelpers.UnregisterRightClickMenu(saveField);

            field = null;
            saveField.SetValueWithoutNotify(null);
            FieldBinding = null;
        }

        ResetContext();

        field = null;
        managedType = null;
        isReleased = true;

        ResetCachedValues();
    }
}
