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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal virtual VisualElement Instantiate(CreationContext cc)
        {
            var ve = (VisualElement) serializedData.CreateInstance();
            serializedData.Deserialize(ve);

            if (cc.hasOverrides)
            {
                // Applying the overrides in reverse order. This means that the deepest overrides, the ones from nested VisualTreeAssets,
                // will be applied first and might be overridden by parent VisualTreeAssets.
                for (var i = cc.serializedDataOverrides.Count - 1; i >= 0; --i)
                {
                    foreach (var attributeOverride in cc.serializedDataOverrides[i].attributeOverrides)
                    {
                        if (attributeOverride.m_ElementId == id)
                        {
                            attributeOverride.m_SerializedData.Deserialize(ve);
                        }
                    }
                }
            }

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
