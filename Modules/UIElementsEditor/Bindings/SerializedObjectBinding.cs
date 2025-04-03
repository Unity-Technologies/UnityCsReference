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
        ((VisualElement) field)?.SetBinding(BindingExtensions.s_SerializedBindingId, newBinding);
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

        this.lastFieldValue = c.value;

        if (c is BaseField<TValue> bf)
        {
            BindingsStyleHelpers.RegisterRightClickMenu(bf, property);
        }
        else if (c is Foldout foldout)
        {
            BindingsStyleHelpers.RegisterRightClickMenu(foldout, property);
        }

        this.field = c;
    }

    public override void OnRelease()
    {
        if (isReleased)
            return;

        base.OnRelease();
    }

    protected override void UpdateLastFieldValue()
    {
        if (field == null)
        {
            return;
        }

        lastFieldValue = field.value;
    }

    protected override void AssignValueToFieldWithoutNotify(TValue lastValue)
    {
        if (field == null)
        {
            return;
        }

        if (field is BaseField<TValue> baseField)
        {
            if (baseField.showMixedValue && boundProperty.hasMultipleDifferentValues)
            {
                // No need to update the field
                return;
            }

            baseField.SetValueWithoutNotify(lastValue);
        }
        else
        {
            field.SetValueWithoutNotify(lastValue);
        }
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

    protected override bool SyncFieldValueToProperty()
    {
        var success = false;

        if (boundProperty.m_SerializedObject.isEditingMultipleObjects
            && field is BaseField<TValue>
            && m_LastFieldExpression != null && m_LastFieldExpression.hasVariables)
        {
            if (typeof(TValue) == typeof(float) || typeof(TValue) == typeof(double))
            {
                success = true;
                var allDoublesValues = boundProperty.allDoubleValues;
                for (var i = 0; i < allDoublesValues.Length; i++)
                {
                    success = success && m_LastFieldExpression.Evaluate(ref allDoublesValues[i], i, allDoublesValues.Length);
                }

                if (success)
                {
                    boundProperty.allDoubleValues = allDoublesValues;
                }
            }
            else if (typeof(TValue) == typeof(int) || typeof(TValue) == typeof(uint) || typeof(TValue) == typeof(long) || typeof(TValue) == typeof(ulong))
            {
                success = true;
                var allLongValues = boundProperty.allLongValues;
                for (var i = 0; i < allLongValues.Length; i++)
                {
                    success = success && m_LastFieldExpression.Evaluate(ref allLongValues[i], i, allLongValues.Length);
                }

                if (success)
                {
                    boundProperty.allLongValues = allLongValues;
                }
            }

            if (success)
            {
                boundProperty.m_SerializedObject.ApplyModifiedProperties();
                return true;
            }

            // Invalid expression. No need to sync the field value to the property.
            return false;
        }

        return base.SyncFieldValueToProperty();
    }
}
