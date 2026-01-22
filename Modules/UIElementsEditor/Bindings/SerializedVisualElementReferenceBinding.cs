// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings;

internal class SerializedVisualElementReferenceBinding : SerializedObjectBindingPropertyToBaseField<VisualElementReference, VisualElementReference>
{
    internal static readonly PropertyName serializedPropertyKey = new PropertyName("--unity-visual-element-reference-field-serialized-property");

    public static void CreateBind(INotifyValueChanged<VisualElementReference> field, SerializedObjectBindingContext context, SerializedProperty property)
    {
        var newBinding = new SerializedVisualElementReferenceBinding();
        newBinding.isReleased = false;
        ((VisualElement)field).SetBinding(BindingExtensions.s_SerializedBindingId, newBinding);
        newBinding.SetBinding(field, context, property);
    }

    void SetBinding(INotifyValueChanged<VisualElementReference> c, SerializedObjectBindingContext context, SerializedProperty property)
    {
        property.unsafeMode = true;
        propGetValue = GetVisualElementReferenceValue;
        propSetValue = SetVisualElementReferenceValue;
        propCompareValues = SerializedPropertyHelper.ValueEquals;

        SetContext(context, property);
        lastFieldValue = c.value;
        if (c is BaseField<VisualElementReference> bf)
        {
            BindingsStyleHelpers.RegisterRightClickMenu(bf, property);
        }
        field = c;
    }

    private static VisualElementReference GetVisualElementReferenceValue(SerializedProperty property)
    {
        return property.structValue as VisualElementReference;
    }

    private static void SetVisualElementReferenceValue(SerializedProperty property, VisualElementReference value)
    {
        var oldValue = property.structValue as VisualElementReference;
        oldValue.SetReference(value.panelRenderer, value.authoringPath);
        property.structValue = oldValue;
    }

    protected override void SyncPropertyToField(INotifyValueChanged<VisualElementReference> c, SerializedProperty p)
    {
        if (c == null)
        {
            throw new ArgumentNullException(nameof(c));
        }

        lastFieldValue = propGetValue(p);
        var previousValue = field.value;

        // We dont want to trigger a change event as this will cause the value to be applied to all targets.
        if (p.hasMultipleDifferentValues)
            AssignValueToFieldWithoutNotify(lastFieldValue);
        else
            AssignValueToField(lastFieldValue);

        // Force update if the reference values changed even if they appear equal
        if (AreReferencesEqual(lastFieldValue, previousValue) && field is VisualElement ve)
        {
            ve.SetProperty(serializedPropertyKey, boundProperty);
            UpdateFieldDisplay(ve);

            // We expect no SerializedProperty when the value is default
            if (IsDefaultReference(lastFieldValue))
            {
                ve.SetProperty(serializedPropertyKey, null);
            }
        }
    }

    protected override bool SyncFieldValueToProperty()
    {
        var currentPropertyValue = propGetValue(boundProperty);
        if (!AreReferencesEqual(lastFieldValue, currentPropertyValue) || boundProperty.hasMultipleDifferentValues)
        {
            propSetValue(boundProperty, lastFieldValue);
            boundProperty.m_SerializedObject.ApplyModifiedProperties();

            if (field is VisualElement ve)
            {
                // Force the field to update its display as its label is dependent on having an up to date SerializedProperty.
                UpdateFieldDisplay(ve);
            }

            return true;
        }
        return false;
    }

    protected override void ResetCachedValues()
    {
        base.ResetCachedValues();

        if (field is VisualElement ve)
        {
            ve.SetProperty(serializedPropertyKey, boundProperty);
            UpdateFieldDisplay(ve);
        }
    }

    protected override void AssignValueToField(VisualElementReference lastValue)
    {
        if (field is BaseField<VisualElementReference> baseField)
        {
            baseField.SetValueWithoutValidation(lastValue);
        }
        else
        {
            field.value = lastValue;
        }
    }

    protected override void AssignValueToFieldWithoutNotify(VisualElementReference lastValue)
    {
        field.SetValueWithoutNotify(lastValue);
    }

    protected override void UpdateLastFieldValue()
    {
        if (field == null)
            return;
        lastFieldValue = field.value;
    }

    private static bool AreReferencesEqual(VisualElementReference a, VisualElementReference b)
    {
        if (a == null && b == null)
            return true;
        if (a == null || b == null)
            return false;
        return a.Equals(b);
    }

    private static bool IsDefaultReference(VisualElementReference reference)
    {
        return reference == null || (reference.panelRenderer == null && reference.authoringPath.path.Length == 0);
    }

    private static void UpdateFieldDisplay(VisualElement field)
    {
        // Call UpdateDisplay if the method exists (through reflection to avoid module dependency)
        var updateDisplayMethod = field.GetType().GetMethod("UpdateDisplay", 
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        updateDisplayMethod?.Invoke(field, null);
    }
}
