// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.GraphToolkit.CSO;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A UI field to display a field from a <see cref="GraphElementModel"/> or its surrogate, if it implements <see cref="IHasInspectorSurrogate"/>. Used by the <see cref="SerializedFieldsInspector"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to display.</typeparam>
    [UnityRestricted]
    internal class ModelSerializedFieldField<TValue> : ModelPropertyField<TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelSerializedFieldField{TValue}"/> class.
        /// </summary>
        /// <param name="commandTarget">The dispatcher to use to dispatch commands when the field is edited.</param>
        /// <param name="models">The inspected model.</param>
        /// <param name="inspectedObjects">The models that owns the field.</param>
        /// <param name="inspectedField">The inspected field.</param>
        /// <param name="fieldTooltip">The tooltip for the field.</param>
        /// <param name="displayName">The field's displayed name.</param>
        public ModelSerializedFieldField(
            ICommandTarget commandTarget,
            IReadOnlyList<Model> models,
            IReadOnlyList<object> inspectedObjects,
            FieldInfo inspectedField,
            string fieldTooltip,
            string displayName)
            : base(commandTarget, models, inspectedField.Name, displayName, inspectedField, fieldTooltip)
        {
            m_ValueGetter = MakeFieldValueGetter(inspectedField, inspectedObjects);

            switch (Field)
            {
                case null:
                    break;

                case PopupField<string>:
                    Debug.Assert(typeof(Enum).IsAssignableFrom(typeof(TValue)), $"Unexpected type for field {Label}.");
                    RegisterChangedCallback<string>(evt => Enum.Parse(typeof(TValue), evt.newValue),
                        inspectedObjects, inspectedField);
                    break;

                case BaseField<Enum>:
                    if (typeof(TValue) == typeof(bool))
                    {
                        Debug.Assert(typeof(bool).IsAssignableFrom(typeof(TValue)), $"Unexpected type for field {Label}.");
                        var invertToggleAttribute = inspectedField?.GetCustomAttribute<InvertToggleAttribute>();
                        if (invertToggleAttribute != null)
                        {
                            RegisterChangedCallback<Enum>(evt => (Bool)evt.newValue == Bool.False, inspectedObjects, inspectedField);
                        }
                        else
                        {
                            RegisterChangedCallback<Enum>(evt => (Bool)evt.newValue == Bool.True, inspectedObjects, inspectedField);
                        }
                    }
                    else
                    {
                        Debug.Assert(typeof(Enum).IsAssignableFrom(typeof(TValue)), $"Unexpected type for field {Label}.");
                        RegisterChangedCallback<Enum>(evt => evt.newValue, inspectedObjects, inspectedField);
                    }
                    break;

                case ObjectField:
                    Debug.Assert(typeof(Object).IsAssignableFrom(typeof(TValue)), $"Unexpected type for field {Label}.");
                    RegisterChangedCallback<Object>(evt => evt.newValue, inspectedObjects, inspectedField);
                    break;

                case LayerMaskField:
                    Debug.Assert(typeof(TValue) == typeof(LayerMask), $"Unexpected type for field {Label}.");
                    RegisterChangedCallback<int>(evt => (LayerMask)evt.newValue, inspectedObjects, inspectedField);
                    break;

                case TextField { maxLength: 1 }:
                    Debug.Assert(typeof(TValue) == typeof(char), $"Unexpected type for field {Label}.");
                    RegisterChangedCallback<string>(evt => evt.newValue[0], inspectedObjects, inspectedField);
                    break;

                // For BaseField<TValue> and fields build by ICustomPropertyFieldBuilder.
                case Toggle:
                {
                    var invertToggleAttribute = inspectedField?.GetCustomAttribute<InvertToggleAttribute>();
                    if (invertToggleAttribute != null)
                    {
                        RegisterChangedCallback<bool>(evt => !evt.newValue, inspectedObjects, inspectedField);
                    }
                    else
                    {
                        RegisterChangedCallback<bool>(evt => evt.newValue, inspectedObjects, inspectedField);
                    }
                }
                break;
                default:
                    RegisterChangedCallback<TValue>(evt => evt.newValue, inspectedObjects, inspectedField);
                    break;
            }
        }

        void RegisterChangedCallback<TCallbackValue>(Func<ChangeEvent<TCallbackValue>, object> valueExtractor,
            IReadOnlyList<object> inspectedObjects, FieldInfo inspectedField)
        {
            if (inspectedObjects.Count > 1)
            {
                Field.RegisterCallback<ChangeEvent<TCallbackValue>, ModelPropertyField<TValue>>(
                    (e, f) =>
                    {
                        var newValue = valueExtractor(e);
                        var command = new SetInspectedModelFieldCommand(newValue, inspectedObjects, inspectedField);
                        f.CommandTarget.Dispatch(command);
                    }, this);
            }
            else if (inspectedObjects.Count == 1)
            {
                Field.RegisterCallback<ChangeEvent<TCallbackValue>, ModelPropertyField<TValue>>(
                    (e, f) =>
                    {
                        var newValue = valueExtractor(e);
                        var command = new SetInspectedGraphModelFieldCommand(newValue, inspectedObjects, inspectedField);
                        f.CommandTarget.Dispatch(command);
                    }, this);
            }
        }

        protected static Func<TValue> MakeFieldValueGetter(FieldInfo fieldInfo, IReadOnlyList<object> inspectedObjects)
        {
            if (fieldInfo != null && inspectedObjects != null)
            {
                var usePropertyAttribute = fieldInfo.GetCustomAttribute<InspectorUsePropertyAttribute>();
                if (usePropertyAttribute != null)
                {
                    var propertyInfo = ModelHelpers.GetCommonBaseType(inspectedObjects).GetProperty(usePropertyAttribute.PropertyName);
                    if (propertyInfo != null)
                    {
                        Debug.Assert(typeof(TValue) == propertyInfo.PropertyType);
                        if (inspectedObjects.Count == 1)
                            return () => (TValue)propertyInfo.GetMethod.Invoke(inspectedObjects[0], null);

                        return MakeMultipleGetter(obj => (TValue)propertyInfo.GetMethod.Invoke(obj, null), inspectedObjects);
                    }
                }

                var invertToggleAttribute = fieldInfo.GetCustomAttribute<InvertToggleAttribute>();
                Debug.Assert(typeof(TValue) == fieldInfo.FieldType);
                if (inspectedObjects.Count == 1)
                {
                    if (invertToggleAttribute != null)
                    {
                        return () => (TValue)(object)!(bool)fieldInfo.GetValue(inspectedObjects[0]);
                    }
                    return () => (TValue)fieldInfo.GetValue(inspectedObjects[0]);
                }

                if (invertToggleAttribute != null)
                {
                    MakeMultipleGetter(obj => (TValue)(object)!(bool)fieldInfo.GetValue(obj), inspectedObjects);
                }

                return MakeMultipleGetter(obj => (TValue)fieldInfo.GetValue(obj), inspectedObjects);
            }

            return null;
        }

        static Func<TValue> MakeMultipleGetter(Func<object, TValue> getFunction, IReadOnlyList<object> objects)
        {
            TValue value = getFunction(objects[0]);

            for (var i = 1; i < objects.Count; i++)
            {
                if (!Equals(value, getFunction(objects[i])))
                    return k_GetMixed;
            }

            return () => getFunction(objects[0]);
        }
    }
}
