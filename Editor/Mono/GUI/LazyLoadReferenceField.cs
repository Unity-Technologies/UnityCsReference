// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    //----------------------------------------------------------------------------------------------------------------------
    // What is this : Custom drawer for fields of type LazyLoadReference<T> that filters presented candidate list to assets of type 'T'.
    // Motivation(s): default object field drawer is not aware that the generic argument of LazyLoadReference<> should be used
    //   to filter the list of candidate assets.
    //----------------------------------------------------------------------------------------------------------------------
    [CustomPropertyDrawer(typeof(LazyLoadReference<>))]
    internal sealed class LazyLoadedReferenceField : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            System.Type fieldType;
            ScriptAttributeUtility.GetFieldInfoFromProperty(property, out fieldType);

            EditorGUI.BeginChangeCheck();

            var value = property.objectReferenceValue;

            position = EditorGUI.PrefixLabel(position, label);
            value = EditorGUI.ObjectField(position, value, fieldType.GetGenericArguments()[0], false);

            if (EditorGUI.EndChangeCheck())
                property.objectReferenceValue = value;
        }
    }
}
