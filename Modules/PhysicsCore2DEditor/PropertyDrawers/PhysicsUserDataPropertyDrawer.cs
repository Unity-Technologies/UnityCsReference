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
