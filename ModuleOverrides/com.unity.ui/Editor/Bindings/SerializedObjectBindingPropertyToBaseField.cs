// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings;

abstract class SerializedObjectBindingPropertyToBaseField<TProperty, TValue> : SerializedObjectBindingToBaseField<TValue, INotifyValueChanged<TValue>>
{
    protected Func<SerializedProperty, TProperty> propGetValue;
    protected Action<SerializedProperty, TProperty> propSetValue;
    protected Func<TProperty, SerializedProperty, Func<SerializedProperty, TProperty>, bool> propCompareValues;

    //we need to keep a copy of the last value since some fields will allocate when getting the value
    protected TProperty lastFieldValue;

    protected override void SyncPropertyToField(INotifyValueChanged<TValue> c, SerializedProperty p)
    {
        if (c == null)
        {
            throw new ArgumentNullException(nameof(c));
        }

        lastFieldValue = propGetValue(p);

        // We dont want to trigger a change event as this will cause the value to be applied to all targets.
        if (p.hasMultipleDifferentValues)
            AssignValueToFieldWithoutNotify(lastFieldValue);
        else
            AssignValueToField(lastFieldValue);
    }

    protected override bool SyncFieldValueToProperty()
    {
        if (boundProperty.hasMultipleDifferentValues || !propCompareValues(lastFieldValue, boundProperty, propGetValue))
        {
            propSetValue(boundProperty, lastFieldValue);
            boundProperty.m_SerializedObject.ApplyModifiedProperties();

            // Force the field to update its display as its label is dependent on having an up to date SerializedProperty. (UUM-27629)
            if (field is ObjectField objectField)
            {
                objectField.UpdateDisplay();
            }

            return true;
        }
        return false;
    }

    protected abstract void AssignValueToField(TProperty lastValue);

    protected abstract void AssignValueToFieldWithoutNotify(TProperty lastValue);

    public override void Release()
    {
        if (isReleased)
            return;

        if (FieldBinding == this)
        {
            FieldBinding = null;

            if (field is BaseField<TValue> bf)
            {
                BindingsStyleHelpers.UnregisterRightClickMenu(bf);
            }else if (field is Foldout foldout)
            {
                BindingsStyleHelpers.UnregisterRightClickMenu(foldout);
            }
        }

        ResetContext();
        field = null;
        propGetValue = null;
        propSetValue = null;
        propCompareValues = null;
        ResetCachedValues();
        isReleased = true;
    }
}
