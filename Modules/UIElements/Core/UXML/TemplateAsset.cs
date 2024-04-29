// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Bindings;
using UnityEngine.Pool;

namespace UnityEngine.UIElements
{
    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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
            public string[] m_NamesPath;
            public string m_AttributeName;
            public string m_Value;

            public bool NamesPathMatchesElementNamesPath(IList<string> elementNamesPath)
            {
                if (elementNamesPath == null || m_NamesPath == null
                                             || elementNamesPath.Count == 0 || m_NamesPath.Length == 0)
                {
                    return false;
                }

                // Old overrides still match elements only by name
                if (m_NamesPath.Length == 1)
                {
                    return m_NamesPath[0] == elementNamesPath[^1];
                }

                if (m_NamesPath.Length != elementNamesPath.Count)
                {
                    return false;
                }

                // New overrides match elements when path is the same
                for (var i = elementNamesPath.Count - 1; i >= 0; --i)
                {
                    if (elementNamesPath[i] != m_NamesPath[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        [SerializeField]
        List<AttributeOverride> m_AttributeOverrides = new List<AttributeOverride>();
        public List<AttributeOverride> attributeOverrides
        {
            get => m_AttributeOverrides;
            set => m_AttributeOverrides = value;
        }

        public bool hasAttributeOverride => m_AttributeOverrides is {Count: > 0};

        [Serializable]
        public struct UxmlSerializedDataOverride
        {
            public int m_ElementId;
            public List<int> m_ElementIdsPath;
            [SerializeReference]
            public UxmlSerializedData m_SerializedData;
        }

        [SerializeField]
        List<UxmlSerializedDataOverride> m_SerializedDataOverride = new List<UxmlSerializedDataOverride>();
        public List<UxmlSerializedDataOverride> serializedDataOverrides
        {
            get => m_SerializedDataOverride;
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

            // Gather the overrides in hierarchical order where overrides coming from the parent VisualTreeAsset will appear in the lists below before the overrides coming from the nested
            // VisualTreeAssets. The overrides will be processed in reverse order.
            using var traitsOverridesHandle = ListPool<CreationContext.AttributeOverrideRange>.Get(out var traitsOverrideRanges);
            using var dataOverridesHandle = ListPool<CreationContext.SerializedDataOverrideRange>.Get(out var serializedDataOverrideRanges);

            // Populate traits attribute overrides.
            // This will be used when an element does not use the Uxml Serialization feature and relies on the Uxml Factory/Traits system.
            if (null != cc.attributeOverrides)
                traitsOverrideRanges.AddRange(cc.attributeOverrides);
            if (attributeOverrides.Count > 0)
                traitsOverrideRanges.Add(new CreationContext.AttributeOverrideRange(cc.visualTreeAsset, attributeOverrides));

            // Populate the serialized data overrides.
            if (null != cc.serializedDataOverrides)
                serializedDataOverrideRanges.AddRange(cc.serializedDataOverrides);
            if (serializedDataOverrides.Count > 0)
                serializedDataOverrideRanges.Add(new CreationContext.SerializedDataOverrideRange(cc.visualTreeAsset, serializedDataOverrides, id));

            var veaIdsPath = cc.veaIdsPath != null ? new List<int>(cc.veaIdsPath) : new List<int>();
            var newCC = new CreationContext(cc.slotInsertionPoints, traitsOverrideRanges, serializedDataOverrideRanges,
                null, null, veaIdsPath, null);

            tc.templateSource.CloneTree(tc, newCC);

            return tc;
        }

        [SerializeField]
        private List<VisualTreeAsset.SlotUsageEntry> m_SlotUsages;

        internal List<VisualTreeAsset.SlotUsageEntry> slotUsages
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get { return m_SlotUsages; }
            set { m_SlotUsages = value; }
        }

        public TemplateAsset(string templateAlias, string fullTypeName, UxmlNamespaceDefinition xmlNamespace = default)
            : base(fullTypeName, xmlNamespace)
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
