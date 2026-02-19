// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.U2D.Physics.Editor
{
    [CustomPropertyDrawer(typeof(PhysicsLayers.LayerNames))]
    sealed class PhysicsLayerNamesPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            const string propertyPath = nameof(PhysicsLayers.LayerNames.m_Names);
            var layerNamesProperty = property.FindPropertyRelative(propertyPath);

            Ensure64Layers();
            
            var layerList = new ListView
            {
                headerTitle = property.displayName,
                fixedItemHeight = EditorGUIUtility.singleLineHeight + 2,
                showBorder = false,
                showBoundCollectionSize = false,
                showFoldoutHeader = true,
                horizontalScrollingEnabled = false,
                bindingPath = $"{propertyPath}",                
                makeItem = () => new TextField { style = { flexDirection = FlexDirection.Row, alignItems  = Align.Center }, },
                bindItem = (element, index) =>
                {
                    if (element is not TextField field)
                        return;

                    var layerName = layerNamesProperty.GetArrayElementAtIndex(index).stringValue;
                    field.label = $"Layer {index}";
                    field.tooltip = $"Layer {index} = (1 << {index})";
                    field.SetValueWithoutNotify(layerName);
                    field.userData = index;
                    
                    field.RegisterValueChangedCallback(PhysicsLayersChanged);
                },
                unbindItem = (element, _) =>
                {
                    if (element is not TextField field)
                        return;

                    field.UnregisterValueChangedCallback(PhysicsLayersChanged);
                }
            };

            layerList.AddToClassList(InspectorElement.ussClassName);
            layerList.Bind(property.serializedObject);
            layerList.TrackPropertyValue(layerNamesProperty, (_) => RefreshItems(layerList) );
            
            return layerList;

            void RefreshItems(ListView listView)
            {
                Ensure64Layers();
                listView.RefreshItems();
            }
            
            void PhysicsLayersChanged(ChangeEvent<string> evt)
            {
                if (evt.target is not TextField textField)
                    return;
                
                evt.StopPropagation();
                var index = (int)textField.userData;

                if (index >= layerNamesProperty.arraySize)
                    return;
                
                layerNamesProperty.GetArrayElementAtIndex(index).stringValue = evt.newValue;
                layerNamesProperty.serializedObject.ApplyModifiedProperties();
            }

            void Ensure64Layers()
            {
                // Ensure the array is always 64 elements!
                if (layerNamesProperty.arraySize == 64)
                    return;

                // Insert elements until filled.
                do
                {
                    layerNamesProperty.InsertArrayElementAtIndex(layerNamesProperty.arraySize);

                } while (layerNamesProperty.arraySize != 64);
                
                layerNamesProperty.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
