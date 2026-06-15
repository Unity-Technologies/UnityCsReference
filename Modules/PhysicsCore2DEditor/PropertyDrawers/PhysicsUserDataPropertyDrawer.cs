// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics.Editor
{
    [CustomPropertyDrawer(typeof(PhysicsUserData))]
    sealed class PhysicsUserDataPropertyDrawer : PropertyDrawer
    {
        SerializedProperty m_EntityIdProperty;

        #region UITK

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            var foldout = new Foldout { text = property.displayName, value = false, viewDataKey = typeof(PhysicsUserDataPropertyDrawer).ToString() };
            root.Add(foldout);

            // Special handling for Entity Id.
            {
                m_EntityIdProperty = property.FindPropertyRelative(nameof(PhysicsUserData.m_EntityId));

                var objectField = new ObjectField("Object") { value = PhysicsGlobal_GetObject(m_EntityIdProperty.entityIdValue) };
                objectField.tooltip = GetEntityTooltip(m_EntityIdProperty.entityIdValue);
                objectField.AddToClassList(ObjectField.alignedFieldUssClassName);
                objectField.RegisterValueChangedCallback(evt =>
                {
                    m_EntityIdProperty.entityIdValue = evt.newValue != null ? evt.newValue.GetEntityId() : EntityId.None;
                    m_EntityIdProperty.serializedObject.ApplyModifiedProperties();

                    // Update the tooltip.
                    objectField.tooltip = GetEntityTooltip(m_EntityIdProperty.entityIdValue);
                });
                foldout.Add(objectField);
            }

            // Remaining properties.
            {
                var physicsMaskProperty = property.FindPropertyRelative(nameof(PhysicsUserData.m_PhysicsMask));
                var floatProperty = property.FindPropertyRelative(nameof(PhysicsUserData.m_Float));
                var intProperty = property.FindPropertyRelative(nameof(PhysicsUserData.m_Int));
                var int64Property = property.FindPropertyRelative(nameof(PhysicsUserData.m_Int64));
                var boolProperty = property.FindPropertyRelative(nameof(PhysicsUserData.m_Bool));

                foldout.Add(new PropertyField(physicsMaskProperty));
                foldout.Add(new PropertyField(floatProperty));
                foldout.Add(new PropertyField(intProperty));
                foldout.Add(new PropertyField(int64Property));
                foldout.Add(new PropertyField(boolProperty));
            }

            return root;
        }

        #endregion

        #region IMGUI

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            return EditorGUIUtility.singleLineHeight
                + (EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight) * 6;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var entityIdProperty = property.FindPropertyRelative(nameof(PhysicsUserData.m_EntityId));
                var physicsMaskProperty = property.FindPropertyRelative(nameof(PhysicsUserData.m_PhysicsMask));
                var floatProperty = property.FindPropertyRelative(nameof(PhysicsUserData.m_Float));
                var intProperty = property.FindPropertyRelative(nameof(PhysicsUserData.m_Int));
                var int64Property = property.FindPropertyRelative(nameof(PhysicsUserData.m_Int64));
                var boolProperty = property.FindPropertyRelative(nameof(PhysicsUserData.m_Bool));

                float y = foldoutRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                var lineHeight = EditorGUIUtility.singleLineHeight;
                var spacing = EditorGUIUtility.standardVerticalSpacing;

                var obj = PhysicsGlobal_GetObject(entityIdProperty.entityIdValue);
                EditorGUI.BeginChangeCheck();
                var newObj = EditorGUI.ObjectField(new Rect(position.x, y, position.width, lineHeight), new GUIContent("Object"), obj, typeof(UnityEngine.Object), true);
                if (EditorGUI.EndChangeCheck())
                    entityIdProperty.entityIdValue = newObj != null ? newObj.GetEntityId() : EntityId.None;
                y += lineHeight + spacing;

                EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), physicsMaskProperty, false);
                y += lineHeight + spacing;

                EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), floatProperty, false);
                y += lineHeight + spacing;

                EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), intProperty, false);
                y += lineHeight + spacing;

                EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), int64Property, false);
                y += lineHeight + spacing;

                EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), boolProperty, false);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        #endregion

        private string GetEntityTooltip(EntityId entityID)
        {
            if (entityID == EntityId.None)
                return "None";

            var obj = PhysicsGlobal_GetObject(m_EntityIdProperty.entityIdValue);
            if (obj == null)
                return "Invalid EntityId";

            return $"EntityId: {m_EntityIdProperty.entityIdValue.ToString()} - \"{obj.name}\" ({obj.GetType()})";
        }
    }
}
