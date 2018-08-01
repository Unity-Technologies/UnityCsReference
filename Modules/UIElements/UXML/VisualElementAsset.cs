// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
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
        private List<string> m_Stylesheets;

        public List<string> stylesheets
        {
            get { return m_Stylesheets == null ? (m_Stylesheets = new List<string>()) : m_Stylesheets; }
            set { m_Stylesheets = value; }
        }

        [SerializeField]
        private List<string> m_Properties;

        public VisualElementAsset(string fullTypeName)
        {
            m_FullTypeName = fullTypeName;
            m_Name = String.Empty;
            m_Text = String.Empty;
            m_PickingMode = PickingMode.Position;
        }

        public VisualElement Create(CreationContext ctx)
        {
            List<IUxmlFactory> factoryList;
            if (!VisualElementFactoryRegistry.TryGetValue(fullTypeName, out factoryList))
            {
                Debug.LogErrorFormat("Element '{0}' has no registered factory method.", fullTypeName);
                return new Label(string.Format("Unknown type: '{0}'", fullTypeName));
            }

            IUxmlFactory factory = null;
            foreach (IUxmlFactory f in factoryList)
            {
                if (f.AcceptsAttributeBag(this, ctx))
                {
                    factory = f;
                    break;
                }
            }

            if (factory == null)
            {
                Debug.LogErrorFormat("Element '{0}' has a no factory that accept the set of XML attributes specified.", fullTypeName);
                return new Label(string.Format("Type with no factory: '{0}'", fullTypeName));
            }

            if (factory is UxmlRootElementFactory)
            {
                return null;
            }

            VisualElement res = factory.Create(this, ctx);
            if (res == null)
            {
                Debug.LogErrorFormat("The factory of Visual Element Type '{0}' has returned a null object", fullTypeName);
                return new Label(string.Format("The factory of Visual Element Type '{0}' has returned a null object", fullTypeName));
            }

            if (classes != null)
            {
                for (int i = 0; i < classes.Length; i++)
                    res.AddToClassList(classes[i]);
            }

            if (stylesheets != null)
            {
                for (int i = 0; i < stylesheets.Count; i++)
                    res.AddStyleSheetPath(stylesheets[i]);
            }

            return res;
        }

        public void OnBeforeSerialize() {}

        public void OnAfterDeserialize()
        {
            // These properties were previously treated in a special way.
            // Now they are trated like all other properties. Put them in
            // the property list.
            if (!m_Properties.Contains("name"))
            {
                AddProperty("name", m_Name);
            }
            if (!m_Properties.Contains("text"))
            {
                AddProperty("text", m_Text);
            }
            if (!m_Properties.Contains("picking-mode") && !m_Properties.Contains("pickingMode"))
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
