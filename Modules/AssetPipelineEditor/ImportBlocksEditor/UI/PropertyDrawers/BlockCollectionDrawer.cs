// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.AssetImporters.ImportBlocks
{
    [CustomPropertyDrawer(typeof(BlockCollection<>))]
    internal class BlockCollectionDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var customHeader = "";

            if (fieldInfo != null)
            {
                var displayNameAttr = fieldInfo.GetCustomAttribute<InspectorNameAttribute>();
                customHeader = displayNameAttr != null
                    ? displayNameAttr.displayName
                    : ObjectNames.NicifyVariableName(fieldInfo.Name);
            }

            var box = new VisualElement();
            box.AddToClassList("block-section-container");
            var styleSheet = UiHelpers.GetBlockItemStyleSheet();
            if (styleSheet != null)
                box.styleSheets.Add(styleSheet);
            box.Add(UiHelpers.CreateListView(property.propertyPath, property.serializedObject, customHeader,
                collectionFieldType: fieldInfo?.FieldType));
            return box;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.HelpBox(position,
                "Import block collections require a UIElements inspector. Override CreateInspectorGUI on the importer's editor.",
                MessageType.Info);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 2f * EditorGUIUtility.singleLineHeight;
        }
    }
}
