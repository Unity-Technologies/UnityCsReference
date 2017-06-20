// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;

using UnityObject = UnityEngine.Object;

namespace UnityEditor
{
    public partial class SerializedProperty
    {
        internal SerializedProperty() {}
        ~SerializedProperty() { Dispose(); }

        // [[SerializedObject]] this property belongs to (RO).
        public SerializedObject serializedObject { get { return m_SerializedObject; } }

        public UnityObject exposedReferenceValue
        {
            get
            {
                if (propertyType != SerializedPropertyType.ExposedReference)
                    return null;

                var defaultValue = FindPropertyRelative("defaultValue");
                if (defaultValue == null)
                    return null;

                var returnedValue = defaultValue.objectReferenceValue;

                var exposedPropertyTable = serializedObject.context as IExposedPropertyTable;
                if (exposedPropertyTable != null)
                {
                    SerializedProperty exposedName = FindPropertyRelative("exposedName");
                    var propertyName = new PropertyName(exposedName.stringValue);

                    bool propertyFoundInTable = false;
                    var objReference = exposedPropertyTable.GetReferenceValue(propertyName, out propertyFoundInTable);
                    if (propertyFoundInTable == true)
                        returnedValue = objReference;
                }
                return returnedValue;
            }

            set
            {
                if (propertyType != SerializedPropertyType.ExposedReference)
                {
                    throw new System.InvalidOperationException("Attempting to set the reference value on a SerializedProperty that is not an ExposedReference");
                }

                var defaultValue = FindPropertyRelative("defaultValue");

                var exposedPropertyTable = serializedObject.context as IExposedPropertyTable;
                if (exposedPropertyTable == null)
                {
                    defaultValue.objectReferenceValue = value;
                    defaultValue.serializedObject.ApplyModifiedProperties();
                    return;
                }

                SerializedProperty exposedName = FindPropertyRelative("exposedName");

                var exposedId = exposedName.stringValue;
                if (String.IsNullOrEmpty(exposedId))
                {
                    exposedId = UnityEditor.GUID.Generate().ToString();
                    exposedName.stringValue = exposedId;
                }
                var propertyName = new PropertyName(exposedId);
                exposedPropertyTable.SetReferenceValue(propertyName, value);
            }
        }

        internal bool isScript
        {
            get { return type == "PPtr<MonoScript>"; }
        }

        // Returns a copy of the SerializedProperty iterator in its current state. This is useful if you want to keep a reference to the current property but continue with the iteration.
        public SerializedProperty Copy()
        {
            SerializedProperty property = CopyInternal();
            property.m_SerializedObject = m_SerializedObject;
            return property;
        }

        // Retrieves the SerializedProperty at a relative path to the current property.
        public SerializedProperty FindPropertyRelative(string relativePropertyPath)
        {
            SerializedProperty prop = Copy();
            if (prop.FindPropertyRelativeInternal(relativePropertyPath))
                return prop;
            else
                return null;
        }

        // Retrieves an iterator that allows you to iterator over the current nexting of a serialized property.
        public System.Collections.IEnumerator GetEnumerator()
        {
            if (isArray)
            {
                for (int i = 0; i < arraySize; i++)
                {
                    yield return GetArrayElementAtIndex(i);
                }
            }
            else
            {
                var end = GetEndProperty();
                while (NextVisible(true) && !SerializedProperty.EqualContents(this, end))
                {
                    yield return this;
                }
            }
        }

        // Returns the element at the specified index in the array.
        public SerializedProperty GetArrayElementAtIndex(int index)
        {
            SerializedProperty prop = Copy();
            if (prop.GetArrayElementAtIndexInternal(index))
                return prop;
            else
                return null;
        }

        internal void SetToValueOfTarget(UnityObject target)
        {
            SerializedProperty targetProperty = new SerializedObject(target).FindProperty(propertyPath);
            if (targetProperty == null)
            {
                Debug.LogError(target.name + " does not have the property " + propertyPath);
                return;
            }
            switch (propertyType)
            {
                case SerializedPropertyType.Integer: intValue = targetProperty.intValue; break;
                case SerializedPropertyType.Boolean: boolValue = targetProperty.boolValue; break;
                case SerializedPropertyType.Float: floatValue = targetProperty.floatValue; break;
                case SerializedPropertyType.String: stringValue = targetProperty.stringValue; break;
                case SerializedPropertyType.Color: colorValue = targetProperty.colorValue; break;
                case SerializedPropertyType.ObjectReference: objectReferenceValue = targetProperty.objectReferenceValue; break;
                case SerializedPropertyType.LayerMask: intValue = targetProperty.intValue; break;
                case SerializedPropertyType.Enum: enumValueIndex = targetProperty.enumValueIndex; break;
                case SerializedPropertyType.Vector2: vector2Value = targetProperty.vector2Value; break;
                case SerializedPropertyType.Vector3: vector3Value = targetProperty.vector3Value; break;
                case SerializedPropertyType.Vector4: vector4Value = targetProperty.vector4Value; break;
                case SerializedPropertyType.Vector2Int: vector2IntValue = targetProperty.vector2IntValue; break;
                case SerializedPropertyType.Vector3Int: vector3IntValue = targetProperty.vector3IntValue; break;
                case SerializedPropertyType.Rect: rectValue = targetProperty.rectValue; break;
                case SerializedPropertyType.RectInt: rectIntValue = targetProperty.rectIntValue; break;
                case SerializedPropertyType.ArraySize: intValue = targetProperty.intValue; break;
                case SerializedPropertyType.Character: intValue = targetProperty.intValue; break;
                case SerializedPropertyType.AnimationCurve: animationCurveValue = targetProperty.animationCurveValue; break;
                case SerializedPropertyType.Bounds: boundsValue = targetProperty.boundsValue; break;
                case SerializedPropertyType.BoundsInt: boundsIntValue = targetProperty.boundsIntValue; break;
                case SerializedPropertyType.Gradient: gradientValue = targetProperty.gradientValue; break;
                case SerializedPropertyType.ExposedReference: exposedReferenceValue = targetProperty.exposedReferenceValue; break;
            }
        }

    }
}
