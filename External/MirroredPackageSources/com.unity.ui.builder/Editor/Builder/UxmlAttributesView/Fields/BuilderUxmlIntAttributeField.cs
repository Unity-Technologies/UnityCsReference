using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderUxmlIntAttributeFieldFactory : BuilderTypedUxmlAttributeFieldFactoryBase<int, BaseField<int>>
    {
        protected override BaseField<int> InstantiateField(object attributeOwner, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute)
        {
            if (attribute.name.Equals("value") && attributeOwner is LayerField)
            {
                return new LayerField();
            }

            if (attribute.name.Equals("value") && attributeOwner is LayerMaskField)
            {
                return new LayerMaskField();
            }

            return new IntegerField();
        }

        public override void SetFieldValue(VisualElement field, object attributeOwner, VisualTreeAsset uxmlDocument, UxmlAsset attributeUxmlOwner, UxmlAttributeDescription attribute, object value)
        {
            if (field is LayerField layerField)
            {
                var layerFieldAttributeOwner = attributeOwner as LayerField;

                layerField.SetValueWithoutNotify(layerFieldAttributeOwner.value);
            }
            else if (field is LayerMaskField layerMaskField)
            {
                var layerMaskFieldAttributeOwner = attributeOwner as LayerMaskField;

                layerMaskField.SetValueWithoutNotify(layerMaskFieldAttributeOwner.value);
            }
            else
            {
                if (value is float)
                    value = Convert.ToInt32(value);
                base.SetFieldValue(field, attributeOwner, uxmlDocument, attributeUxmlOwner, attribute, value);
            }
        }
    }
}
