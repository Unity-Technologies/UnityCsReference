using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class FoldoutColorField : FoldoutField
    {
        static readonly string k_FieldClassName = BuilderConstants.FoldoutFieldPropertyName + "__color-field";
        static readonly string k_MixedValueLineClassName = BuilderConstants.FoldoutFieldPropertyName + "__mixed-value-line";

        public new class UxmlFactory : UxmlFactory<FoldoutColorField, UxmlTraits> {}

        public new class UxmlTraits : FoldoutField.UxmlTraits {}

        ColorField m_ColorField;
        VisualElement m_MixedValueLine;
        public List<Color> fieldValues = new List<Color>();

        public bool isMixed
        {
            get
            {
                if (fieldValues.Count == 0)
                    return true;

                var allSame = fieldValues.All(o => o == fieldValues[0]);
                return !allSame;
            }
        }

        public ColorField headerInputField
        {
            get
            {
                return m_ColorField;
            }
        }

        public FoldoutColorField()
        {
            m_ColorField = new ColorField();
            m_ColorField.name = "field";
            m_ColorField.AddToClassList(k_FieldClassName);
            m_ColorField.RegisterValueChangedCallback((e) => m_MixedValueLine.style.display = DisplayStyle.None);
            header.hierarchy.Add(m_ColorField);

            m_MixedValueLine = new VisualElement();
            m_MixedValueLine.name = "mixed-value-line";
            m_MixedValueLine.AddToClassList(k_MixedValueLineClassName);
            m_ColorField.Q<IMGUIContainer>().hierarchy.Add(m_MixedValueLine);
        }

        public override void UpdateFromChildFields()
        {
            var styleFields = this.contentContainer.Query<ColorField>().ToList();
            for (int i = 0; i < styleFields.Count; ++i)
            {
                var styleField = styleFields[i];
                UpdateFromChildField(bindingPathArray[i], styleField.value);
            }
        }

        public void UpdateFromChildField(string bindingPath, Color newValue)
        {
            while (fieldValues.Count != bindingPathArray.Length)
                fieldValues.Add(new Color());

            var fieldIndex = Array.IndexOf(bindingPathArray, bindingPath);
            fieldValues[fieldIndex] = newValue;

            var value = GetCommonValueFromChildFields();
            m_ColorField.SetValueWithoutNotify(value);

            if (isMixed)
                m_MixedValueLine.style.display = DisplayStyle.Flex;
            else
                m_MixedValueLine.style.display = DisplayStyle.None;
        }

        public Color GetCommonValueFromChildFields()
        {
            if (!isMixed)
                return fieldValues[0];
            else
                return Color.white;
        }
    }
}
