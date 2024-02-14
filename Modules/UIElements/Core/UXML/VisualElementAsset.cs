// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.Pool;

namespace UnityEngine.UIElements
{
    [Serializable]
    internal class VisualElementAsset : UxmlAsset, ISerializationCallbackReceiver
    {
        [SerializeField]
        private string m_Name = string.Empty;

        [SerializeField]
        private int m_RuleIndex = -1;

        public int ruleIndex
        {
            get { return m_RuleIndex; }
            set { m_RuleIndex = value; }
        }

        [SerializeField]
        private string m_Text = string.Empty;

        [SerializeField]
        private PickingMode m_PickingMode = PickingMode.Position;

        [SerializeField]
        private string[] m_Classes;

        public string[] classes
        {
            get { return m_Classes; }
            set { m_Classes = value; }
        }

        [SerializeField]
        private List<string> m_StylesheetPaths;

        public List<string> stylesheetPaths
        {
            get { return m_StylesheetPaths ?? (m_StylesheetPaths = new List<string>()); }
            set { m_StylesheetPaths = value; }
        }

        public bool hasStylesheetPaths => m_StylesheetPaths != null;

        [SerializeField]
        private List<StyleSheet> m_Stylesheets;

        public List<StyleSheet> stylesheets
        {
            get => m_Stylesheets ??= new List<StyleSheet>();
            set => m_Stylesheets = value;
        }

        public bool hasStylesheets => m_Stylesheets != null;

        [SerializeReference]
        internal UxmlSerializedData m_SerializedData;

        public UxmlSerializedData serializedData
        {
            get => m_SerializedData;
            set => m_SerializedData = value;
        }

        [SerializeField] private bool m_SkipClone;

        internal bool skipClone
        {
            get => m_SkipClone;
            set => m_SkipClone = value;
        }

        public VisualElementAsset(string fullTypeName, UxmlNamespaceDefinition xmlNamespace = default)
            : base(fullTypeName, xmlNamespace)
        {
        }

        public void OnBeforeSerialize() {}

        public void OnAfterDeserialize()
        {
            // These properties were previously treated in a special way.
            // Now they are treated like all other properties. Put them in
            // the property list.
            if (!string.IsNullOrEmpty(m_Name) && !m_Properties.Contains("name"))
            {
                SetAttribute("name", m_Name);
            }
            if (!string.IsNullOrEmpty(m_Text) && !m_Properties.Contains("text"))
            {
                SetAttribute("text", m_Text);
            }
            if (m_PickingMode != PickingMode.Position && !m_Properties.Contains("picking-mode") && !m_Properties.Contains("pickingMode"))
            {
                SetAttribute("picking-mode", m_PickingMode.ToString());
            }
        }

        internal void DeserializeOverride(UxmlSerializedData target, UxmlSerializedData overridenData, List<string> uxmlPropertyNames = null)
        {
            Assert.AreEqual(target.GetType(), overridenData.GetType());

            var desc = UxmlDescriptionRegistry.GetDescription(target.GetType());

            void ApplyOverride(UxmlTypeDescription description, int index, UxmlSerializedData from, UxmlSerializedData to)
            {
                var attDescription = description.attributeDescriptions[index];
                var fieldInfo = attDescription.serializedField;
                var value = fieldInfo.GetValue(from);
                fieldInfo.SetValue(to, value);
            }

            // Override all properties
            if (null == uxmlPropertyNames)
            {
                for (var index = 0; index < desc.attributeDescriptions.Count; ++index)
                    ApplyOverride(desc, index, overridenData, target);
            }
            else
            {
                // Override specified properties
                foreach (var uxmlName in uxmlPropertyNames)
                {
                    if (desc.uxmlNameToIndex.TryGetValue(uxmlName, out var index))
                    {
                        ApplyOverride(desc, index, overridenData, target);
                    }
                    else
                    {
                        // Try to map to an obsolete name
                        for (var i = 0; i < desc.attributeDescriptions.Count; ++i)
                        {
                            var attributeDescription = desc.attributeDescriptions[i];
                            var obsoleteNames = attributeDescription.obsoleteNames;
                            var matchedObsoleteName = false;
                            for (var j = 0; j < obsoleteNames.Length; ++j)
                            {
                                if (obsoleteNames[j] != uxmlName)
                                    continue;

                                ApplyOverride(desc, i, overridenData, target);
                                matchedObsoleteName = true;
                                break;
                            }

                            if (matchedObsoleteName)
                                break;
                        }
                    }
                }
            }
        }

        internal virtual VisualElement Instantiate(CreationContext cc)
        {
            var ve = (VisualElement) serializedData.CreateInstance();
            var data = serializedData;

            if (cc.hasOverrides)
            {
                // If there are overrides, we need to merge them into a single UxmlSerializedData that we can then call
                // Deserialize on. We'll create a new instance of the serialized data and transfer the data of the current
                // UxmlAsset in it. Then, going bottom-up, we'll overwrite properties of this data with the overrides.
                data = (UxmlSerializedData) Activator.CreateInstance(serializedData.GetType());
                DeserializeOverride(data, serializedData);

                // To partially override the UxmlSerializedData, we need to look at the traits overrides to figure out which properties
                // were overridden. That system works on the name of the element, which we extract here.
                var desc = UxmlDescriptionRegistry.GetDescription(serializedData.GetType());
                var elementName = (string)desc.attributeDescriptions[desc.uxmlNameToIndex["name"]].serializedField.GetValue(serializedData);

                Assert.AreEqual(cc.attributeOverrides.Count, cc.serializedDataOverrides.Count);

                // Applying the overrides in reverse order. This means that the deepest overrides, the ones from nested VisualTreeAssets,
                // will be applied first and might be overridden by parent VisualTreeAssets.
                for (var i = cc.serializedDataOverrides.Count - 1; i >= 0; --i)
                {
                    var attributeOverrideRange = cc.attributeOverrides[i];
                    var serializedDataOverrideRange = cc.serializedDataOverrides[i];

                    using var propertyNamesPooledHandle = ListPool<string>.Get(out var propertyNames);

                    for (var j = 0; j < attributeOverrideRange.attributeOverrides.Count; ++j)
                    {
                        var attributeOverride = attributeOverrideRange.attributeOverrides[j];
                        if (attributeOverride.m_ElementName == elementName)
                            propertyNames.Add(attributeOverride.m_AttributeName);
                    }

                    for (var j = 0; j < serializedDataOverrideRange.attributeOverrides.Count; ++j)
                    {
                        var attributeOverride = serializedDataOverrideRange.attributeOverrides[j];
                        if (attributeOverride.m_ElementId == id)
                        {
                            DeserializeOverride(data, attributeOverride.m_SerializedData, propertyNames);
                        }
                    }
                }
            }

            data.Deserialize(ve);

            if (hasStylesheetPaths)
            {
                for (var i = 0; i < stylesheetPaths.Count; i++)
                    ve.AddStyleSheetPath(stylesheetPaths[i]);
            }

            if (hasStylesheets)
            {
                for (var i = 0; i < stylesheets.Count; ++i)
                {
                    if (stylesheets[i] != null)
                        ve.styleSheets.Add(stylesheets[i]);
                }
            }

            if (classes != null)
            {
                for (var i = 0; i < classes.Length; i++)
                    ve.AddToClassList(classes[i]);
            }

            return ve;
        }

        public override string ToString() => $"{m_Name}({fullTypeName})({id})";
    }
}
