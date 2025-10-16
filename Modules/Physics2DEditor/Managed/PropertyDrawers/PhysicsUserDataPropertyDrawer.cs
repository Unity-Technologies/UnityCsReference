// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine.LowLevelPhysics2D;
using UnityEngine.UIElements;

using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEditor.LowLevelPhysics2D
{
    [CustomPropertyDrawer(typeof(PhysicsUserData))]
    sealed class PhysicsUserDataPropertyDrawer : PropertyDrawer
    {
        SerializedProperty m_EntityIdProperty;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            var foldout = new Foldout { text = property.displayName, viewDataKey = typeof(PhysicsUserDataPropertyDrawer).ToString() };
            root.Add(foldout);

            // Special handling for Entity Id.
            {
                m_EntityIdProperty = property.FindPropertyRelative(nameof(PhysicsUserData.m_EntityId));

                var objectField = new ObjectField("Object") { value = PhysicsUserData_GetObject(m_EntityIdProperty.entityIdValue) };
                objectField.AddToClassList(ObjectField.alignedFieldUssClassName);
                objectField.RegisterValueChangedCallback(evt =>
                {
                    m_EntityIdProperty.entityIdValue = evt.newValue != null ? evt.newValue.GetEntityId() : 0;
                    m_EntityIdProperty.serializedObject.ApplyModifiedProperties();
                });
                foldout.Add(objectField);
            }

            // Remaining properties.
            {
                var physicsMaskProperty = property.FindPropertyRelative(nameof(PhysicsUserData.m_PhysicsMask));
                var floatProperty = property.FindPropertyRelative(nameof(PhysicsUserData.m_Float));
                var intProperty = property.FindPropertyRelative(nameof(PhysicsUserData.m_Int));
                var boolProperty = property.FindPropertyRelative(nameof(PhysicsUserData.m_Bool));

                foldout.Add(new PropertyField(physicsMaskProperty));
                foldout.Add(new PropertyField(floatProperty));
                foldout.Add(new PropertyField(intProperty));
                foldout.Add(new PropertyField(boolProperty));
            }

            return root;
        }
    }
}
