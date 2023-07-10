// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEngine.UIElements
{
    [Serializable]
    internal class TemplateAsset : VisualElementAsset //<TemplateContainer>
    {
        [SerializeField]
        private string m_TemplateAlias;

        public string templateAlias
        {
            get { return m_TemplateAlias; }
            set { m_TemplateAlias = value; }
        }

        [Serializable]
        public struct AttributeOverride
        {
            public string m_ElementName;
            public string m_AttributeName;
            public string m_Value;
        }
        [SerializeField]
        private List<AttributeOverride> m_AttributeOverrides;
        public List<AttributeOverride> attributeOverrides
        {
            get { return m_AttributeOverrides == null ? (m_AttributeOverrides = new List<AttributeOverride>()) : m_AttributeOverrides; }
            set { m_AttributeOverrides = value; }
        }

        public bool hasAttributeOverride => m_AttributeOverrides is {Count: > 0};

        [Serializable]
        public struct UxmlSerializedDataOverride
        {
            public int m_ElementId;
            [SerializeReference]
            public UxmlSerializedData m_SerializedData;
        }

        [SerializeField] private List<UxmlSerializedDataOverride> m_SerializedDataOverride;
        public List<UxmlSerializedDataOverride> serializedDataOverrides
        {
            get => m_SerializedDataOverride ??= new List<UxmlSerializedDataOverride>();
            set => m_SerializedDataOverride = value;
        }

        internal override VisualElement Instantiate(CreationContext cc)
        {
            var tc = (TemplateContainer)base.Instantiate(cc);
            if (tc.templateSource == null)
            {
                // If the template is defined with the path attribute instead of src it may not be resolved at import time
                // due to import order. This is because the dependencies are not declared with the path attribute
                // since resource folders makes it not possible to obtain non ambiguous asset paths.
                // In that case try to resolve it here at runtime.
                tc.templateSource = cc.visualTreeAsset?.ResolveTemplate(tc.templateId);
                if (tc.templateSource == null)
                {
                    tc.Add(new Label($"Unknown Template: '{tc.templateId}'"));
                    return tc;
                }
            }

            // Handle "classic" attribute override.
            // It's possible that some elements of a template don't have any UxmlSerializedData.
            // In that case we still need to populate the CreationContext accordingly.
            var contextOverrides = cc.attributeOverrides;
            if (m_AttributeOverrides is {Count: > 0})
            {
                if (contextOverrides == null)
                    contextOverrides = new();

                // We want to add new overrides at the end of the list, as we
                // want parent instances to always override child instances.
                contextOverrides.Add(new CreationContext.AttributeOverrideRange(cc.visualTreeAsset, attributeOverrides));
            }

            // If the context has serialized data overrides they take over the one inside the template asset because
            // the importer only store the data of the highest level.
            // The first template instantiated define the overrides of the whole hierarchy.
            var serializeDataOverride = cc.serializedDataOverrides ?? serializedDataOverrides;
            tc.templateSource.CloneTree(tc, new CreationContext(cc.slotInsertionPoints, contextOverrides, serializeDataOverride, null, null));
            return tc;
        }

        [SerializeField]
        private List<VisualTreeAsset.SlotUsageEntry> m_SlotUsages;

        internal List<VisualTreeAsset.SlotUsageEntry> slotUsages
        {
            get { return m_SlotUsages; }
            set { m_SlotUsages = value; }
        }

        public TemplateAsset(string templateAlias, string fullTypeName)
            : base(fullTypeName)
        {
            Assert.IsFalse(string.IsNullOrEmpty(templateAlias), "Template alias must not be null or empty");
            m_TemplateAlias = templateAlias;
        }

        public void AddSlotUsage(string slotName, int resId)
        {
            if (m_SlotUsages == null)
                m_SlotUsages = new List<VisualTreeAsset.SlotUsageEntry>();
            m_SlotUsages.Add(new VisualTreeAsset.SlotUsageEntry(slotName, resId));
        }
    }
}
