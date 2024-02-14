// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings;

//One-way binding
class SerializedObjectStringConversionBinding<TValue> : SerializedObjectBindingPropertyToBaseField<TValue, string>
{
    public static void CreateBind(INotifyValueChanged<string> field,
        SerializedObjectBindingContext context,
        SerializedProperty property,
        Func<SerializedProperty, TValue> propGetValue,
        Action<SerializedProperty, TValue> propSetValue,
        Func<TValue, SerializedProperty, Func<SerializedProperty, TValue>, bool> propCompareValues)
    {
        var newBinding = new SerializedObjectStringConversionBinding<TValue>();
        newBinding.isReleased = false;
        newBinding.SetBinding(field, context, property, propGetValue, propSetValue, propCompareValues);
    }

    private void SetBinding(INotifyValueChanged<string> c,
        SerializedObjectBindingContext context,
        SerializedProperty property,
        Func<SerializedProperty, TValue> getValue,
        Action<SerializedProperty, TValue> setValue,
        Func<TValue, SerializedProperty, Func<SerializedProperty, TValue>, bool> compareValues)
    {
        property.unsafeMode = true;

        this.propGetValue = getValue;
        this.propSetValue = setValue;
        this.propCompareValues = compareValues;

        SetContext(context, property);

        this.field = c;

        if (c is BaseField<TValue> bf)
        {
            BindingsStyleHelpers.RegisterRightClickMenu(bf, property);
        }

        var previousFieldValue = field.value;

        // In this subclass implementation the lastFieldValue is in fact the propertyValue assigned to the field.
        // this is made to compare TValues instead of strings
        UpdateLastFieldValue();
        AssignValueToField(lastFieldValue);

        if (previousFieldValue == field.value) //the value hasn't changed, but we want the binding to send an event no matter what
        {
            if (this.field is VisualElement handler)
            {
                using (ChangeEvent<string> evt = ChangeEvent<string>.GetPooled(previousFieldValue, previousFieldValue))
                {
                    evt.target = handler;
                    handler.SendEvent(evt);
                }
            }
        }
    }

    protected override void UpdateLastFieldValue()
    {
        if (field == null)
        {
            lastFieldValue = default(TValue);
            return;
        }

        lastFieldValue = propGetValue(boundProperty);
    }

    protected override void AssignValueToField(TValue lastValue)
    {
        if (field == null)
        {
            return;
        }

        field.value = $"{lastFieldValue}";
    }
}
