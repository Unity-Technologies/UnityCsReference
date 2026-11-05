// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings;

abstract class SerializedObjectBindingPropertyToBaseField<TProperty, TValue> : SerializedObjectBindingToBaseField<TValue, INotifyValueChanged<TValue>>
{
    protected Func<SerializedProperty, TProperty> propGetValue;
    protected Action<SerializedProperty, TProperty> propSetValue;
    protected Func<TProperty, SerializedProperty, Func<SerializedProperty, TProperty>, bool> propCompareValues;

    //we need to keep a copy of the last value since some fields will allocate when getting the value
    protected TProperty lastFieldValue;

    TProperty m_PushedPropertyValue;
    TValue m_PushedFieldValue;
    bool m_HasPushedValue;

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

        if (p.hasMultipleDifferentValues)
            ClearPushedValue();
        else
        {
            m_HasPushedValue = true;
            m_PushedPropertyValue = lastFieldValue;
            m_PushedFieldValue = c.value;
        }
    }

    void ClearPushedValue()
    {
        m_HasPushedValue = false;
        m_PushedPropertyValue = default;
        m_PushedFieldValue = default;
    }

    // A field can normalise what it is given (a MaskField turns "all choices" into ~0); its ChangeEvent is not an edit.
    bool FieldStillHoldsPushedValue()
    {
        return m_HasPushedValue
            && EqualsValue(field.value, m_PushedFieldValue)
            && propCompareValues(m_PushedPropertyValue, boundProperty, propGetValue);
    }

    protected override bool SyncFieldValueToProperty()
    {
        var hasMultipleDifferentValues = boundProperty.hasMultipleDifferentValues;
        if (!hasMultipleDifferentValues && FieldStillHoldsPushedValue())
            return false;

        if (hasMultipleDifferentValues || !propCompareValues(lastFieldValue, boundProperty, propGetValue))
        {
            propSetValue(boundProperty, lastFieldValue);
            boundProperty.m_SerializedObject.ApplyModifiedProperties();
            return true;
        }
        return false;
    }

    protected abstract void AssignValueToField(TProperty lastValue);

    protected abstract void AssignValueToFieldWithoutNotify(TProperty lastValue);

    public override void OnRelease()
    {
        if (isReleased)
            return;

        if (field is BaseField<TValue> bf)
        {
            BindingsStyleHelpers.UnregisterRightClickMenu(bf);
        }
        else if (field is Foldout foldout)
        {
            BindingsStyleHelpers.UnregisterRightClickMenu(foldout);
        }

        ResetContext();
        field = null;
        propGetValue = null;
        propSetValue = null;
        propCompareValues = null;
        ClearPushedValue();
        ResetCachedValues();
        isReleased = true;
    }
}
