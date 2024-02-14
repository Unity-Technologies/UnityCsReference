// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings;

class SerializedIsExpandedBinding : SerializedObjectBindingPropertyToBaseField<bool, bool>
{
    const string serializedBindingId = "--unity-serialized-is-expanded-bindings";

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
        field?.SetProperty(serializedBindingId, newBinding);
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
                evt.target = foldout;
                foldout.SendEvent(evt);
            }
        }
    }

    public override void Release()
    {
        if (isReleased)
            return;

        base.Release();
        RemoveClickedManipulator();
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

    void AddClickedManipulator() => ((Foldout)field).toggle.AddManipulator(m_ClickedWithAlt);
    void RemoveClickedManipulator() => ((Foldout)field).RemoveManipulator(m_ClickedWithAlt);

    void OnClickWithAlt()
    {
        EditorGUI.SetExpandedRecurse(boundProperty, !boundProperty.isExpanded);

        // Force all visible field to update
        foreach (var f in ((Foldout)field).Query<Foldout>().Build())
        {
            if (f.GetProperty(serializedBindingId) is SerializedIsExpandedBinding binding)
                binding.OnPropertyValueChanged(binding.boundProperty);
        }
    }
}
