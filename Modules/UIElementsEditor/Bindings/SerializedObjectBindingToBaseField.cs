// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.Bindings;

abstract class SerializedObjectBindingToBaseField<TValue, TField> : SerializedObjectBindingBase where TField : class, INotifyValueChanged<TValue>
{
    private bool isUpdating;

    EventCallback<ChangeEvent<TValue>> m_FieldValueChanged;

    private static EqualityComparer<TValue> s_EqualityComparer = EqualityComparer<TValue>.Default;

    protected override string bindingId { get; } = BindingExtensions.s_SerializedBindingId;

    protected ExpressionEvaluator.Expression m_LastFieldExpression;

    protected TField field
    {
        get { return m_Field as TField; }
        set
        {
            var ve = field as VisualElement;
            ve?.UnregisterCallback(m_FieldValueChanged, TrickleDown.TrickleDown);

            if (ve is BaseField<TValue> oldTextValueField)
            {
                oldTextValueField.expressionEvaluated -= OnExpressionEvaluated;
                oldTextValueField.UnregisterCallback<BlurEvent>(OnFieldBlur);
            }

            boundElement = value as IBindable;

            ve = field as VisualElement;
            ve?.RegisterCallback(m_FieldValueChanged, TrickleDown.TrickleDown);

            if (ve is BaseField<TValue> newTextValueField)
            {
                newTextValueField.expressionEvaluated += OnExpressionEvaluated;
                newTextValueField.RegisterCallback<BlurEvent>(OnFieldBlur);
            }
        }
    }

    private void OnFieldBlur(BlurEvent evt)
    {
        // We need to update the mixed value state when the field loses focus since we skip setting this value when typing
        if (field is IMixedValueSupport mixedValueSupport)
        {
            mixedValueSupport.showMixedValue = boundProperty.hasMultipleDifferentValues;
        }
    }

    private void OnExpressionEvaluated(ExpressionEvaluator.Expression expression)
    {
        // If we are not editing multiple objects, FieldValueChange will take care of the update
        if (!boundProperty.serializedObject.isEditingMultipleObjects)
        {
            return;
        }

        // If the expression is the same as the last one, we don't need to update the serialized values
        if (expression != null && expression.Equals(m_LastFieldExpression))
        {
            return;
        }

        m_LastFieldExpression = expression;

        if (m_LastFieldExpression != null)
        {
            FieldValueChange(m_Field as IEventHandler);
        }
    }

    protected SerializedObjectBindingToBaseField()
    {
        m_FieldValueChanged = FieldValueChanged;
    }

    private void FieldValueChanged(ChangeEvent<TValue> evt)
    {
        // We skip this case since it will be taken care of by the expression evaluation event
        if (boundProperty.m_SerializedObject.isEditingMultipleObjects && m_LastFieldExpression != null && m_LastFieldExpression.hasVariables)
        {
            return;
        }

        FieldValueChange(evt.target);
    }

    void FieldValueChange(IEventHandler target)
    {
        if (isReleased || isUpdating)
            return;

        if (target != m_Field)
            return;

        try
        {
            var undoGroup = Undo.GetCurrentGroup();

            var bindable = target as IBindable;
            var element = (VisualElement) bindable;

            if (element?.GetBinding(BindingExtensions.s_SerializedBindingId) == this && ResolveProperty())
            {
                if (!isFieldAttached)
                {
                    //we don't update when field is not attached to a panel
                    //but we don't kill binding either
                    return;
                }

                UpdateLastFieldValue();

                if (SyncFieldValueToProperty())
                {
                    bindingContext.UpdateRevision(); //we make sure to Poll the ChangeTracker here
                    bindingContext?.ResetUpdate();
                }

                var fieldUndoGroup = (int?)(field as VisualElement)?.GetProperty(UndoGroupPropertyKey);
                Undo.CollapseUndoOperations(fieldUndoGroup ?? undoGroup);

                BindingsStyleHelpers.UpdateElementStyle(field as VisualElement, boundProperty);

                return;
            }
        }
        catch (NullReferenceException e) when (e.Message.Contains("SerializedObject of SerializedProperty has been Disposed."))
        {
            //this can happen when serializedObject has been disposed of
        }

        // Something was wrong
        Unbind();
    }

    protected override void ResetCachedValues()
    {
        UpdateLastFieldValue();
        UpdateFieldIsAttached();
    }

    public override void OnPropertyValueChanged(SerializedProperty currentPropertyIterator)
    {
        if (isReleased)
            return;
        try
        {
            isUpdating = true;
            var veField = field as VisualElement;

            if (veField?.GetBinding(bindingId) == this)
            {
                SyncPropertyToField(field, currentPropertyIterator);
                BindingsStyleHelpers.UpdateElementStyle(veField, currentPropertyIterator);
                return;
            }
        }
        catch (NullReferenceException e) when (e.Message.Contains("SerializedObject of SerializedProperty has been Disposed."))
        {
            //this can happen when serializedObject has been disposed of
        }
        finally
        {
            isUpdating = false;
        }
        // We unbind here
        Unbind();
    }

    protected virtual bool EqualsValue(TValue a, TValue b) => s_EqualityComparer.Equals(a, b);

    protected override void OnFieldAttached()
    {
        var previousValue = field.value;

        base.OnFieldAttached();

        if (field is VisualElement handler && !boundProperty.hasMultipleDifferentValues &&
            EqualsValue(previousValue, field.value))
        {
            using var evt = ChangeEvent<TValue>.GetPooled(field.value, field.value);
            evt.elementTarget = handler;
            handler.SendEvent(evt);
        }
    }

    public override void SyncValueWithoutNotify(object value)
    {
        if (value is TValue castValue)
        {
            SyncFieldValueToPropertyWithoutNotify(castValue);
        }
    }

    public override BindingResult OnUpdate(in BindingContext context)
    {
        if (isReleased)
        {
            return new BindingResult(BindingStatus.Pending);
        }

        try
        {
            ResetUpdate();

            if (!IsSynced())
            {
                return new BindingResult(BindingStatus.Pending);
            }

            isUpdating = true;


            var veField = field as VisualElement;

            // Value might not have changed but prefab state could have been reverted, so we need to
            // at least update the prefab override visual if necessary. Happens when user reverts a
            // field where the value is the same as the prefab registered value. Case 1276154.
            BindingsStyleHelpers.UpdatePrefabStateStyle(veField, boundProperty);

            if (EditorApplication.isPlaying && SerializedObject.GetLivePropertyFeatureGlobalState() && boundProperty.isLiveModified)
                BindingsStyleHelpers.UpdateLivePropertyStateStyle(veField, boundProperty);

            return default;
        }
        catch (NullReferenceException e) when (e.Message.Contains("SerializedObject of SerializedProperty has been Disposed."))
        {
            //this can happen when serializedObject has been disposed of
        }
        finally
        {
            isUpdating = false;
        }

        // Something failed, we unbind here
        Unbind();
        return new BindingResult(BindingStatus.Pending);
    }

    protected internal static string GetUndoMessage(SerializedProperty serializedProperty)
    {
        var undoMessage = $"Modified {serializedProperty.name}";
        var target = serializedProperty.m_SerializedObject.targetObject;
        if (target != null && target.name != string.Empty)
        {
            undoMessage += $" in {serializedProperty.m_SerializedObject.targetObject.name}";
        }

        return undoMessage;
    }

    // Read the value from the ui field and save it.
    protected abstract void UpdateLastFieldValue();

    protected abstract bool SyncFieldValueToProperty();
    protected abstract void SyncPropertyToField(TField c, SerializedProperty p);

    protected void SyncFieldValueToPropertyWithoutNotify(TValue value)
    {
        if (EqualityComparer<TValue>.Default.Equals(value, field.value))
            return;
        field.SetValueWithoutNotify(value);
    }
}
