// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class BuilderInspectorUtilities
    {
        public static bool HasOverriddenField(VisualElement ve)
        {
            return ve.Q(className: BuilderConstants.InspectorLocalStyleOverrideClassName) != null;
        }

        public static VisualElement FindInspectorField(BuilderInspector inspector, string propertyPath)
        {
            if (propertyPath.Contains("style."))
            {
                var bindingPath = propertyPath.Replace("style.", "");
                bindingPath = BuilderNameUtilities.ConvertStyleCSharpNameToUssName(bindingPath);

                if (inspector.styleFields.m_StyleFields.TryGetValue(bindingPath, out var fields))
                {
                    foreach (var field in fields)
                    {
                        // We can have multiple fields with the same binding path. Return the one that has the value info.
                        if (field.HasProperty(BuilderConstants.InspectorFieldValueInfoVEPropertyName))
                        {
                            return field;
                        }
                    }
                }
            }

            var dataFields = inspector.Query<BuilderUxmlAttributesView.UxmlSerializedDataAttributeField>();

            BuilderUxmlAttributesView.UxmlSerializedDataAttributeField dataField = null;
            dataFields.ForEach(x =>
            {
                var serializedAttribute =
                    x.GetLinkedAttributeDescription() as UxmlSerializedAttributeDescription;
                if (serializedAttribute.serializedField.Name == propertyPath)
                {
                    dataField = x;
                }
            });

            return dataField;
        }

        internal static string GetBindingProperty(VisualElement fieldElement)
        {
            string bindingProperty = null;

            if (fieldElement.HasProperty(BuilderConstants.InspectorStylePropertyNameVEPropertyName))
            {
                bindingProperty = fieldElement.GetProperty(BuilderConstants.InspectorStyleBindingPropertyNameVEPropertyName) as string;
            }
            else if (fieldElement.HasLinkedAttributeDescription())
            {
                bindingProperty = fieldElement.GetProperty(BuilderConstants.InspectorAttributeBindingPropertyNameVEPropertyName) as string;
            }

            return bindingProperty;
        }

        public static bool HasBinding(BuilderInspector inspector, VisualElement fieldElement)
        {
            if (inspector.attributeSection.currentFieldSource == BuilderUxmlAttributesView.AttributeFieldSource.UxmlTraits)
            {
                return false;
            }

            var selectionIsSelector = BuilderSharedStyles.IsSelectorElement(inspector.currentVisualElement);

            if (selectionIsSelector)
            {
                return false;
            }

            var bindingProperty = GetBindingProperty(fieldElement);

            return inspector.attributeSection.uxmlSerializedData is VisualElement.UxmlSerializedData serializedData &&
                serializedData.HasBindingInternal(bindingProperty);
        }

        // Useful for loading the Builder inspector's icons
        public static Texture2D LoadIcon(string iconName, string subfolder = "")
        {
            return EditorGUIUtility.Load(EditorGUIUtility.isProSkin
                ? $"{BuilderConstants.IconsResourcesPath}/Dark/Inspector/{subfolder}{iconName}.png"
                : $"{BuilderConstants.IconsResourcesPath}/Light/Inspector/{subfolder}{iconName}.png") as Texture2D;
        }
    }
}
