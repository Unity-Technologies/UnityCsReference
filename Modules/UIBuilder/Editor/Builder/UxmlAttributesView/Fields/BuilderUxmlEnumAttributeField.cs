// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderUxmlEnumAttributeFieldFactory : BuilderTypedUxmlAttributeFieldFactoryBase<Enum, BaseField<Enum>>
    {
        public override bool CanCreateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            var attributeType = attribute.GetType();
            return (attributeType.IsGenericType && attributeType.GetGenericArguments()[0].IsEnum);
        }

        protected override BaseField<Enum> InstantiateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            var attributeType = attribute.GetType();
            var propInfo = attributeType.GetProperty("defaultValue");
            var defaultEnumValue = propInfo.GetValue(attribute, null) as Enum;

            if (defaultEnumValue.GetType().GetCustomAttribute<FlagsAttribute>() == null)
            {
                // Create and initialize the EnumField.
                var uiField = new EnumField();
                uiField.Init(defaultEnumValue);
                return uiField;
            }
            else
            {
                // Create and initialize the EnumFlagsField.
                var uiField = new EnumFlagsField();
                uiField.Init(defaultEnumValue);
                return uiField;
            }
        }

        public override void SetFieldValue(VisualElement field
            , object attributeOwner
            , VisualTreeAsset uxmlDocument
            , UxmlAsset attributeUxmlOwner
            , UxmlAttributeDescription attribute, object value)
        {
            var attributeType = attribute.GetType();
            var propInfo = attributeType.GetProperty("defaultValue");
            var defaultEnumValue = propInfo.GetValue(attribute, null) as Enum;
            string enumAttributeValueStr;
            VisualElement parentTemplate = null;
            VisualElement veAttributeOwner = null;

            if (attributeOwner is VisualElement)
            {
                veAttributeOwner = attributeOwner as VisualElement;
                parentTemplate = BuilderAssetUtilities.GetVisualElementRootTemplate(veAttributeOwner);
            }

            // In a template instance
            if (parentTemplate != null)
            {
                var parentTemplateAsset = parentTemplate.GetVisualElementAsset() as TemplateAsset;
                var fieldAttributeOverride = parentTemplateAsset.attributeOverrides.FirstOrDefault(x =>
                    x.m_AttributeName == attribute.name && x.m_ElementName == veAttributeOwner.name);

                enumAttributeValueStr = fieldAttributeOverride.m_ElementName == veAttributeOwner.name ? fieldAttributeOverride.m_Value : value.ToString();
            }
            else
            {
                enumAttributeValueStr = attributeUxmlOwner?.GetAttributeValue(attribute.name);
            }

            // Set the value from the UXML attribute.
            if (!string.IsNullOrEmpty(enumAttributeValueStr))
            {
                try
                {
                    value = Enum.Parse(defaultEnumValue.GetType(), enumAttributeValueStr, true) as Enum;
                }
                catch (ArgumentException exception)
                {
                    Debug.LogException(exception);
                    value = defaultEnumValue;
                }
                catch (OverflowException exception)
                {
                    Debug.LogException(exception);
                    value = defaultEnumValue;
                }
            }

            base.SetFieldValue(field, attributeOwner, uxmlDocument, attributeUxmlOwner, attribute, value);
        }

        public override void ResetFieldValue(VisualElement field
            , object attributeOwner
            , VisualTreeAsset uxmlDocument
            , UxmlAsset attributeUxmlOwner
            , UxmlAttributeDescription attribute)
        {
            var attributeType = attribute.GetType();
            var propInfo = attributeType.GetProperty("defaultValue");
            var defaultEnumValue = propInfo.GetValue(attribute, null) as Enum;
            var uiField = field as BaseField<Enum>;
            uiField.SetValueWithoutNotify(defaultEnumValue);
        }
    }
}
