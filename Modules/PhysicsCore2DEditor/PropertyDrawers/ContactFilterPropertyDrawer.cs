// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor;

namespace Unity.U2D.Physics.Editor
{
    [CustomPropertyDrawer(typeof(PhysicsShape.ContactFilter))]
    sealed class ContactFilterPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            var foldout = new Foldout { text = property.displayName, value = false, viewDataKey = typeof(ContactFilterPropertyDrawer).ToString() };
            root.Add(foldout);

            const string categoriesTypeName = nameof(PhysicsShape.ContactFilter.m_Categories);
            const string contactsTypeName = nameof(PhysicsShape.ContactFilter.m_Contacts);
            const string groupIndexTypeName = nameof(PhysicsShape.ContactFilter.m_GroupIndex);

            var categoriesProperty = property.FindPropertyRelative(categoriesTypeName);
            var contactsProperty = property.FindPropertyRelative(contactsTypeName);
            var groupIndexProperty = property.FindPropertyRelative(groupIndexTypeName);

            var categoriesBindingPath = $"{categoriesTypeName}.{nameof(PhysicsShape.ContactFilter.categories.bitMask)}";
            var contactsBindingPath = $"{contactsTypeName}.{nameof(PhysicsShape.ContactFilter.contacts.bitMask)}";
            var groupIndexBindingPath = $"{contactsTypeName}.{nameof(PhysicsShape.ContactFilter.m_GroupIndex)}";

            // Find if we need to show as a basic 64-bit mask.
            var showAsPhysicsMask = PhysicsMaskPropertyDrawer.ShowAsPhysicsMask<PhysicsShape.ContactFilter>(property);

            // 64-bit mask.
            if (showAsPhysicsMask || PhysicsWorld.usePhysicsLayers)
            {
                var layerNames = new List<String>(capacity: 64);
                var layerMasks = new List<UInt64>(capacity: 64);

                // Are we showing as a physics mask?
                if (showAsPhysicsMask)
                {
                    // Yes, so set layer names and masks for each bit.
                    PhysicsLayers.GetBitNamesAndMasks(layerNames, layerMasks);
                }
                else
                {
                    // No, so get the layer names and masks.
                    PhysicsLayers.GetLayerNamesAndMasks(layerNames, layerMasks);
                }

                var categories = new Mask64Field(categoriesProperty.displayName, layerNames, PhysicsMask.One)
                {
                    bindingPath = categoriesBindingPath,
                    choicesMasks = layerMasks
                };
                categories.AddToClassList(Mask64Field.alignedFieldUssClassName);
                foldout.Add(categories);

                var contacts = new Mask64Field(contactsProperty.displayName, layerNames, PhysicsMask.All)
                {
                    bindingPath = contactsBindingPath,
                    choicesMasks = layerMasks
                };
                contacts.AddToClassList(Mask64Field.alignedFieldUssClassName);
                foldout.Add(contacts);

                var groupIndex = new IntegerField(groupIndexProperty.displayName) { bindingPath = groupIndexBindingPath };
                groupIndex.AddToClassList(IntegerField.alignedFieldUssClassName);
                foldout.Add(groupIndex);

                return root;
            }

            // 32-bit mask.
            {
                var categories = new LayerMaskField(categoriesProperty.displayName) { bindingPath = categoriesBindingPath };
                var contacts = new LayerMaskField(contactsProperty.displayName) { bindingPath = contactsBindingPath };
                var groupIndex = new IntegerField(groupIndexProperty.displayName) { bindingPath = groupIndexBindingPath };

                categories.AddToClassList(LayerMaskField.alignedFieldUssClassName);
                contacts.AddToClassList(LayerMaskField.alignedFieldUssClassName);
                groupIndex.AddToClassList(IntegerField.alignedFieldUssClassName);

                foldout.Add(categories);
                foldout.Add(contacts);
                foldout.Add(groupIndex);

                return root;
            }
        }
    }
}
