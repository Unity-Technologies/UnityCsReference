// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    [Serializable]
    internal class VisualElementAsset : UxmlAsset, ISerializationCallbackReceiver
    {
        [SerializeField]
        private string m_Name;

        [SerializeField]
        private int m_RuleIndex = -1;

        public int ruleIndex
        {
            get { return m_RuleIndex; }
            set { m_RuleIndex = value; }
        }

        [SerializeField]
        private string m_Text;

        [SerializeField]
        private PickingMode m_PickingMode;

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
            get { return m_Stylesheets ?? (m_Stylesheets = new List<StyleSheet>()); }
            set { m_Stylesheets = value; }
        }

        public bool hasStylesheets => m_Stylesheets != null;

        public VisualElementAsset(string fullTypeName)
            : base(fullTypeName)
        {
            m_Name = String.Empty;
            m_Text = String.Empty;
            m_PickingMode = PickingMode.Position;
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
    }
}
