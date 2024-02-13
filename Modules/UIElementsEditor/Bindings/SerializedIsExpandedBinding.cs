// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings;

class SerializedIsExpandedBinding : SerializedObjectBindingPropertyToBaseField<bool, bool>
{
    readonly Clickable m_ClickedWithAlt;

    public SerializedIsExpandedBinding()
    {
        m_ClickedWithAlt = new Clickable(OnClickWithAlt);
        m_ClickedWithAlt.activators.Clear();
        m_ClickedWithAlt.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Alt });
    }

    public static void CreateBind(Foldout field, SerializedObjectBindingContext context, SerializedProperty property)
    {
        var newBinding = new SerializedIsExpandedBinding();
        newBinding.isReleased = false;
        field?.SetBinding(BindingExtensions.s_SerializedBindingId, newBinding);
        newBinding.SetBinding(field, context, property);
        newBinding.AddClickedManipulator();
    }

    protected void SetBinding(Foldout foldout, SerializedObjectBindingContext context, SerializedProperty property)
    {
        property.unsafeMode = true;
        propGetValue = GetValue;
        propSetValue = SetValue;
        propCompareValues = SerializedPropertyHelper.ValueEquals<bool>;

        SetContext(context, property);
        var originalValue = this.lastFieldValue = foldout.value;
        BindingsStyleHelpers.RegisterRightClickMenu(foldout, property);
        field = foldout;

        if (propCompareValues(originalValue, property, propGetValue)) //the value hasn't changed, but we want the binding to send an event no matter what
        {
            using (ChangeEvent<bool> evt = ChangeEvent<bool>.GetPooled(originalValue, originalValue))
            {
                evt.elementTarget = foldout;
                foldout.SendEvent(evt);
            }
        }
    }

    public override void OnRelease()
    {
        if (isReleased)
            return;

        RemoveClickedManipulator();
        base.OnRelease();
    }

    protected override void UpdateLastFieldValue()
    {
        if (field is Foldout foldout)
            lastFieldValue = foldout.value;
    }

    protected override void AssignValueToField(bool lastValue)
    {
        if (field is Foldout foldout)
            foldout.value = lastValue;
    }

    static bool GetValue(SerializedProperty property) => property.isExpanded;
    static void SetValue(SerializedProperty property, bool value) => property.isExpanded = value;

    void AddClickedManipulator()
    {
        if (m_ClickedWithAlt != null)
        {
            m_ClickedWithAlt.target = ((Foldout)field)?.toggle;
        }
    }

    void RemoveClickedManipulator()
    {
        if (m_ClickedWithAlt != null)
        {
            m_ClickedWithAlt.target = null;
        }
    }

    void OnClickWithAlt()
    {
        EditorGUI.SetExpandedRecurse(boundProperty, !boundProperty.isExpanded);

        // Force all visible field to update
        foreach (var f in ((Foldout)field).Query<Foldout>().Build())
        {
            if (f.GetBinding(BindingExtensions.s_SerializedBindingId) is SerializedIsExpandedBinding binding)
                binding.OnPropertyValueChanged(binding.boundProperty);
        }
    }
}
