// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements.Bindings;

internal class SerializedObjectReferenceBinding : SerializedObjectBindingPropertyToBaseField<Object, Object>
{
    public static void CreateBind(ObjectField field, SerializedObjectBindingContext context, SerializedProperty property)
    {
        var newBinding = new SerializedObjectReferenceBinding();
        newBinding.isReleased = false;
        field.SetBinding(BindingExtensions.s_SerializedBindingId, newBinding);
        newBinding.SetBinding(field, context, property);
    }

    void SetBinding(ObjectField c, SerializedObjectBindingContext context, SerializedProperty property)
    {
        property.unsafeMode = true;
        propGetValue = SerializedPropertyHelper.GetObjectRefPropertyValue;
        propSetValue = SerializedPropertyHelper.SetObjectRefPropertyValue;
        propCompareValues = SerializedPropertyHelper.ValueEquals;

        SetContext(context, property);
        lastFieldValue = c.value;
        if (c is BaseField<Object> bf)
        {
            BindingsStyleHelpers.RegisterRightClickMenu(bf, property);
        }
        field = c;
    }

    protected override void SyncPropertyToField(INotifyValueChanged<Object> c, SerializedProperty p)
    {
        if (c == null)
        {
            throw new ArgumentNullException(nameof(c));
        }

        lastFieldValue = propGetValue(p);
        var previousValue = field.value;

        // We dont want to trigger a change event as this will cause the value to be applied to all targets.
        if (p.hasMultipleDifferentValues || ObjectField.IsMissingObjectReference(p))
            AssignValueToFieldWithoutNotify(lastFieldValue);
        else
            AssignValueToField(lastFieldValue);

        // If the value assigned is null but missing then we force an update as there are 2 possible null values for an ObjectField (None and Missing).
        if (lastFieldValue == previousValue && lastFieldValue == null && field is ObjectField objectField)
        {
            objectField.SetProperty(ObjectField.serializedPropertyKey, boundProperty);
            objectField.UpdateDisplay();

            // We expect no SerializedProperty when the value is null
            objectField.SetProperty(ObjectField.serializedPropertyKey, null);
        }
    }

    protected override bool SyncFieldValueToProperty()
    {
        if (lastFieldValue != boundProperty.objectReferenceValue || boundProperty.hasMultipleDifferentValues || ObjectField.IsMissingObjectReference(boundProperty))
        {
            boundProperty.objectReferenceValue = lastFieldValue;
            boundProperty.m_SerializedObject.ApplyModifiedProperties();

            if (field is ObjectField objectField)
            {
                // Force the field to update its display as its label is dependent on having an up to date SerializedProperty. (UUM-27629)
                objectField.UpdateDisplay();
            }

            return true;
        }
        return false;
    }

    protected override void ResetCachedValues()
    {
        base.ResetCachedValues();

        if (field is ObjectField objectField)
        {
            objectField.SetProperty(ObjectField.serializedPropertyKey, boundProperty);
            objectField.UpdateDisplay();
        }
    }

    protected override bool EqualsValue(Object a, Object b)
    {
        if (ObjectField.IsMissingObjectReference(boundProperty))
            return false;
        return base.EqualsValue(a, b);
    }

    protected override void AssignValueToField(Object lastValue)
    {
        ((ObjectField)field).SetValueWithoutValidation(lastValue);
    }

    protected override void AssignValueToFieldWithoutNotify(Object lastValue)
    {
        field.SetValueWithoutNotify(lastValue);
    }

    protected override void UpdateLastFieldValue()
    {
        if (field == null)
            return;
        lastFieldValue = field.value;
    }
}
