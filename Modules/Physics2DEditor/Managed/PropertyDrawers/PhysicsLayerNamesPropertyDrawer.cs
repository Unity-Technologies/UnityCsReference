// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.LowLevelPhysics2D;
using UnityEngine.UIElements;

namespace UnityEditor.LowLevelPhysics2D
{
    [CustomPropertyDrawer(typeof(PhysicsLayers.LayerNames))]
    sealed class PhysicsLayerNamesPropertyDrawer : PropertyDrawer
    {
        #region UITK

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

        #endregion

        #region IMGUI

        const int k_LayerCount = 64;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            // Foldout + one row per layer.
            return EditorGUIUtility.singleLineHeight
                + (EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight) * k_LayerCount;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var namesProperty = property.FindPropertyRelative(nameof(PhysicsLayers.LayerNames.m_Names));
                Ensure64Layers(namesProperty);

                var y = foldoutRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                var lineHeight = EditorGUIUtility.singleLineHeight;
                var spacing = EditorGUIUtility.standardVerticalSpacing;

                for (var i = 0; i < k_LayerCount; i++)
                {
                    var element = namesProperty.GetArrayElementAtIndex(i);
                    var rowRect = new Rect(position.x, y, position.width, lineHeight);
                    var rowLabel = new GUIContent($"Layer {i}", $"Layer {i} = (1 << {i})");

                    EditorGUI.BeginChangeCheck();
                    var newName = EditorGUI.TextField(rowRect, rowLabel, element.stringValue);
                    if (EditorGUI.EndChangeCheck())
                        element.stringValue = newName;

                    y += lineHeight + spacing;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        // Keep the backing array at exactly 64 elements (mirrors the UITK path's Ensure64Layers).
        static void Ensure64Layers(SerializedProperty namesProperty)
        {
            if (namesProperty.arraySize == k_LayerCount)
                return;

            while (namesProperty.arraySize < k_LayerCount)
                namesProperty.InsertArrayElementAtIndex(namesProperty.arraySize);
            while (namesProperty.arraySize > k_LayerCount)
                namesProperty.DeleteArrayElementAtIndex(namesProperty.arraySize - 1);

            namesProperty.serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}
