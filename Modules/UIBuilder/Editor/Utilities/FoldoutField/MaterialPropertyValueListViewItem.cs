// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;


namespace Unity.UI.Builder
{
    sealed internal class MaterialPropertyValueListViewItem : VisualElement
    {
        const string k_UxmlPath = BuilderConstants.UtilitiesPath + "/StyleField/MaterialDefinitionProp.uxml";
        const string k_UssPath = BuilderConstants.UtilitiesPath + "/StyleField/MaterialDefinitionProp.uss";
        const string k_UssDarkSkinPath = BuilderConstants.UtilitiesPath + "/StyleField/MaterialDefinitionPropDark.uss";
        const string k_UssLightSkinPath = BuilderConstants.UtilitiesPath + "/StyleField/MaterialDefinitionPropLight.uss";

        const string k_BaseClass = "unity-foldout-filter-field";
        const string k_RemoveButtonClass = k_BaseClass + "__remove-filter-button";
        const string k_FilterFunctionTypeName = "filter-function-type";
        const string k_ParamtersContainerName = "parameters-container";

        MaterialDefinitionStyleField m_ParentField;
        VisualElement m_FieldContainer;
        MaterialPropertyValue m_Value;

        public int itemIndex;

        public MaterialPropertyValueListViewItem(MaterialDefinitionStyleField parentField)
        {
            m_ParentField = parentField;
            m_FieldContainer = new VisualElement();
            m_FieldContainer.style.flexGrow = 1;
            contentContainer.Add(m_FieldContainer);
        }

        public void SetValue(MaterialPropertyValue value)
        {
            m_Value = value;

            m_FieldContainer.Clear();

            string label = MaterialDefinitionStyleField.SanitizePropertyName(value.name);

            switch (value.type)
            {
                case MaterialPropertyValueType.Float:
                    var floatField = new FloatField();
                    floatField.label = label;
                    floatField.value = value.GetFloat();
                    floatField.AddToClassList(FloatField.alignedFieldUssClassName);
                    floatField.RegisterValueChangedCallback(OnFloatChanged);
                    m_FieldContainer.Add(floatField);
                    break;
                case MaterialPropertyValueType.Vector:
                    var vectorField = new Vector4Field();
                    vectorField.label = label;
                    vectorField.value = value.GetVector();
                    vectorField.AddToClassList(Vector4Field.alignedFieldUssClassName);
                    vectorField.RegisterValueChangedCallback(OnVectorChanged);
                    m_FieldContainer.Add(vectorField);
                    break;
                case MaterialPropertyValueType.Color:
                    var colorField = new ColorField();
                    colorField.label = label;
                    colorField.value = value.GetColor();
                    colorField.AddToClassList(ColorField.alignedFieldUssClassName);
                    colorField.RegisterValueChangedCallback(OnColorChanged);
                    m_FieldContainer.Add(colorField);
                    break;
                case MaterialPropertyValueType.Texture:
                    var objField = new ObjectField();
                    objField.objectType = typeof(Texture);
                    objField.label = label;
                    objField.value = value.textureValue;
                    objField.AddToClassList(ObjectField.alignedFieldUssClassName);
                    objField.RegisterValueChangedCallback(OnTextureChanged);
                    m_FieldContainer.Add(objField);
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

                // Update the parent field value, but there's no need to refresh the list
                m_ParentField.SetValueWithoutRefresh(newMatDef);

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
