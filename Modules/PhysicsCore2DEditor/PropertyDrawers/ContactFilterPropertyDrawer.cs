// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace Unity.U2D.Physics.Editor
{
    /// <summary>
    /// Builds the UI Toolkit fields for a <see cref="PhysicsShape.ContactFilter"/> serialized property.
    /// The categories and contacts use 64-bit or 32-bit mask fields depending on the project's layer configuration.
    /// Add the returned element directly to embed the fields inline, or wrap it in a <see cref="Foldout"/> for a collapsible section.
    /// </summary>
    public static class ContactFilterInspector
    {
        /// <summary>
        /// Create the editable fields for a <see cref="PhysicsShape.ContactFilter"/> property, with no wrapping foldout.
        /// </summary>
        /// <param name="property">The serialized <see cref="PhysicsShape.ContactFilter"/> property to build the fields for.</param>
        /// <returns>A container with the categories, contacts, and group index fields.</returns>
        public static VisualElement CreateFields(SerializedProperty property)
        {
            var body = new VisualElement();

            const string categoriesTypeName = nameof(PhysicsShape.ContactFilter.m_Categories);
            const string contactsTypeName = nameof(PhysicsShape.ContactFilter.m_Contacts);
            const string groupIndexTypeName = nameof(PhysicsShape.ContactFilter.m_GroupIndex);

            var categoriesProperty = property.FindPropertyRelative(categoriesTypeName);
            var contactsProperty = property.FindPropertyRelative(contactsTypeName);
            var groupIndexProperty = property.FindPropertyRelative(groupIndexTypeName);

            var categoriesBindingPath = $"{categoriesTypeName}.{nameof(PhysicsShape.ContactFilter.categories.bitMask)}";
            var contactsBindingPath = $"{contactsTypeName}.{nameof(PhysicsShape.ContactFilter.contacts.bitMask)}";

            // Find if we need to show as a basic 64-bit mask.
            var showAsPhysicsMask = PhysicsMaskPropertyDrawer.ShowAsPhysicsMask(property);

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
                body.Add(categories);

                var contacts = new Mask64Field(contactsProperty.displayName, layerNames, PhysicsMask.All)
                {
                    bindingPath = contactsBindingPath,
                    choicesMasks = layerMasks
                };
                contacts.AddToClassList(Mask64Field.alignedFieldUssClassName);
                body.Add(contacts);
            }
            else
            {
                // 32-bit mask.
                var categories = new LayerMaskField32(categoriesProperty.displayName) { bindingPath = categoriesBindingPath };
                var contacts = new LayerMaskField32(contactsProperty.displayName) { bindingPath = contactsBindingPath };

                body.Add(categories);
                body.Add(contacts);
            }

            body.Add(new PropertyField(groupIndexProperty));
            return body;
        }
    }

    [CustomPropertyDrawer(typeof(PhysicsShape.ContactFilter))]
    sealed class ContactFilterPropertyDrawer : PropertyDrawer
    {
        #region UITK

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            var foldout = new Foldout { text = property.displayName, value = false, viewDataKey = typeof(ContactFilterPropertyDrawer).ToString() };
            root.Add(foldout);
            foldout.Add(ContactFilterInspector.CreateFields(property));
            return root;
        }

        #endregion

        #region IMGUI

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            return EditorGUIUtility.singleLineHeight
                + (EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight) * 3;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            var showAsPhysicsMask = PhysicsMaskPropertyDrawer.ShowAsPhysicsMask(property);

            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var categoriesProperty = property.FindPropertyRelative(nameof(PhysicsShape.ContactFilter.m_Categories));
                var contactsProperty = property.FindPropertyRelative(nameof(PhysicsShape.ContactFilter.m_Contacts));
                var groupIndexProperty = property.FindPropertyRelative(nameof(PhysicsShape.ContactFilter.m_GroupIndex));

                var categoriesBitMask = categoriesProperty.FindPropertyRelative(nameof(PhysicsMask.bitMask));
                var contactsBitMask = contactsProperty.FindPropertyRelative(nameof(PhysicsMask.bitMask));

                float y = foldoutRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                var lineHeight = EditorGUIUtility.singleLineHeight;
                var spacing = EditorGUIUtility.standardVerticalSpacing;

                PhysicsMaskPropertyDrawer.DrawMaskField(new Rect(position.x, y, position.width, lineHeight), new GUIContent(categoriesProperty.displayName), categoriesBitMask, showAsPhysicsMask);
                y += lineHeight + spacing;

                PhysicsMaskPropertyDrawer.DrawMaskField(new Rect(position.x, y, position.width, lineHeight), new GUIContent(contactsProperty.displayName), contactsBitMask, showAsPhysicsMask);
                y += lineHeight + spacing;

                EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), groupIndexProperty, false);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        #endregion
    }
}
