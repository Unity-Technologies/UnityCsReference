// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor
{
    sealed internal class MaterialPropertyValueListViewItem : VisualElement
    {
        MaterialDefinitionStyleField m_ParentField;
        VisualElement m_FieldContainer;
        MaterialPropertyValue m_Value;

        FloatField m_FloatField;
        Vector4Field m_Vector4Field;
        ColorField m_ColorField;
        ObjectField m_ObjectField;

        public int itemIndex;

        public MaterialPropertyValueListViewItem(MaterialDefinitionStyleField parentField)
        {
            m_ParentField = parentField;
            m_FieldContainer = new VisualElement();
            m_FieldContainer.style.flexGrow = 1;
            contentContainer.Add(m_FieldContainer);

            // Create all fields once
            m_FloatField = new FloatField();
            m_FloatField.AddToClassList(FloatField.alignedFieldUssClassName);
            m_FloatField.RegisterValueChangedCallback(OnFloatChanged);
            m_FieldContainer.Add(m_FloatField);

            m_Vector4Field = new Vector4Field();
            m_Vector4Field.AddToClassList(Vector4Field.alignedFieldUssClassName);
            m_Vector4Field.RegisterValueChangedCallback(OnVectorChanged);
            m_FieldContainer.Add(m_Vector4Field);

            m_ColorField = new ColorField();
            m_ColorField.AddToClassList(ColorField.alignedFieldUssClassName);
            m_ColorField.RegisterValueChangedCallback(OnColorChanged);
            m_FieldContainer.Add(m_ColorField);

            m_ObjectField = new ObjectField();
            m_ObjectField.objectType = typeof(Texture);
            m_ObjectField.AddToClassList(ObjectField.alignedFieldUssClassName);
            m_ObjectField.RegisterValueChangedCallback(OnTextureChanged);
            m_FieldContainer.Add(m_ObjectField);
        }

        public void SetValue(MaterialPropertyValue value)
        {
            m_Value = value;

            // Hide all fields first
            m_FloatField.style.display = DisplayStyle.None;
            m_Vector4Field.style.display = DisplayStyle.None;
            m_ColorField.style.display = DisplayStyle.None;
            m_ObjectField.style.display = DisplayStyle.None;

            string label = MaterialDefinitionStyleField.SanitizePropertyName(value.name);

            // Show the appropriate field
            switch (value.type)
            {
                case MaterialPropertyValueType.Float:
                    m_FloatField.label = label;
                    m_FloatField.SetValueWithoutNotify(value.GetFloat());
                    m_FloatField.style.display = DisplayStyle.Flex;
                    break;
                case MaterialPropertyValueType.Vector:
                    m_Vector4Field.label = label;
                    m_Vector4Field.SetValueWithoutNotify(value.GetVector());
                    m_Vector4Field.style.display = DisplayStyle.Flex;
                    break;
                case MaterialPropertyValueType.Color:
                    m_ColorField.label = label;
                    m_ColorField.SetValueWithoutNotify(value.GetColor());
                    m_ColorField.style.display = DisplayStyle.Flex;
                    break;
                case MaterialPropertyValueType.Texture:
                    m_ObjectField.label = label;
                    m_ObjectField.SetValueWithoutNotify(value.textureValue);
                    m_ObjectField.style.display = DisplayStyle.Flex;
                    break;
            }
        }

        void SendChangeEvent(MaterialPropertyValue value)
        {
            using (var evt = MaterialDefinitionChangedEvent.GetPooled())
            {
                evt.elementTarget = m_ParentField;

                // Build the new MaterialDefinition with the changed property
                var currentValue = m_ParentField.value;
                var propertyValues = new List<MaterialPropertyValue>(currentValue.propertyValues ?? new List<MaterialPropertyValue>());
                if (itemIndex >= 0 && itemIndex < propertyValues.Count)
                    propertyValues[itemIndex] = value;

                m_Value = value;

                var newMatDef = new MaterialDefinition(currentValue.material, propertyValues);
                evt.newMaterialDefinition = newMatDef;
                evt.refreshField = false;

                // Update the parent field value
                m_ParentField.value = newMatDef;

                SendEvent(evt);
            }
        }

        void OnFloatChanged(ChangeEvent<float> e)
        {
            var prop = new MaterialPropertyValue()
            {
                name = m_Value.name,
                type = m_Value.type
            };
            prop.SetFloat(e.newValue);
            SendChangeEvent(prop);
        }

        void OnVectorChanged(ChangeEvent<Vector4> e)
        {
            var prop = new MaterialPropertyValue()
            {
                name = m_Value.name,
                type = m_Value.type
            };
            prop.SetVector(e.newValue);
            SendChangeEvent(prop);
        }

        void OnColorChanged(ChangeEvent<Color> e)
        {
            var prop = new MaterialPropertyValue()
            {
                name = m_Value.name,
                type = m_Value.type
            };
            prop.SetColor(e.newValue);
            SendChangeEvent(prop);
        }

        void OnTextureChanged(ChangeEvent<UnityEngine.Object> e)
        {
            SendChangeEvent(new MaterialPropertyValue()
            {
                name = m_Value.name,
                type = m_Value.type,
                textureValue = e.newValue as Texture
            });
        }
    }
}
