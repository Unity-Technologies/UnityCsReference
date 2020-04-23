using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    [Serializable]
    internal class VisualElementAsset : IUxmlAttributes, ISerializationCallbackReceiver
    {
        [SerializeField]
        private string m_Name;

        [SerializeField]
        private int m_Id;

        public int id
        {
            get { return m_Id; }
            set { m_Id = value; }
        }

        [SerializeField]
        private int m_OrderInDocument;

        public int orderInDocument
        {
            get { return m_OrderInDocument; }
            set { m_OrderInDocument = value; }
        }

        [SerializeField]
        private int m_ParentId;

        public int parentId
        {
            get { return m_ParentId; }
            set { m_ParentId = value; }
        }

        [SerializeField]
        private int m_RuleIndex;

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
        private string m_FullTypeName;

        public string fullTypeName
        {
            get { return m_FullTypeName; }
            set { m_FullTypeName = value; }
        }

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

        [SerializeField]
        private List<string> m_Properties;

        public VisualElementAsset(string fullTypeName)
        {
            m_FullTypeName = fullTypeName;

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
                AddProperty("name", m_Name);
            }
            if (!string.IsNullOrEmpty(m_Text) && !m_Properties.Contains("text"))
            {
                AddProperty("text", m_Text);
            }
            if (m_PickingMode != PickingMode.Position && !m_Properties.Contains("picking-mode") && !m_Properties.Contains("pickingMode"))
            {
                AddProperty("picking-mode", m_PickingMode.ToString());
            }
        }

        public void AddProperty(string propertyName, string propertyValue)
        {
            SetOrAddProperty(propertyName, propertyValue);
        }

        void SetOrAddProperty(string propertyName, string propertyValue)
        {
            if (m_Properties == null)
                m_Properties = new List<string>();

            for (int i = 0; i < m_Properties.Count - 1; i += 2)
            {
                if (m_Properties[i] == propertyName)
                {
                    m_Properties[i + 1] = propertyValue;
                    return;
                }
            }

            m_Properties.Add(propertyName);
            m_Properties.Add(propertyValue);
        }

        public bool TryGetAttributeValue(string propertyName, out string value)
        {
            if (m_Properties == null)
            {
                value = null;
                return false;
            }

            for (int i = 0; i < m_Properties.Count - 1; i += 2)
            {
                if (m_Properties[i] == propertyName)
                {
                    value = m_Properties[i + 1];
                    return true;
                }
            }

            value = null;
            return false;
        }
    }
}
