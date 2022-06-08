// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    [Serializable]
    internal class UxmlAsset : IUxmlAttributes
    {
        public UxmlAsset(string fullTypeName)
        {
            m_FullTypeName = fullTypeName;
        }

        [SerializeField]
        private string m_FullTypeName;

        public string fullTypeName
        {
            get => m_FullTypeName;
            set => m_FullTypeName = value;
        }

        [SerializeField]
        private int m_Id;

        public int id
        {
            get => m_Id;
            set => m_Id = value;
        }

        [SerializeField]
        private int m_OrderInDocument;

        public int orderInDocument
        {
            get => m_OrderInDocument;
            set => m_OrderInDocument = value;
        }

        [SerializeField]
        private int m_ParentId;

        public int parentId
        {
            get => m_ParentId;
            set => m_ParentId = value;
        }

        [SerializeField]
        protected List<string> m_Properties;

        public List<string> GetProperties() => m_Properties;

        int m_DirtyCount;
        public bool HasParent() => m_ParentId != 0;

        public bool HasAttribute(string attributeName)
        {
            if (m_Properties == null || m_Properties.Count <= 0)
                return false;

            for (var i = 0; i < m_Properties.Count; i += 2)
            {
                var name = m_Properties[i];
                if (name == attributeName)
                    return true;
            }

            return false;
        }

        public string GetAttributeValue(string attributeName)
        {
            TryGetAttributeValue(attributeName, out var value);
            return value;
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

        public void SetAttribute(string name, string value)
        {
            SetOrAddProperty(name, value);
        }

        public void RemoveAttribute(string attributeName)
        {
            if (m_Properties == null || m_Properties.Count <= 0)
                return;

            for (var i = 0; i < m_Properties.Count; i += 2)
            {
                var name = m_Properties[i];
                if (name != attributeName)
                    continue;

                m_Properties.RemoveAt(i); // Removing the name at i.
                m_Properties.RemoveAt(i); // Removing the value at i + 1.
                return;
            }
        }

        void SetOrAddProperty(string propertyName, string propertyValue)
        {
            if (m_Properties == null)
                m_Properties = new List<string>();

            m_DirtyCount++;

            for (var i = 0; i < m_Properties.Count - 1; i += 2)
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

        internal int GetPropertiesDirtyCount()
        {
            return m_DirtyCount;
        }
    }

    [Serializable]
    internal sealed class UxmlObjectAsset : UxmlAsset
    {
        public UxmlObjectAsset(string fullTypeName)
            : base(fullTypeName) {}
    }
}
