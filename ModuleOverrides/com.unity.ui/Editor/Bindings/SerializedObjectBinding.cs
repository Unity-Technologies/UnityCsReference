// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings;

class SerializedObjectBinding<TValue> : SerializedObjectBindingPropertyToBaseField<TValue, TValue>
{
    public static void CreateBind(INotifyValueChanged<TValue> field,
        SerializedObjectBindingContext context,
        SerializedProperty property,
        Func<SerializedProperty, TValue> propGetValue,
        Action<SerializedProperty, TValue> propSetValue,
        Func<TValue, SerializedProperty, Func<SerializedProperty, TValue>, bool> propCompareValues)
    {
        var newBinding = new SerializedObjectBinding<TValue>();
        newBinding.isReleased = false;
        newBinding.SetBinding(field, context, property, propGetValue, propSetValue, propCompareValues);
    }

    private void SetBinding(INotifyValueChanged<TValue> c,
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

        var originalValue = this.lastFieldValue = c.value;

        if (c is BaseField<TValue> bf)
        {
            BindingsStyleHelpers.RegisterRightClickMenu(bf, property);
        }
        else if (c is Foldout foldout)
        {
            BindingsStyleHelpers.RegisterRightClickMenu(foldout, property);
        }

        this.field = c;

        if (compareValues(originalValue, property, getValue)) //the value hasn't changed, but we want the binding to send an event no matter what
        {
            if (this.field is VisualElement handler)
            {
                using (ChangeEvent<TValue> evt = ChangeEvent<TValue>.GetPooled(originalValue, originalValue))
                {
                    evt.target = handler;
                    handler.SendEvent(evt);
                }
            }
        }
    }

    public override void Release()
    {
        base.Release();
    }

    protected override void UpdateLastFieldValue()
    {
        if (field == null)
        {
            return;
        }

        lastFieldValue = field.value;
    }

    protected override void AssignValueToField(TValue lastValue)
    {
        if (field == null)
        {
            return;
        }

        if (field is BaseField<TValue> baseField)
        {
            baseField.SetValueWithoutValidation(lastValue);
        }
        else
        {
            field.value = lastValue;
        }
    }
}
