// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.LowLevelPhysics2D;
using UnityEngine.UIElements;

namespace UnityEditor.LowLevelPhysics2D
{
    [CustomPropertyDrawer(typeof(PhysicsQuery.QueryFilter))]
    sealed class QueryFilterPropertyDrawer : PropertyDrawer
    {
        #region UITK

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            var foldout = new Foldout { text = property.displayName, value = false, viewDataKey = typeof(QueryFilterPropertyDrawer).ToString() };
            root.Add(foldout);

            const string categoriesTypeName = nameof(PhysicsQuery.QueryFilter.m_Categories);
            const string hitCategoriesTypeName = nameof(PhysicsQuery.QueryFilter.m_HitCategories);

            var categoriesProperty = property.FindPropertyRelative(categoriesTypeName);
            var hitCategoriesProperty = property.FindPropertyRelative(hitCategoriesTypeName);

            var categoriesBindingPath = $"{categoriesTypeName}.{nameof(PhysicsQuery.QueryFilter.categories.bitMask)}";
            var hitCategoriesBindingPath = $"{hitCategoriesTypeName}.{nameof(PhysicsQuery.QueryFilter.hitCategories.bitMask)}";

            // Find if we need to show as a basic 64-bit mask.
            var showAsPhysicsMask = PhysicsMaskPropertyDrawer.ShowAsPhysicsMask<PhysicsQuery.QueryFilter>(property);

            // 64-bit mask.
            if (showAsPhysicsMask || PhysicsWorld.useFullLayers)
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

                var hitCategories = new Mask64Field(hitCategoriesProperty.displayName, layerNames, PhysicsMask.All)
                {
                    bindingPath = hitCategoriesBindingPath,
                    choicesMasks = layerMasks
                };
                hitCategories.AddToClassList(Mask64Field.alignedFieldUssClassName);
                foldout.Add(hitCategories);

                return root;
            }

            // 32-bit mask.
            {
                var categories = new LayerMaskField(categoriesProperty.displayName) { bindingPath = categoriesBindingPath };
                var hitCategories = new LayerMaskField(hitCategoriesProperty.displayName) { bindingPath = hitCategoriesBindingPath };

                categories.AddToClassList(LayerMaskField.alignedFieldUssClassName);
                hitCategories.AddToClassList(LayerMaskField.alignedFieldUssClassName);

                foldout.Add(categories);
                foldout.Add(hitCategories);

                return root;
            }
        }

        #endregion

        #region IMGUI

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            return EditorGUIUtility.singleLineHeight
                + (EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight) * 2;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            var showAsPhysicsMask = PhysicsMaskPropertyDrawer.ShowAsPhysicsMask<PhysicsQuery.QueryFilter>(property);

            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var categoriesProperty = property.FindPropertyRelative(nameof(PhysicsQuery.QueryFilter.m_Categories));
                var hitCategoriesProperty = property.FindPropertyRelative(nameof(PhysicsQuery.QueryFilter.m_HitCategories));

                var categoriesBitMask = categoriesProperty.FindPropertyRelative(nameof(PhysicsMask.bitMask));
                var hitCategoriesBitMask = hitCategoriesProperty.FindPropertyRelative(nameof(PhysicsMask.bitMask));

                float y = foldoutRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                var lineHeight = EditorGUIUtility.singleLineHeight;
                var spacing = EditorGUIUtility.standardVerticalSpacing;

                PhysicsMaskPropertyDrawer.DrawMaskField(new Rect(position.x, y, position.width, lineHeight), new GUIContent(categoriesProperty.displayName), categoriesBitMask, showAsPhysicsMask);
                y += lineHeight + spacing;

                PhysicsMaskPropertyDrawer.DrawMaskField(new Rect(position.x, y, position.width, lineHeight), new GUIContent(hitCategoriesProperty.displayName), hitCategoriesBitMask, showAsPhysicsMask);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        #endregion
    }
}
