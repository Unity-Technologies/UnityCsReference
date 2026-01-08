// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor;
using UnityEngine;

namespace Unity.Mathematics.Editor
{
    [CustomPropertyDrawer(typeof(bool2x2)), CustomPropertyDrawer(typeof(bool2x3)), CustomPropertyDrawer(typeof(bool2x4))]
    [CustomPropertyDrawer(typeof(bool3x2)), CustomPropertyDrawer(typeof(bool3x3)), CustomPropertyDrawer(typeof(bool3x4))]
    [CustomPropertyDrawer(typeof(bool4x2)), CustomPropertyDrawer(typeof(bool4x3)), CustomPropertyDrawer(typeof(bool4x4))]
    [CustomPropertyDrawer(typeof(double2x2)), CustomPropertyDrawer(typeof(double2x3)), CustomPropertyDrawer(typeof(double2x4))]
    [CustomPropertyDrawer(typeof(double3x2)), CustomPropertyDrawer(typeof(double3x3)), CustomPropertyDrawer(typeof(double3x4))]
    [CustomPropertyDrawer(typeof(double4x2)), CustomPropertyDrawer(typeof(double4x3)), CustomPropertyDrawer(typeof(double4x4))]
    [CustomPropertyDrawer(typeof(float2x2)), CustomPropertyDrawer(typeof(float2x3)), CustomPropertyDrawer(typeof(float2x4))]
    [CustomPropertyDrawer(typeof(float3x2)), CustomPropertyDrawer(typeof(float3x3)), CustomPropertyDrawer(typeof(float3x4))]
    [CustomPropertyDrawer(typeof(float4x2)), CustomPropertyDrawer(typeof(float4x3)), CustomPropertyDrawer(typeof(float4x4))]
    [CustomPropertyDrawer(typeof(int2x2)), CustomPropertyDrawer(typeof(int2x3)), CustomPropertyDrawer(typeof(int2x4))]
    [CustomPropertyDrawer(typeof(int3x2)), CustomPropertyDrawer(typeof(int3x3)), CustomPropertyDrawer(typeof(int3x4))]
    [CustomPropertyDrawer(typeof(int4x2)), CustomPropertyDrawer(typeof(int4x3)), CustomPropertyDrawer(typeof(int4x4))]
    [CustomPropertyDrawer(typeof(uint2x2)), CustomPropertyDrawer(typeof(uint2x3)), CustomPropertyDrawer(typeof(uint2x4))]
    [CustomPropertyDrawer(typeof(uint3x2)), CustomPropertyDrawer(typeof(uint3x3)), CustomPropertyDrawer(typeof(uint3x4))]
    [CustomPropertyDrawer(typeof(uint4x2)), CustomPropertyDrawer(typeof(uint4x3)), CustomPropertyDrawer(typeof(uint4x4))]
    class MatrixDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;
            var rows = 1 + property.type[property.type.Length - 3] - '0';
            return rows * EditorGUIUtility.singleLineHeight + (rows - 1) * EditorGUIUtility.standardVerticalSpacing;
        }

        static ReadOnlyCollection<string> k_ColPropertyPaths =
            new ReadOnlyCollection<string>(new[] { "c0", "c1", "c2", "c3" });
        static ReadOnlyCollection<string> k_RowPropertyPaths =
            new ReadOnlyCollection<string>(new[] { "x", "y", "z", "w" });

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(position, property, label, false);

            if (Event.current.type == EventType.ContextClick && position.Contains(Event.current.mousePosition))
            {
                DoUtilityMenu(property);
                Event.current.Use();
            }

            if (!property.isExpanded)
                return;

            var rows = property.type[property.type.Length - 3] - '0';
            var cols = property.type[property.type.Length - 1] - '0';

            ++EditorGUI.indentLevel;
            position = EditorGUI.IndentedRect(position);
            --EditorGUI.indentLevel;

            var elementType = property.FindPropertyRelative("c0.x").propertyType;
            for (var row = 0; row < rows; ++row)
            {
                position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
                var elementRect = new Rect(position)
                {
                    width = elementType == SerializedPropertyType.Boolean
                        ? EditorGUIUtility.singleLineHeight
                        : (position.width - (cols - 1) * EditorGUIUtility.standardVerticalSpacing) / cols
                };
                for (var col = 0; col < cols; ++col)
                {
                    EditorGUI.PropertyField(
                        elementRect,
                        property.FindPropertyRelative($"{k_ColPropertyPaths[col]}.{k_RowPropertyPaths[row]}"),
                        GUIContent.none
                    );
                    elementRect.x += elementRect.width + EditorGUIUtility.standardVerticalSpacing;
                }
            }
        }

        Dictionary<SerializedPropertyType, Action<SerializedProperty, bool>> k_UtilityValueSetters =
            new Dictionary<SerializedPropertyType, Action<SerializedProperty, bool>>
            {
                { SerializedPropertyType.Boolean, (property, b) => property.boolValue = b },
                { SerializedPropertyType.Float, (property, b) => property.floatValue = b ? 1f : 0f },
                { SerializedPropertyType.Integer, (property, b) => property.intValue = b ? 1 : 0 }
            };

        void DoUtilityMenu(SerializedProperty property)
        {
            var rows = property.type[property.type.Length - 3] - '0';
            var cols = property.type[property.type.Length - 1] - '0';
            var elementType = property.FindPropertyRelative("c0.x").propertyType;
            var setValue = k_UtilityValueSetters[elementType];
            var menu = new GenericMenu();
            property = property.Copy();
            menu.AddItem(
                EditorGUIUtility.TrTextContent("Set to Zero"),
                false,
                () =>
                {
                    property.serializedObject.Update();;
                    for (var row = 0; row < rows; ++row)
                    for (var col = 0; col < cols; ++col)
                        setValue(
                            property.FindPropertyRelative($"{k_ColPropertyPaths[col]}.{k_RowPropertyPaths[row]}"),
                            false
                        );
                    property.serializedObject.ApplyModifiedProperties();
                }
            );
            if (rows == cols)
            {
                menu.AddItem(
                    EditorGUIUtility.TrTextContent("Reset to Identity"),
                    false,
                    () =>
                    {
                        property.serializedObject.Update();
                        for (var row = 0; row < rows; ++row)
                        for (var col = 0; col < cols; ++col)
                            setValue(
                                property.FindPropertyRelative($"{k_ColPropertyPaths[col]}.{k_RowPropertyPaths[row]}"),
                                row == col
                            );
                        property.serializedObject.ApplyModifiedProperties();
                    }
                );
            }
            menu.ShowAsContext();
        }
    }
}
