// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEditor.UIElements;
using UnityEngine.LowLevelPhysics2D;
using UnityEngine.UIElements;

namespace UnityEditor.LowLevelPhysics2D
{
    [CustomPropertyDrawer(typeof(PhysicsMask))]
    sealed class PhysicsMaskPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            const string bitMaskTypeName = nameof(PhysicsMask.bitMask);
            var bitMaskProperty = property.FindPropertyRelative(bitMaskTypeName);
            var bitMaskBindingPath = bitMaskTypeName;

            // Find if we need to show as a basic 64-bit mask.
            var showAsPhysicsMask = ShowAsPhysicsMask<PhysicsMask>(bitMaskProperty);

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

                var categories = new Mask64Field(property.displayName, layerNames, bitMaskProperty.ulongValue)
                {
                    bindingPath = bitMaskBindingPath,
                    choicesMasks = layerMasks
                };
                categories.AddToClassList(Mask64Field.alignedFieldUssClassName);
                root.Add(categories);

                return root;
            }

            // 32-bit mask.
            {
                var categories = new LayerMaskField(property.displayName) { bindingPath = bitMaskBindingPath };
                categories.AddToClassList(LayerMaskField.alignedFieldUssClassName);
                root.Add(categories);

                return root;
            }
        }

        internal static bool ShowAsPhysicsMask<T>(SerializedProperty property)
        {
            var targetObjectType = property.serializedObject.targetObject.GetType();
            var type = typeof(T);
            var showAsPhysicsMaskAttributeType = typeof(PhysicsMask.ShowAsPhysicsMaskAttribute);

            // Search for the property type.
            foreach (var propertyPath in property.propertyPath.Split('.'))
            {
                // Find fields with the attribute.
                var fieldInfo = targetObjectType.GetField(propertyPath, (BindingFlags)~0);
                if (fieldInfo != null &&
                    fieldInfo.FieldType == type
                    && fieldInfo.IsDefined(showAsPhysicsMaskAttributeType))
                        return true;

                // Find properties with the attribute.
                var propertyInfo = targetObjectType.GetProperty(propertyPath, (BindingFlags)~0);
                if (propertyInfo != null &&
                    propertyInfo.PropertyType == type &&
                    propertyInfo.IsDefined(showAsPhysicsMaskAttributeType))
                        return true;
            }

            // Not found.
            return false;
        }
    }
}
