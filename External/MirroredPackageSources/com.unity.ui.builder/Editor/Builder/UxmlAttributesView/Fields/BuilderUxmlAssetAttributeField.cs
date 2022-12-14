using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    class BuilderObjectField : ObjectField
    {
        static readonly string displayUssClassName = "unity-object-field-display";
        static readonly string displayIconUssClassName = displayUssClassName + "__icon";
        static readonly string displayLabelUssClassName = displayUssClassName + "__label";
        private NonUnityObjectValue m_NonUnityObjectValue;
        private Image m_ObjectIcon;
        private Label m_ObjectLabel;

        // To be able to change the value to null, we need to set the value of the object field to a non null value first.
        public class NonUnityObjectValue : ScriptableObject
        {
            public object data { get; set;  }
        }

        public BuilderObjectField()
        {
            m_ObjectIcon = this.Q<Image>(classes: displayIconUssClassName);
            m_ObjectLabel = this.Q<Label>(classes: displayLabelUssClassName);
            RegisterCallback<DetachFromPanelEvent>((e) =>
            {
                if (m_NonUnityObjectValue != null)
                    ScriptableObject.DestroyImmediate(m_NonUnityObjectValue);
            });
        }

        public void SetNonUnityObject(object obj)
        {
            var valueChanged = m_NonUnityObjectValue == null || m_NonUnityObjectValue.data == null || !EqualityComparer<object>.Default.Equals(m_NonUnityObjectValue.data, obj);

            if (valueChanged)
            {
                if (m_NonUnityObjectValue == null)
                    m_NonUnityObjectValue = ScriptableObject.CreateInstance<NonUnityObjectValue>();
                m_NonUnityObjectValue.data = obj;
                SetValueWithoutNotify(m_NonUnityObjectValue);
                UpdateDisplay();
            }
        }

        public void SetObjectWithoutNotify(object obj)
        {
            if (obj == null || obj is Object)
            {
                SetValueWithoutNotify(obj as Object);
            }
            else
            {
                SetNonUnityObject(obj);
            }
        }

        public override void SetValueWithoutNotify(Object obj)
        {
            if (m_NonUnityObjectValue != null && m_NonUnityObjectValue != obj)
                m_NonUnityObjectValue.data = null;
            base.SetValueWithoutNotify(obj);
        }

        internal override void UpdateDisplay()
        {
            if (m_NonUnityObjectValue != null && m_NonUnityObjectValue.data != null)
            {
                var type = m_NonUnityObjectValue.data.GetType();
                var objName = BuilderNameUtilities.GetNameByReflection(m_NonUnityObjectValue.data);

                m_ObjectIcon.image = AssetPreview.GetMiniTypeThumbnail(typeof(DefaultAsset));
                m_ObjectLabel.text = $"{objName} ({type.Name})";
            }
            else
            {
                base.UpdateDisplay();
            }
        }
    }

    internal class BuilderUxmlAssetAttributeFieldFactory : IBuilderUxmlAttributeFieldFactory
    {
        public bool CanCreateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            var attributeType = attribute.GetType();

            return (attributeType.IsGenericType && attributeType.GetGenericTypeDefinition() == typeof(UxmlAssetAttributeDescription<>));
        }

        public VisualElement CreateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute, Action<VisualElement, UxmlAttributeDescription, object, string> onValueChange)
        {
            var fieldLabel = BuilderNameUtilities.ConvertDashToHuman(attribute.name);
            var field = new BuilderObjectField();
            var attributeType = attribute.GetType();
            var assetType = attributeType.GetGenericArguments()[0];
            field.objectType = assetType;

            field.label = fieldLabel;

            field.RegisterValueChangedCallback((evt) =>
            {
                NotifyValueChanged(evt, field, attributeOwner, attributeUxmlOwner, attribute, ValueToUxml.Convert(evt.newValue), onValueChange);
            });
            return field;
        }

        public void SetFieldValue(VisualElement field, object attributeOwner, VisualTreeAsset uxmlDocument, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute, object value)
        {
            var objField = field as BuilderObjectField;

            if (value != null && value is not Object)
            {
                objField.SetNonUnityObject(value);
            }
            else
            {
                if (value == null)
                {
                    if (attributeUxmlOwner != null && attribute.TryGetValueFromBagAsString(attributeUxmlOwner, CreationContext.Default, out var attrValue))
                    {
                        // Asset wasn't loaded correctly, most likely due to an invalid path. Show the missing reference.
                        value = uxmlDocument.GetAsset(attrValue, objField.objectType);
                    }
                }
                objField.SetValueWithoutNotify(value as Object);
            }
        }

        public void ResetFieldValue(VisualElement field, object attributeOwner, VisualTreeAsset uxmlDocument, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            (field as ObjectField).SetValueWithoutNotify(null);
        }

        protected void NotifyValueChanged(ChangeEvent<UnityEngine.Object> evt
            , ObjectField field
            , object attributeOwner
            , UxmlAsset attributeUxmlOwner
            , UxmlAttributeDescription attribute
            , string uxmlValue
            , Action<VisualElement, UxmlAttributeDescription, object, string> onValueChange)
        {
            var assetPath = AssetDatabase.GetAssetPath(evt.newValue);
            if (BuilderAssetUtilities.IsBuiltinPath(assetPath))
            {
                Builder.ShowWarning(BuilderConstants.BuiltInAssetPathsNotSupportedMessageUxml);

                // Revert the change.
                field.SetValueWithoutNotify(evt.previousValue);
                return;
            }

            onValueChange?.Invoke(field, attribute, evt.newValue, uxmlValue);
        }
    }
}
