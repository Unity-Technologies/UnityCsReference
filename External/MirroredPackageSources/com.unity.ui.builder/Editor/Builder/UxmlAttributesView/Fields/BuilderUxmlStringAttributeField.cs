using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal sealed class BuilderUxmlStringAttributeFieldFactory :  IBuilderUxmlAttributeFieldFactory
    {
        public bool CanCreateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            return attribute is UxmlStringAttributeDescription;
        }

        static bool CheckNullOrEmptyTextChange(ChangeEvent<string> evt)
        {
            // Ignore change if both texts are null or empty
            return !(string.IsNullOrEmpty(evt.newValue) && string.IsNullOrEmpty(evt.previousValue));
        }

        public VisualElement CreateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute, Action<VisualElement, UxmlAttributeDescription, object, string> onValueChange)
        {
            var fieldLabel = BuilderNameUtilities.ConvertDashToHuman(attribute.name);

            if (attribute.name.Equals("value") && attributeOwner is EnumField enumField)
            {
                var uiField = new EnumField(fieldLabel);

                if (null != enumField.value)
                    uiField.Init(enumField.value, enumField.includeObsoleteValues);
                else
                    uiField.SetValueWithoutNotify(null);
                uiField.RegisterValueChangedCallback(evt =>
                {
                    if (evt.leafTarget == uiField.labelElement)
                        return;
                    InvokeValueChangedCallback(uiField, attribute, evt.newValue, ValueToUxml.Convert(uiField.value), onValueChange);
                });

                return uiField;
            }

            if (attribute.name.Equals("value") && attributeOwner is EnumFlagsField enumFlagsField)
            {
                var uiField = new EnumFlagsField(fieldLabel);

                if (null != enumFlagsField.value)
                    uiField.Init(enumFlagsField.value, enumFlagsField.includeObsoleteValues);
                else
                    uiField.SetValueWithoutNotify(null);
                uiField.RegisterValueChangedCallback(evt =>
                {
                    if (evt.leafTarget == uiField.labelElement)
                        return;
                    InvokeValueChangedCallback(uiField, attribute, evt.newValue, ValueToUxml.Convert(uiField.value), onValueChange);
                });

                return uiField;
            }

            if (attribute.name.Equals("value") && attributeOwner is TagField)
            {
                var uiField = new TagField(fieldLabel);
                uiField.RegisterValueChangedCallback(evt =>
                {
                    if (evt.leafTarget == uiField.labelElement)
                        return;
                    InvokeValueChangedCallback(uiField, attribute, evt.newValue, ValueToUxml.Convert(uiField.value), onValueChange);
                });

                return uiField;
            }
            else
            {
                var uiField = new TextField(fieldLabel);
                if (attribute.name.Equals("name") || attribute.name.Equals("view-data-key"))
                    uiField.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.leafTarget == uiField.labelElement || !CheckNullOrEmptyTextChange(evt))
                            return;
                        OnValidatedAttributeValueChange(evt, BuilderNameUtilities.attributeRegex,
                            BuilderConstants.AttributeValidationSpacialCharacters, attribute, onValueChange);
                    });
                else if (attributeOwner is VisualElement && attribute.name.Equals("binding-path") || attribute.name.Equals("data-source-path"))
                    uiField.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.leafTarget == uiField.labelElement || !CheckNullOrEmptyTextChange(evt))
                            return;
                        OnValidatedAttributeValueChange(evt, BuilderNameUtilities.bindingPathAttributeRegex,
                            BuilderConstants.BindingPathAttributeValidationSpacialCharacters, attribute, onValueChange);
                    });
                else
                {
                    uiField.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.leafTarget == uiField.labelElement || !CheckNullOrEmptyTextChange(evt))
                            return;
                        InvokeValueChangedCallback(uiField, attribute, evt.newValue, ValueToUxml.Convert(uiField.value), onValueChange);
                    });
                }

                if (attribute.name.Equals("text") || attribute.name.Equals("label"))
                {
                    uiField.multiline = true;
                    uiField.AddToClassList(BuilderConstants.InspectorMultiLineTextFieldClassName);
                }

                return uiField;
            }
        }

        public void SetFieldValue(VisualElement field, object attributeOwner, VisualTreeAsset uxmlDocument, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute, object value)
        {
            if (field is EnumField enumField)
            {
                var enumFieldAttributeOwner = attributeOwner as EnumField;
                var hasValue = enumFieldAttributeOwner.value != null;
                if (hasValue)
                    enumField.Init(enumFieldAttributeOwner.value, enumFieldAttributeOwner.includeObsoleteValues);
                else
                    enumField.SetValueWithoutNotify(null);
                enumField.SetEnabled(hasValue);
            }
            else if (field is TagField tagField)
            {
                var tagFieldAttributeOwner = attributeOwner as TagField;

                tagField.SetValueWithoutNotify(tagFieldAttributeOwner.value);
            }
            else if (field is EnumFlagsField enumFlagsField)
            {
                var enumFlagsFieldAttributeOwner = attributeOwner as EnumFlagsField;
                var hasValue = enumFlagsFieldAttributeOwner.value != null;
                if (hasValue)
                    enumFlagsField.Init(enumFlagsFieldAttributeOwner.value, enumFlagsFieldAttributeOwner.includeObsoleteValues);
                else
                    enumFlagsField.SetValueWithoutNotify(null);
                enumFlagsField.SetEnabled(hasValue);
            }
            else
            {
                var strValue = GetAttributeStringValue(value);

                if (field is TextField textField)
                {
                    textField.SetValueWithoutNotify(strValue);
                }
            }
        }

        public void ResetFieldValue(VisualElement field, object attributeOwner, VisualTreeAsset uxmlDocument, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            var a = attribute as UxmlStringAttributeDescription;

            if (field is EnumField enumField)
            {
                if (null == enumField.type)
                    enumField.SetValueWithoutNotify(null);
                else
                    enumField.SetValueWithoutNotify((Enum)Enum.ToObject(enumField.type, 0));
            }
            else if (field is TagField tagField)
            {
                tagField.SetValueWithoutNotify(a.defaultValue);
            }
            else if (field is EnumFlagsField enumFlagsField)
            {
                if (null == enumFlagsField.type)
                    enumFlagsField.SetValueWithoutNotify(null);
                else
                    enumFlagsField.SetValueWithoutNotify((Enum)Enum.ToObject(enumFlagsField.type, 0));
            }
            else
            {
                (field as TextField).SetValueWithoutNotify(a.defaultValue);
            }
        }

        private static string GetAttributeStringValue(object attributeValue)
        {
            string value;
            if (attributeValue is Enum @enum)
                value = @enum.ToString();
            else if (attributeValue is IEnumerable<string> list)
            {
                value = string.Join(",", list.ToArray());
            }
            else
            {
                value = attributeValue?.ToString();
            }

            return value;
        }

        private static void OnValidatedAttributeValueChange(ChangeEvent<string> evt, Regex regex, string message, UxmlAttributeDescription attribute, Action<VisualElement, UxmlAttributeDescription, object, string> onValueChange)
        {
            // Prevent ChangeEvent caused by changing the label of the field to be propagated
            if (string.IsNullOrEmpty(evt.newValue) && string.IsNullOrEmpty(evt.previousValue))
            {
                evt.StopPropagation();
                return;
            }

            var field = evt.elementTarget as TextField;
            if (!string.IsNullOrEmpty(evt.newValue) && !regex.IsMatch(evt.newValue))
            {
                Builder.ShowWarning(string.Format(message, field.label));
                field.SetValueWithoutNotify(evt.previousValue);
                evt.StopPropagation();
                return;
            }

            onValueChange?.Invoke(field, attribute, evt.newValue, ValueToUxml.Convert(evt.newValue));
        }

        private static void InvokeValueChangedCallback(VisualElement field, UxmlAttributeDescription attribute, object value, string uxmlValue, Action<VisualElement, UxmlAttributeDescription, object, string> onValueChange)
        {
            onValueChange?.Invoke(field, attribute, value, uxmlValue);
        }
    }
}
