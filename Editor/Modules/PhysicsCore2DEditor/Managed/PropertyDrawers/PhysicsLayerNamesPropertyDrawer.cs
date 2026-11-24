// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.U2D.Physics.Editor
{
    [CustomPropertyDrawer(typeof(PhysicsLayers.LayerNames))]
    sealed class PhysicsLayerNamesPropertyDrawer : PropertyDrawer
    {
        #region UITK

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var layerNamesProperty = property.FindPropertyRelative(nameof(PhysicsLayers.LayerNames.m_Names));

            Ensure64Layers(layerNamesProperty);

            var layerList = new ListView
            {
                headerTitle = property.displayName,
                fixedItemHeight = EditorGUIUtility.singleLineHeight + 2,
                showBorder = false,
                showBoundCollectionSize = false,
                showFoldoutHeader = true,
                horizontalScrollingEnabled = false,
                selectionType = SelectionType.None,
                bindingPath = layerNamesProperty.propertyPath,
                makeItem = () => new TextField { style = { flexDirection = FlexDirection.Row, alignItems  = Align.Center }, },
                // Each name is bound to its own array element, so binding keeps it up to date, including its mixed value, prefab override, context menu and undo.
                bindItem = (element, index) =>
                {
                    if (element is not TextField field)
                        return;

                    field.label = $"Layer {index}";
                    field.tooltip = $"Layer {index} = (1 << {index})";
                    field.BindProperty(layerNamesProperty.GetArrayElementAtIndex(index));
                },
                unbindItem = (element, _) => (element as TextField)?.Unbind()
            };

            layerList.AddToClassList(InspectorElement.ussClassName);
            layerList.Bind(property.serializedObject);

            return layerList;
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
            // The layer names' property scope covers only the header, and each name has its own, so the mixed value, override bar and context menu of each name are its own.
            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            label = EditorGUI.BeginProperty(foldoutRect, label, property);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
            EditorGUI.EndProperty();

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
                    var rowLabel = EditorGUI.BeginProperty(rowRect, new GUIContent($"Layer {i}", $"Layer {i} = (1 << {i})"), element);

                    EditorGUI.BeginChangeCheck();
                    var newName = EditorGUI.TextField(rowRect, rowLabel, element.stringValue);
                    if (EditorGUI.EndChangeCheck())
                        element.stringValue = newName;

                    EditorGUI.EndProperty();

                    y += lineHeight + spacing;
                }

                EditorGUI.indentLevel--;
            }
        }

        // Keep the backing array at exactly 64 elements, for both the UI Toolkit and IMGUI paths.
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
