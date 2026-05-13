// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class BuilderInspectorUtilities
    {
        public static bool HasOverriddenField(VisualElement ve)
        {
            return ve.Q(className: BuilderConstants.InspectorLocalStyleOverrideClassName) != null;
        }


        public static IEnumerable<VisualElement> FindInspectorFields(BuilderInspector inspector, string propertyPath)
        {
            if (propertyPath.Contains("style."))
            {
                var bindingPath = propertyPath.Replace("style.", "");
                bindingPath = BuilderNameUtilities.ConvertStyleCSharpNameToUssName(bindingPath);

                if (inspector.styleFields.m_StyleFields.TryGetValue(bindingPath, out var fields))
                {
                    foreach (var field in fields)
                    {
                        if (field.HasProperty(BuilderConstants.InspectorFieldValueInfoVEPropertyName))
                            yield return field;
                    }
                }
                yield break;
            }

            var dataFields = inspector.Query<BuilderUxmlAttributesView.UxmlSerializedDataAttributeField>().Build();
            foreach (var x in dataFields)
            {
                var serializedAttribute = x.GetLinkedAttributeDescription();
                if (serializedAttribute.serializedField.Name == propertyPath || serializedAttribute.bindingPath == propertyPath)
                {
                    yield return x;
                    yield break;
                }
            }
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
            var selectionIsSelector = BuilderSharedStyles.IsSelectorElement(inspector.currentVisualElement);

            if (selectionIsSelector)
            {
                return false;
            }

            var bindingProperty = GetBindingProperty(fieldElement);

            return inspector.attributeSection.context.uxmlSerializedData is VisualElement.UxmlSerializedData serializedData &&
                HasBindingInternal(serializedData, bindingProperty);
        }

        static bool HasBindingInternal(VisualElement.UxmlSerializedData serializedDatastring, string property)
        {
            if (serializedDatastring.bindings == null)
                return false;

            foreach (var binding in serializedDatastring.bindings)
            {
                if (binding.property == property)
                    return true;
            }

            return false;
        }

        // Useful for loading the Builder inspector's icons
        public static Texture2D LoadIcon(string iconName, string subfolder = "")
        {
            return EditorGUIUtility.Load(EditorGUIUtility.isProSkin
                ? $"{BuilderConstants.IconsResourcesPath}/Dark/Inspector/{subfolder}{iconName}.png"
                : $"{BuilderConstants.IconsResourcesPath}/Light/Inspector/{subfolder}{iconName}.png") as Texture2D;
        }

        // Useful for loading icons from the authoring module
        public static Texture2D LoadIcon(string iconName, string subfolder, string iconsRootPath)
        {
            return EditorGUIUtility.Load(EditorGUIUtility.isProSkin
                ? $"{iconsRootPath}/Dark/Inspector/{subfolder}{iconName}.png"
                : $"{iconsRootPath}/Light/Inspector/{subfolder}{iconName}.png") as Texture2D;
        }
    }
}
