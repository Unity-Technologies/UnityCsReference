using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class TemplateAssetExtensions
    {
        public static void SetAttributeOverride(
            this TemplateAsset ta, string elementName, string attributeName, string value)
        {
            if (ta.attributeOverrides == null)
                ta.attributeOverrides = new List<TemplateAsset.AttributeOverride>();

            // See if the override already exists.
            for (int i = 0; i < ta.attributeOverrides.Count; ++i)
            {
                var over = ta.attributeOverrides[i];
                if (over.m_ElementName == elementName
                    && over.m_AttributeName == attributeName)
                {
                    over.m_ElementName = elementName;
                    over.m_AttributeName = attributeName;
                    over.m_Value = value;

                    ta.attributeOverrides[i] = over;

                    return;
                }
            }

            // If the override does not exist, add it.
            var attributeOverride = new TemplateAsset.AttributeOverride();
            attributeOverride.m_ElementName = elementName;
            attributeOverride.m_AttributeName = attributeName;
            attributeOverride.m_Value = value;
            ta.attributeOverrides.Add(attributeOverride);
        }

        public static void RemoveAttributeOverride(
            this TemplateAsset ta, string elementName, string attributeName)
        {
            if (ta.attributeOverrides == null)
                return;

            // See if the override already exists.
            for (int i = 0; i < ta.attributeOverrides.Count; ++i)
            {
                var over = ta.attributeOverrides[i];
                if (over.m_ElementName == elementName
                    && over.m_AttributeName == attributeName)
                {
                    ta.attributeOverrides.RemoveAt(i);

                    return;
                }
            }
        }
    }
}
