// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [CustomPropertyDrawer(typeof(FilterFunction))]
    class FilterFunctionPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();

            var typeValue = property.FindPropertyRelative("m_Type");
            var typeField = new PropertyField(typeValue);
            typeField.RegisterValueChangeCallback(evt => OnFilterFunctionTypeChanged(typeField, property, true));
            container.Add(typeField);

            var parameterContainer = new VisualElement() { name = "parameter-container" };
            container.Add(parameterContainer);

            OnFilterFunctionTypeChanged(typeField, property, false);

            return container;
        }

        void OnObjectFieldInitialized(GeometryChangedEvent evt, SerializedProperty funcProp)
        {
            var field = evt.target as ObjectField;
            field.RegisterValueChangedCallback((evt) => OnObjectFieldValueChanged(evt, funcProp));
        }

        void OnObjectFieldValueChanged(ChangeEvent<UnityEngine.Object> evt, SerializedProperty funcProp)
        {
            var field = evt.target as ObjectField;
            var target = field.parent.parent.Q<PropertyField>();

            OnFilterFunctionTypeChanged(target, funcProp, true);
        }

        void OnFilterFunctionTypeChanged(PropertyField target, SerializedProperty funcProp, bool performBinding)
        {
            var parameterContainer = target.parent.Q("parameter-container");
            parameterContainer.Clear();

            var typeProp = funcProp.FindPropertyRelative("m_Type");
            FilterFunctionType type = (FilterFunctionType)typeProp.enumValueIndex;

            FilterFunctionDefinition def = null;

            if (type == FilterFunctionType.Custom)
            {
                var defProp = funcProp.FindPropertyRelative("m_CustomDefinition");
                def = defProp.objectReferenceValue as FilterFunctionDefinition;

                var defField = new ObjectField();
                defField.bindingPath = defProp.propertyPath;
                defField.objectType = typeof(FilterFunctionDefinition);

                defField.RegisterCallbackOnce<GeometryChangedEvent>((evt) => OnObjectFieldInitialized(evt, funcProp));

                parameterContainer.Add(defField);
            }
            else
            {
                def = FilterFunctionDefinitionUtils.GetBuiltinDefinition(type);
                if (def == null)
                {
                    funcProp.FindPropertyRelative("m_ParameterCount").intValue = 0;
                    funcProp.FindPropertyRelative("m_CustomDefinition").objectReferenceValue = null;
                    return;
                }
            }

            var paramsField = funcProp.FindPropertyRelative("m_Parameters");
            SerializedProperty[] paramProps =
            {
                paramsField.FindPropertyRelative("__0"),
                paramsField.FindPropertyRelative("__1"),
                paramsField.FindPropertyRelative("__2"),
                paramsField.FindPropertyRelative("__3")
            };

            for (int i = 0; i < def?.parameters.Length; ++i)
            {
                var typeField = paramProps[i].FindPropertyRelative("m_Type");

                var p = def.parameters[i];

                SerializedProperty fieldProp = null;
                if (p.defaultValue.type == FilterParameterType.Float)
                {
                    typeField.enumValueIndex = (int)FilterParameterType.Float;
                    fieldProp = paramProps[i].FindPropertyRelative("m_FloatValue");
                }
                else if (p.defaultValue.type == FilterParameterType.Color)
                {
                    typeField.enumValueIndex = (int)FilterParameterType.Color;
                    fieldProp = paramProps[i].FindPropertyRelative("m_ColorValue");
                }

                if (fieldProp != null)
                {
                    var field = new PropertyField(fieldProp);
                    field.label = $"Value {i}";
                    parameterContainer.Add(field);
                }
            }

            if (performBinding)
            {
                funcProp.serializedObject.ApplyModifiedProperties();
                parameterContainer.Bind(funcProp.serializedObject);
            }
        }
    }
}
